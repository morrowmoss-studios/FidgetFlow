using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class StarloomRingVines : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Transparent additive material used by the vines.")]
    [SerializeField] private Material vineMaterial;

    [Tooltip("Camera viewing the portal. Leave empty to use Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Torus Dimensions")]
    [Tooltip("Distance from the ring center to the center of the torus tube.")]
    [Min(0.01f)]
    [SerializeField] private float majorRadius = 1f;

    [Tooltip("Thickness radius of the torus tube.")]
    [Min(0.001f)]
    [SerializeField] private float tubeRadius = 0.10f;

    [Tooltip("Raises the vines slightly above the torus surface.")]
    [Min(0f)]
    [SerializeField] private float surfaceOffset = 0.008f;

    [Tooltip("Extra lift toward the camera so vines stay above the ring and PortalSurface.")]
    [Min(0f)]
    [SerializeField] private float cameraSurfaceLift = 0.015f;

    [Header("Vine Population")]
    [Range(1, 12)]
    [SerializeField] private int maximumActiveVines = 6;

    [Min(0.05f)]
    [SerializeField] private float minimumSpawnDelay = 0.20f;

    [Min(0.05f)]
    [SerializeField] private float maximumSpawnDelay = 0.55f;

    [Header("Vine Shape")]
    [Tooltip("How far inside the portal each strand begins.")]
    [Min(0f)]
    [SerializeField] private float escapeLength = 0.08f;

    [Range(2f, 120f)]
    [SerializeField] private float minimumRingTravelDegrees = 14f;

    [Range(2f, 180f)]
    [SerializeField] private float maximumRingTravelDegrees = 34f;

    [Range(0f, 180f)]
    [SerializeField] private float tubeWrapDegrees = 105f;

    [Min(0f)]
    [SerializeField] private float wobbleAmount = 0.012f;

    [Range(8, 64)]
    [SerializeField] private int pointsPerVine = 28;

    [Header("Vine Thickness")]
    [Min(0.0005f)]
    [SerializeField] private float minimumWidth = 0.004f;

    [Min(0.0005f)]
    [SerializeField] private float maximumWidth = 0.009f;

    [Header("Animation")]
    [Min(0.05f)]
    [SerializeField] private float minimumGrowTime = 0.35f;

    [Min(0.05f)]
    [SerializeField] private float maximumGrowTime = 0.70f;

    [Min(0.05f)]
    [SerializeField] private float minimumHoldTime = 0.80f;

    [Min(0.05f)]
    [SerializeField] private float maximumHoldTime = 1.60f;

    [Min(0.05f)]
    [SerializeField] private float minimumFadeTime = 0.45f;

    [Min(0.05f)]
    [SerializeField] private float maximumFadeTime = 0.90f;

    [Header("Color")]
    [SerializeField] private Color[] vineColors =
    {
        new Color(0.10f, 0.85f, 1.00f, 1f),
        new Color(0.18f, 0.40f, 1.00f, 1f),
        new Color(0.55f, 0.22f, 1.00f, 1f),
        new Color(1.00f, 0.18f, 0.72f, 1f),
        new Color(0.28f, 1.00f, 0.35f, 1f),
        new Color(1.00f, 0.72f, 0.12f, 1f)
    };

    private readonly List<LineRenderer> activeVines = new();
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        for (int i = activeVines.Count - 1; i >= 0; i--)
        {
            if (activeVines[i] != null)
            {
                Destroy(activeVines[i].gameObject);
            }
        }

        activeVines.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            activeVines.RemoveAll(vine => vine == null);

            if (activeVines.Count < maximumActiveVines)
            {
                CreateVine();
            }

            yield return new WaitForSeconds(
                Random.Range(minimumSpawnDelay, maximumSpawnDelay)
            );
        }
    }

    private void CreateVine()
    {
        if (vineMaterial == null)
        {
            Debug.LogError(
                "StarloomRingVines needs a Vine Material assigned.",
                this
            );

            enabled = false;
            return;
        }

        GameObject vineObject = new GameObject("StarloomRingVine");
        vineObject.transform.SetParent(transform, false);

        LineRenderer line = vineObject.AddComponent<LineRenderer>();

        ConfigureLine(line);

        Vector3[] points = GenerateVinePoints();
        Color color = GetRandomColor();

        activeVines.Add(line);

        StartCoroutine(
            AnimateVine(
                line,
                points,
                color
            )
        );
    }

    private void ConfigureLine(LineRenderer line)
    {
        line.useWorldSpace = false;
        line.loop = false;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;

        line.sharedMaterial = vineMaterial;

        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        line.numCapVertices = 4;
        line.numCornerVertices = 3;
        line.sortingOrder = 110;

        float width = Random.Range(
            minimumWidth,
            maximumWidth
        );

        line.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.15f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.72f, 0.7f),
            new Keyframe(1f, 0f)
        );

        line.widthMultiplier = width;
        line.positionCount = 0;
    }

    private Vector3[] GenerateVinePoints()
    {
        Vector3[] points = new Vector3[pointsPerVine];

        float startingAngle = Random.Range(
            0f,
            Mathf.PI * 2f
        );

        float direction =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float ringTravel =
            Mathf.Deg2Rad *
            Random.Range(
                minimumRingTravelDegrees,
                maximumRingTravelDegrees
            ) *
            direction;

        float tubeDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float wobbleFrequency =
            Random.Range(1.5f, 3.5f);

        float wobblePhase =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );

        Vector3 localCameraDirection =
            GetLocalCameraDirection();

        const float escapePortion = 0.18f;

        for (int i = 0; i < pointsPerVine; i++)
        {
            float progress =
                i /
                (float)(pointsPerVine - 1);

            if (progress < escapePortion)
            {
                float escapeProgress =
                    progress /
                    escapePortion;

                float innerLipRadius =
                    majorRadius -
                    tubeRadius;

                float startingRadius =
                    innerLipRadius -
                    escapeLength;

                float currentRadius =
                    Mathf.Lerp(
                        startingRadius,
                        innerLipRadius,
                        SmoothStep01(escapeProgress)
                    );

                Vector3 radialDirection =
                    new Vector3(
                        Mathf.Cos(startingAngle),
                        0f,
                        Mathf.Sin(startingAngle)
                    );

                Vector3 tangentDirection =
                    new Vector3(
                        -radialDirection.z,
                        0f,
                        radialDirection.x
                    );

                Vector3 escapePoint =
                    radialDirection *
                    currentRadius;

                float smallWobble =
                    Mathf.Sin(
                        escapeProgress *
                        Mathf.PI *
                        wobbleFrequency +
                        wobblePhase
                    ) *
                    wobbleAmount *
                    escapeProgress;

                escapePoint +=
                    tangentDirection *
                    smallWobble;

                escapePoint +=
                    localCameraDirection *
                    cameraSurfaceLift;

                points[i] = escapePoint;
                continue;
            }

            float vineProgress =
                (
                    progress -
                    escapePortion
                ) /
                (
                    1f -
                    escapePortion
                );

            float mainAngle =
                startingAngle +
                ringTravel *
                vineProgress;

            Vector3 radial =
                new Vector3(
                    Mathf.Cos(mainAngle),
                    0f,
                    Mathf.Sin(mainAngle)
                );

            Vector3 tangent =
                new Vector3(
                    -radial.z,
                    0f,
                    radial.x
                );

            Vector3 tubeCenter =
                radial *
                majorRadius;

            float cameraRadialAmount =
                Vector3.Dot(
                    localCameraDirection,
                    radial
                );

            float cameraUpAmount =
                Vector3.Dot(
                    localCameraDirection,
                    Vector3.up
                );

            float cameraFacingTubeAngle =
                Mathf.Atan2(
                    cameraUpAmount,
                    cameraRadialAmount
                );

            float climbProgress =
                SmoothStep01(
                    Mathf.Clamp01(
                        vineProgress /
                        0.32f
                    )
                );

            float tubeAngle =
                Mathf.LerpAngle(
                    Mathf.PI,
                    cameraFacingTubeAngle,
                    climbProgress
                );

            float surfaceCurl =
                Mathf.Sin(
                    vineProgress *
                    Mathf.PI
                ) *
                Mathf.Deg2Rad *
                tubeWrapDegrees *
                0.16f *
                tubeDirection;

            tubeAngle += surfaceCurl;

            float raisedTubeRadius =
                tubeRadius +
                surfaceOffset;

            Vector3 point =
                tubeCenter +
                radial *
                (
                    Mathf.Cos(tubeAngle) *
                    raisedTubeRadius
                ) +
                Vector3.up *
                (
                    Mathf.Sin(tubeAngle) *
                    raisedTubeRadius
                );

            float wobble =
                Mathf.Sin(
                    vineProgress *
                    Mathf.PI *
                    2f *
                    wobbleFrequency +
                    wobblePhase
                ) *
                wobbleAmount *
                Mathf.Sin(
                    vineProgress *
                    Mathf.PI
                );

            point +=
                tangent *
                wobble;

            point +=
                localCameraDirection *
                cameraSurfaceLift;

            points[i] = point;
        }

        return points;
    }

    private Vector3 GetLocalCameraDirection()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return Vector3.back;
        }

        Vector3 worldDirection =
            (
                targetCamera.transform.position -
                transform.position
            ).normalized;

        return transform
            .InverseTransformDirection(worldDirection)
            .normalized;
    }

    private IEnumerator AnimateVine(
        LineRenderer line,
        Vector3[] points,
        Color baseColor
    )
    {
        float growTime = Random.Range(
            minimumGrowTime,
            maximumGrowTime
        );

        float holdTime = Random.Range(
            minimumHoldTime,
            maximumHoldTime
        );

        float fadeTime = Random.Range(
            minimumFadeTime,
            maximumFadeTime
        );

        line.startColor = WithAlpha(baseColor, 1f);
        line.endColor = WithAlpha(baseColor, 0.15f);

        float elapsed = 0f;

        while (elapsed < growTime && line != null)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / growTime
            );

            int visiblePoints = Mathf.Clamp(
                Mathf.CeilToInt(progress * points.Length),
                2,
                points.Length
            );

            line.positionCount = visiblePoints;

            for (int i = 0; i < visiblePoints; i++)
            {
                line.SetPosition(i, points[i]);
            }

            yield return null;
        }

        if (line == null)
        {
            yield break;
        }

        line.positionCount = points.Length;
        line.SetPositions(points);

        yield return new WaitForSeconds(holdTime);

        elapsed = 0f;

        while (elapsed < fadeTime && line != null)
        {
            elapsed += Time.deltaTime;

            float alpha =
                1f -
                Mathf.Clamp01(
                    elapsed / fadeTime
                );

            line.startColor =
                WithAlpha(
                    baseColor,
                    alpha
                );

            line.endColor =
                WithAlpha(
                    baseColor,
                    alpha * 0.15f
                );

            yield return null;
        }

        activeVines.Remove(line);

        if (line != null)
        {
            Destroy(line.gameObject);
        }
    }

    private Color GetRandomColor()
    {
        if (
            vineColors == null ||
            vineColors.Length == 0
        )
        {
            return Color.cyan;
        }

        return vineColors[
            Random.Range(
                0,
                vineColors.Length
            )
        ];
    }

    private static Color WithAlpha(
        Color color,
        float alpha
    )
    {
        color.a = alpha;
        return color;
    }

    private static float SmoothStep01(float value)
    {
        value = Mathf.Clamp01(value);

        return value *
               value *
               (
                   3f -
                   2f *
                   value
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

        if (maximumRingTravelDegrees < minimumRingTravelDegrees)
        {
            maximumRingTravelDegrees =
                minimumRingTravelDegrees;
        }

        if (maximumWidth < minimumWidth)
        {
            maximumWidth =
                minimumWidth;
        }

        if (maximumGrowTime < minimumGrowTime)
        {
            maximumGrowTime =
                minimumGrowTime;
        }

        if (maximumHoldTime < minimumHoldTime)
        {
            maximumHoldTime =
                minimumHoldTime;
        }

        if (maximumFadeTime < minimumFadeTime)
        {
            maximumFadeTime =
                minimumFadeTime;
        }
    }
#endif
}