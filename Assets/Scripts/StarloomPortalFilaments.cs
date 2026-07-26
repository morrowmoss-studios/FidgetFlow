using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class StarloomPortalFilaments : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Transparent additive material used by the glowing filament lines.")]
    [SerializeField] private Material filamentMaterial;

    [Tooltip("Camera viewing the portal. Leave empty to use Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Portal Shape")]
    [Tooltip("Radius where the filament begins inside the StarLoom surface.")]
    [Min(0.01f)]
    [SerializeField] private float startRadius = 2.38f;

    [Tooltip("Radius where the filament ends over the inner part of the ring.")]
    [Min(0.01f)]
    [SerializeField] private float endRadius = 2.78f;

    [Tooltip("Pushes the filaments toward the camera so the portal surface cannot hide them.")]
    [Min(0f)]
    [SerializeField] private float cameraDepthOffset = 0.15f;

    [Header("Spawning")]
    [Min(0.05f)]
    [SerializeField] private float minimumSpawnDelay = 0.18f;

    [Min(0.05f)]
    [SerializeField] private float maximumSpawnDelay = 0.55f;

    [Range(1, 12)]
    [SerializeField] private int maximumActiveFilaments = 4;

    [Header("Appearance")]
    [Min(0.001f)]
    [SerializeField] private float minimumWidth = 0.012f;

    [Min(0.001f)]
    [SerializeField] private float maximumWidth = 0.024f;

    [Min(0.05f)]
    [SerializeField] private float minimumLifetime = 0.35f;

    [Min(0.05f)]
    [SerializeField] private float maximumLifetime = 0.80f;

    [Tooltip("Small sideways bend so the lines resemble StarLoom threads.")]
    [Min(0f)]
    [SerializeField] private float bendAmount = 0.015f;

    [Tooltip("Number of points forming each filament.")]
    [Range(3, 12)]
    [SerializeField] private int linePoints = 4;

    [Header("Colors")]
    [SerializeField] private Color[] filamentColors =
    {
        new Color(0.20f, 0.70f, 1.00f, 1f),
        new Color(0.45f, 0.35f, 1.00f, 1f),
        new Color(0.90f, 0.25f, 0.75f, 1f),
        new Color(0.25f, 0.95f, 0.55f, 1f)
    };

    private readonly List<LineRenderer> activeFilaments = new();

    private Coroutine spawnRoutine;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        for (int i = activeFilaments.Count - 1; i >= 0; i--)
        {
            LineRenderer line = activeFilaments[i];

            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }

        activeFilaments.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            RemoveDestroyedFilaments();

            if (activeFilaments.Count < maximumActiveFilaments)
            {
                CreateFilament();
            }

            float delay = Random.Range(
                minimumSpawnDelay,
                maximumSpawnDelay
            );

            yield return new WaitForSeconds(delay);
        }
    }

    private void CreateFilament()
    {
        if (filamentMaterial == null)
        {
            Debug.LogError(
                "StarloomPortalFilaments has no filament material assigned.",
                this
            );

            enabled = false;
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        GameObject filamentObject =
            new GameObject("StarloomFilament");

        filamentObject.transform.SetParent(
            transform,
            false
        );

        LineRenderer line =
            filamentObject.AddComponent<LineRenderer>();

        ConfigureLineRenderer(line);

        float angle =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );

        Vector3 radialDirection =
            new Vector3(
                Mathf.Cos(angle),
                Mathf.Sin(angle),
                0f
            );

        Vector3 tangentDirection =
            new Vector3(
                -radialDirection.y,
                radialDirection.x,
                0f
            );

        Vector3 cameraOffset =
            GetLocalCameraOffset();

        Vector3 start =
            radialDirection * startRadius;

        Vector3 end =
            radialDirection * endRadius;

        float randomBend =
            Random.Range(
                -bendAmount,
                bendAmount
            );

        for (int i = 0; i < linePoints; i++)
        {
            float progress =
                i / (float)(linePoints - 1);

            Vector3 position =
                Vector3.Lerp(
                    start,
                    end,
                    progress
                );

            float curvedAmount =
                Mathf.Sin(
                    progress * Mathf.PI
                ) *
                randomBend;

            position +=
                tangentDirection *
                curvedAmount;

            /*
             * This moves the line toward the camera regardless of whether
             * the portal faces local +Z or local -Z.
             */
            position += cameraOffset;

            line.SetPosition(
                i,
                position
            );
        }

        Color filamentColor =
            GetRandomFilamentColor();

        line.startColor = filamentColor;

        line.endColor =
            new Color(
                filamentColor.r,
                filamentColor.g,
                filamentColor.b,
                0f
            );

        activeFilaments.Add(line);

        float lifetime =
            Random.Range(
                minimumLifetime,
                maximumLifetime
            );

        StartCoroutine(
            FadeAndDestroy(
                line,
                filamentColor,
                lifetime
            )
        );
    }

    private void ConfigureLineRenderer(
        LineRenderer line
    )
    {
        line.useWorldSpace = false;
        line.loop = false;

        line.alignment =
            LineAlignment.View;

        line.textureMode =
            LineTextureMode.Stretch;

        line.positionCount =
            linePoints;

        line.numCapVertices = 3;
        line.numCornerVertices = 2;

        line.sharedMaterial =
            filamentMaterial;

        line.sortingOrder = 100;

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows = false;

        float width =
            Random.Range(
                minimumWidth,
                maximumWidth
            );

        line.startWidth = width;
        line.endWidth = width * 0.25f;
    }

    private Vector3 GetLocalCameraOffset()
    {
        if (targetCamera == null)
        {
            /*
             * Safe fallback. The normal case should use the camera-based
             * direction below.
             */
            return Vector3.back *
                   cameraDepthOffset;
        }

        Vector3 directionToCameraWorld =
            (
                targetCamera.transform.position -
                transform.position
            ).normalized;

        Vector3 directionToCameraLocal =
            transform.InverseTransformDirection(
                directionToCameraWorld
            ).normalized;

        return directionToCameraLocal *
               cameraDepthOffset;
    }

    private Color GetRandomFilamentColor()
    {
        if (
            filamentColors == null ||
            filamentColors.Length == 0
        )
        {
            return Color.cyan;
        }

        int index =
            Random.Range(
                0,
                filamentColors.Length
            );

        return filamentColors[index];
    }

    private IEnumerator FadeAndDestroy(
        LineRenderer line,
        Color baseColor,
        float lifetime
    )
    {
        float elapsed = 0f;

        while (
            elapsed < lifetime &&
            line != null
        )
        {
            elapsed += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed / lifetime
                );

            float alpha;

            if (normalizedTime < 0.25f)
            {
                alpha =
                    normalizedTime /
                    0.25f;
            }
            else
            {
                alpha =
                    1f -
                    (
                        normalizedTime -
                        0.25f
                    ) /
                    0.75f;
            }

            alpha =
                Mathf.Clamp01(alpha);

            Color startColor =
                baseColor;

            startColor.a = alpha;

            Color endColor =
                baseColor;

            endColor.a = 0f;

            line.startColor =
                startColor;

            line.endColor =
                endColor;

            yield return null;
        }

        activeFilaments.Remove(line);

        if (line != null)
        {
            Destroy(line.gameObject);
        }
    }

    private void RemoveDestroyedFilaments()
    {
        activeFilaments.RemoveAll(
            line => line == null
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maximumSpawnDelay < minimumSpawnDelay)
        {
            maximumSpawnDelay =
                minimumSpawnDelay;
        }

        if (maximumWidth < minimumWidth)
        {
            maximumWidth =
                minimumWidth;
        }

        if (maximumLifetime < minimumLifetime)
        {
            maximumLifetime =
                minimumLifetime;
        }

        if (endRadius < startRadius)
        {
            endRadius =
                startRadius;
        }
    }
#endif
}