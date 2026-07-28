using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class KaleidoscopeRingFacets : MonoBehaviour, IPortalAccessory
{
    [Header("Required")]
    [SerializeField] private Material facetMaterial;

    [Header("Ring Geometry")]
    [Min(0.01f)] [SerializeField] private float majorRadius = 1f;
    [Min(0.001f)] [SerializeField] private float tubeRadius = 0.10f;
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.020f;
    [Range(0f, 1.5f)] [SerializeField] private float sizeBasedLift = 0.65f;

    [Header("Population")]
    [Range(1, 32)] [SerializeField] private int maximumActiveFacets = 18;
    [Min(0.02f)] [SerializeField] private float minimumSpawnDelay = 0.08f;
    [Min(0.02f)] [SerializeField] private float maximumSpawnDelay = 0.22f;

    [Header("Facet Size")]
    [Min(0.002f)] [SerializeField] private float minimumSize = 0.018f;
    [Min(0.002f)] [SerializeField] private float maximumSize = 0.045f;
    [Range(0.35f, 1.5f)] [SerializeField] private float minimumStretch = 0.65f;
    [Range(0.35f, 1.5f)] [SerializeField] private float maximumStretch = 1.15f;

    [Header("Facet Motion")]
    [Range(0f, 45f)] [SerializeField] private float maximumFlipDegrees = 18f;
    [Range(0f, 45f)] [SerializeField] private float maximumTwistDegrees = 12f;
    [Min(0f)] [SerializeField] private float minimumFlipSpeed = 0.55f;
    [Min(0f)] [SerializeField] private float maximumFlipSpeed = 1.40f;

    [Header("Animation")]
    [Min(0.05f)] [SerializeField] private float minimumGrowTime = 0.18f;
    [Min(0.05f)] [SerializeField] private float maximumGrowTime = 0.38f;
    [Min(0f)] [SerializeField] private float minimumHoldTime = 0.35f;
    [Min(0f)] [SerializeField] private float maximumHoldTime = 0.90f;
    [Min(0.05f)] [SerializeField] private float minimumFadeTime = 0.25f;
    [Min(0.05f)] [SerializeField] private float maximumFadeTime = 0.55f;

    [Header("Motion Reaction")]
    [Range(0f, 2f)] [SerializeField] private float motionSpawnBoost = 0.75f;
    [Range(0f, 2f)] [SerializeField] private float motionScaleBoost = 0.20f;
    [Range(0f, 2f)] [SerializeField] private float motionFlipBoost = 0.50f;

    [Header("Colors")]
    [SerializeField] private Color[] facetColors =
    {
        new Color(0.00f, 0.95f, 1.00f, 1f),
        new Color(0.08f, 0.45f, 1.00f, 1f),
        new Color(0.65f, 0.08f, 1.00f, 1f),
        new Color(1.00f, 0.05f, 0.70f, 1f),
        new Color(1.00f, 0.35f, 0.03f, 1f),
        new Color(1.00f, 0.92f, 0.02f, 1f),
        new Color(0.25f, 1.00f, 0.10f, 1f)
    };

    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");

    private sealed class Facet
    {
        public GameObject Object;
        public MeshRenderer Renderer;
        public MaterialPropertyBlock Block;

        public Vector3 BaseScale;
        public Quaternion BaseRotation;
        public float FlipSpeed;
        public float FlipPhase;
        public float Seed;
        public Color Color;
    }

    private readonly List<Facet> activeFacets = new();

    private Mesh triangleMesh;
    private Coroutine spawnRoutine;

    private float motionEnergy;
    private bool selected;
    private bool opening;

    private void Awake()
    {
        triangleMesh = CreateTriangleMesh();
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

        for (int i = activeFacets.Count - 1; i >= 0; i--)
        {
            DestroyFacet(activeFacets[i]);
        }

        activeFacets.Clear();
    }

    private void OnDestroy()
    {
        if (triangleMesh != null)
        {
            Destroy(triangleMesh);
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            activeFacets.RemoveAll(
                facet => facet == null || facet.Object == null
            );

            int allowed = maximumActiveFacets + (opening ? 4 : 0);

            if (activeFacets.Count < allowed)
            {
                CreateFacet();
            }

            float delay = Random.Range(
                minimumSpawnDelay,
                maximumSpawnDelay
            );

            delay /= 1f + motionEnergy * motionSpawnBoost;

            yield return new WaitForSeconds(delay);
        }
    }

    private void CreateFacet()
    {
        if (facetMaterial == null)
        {
            Debug.LogError(
                "KaleidoscopeRingFacets needs a Facet Material assigned.",
                this
            );

            enabled = false;
            return;
        }

        GameObject go = new GameObject("KaleidoscopeRingFacet");
        go.transform.SetParent(transform, false);

        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();

        filter.sharedMesh = triangleMesh;
        renderer.sharedMaterial = facetMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        float majorAngle = Random.Range(0f, Mathf.PI * 2f);
        float tubeAngle = Random.Range(0f, Mathf.PI * 2f);

        Vector3 radial = new Vector3(
            Mathf.Cos(majorAngle),
            0f,
            Mathf.Sin(majorAngle)
        );

        Vector3 tangent = new Vector3(
            -radial.z,
            0f,
            radial.x
        );

        Vector3 tubeOut =
            radial * Mathf.Cos(tubeAngle) +
            Vector3.up * Mathf.Sin(tubeAngle);

        Vector3 tubeTangent =
            -radial * Mathf.Sin(tubeAngle) +
            Vector3.up * Mathf.Cos(tubeAngle);

        float size = Random.Range(
            minimumSize,
            maximumSize
        );

        float stretch = Random.Range(
            minimumStretch,
            maximumStretch
        );

        float visibleLift =
            surfaceOffset +
            size * sizeBasedLift;

        Vector3 position =
            radial * majorRadius +
            tubeOut * (tubeRadius + visibleLift);

        Quaternion orientation =
            Quaternion.LookRotation(
                tubeOut,
                tubeTangent
            );

        orientation *= Quaternion.AngleAxis(
            Random.Range(0f, 360f),
            Vector3.forward
        );

        go.transform.localPosition = position;
        go.transform.localRotation = orientation;
        go.transform.localScale = Vector3.zero;

        Color color = GetRandomColor();

        Facet facet = new Facet
        {
            Object = go,
            Renderer = renderer,
            Block = new MaterialPropertyBlock(),
            BaseScale = new Vector3(
                size * stretch,
                size,
                1f
            ),
            BaseRotation = orientation,
            FlipSpeed = Random.Range(
                minimumFlipSpeed,
                maximumFlipSpeed
            ),
            FlipPhase = Random.Range(
                0f,
                Mathf.PI * 2f
            ),
            Seed = Random.Range(0f, 1000f),
            Color = color
        };

        activeFacets.Add(facet);
        StartCoroutine(AnimateFacet(facet));
    }

    private IEnumerator AnimateFacet(Facet facet)
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

        float elapsed = 0f;

        while (elapsed < growTime && facet.Object != null)
        {
            elapsed += Time.deltaTime;
            float progress = Smooth01(elapsed / growTime);

            UpdateFacet(
                facet,
                scaleProgress: progress,
                opacity: progress
            );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < holdTime && facet.Object != null)
        {
            elapsed += Time.deltaTime;

            UpdateFacet(
                facet,
                scaleProgress: 1f,
                opacity: 1f
            );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < fadeTime && facet.Object != null)
        {
            elapsed += Time.deltaTime;
            float progress = Smooth01(elapsed / fadeTime);

            UpdateFacet(
                facet,
                scaleProgress: 1f - progress * 0.18f,
                opacity: 1f - progress
            );

            yield return null;
        }

        activeFacets.Remove(facet);
        DestroyFacet(facet);
    }

    private void UpdateFacet(
        Facet facet,
        float scaleProgress,
        float opacity
    )
    {
        if (facet.Object == null)
        {
            return;
        }

        float selectedMultiplier = selected ? 0.92f : 1f;
        float openingMultiplier = opening ? 1.15f : 1f;
        float motionScale =
            1f +
            motionEnergy *
            motionScaleBoost;

        facet.Object.transform.localScale =
            facet.BaseScale *
            scaleProgress *
            selectedMultiplier *
            openingMultiplier *
            motionScale;

        float flipAmount =
            maximumFlipDegrees *
            (
                1f +
                motionEnergy *
                motionFlipBoost
            );

        float flip =
            Mathf.Sin(
                Time.time *
                facet.FlipSpeed *
                Mathf.PI *
                2f +
                facet.FlipPhase
            ) *
            flipAmount;

        float twist =
            Mathf.Sin(
                Time.time *
                facet.FlipSpeed *
                Mathf.PI *
                1.37f +
                facet.FlipPhase *
                0.73f
            ) *
            maximumTwistDegrees;

        facet.Object.transform.localRotation =
            facet.BaseRotation *
            Quaternion.Euler(
                flip,
                twist,
                0f
            );

        facet.Renderer.GetPropertyBlock(
            facet.Block
        );

        facet.Block.SetColor(
            TintId,
            facet.Color
        );

        facet.Block.SetFloat(
            OpacityId,
            Mathf.Clamp01(opacity)
        );

        facet.Block.SetFloat(
            SeedId,
            facet.Seed
        );

        facet.Renderer.SetPropertyBlock(
            facet.Block
        );
    }

    private static Mesh CreateTriangleMesh()
    {
        Mesh mesh = new Mesh
        {
            name = "KaleidoscopeFacetTriangle"
        };

        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.35f, 0f),
            new Vector3( 0.5f, -0.35f, 0f),
            new Vector3( 0.0f,  0.60f, 0f)
        };

        mesh.normals = new[]
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 1f)
        };

        mesh.triangles = new[]
        {
            0, 1, 2
        };

        mesh.RecalculateBounds();

        return mesh;
    }

    private Color GetRandomColor()
    {
        if (
            facetColors == null ||
            facetColors.Length == 0
        )
        {
            return Color.cyan;
        }

        return facetColors[
            Random.Range(
                0,
                facetColors.Length
            )
        ];
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static void DestroyFacet(Facet facet)
    {
        if (
            facet != null &&
            facet.Object != null
        )
        {
            Destroy(facet.Object);
        }
    }

    public void SetMotionEnergy(float value)
    {
        motionEnergy = Mathf.Clamp01(value);
    }

    public void SetSelected(bool value)
    {
        selected = value;
    }

    public void OnPortalOpening()
    {
        opening = true;
        motionEnergy = 1f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maximumSpawnDelay =
            Mathf.Max(
                maximumSpawnDelay,
                minimumSpawnDelay
            );

        maximumSize =
            Mathf.Max(
                maximumSize,
                minimumSize
            );

        maximumStretch =
            Mathf.Max(
                maximumStretch,
                minimumStretch
            );

        maximumFlipSpeed =
            Mathf.Max(
                maximumFlipSpeed,
                minimumFlipSpeed
            );

        maximumGrowTime =
            Mathf.Max(
                maximumGrowTime,
                minimumGrowTime
            );

        maximumHoldTime =
            Mathf.Max(
                maximumHoldTime,
                minimumHoldTime
            );

        maximumFadeTime =
            Mathf.Max(
                maximumFadeTime,
                minimumFadeTime
            );
    }
#endif
}
