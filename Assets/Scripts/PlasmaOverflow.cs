using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class PlasmaOverflow : MonoBehaviour, IPortalAccessory
{
    [Header("Required")]
    [SerializeField] private Material overflowMaterial;

    [Header("Ring Geometry")]
    [Min(0.01f)] [SerializeField] private float majorRadius = 1f;
    [Min(0.001f)] [SerializeField] private float tubeRadius = 0.10f;
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.010f;

    [Header("Population")]
    [Range(1, 16)] [SerializeField] private int maximumActiveLeaks = 6;
    [Min(0.05f)] [SerializeField] private float minimumSpawnDelay = 0.30f;
    [Min(0.05f)] [SerializeField] private float maximumSpawnDelay = 0.75f;

    [Header("Leak Shape")]
    [Range(8, 32)] [SerializeField] private int radialSegments = 18;
    [Range(2, 8)] [SerializeField] private int ringSegments = 4;
    [Min(0.003f)] [SerializeField] private float minimumRadius = 0.025f;
    [Min(0.003f)] [SerializeField] private float maximumRadius = 0.060f;
    [Range(0.35f, 1.5f)] [SerializeField] private float minimumStretch = 0.65f;
    [Range(0.35f, 1.5f)] [SerializeField] private float maximumStretch = 1.10f;
    [Range(0f, 0.8f)] [SerializeField] private float outlineIrregularity = 0.38f;
    [Range(0f, 0.8f)] [SerializeField] private float surfaceIrregularity = 0.22f;
    [Range(0f, 180f)] [SerializeField] private float maximumRingDriftDegrees = 10f;
    [Range(0f, 180f)] [SerializeField] private float maximumTubeDriftDegrees = 28f;

    [Header("Animation")]
    [Min(0.05f)] [SerializeField] private float minimumGrowTime = 0.35f;
    [Min(0.05f)] [SerializeField] private float maximumGrowTime = 0.75f;
    [Min(0f)] [SerializeField] private float minimumHoldTime = 0.55f;
    [Min(0f)] [SerializeField] private float maximumHoldTime = 1.25f;
    [Min(0.05f)] [SerializeField] private float minimumMeltTime = 0.45f;
    [Min(0.05f)] [SerializeField] private float maximumMeltTime = 0.95f;
    [Range(0f, 0.4f)] [SerializeField] private float pulseAmount = 0.10f;
    [Min(0f)] [SerializeField] private float minimumPulseSpeed = 0.45f;
    [Min(0f)] [SerializeField] private float maximumPulseSpeed = 0.95f;

    [Header("Motion Reaction")]
    [Range(0f, 2f)] [SerializeField] private float motionSpawnBoost = 0.65f;
    [Range(0f, 2f)] [SerializeField] private float motionSizeBoost = 0.25f;

    [Header("Colors")]
    [SerializeField] private Color[] leakColors =
    {
        new Color(0.00f, 0.95f, 0.82f, 1f), // cyan-teal
        new Color(0.06f, 0.55f, 1.00f, 1f), // electric blue
        new Color(0.48f, 0.12f, 1.00f, 1f), // violet
        new Color(1.00f, 0.08f, 0.72f, 1f), // magenta
        new Color(1.00f, 0.42f, 0.04f, 1f), // orange
        new Color(1.00f, 0.88f, 0.02f, 1f), // yellow
        new Color(0.45f, 1.00f, 0.08f, 1f)  // acid green
    };

    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");

    private sealed class Leak
    {
        public GameObject Object;
        public Mesh Mesh;
        public MeshRenderer Renderer;
        public MaterialPropertyBlock Block;
        public float MajorAngle;
        public float TubeAngle;
        public float MajorDrift;
        public float TubeDrift;
        public float Radius;
        public float Stretch;
        public float Seed;
        public float PulseSpeed;
        public float PulsePhase;
        public Color Color;
        public Vector3[] Vertices;
        public Vector2[] Uvs;
        public int[] Triangles;
    }

    private readonly List<Leak> activeLeaks = new();
    private Coroutine spawnRoutine;
    private float motionEnergy;
    private bool selected;
    private bool opening;

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

        for (int i = activeLeaks.Count - 1; i >= 0; i--)
        {
            DestroyLeak(activeLeaks[i]);
        }

        activeLeaks.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            activeLeaks.RemoveAll(leak => leak == null || leak.Object == null);

            int allowed = maximumActiveLeaks + (opening ? 2 : 0);

            if (activeLeaks.Count < allowed)
            {
                CreateLeak();
            }

            float delay = Random.Range(minimumSpawnDelay, maximumSpawnDelay);
            delay /= 1f + motionEnergy * motionSpawnBoost;

            yield return new WaitForSeconds(delay);
        }
    }

    private void CreateLeak()
    {
        if (overflowMaterial == null)
        {
            Debug.LogError("PlasmaOverflow needs an Overflow Material assigned.", this);
            enabled = false;
            return;
        }

        GameObject go = new GameObject("PlasmaOverflowLeak");
        go.transform.SetParent(transform, false);

        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();

        renderer.sharedMaterial = overflowMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Mesh mesh = new Mesh { name = "PlasmaOverflowLeakMesh" };
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;

        Leak leak = new Leak
        {
            Object = go,
            Mesh = mesh,
            Renderer = renderer,
            Block = new MaterialPropertyBlock(),
            MajorAngle = Random.Range(0f, Mathf.PI * 2f),
            TubeAngle = Random.Range(0f, Mathf.PI * 2f),
            MajorDrift = Mathf.Deg2Rad * Random.Range(-maximumRingDriftDegrees, maximumRingDriftDegrees),
            TubeDrift = Mathf.Deg2Rad * Random.Range(-maximumTubeDriftDegrees, maximumTubeDriftDegrees),
            Radius = Random.Range(minimumRadius, maximumRadius),
            Stretch = Random.Range(minimumStretch, maximumStretch),
            Seed = Random.Range(0f, 1000f),
            PulseSpeed = Random.Range(minimumPulseSpeed, maximumPulseSpeed),
            PulsePhase = Random.Range(0f, Mathf.PI * 2f),
            Color = GetRandomColor()
        };

        BuildTopology(leak);
        activeLeaks.Add(leak);
        StartCoroutine(AnimateLeak(leak));
    }

    private void BuildTopology(Leak leak)
    {
        int columns = radialSegments + 1;
        int rows = ringSegments + 1;

        leak.Vertices = new Vector3[columns * rows];
        leak.Uvs = new Vector2[columns * rows];

        List<int> triangles = new List<int>(radialSegments * ringSegments * 6);

        for (int x = 0; x < columns; x++)
        {
            float angle01 = x / (float)radialSegments;

            for (int y = 0; y < rows; y++)
            {
                float radius01 = y / (float)ringSegments;
                int index = x * rows + y;
                leak.Uvs[index] = new Vector2(angle01, radius01);
            }
        }

        for (int x = 0; x < radialSegments; x++)
        {
            for (int y = 0; y < ringSegments; y++)
            {
                int current = x * rows + y;
                int next = current + rows;

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);

                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }

        leak.Triangles = triangles.ToArray();
        leak.Mesh.vertices = leak.Vertices;
        leak.Mesh.uv = leak.Uvs;
        leak.Mesh.triangles = leak.Triangles;
    }

    private IEnumerator AnimateLeak(Leak leak)
    {
        float growTime = Random.Range(minimumGrowTime, maximumGrowTime);
        float holdTime = Random.Range(minimumHoldTime, maximumHoldTime);
        float meltTime = Random.Range(minimumMeltTime, maximumMeltTime);

        float elapsed = 0f;

        while (elapsed < growTime && leak.Object != null)
        {
            elapsed += Time.deltaTime;
            float progress = Smooth01(elapsed / growTime);
            UpdateLeak(leak, progress, progress, progress);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < holdTime && leak.Object != null)
        {
            elapsed += Time.deltaTime;
            float progress = holdTime <= 0f ? 1f : elapsed / holdTime;
            UpdateLeak(leak, progress, 1f, 1f);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < meltTime && leak.Object != null)
        {
            elapsed += Time.deltaTime;
            float progress = Smooth01(elapsed / meltTime);
            UpdateLeak(leak, 1f, 1f - progress, 1f - progress);
            yield return null;
        }

        activeLeaks.Remove(leak);
        DestroyLeak(leak);
    }

    private void UpdateLeak(Leak leak, float pathProgress, float scaleProgress, float opacity)
    {
        if (leak.Object == null)
        {
            return;
        }

        int rows = ringSegments + 1;

        float majorAngle = leak.MajorAngle + leak.MajorDrift * pathProgress;
        float tubeAngle = leak.TubeAngle + leak.TubeDrift * pathProgress;

        Vector3 radial = new Vector3(Mathf.Cos(majorAngle), 0f, Mathf.Sin(majorAngle));
        Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);

        Vector3 tubeOut =
            radial * Mathf.Cos(tubeAngle) +
            Vector3.up * Mathf.Sin(tubeAngle);

        Vector3 tubeTangent =
            -radial * Mathf.Sin(tubeAngle) +
            Vector3.up * Mathf.Cos(tubeAngle);

        Vector3 center =
            radial * majorRadius +
            tubeOut * (tubeRadius + surfaceOffset);

        float pulse =
            1f +
            Mathf.Sin(
                Time.time * leak.PulseSpeed * Mathf.PI * 2f +
                leak.PulsePhase
            ) *
            pulseAmount;

        float selectedMultiplier = selected ? 0.92f : 1f;
        float openingMultiplier = opening ? 1.18f : 1f;
        float motionMultiplier = 1f + motionEnergy * motionSizeBoost;

        float effectiveRadius =
            leak.Radius *
            scaleProgress *
            pulse *
            selectedMultiplier *
            openingMultiplier *
            motionMultiplier;

        for (int x = 0; x <= radialSegments; x++)
        {
            float angle01 = x / (float)radialSegments;
            float angle = angle01 * Mathf.PI * 2f;

            float outlineNoise =
                Mathf.Sin(angle * 3f + leak.Seed) * 0.55f +
                Mathf.Sin(angle * 5f + leak.Seed * 1.73f) * 0.30f +
                Mathf.Sin(angle * 8f + leak.Seed * 2.17f) * 0.15f;

            float edgeRadius =
                effectiveRadius *
                (1f + outlineNoise * outlineIrregularity);

            Vector3 edgeDirection =
                tangent * Mathf.Cos(angle) * leak.Stretch +
                tubeTangent * Mathf.Sin(angle);

            for (int y = 0; y <= ringSegments; y++)
            {
                float radius01 = y / (float)ringSegments;
                float curvedRadius = Mathf.Pow(radius01, 0.85f);
                float dome = Mathf.Sin(radius01 * Mathf.PI);

                float surfaceNoiseValue =
                    Mathf.Sin(
                        leak.Seed * 2.31f +
                        angle * 4.7f +
                        radius01 * 7.9f +
                        Time.time * 0.75f
                    );

                Vector3 vertex =
                    center +
                    edgeDirection * edgeRadius * curvedRadius +
                    tubeOut *
                    (
                        dome * effectiveRadius * 0.28f +
                        surfaceNoiseValue *
                        surfaceIrregularity *
                        effectiveRadius *
                        0.10f
                    );

                leak.Vertices[x * rows + y] = vertex;
            }
        }

        leak.Mesh.vertices = leak.Vertices;
        leak.Mesh.RecalculateNormals();
        leak.Mesh.RecalculateBounds();

        leak.Renderer.GetPropertyBlock(leak.Block);
        leak.Block.SetColor(TintId, leak.Color);
        leak.Block.SetFloat(OpacityId, Mathf.Clamp01(opacity));
        leak.Block.SetFloat(SeedId, leak.Seed);
        leak.Renderer.SetPropertyBlock(leak.Block);
    }

    private Color GetRandomColor()
    {
        if (leakColors == null || leakColors.Length == 0)
        {
            return Color.cyan;
        }

        return leakColors[Random.Range(0, leakColors.Length)];
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static void DestroyLeak(Leak leak)
    {
        if (leak == null)
        {
            return;
        }

        if (leak.Mesh != null)
        {
            Destroy(leak.Mesh);
        }

        if (leak.Object != null)
        {
            Destroy(leak.Object);
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
        maximumSpawnDelay = Mathf.Max(maximumSpawnDelay, minimumSpawnDelay);
        maximumRadius = Mathf.Max(maximumRadius, minimumRadius);
        maximumStretch = Mathf.Max(maximumStretch, minimumStretch);
        maximumGrowTime = Mathf.Max(maximumGrowTime, minimumGrowTime);
        maximumHoldTime = Mathf.Max(maximumHoldTime, minimumHoldTime);
        maximumMeltTime = Mathf.Max(maximumMeltTime, minimumMeltTime);
        maximumPulseSpeed = Mathf.Max(maximumPulseSpeed, minimumPulseSpeed);
    }
#endif
}
