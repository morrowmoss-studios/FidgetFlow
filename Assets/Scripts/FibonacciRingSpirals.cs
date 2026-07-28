using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class FibonacciRingSpirals : MonoBehaviour, IPortalAccessory
{
    [Header("Required")]
    [SerializeField] private Material spiralMaterial;

    [Header("Ring Geometry")]
    [Min(0.01f)] [SerializeField] private float majorRadius = 1f;
    [Min(0.001f)] [SerializeField] private float tubeRadius = 0.10f;
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.012f;

    [Header("Population")]
    [Range(1, 24)] [SerializeField] private int maximumActiveSpirals = 14;
    [Min(0.05f)] [SerializeField] private float minimumSpawnDelay = 0.12f;
    [Min(0.05f)] [SerializeField] private float maximumSpawnDelay = 0.32f;

    [Header("Spiral Geometry")]
    [Range(8, 64)] [SerializeField] private int pointsPerSpiral = 24;

    [Tooltip("Very low values create broad curved streaks instead of curls.")]
    [Range(0.02f, 1.5f)] [SerializeField] private float minimumTurns = 0.08f;

    [Tooltip("Keep this below about 0.35 to avoid curly spirals.")]
    [Range(0.02f, 2f)] [SerializeField] private float maximumTurns = 0.24f;

    [Min(0.002f)] [SerializeField] private float minimumStartRadius = 0.018f;
    [Min(0.002f)] [SerializeField] private float maximumStartRadius = 0.035f;

    [Min(0.005f)] [SerializeField] private float minimumEndRadius = 0.075f;
    [Min(0.005f)] [SerializeField] private float maximumEndRadius = 0.145f;

    [Min(0.0005f)] [SerializeField] private float minimumWidth = 0.007f;
    [Min(0.0005f)] [SerializeField] private float maximumWidth = 0.015f;

    [Range(0f, 1f)] [SerializeField] private float irregularity = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float liftAmount = 0.10f;

    [Header("Animation")]
    [Min(0.05f)] [SerializeField] private float minimumGrowTime = 0.28f;
    [Min(0.05f)] [SerializeField] private float maximumGrowTime = 0.58f;
    [Min(0f)] [SerializeField] private float minimumHoldTime = 0.50f;
    [Min(0f)] [SerializeField] private float maximumHoldTime = 1.10f;
    [Min(0.05f)] [SerializeField] private float minimumRetractTime = 0.34f;
    [Min(0.05f)] [SerializeField] private float maximumRetractTime = 0.70f;

    [Range(0f, 0.5f)] [SerializeField] private float pulseAmount = 0.06f;
    [Min(0f)] [SerializeField] private float minimumPulseSpeed = 0.55f;
    [Min(0f)] [SerializeField] private float maximumPulseSpeed = 1.20f;

    [Header("Motion Reaction")]
    [Range(0f, 2f)] [SerializeField] private float motionSpawnBoost = 0.60f;
    [Range(0f, 2f)] [SerializeField] private float motionScaleBoost = 0.22f;

    [Header("Colors")]
    [SerializeField] private Color[] spiralColors =
    {
        new Color(1.00f, 0.82f, 0.04f, 1f),
        new Color(1.00f, 0.28f, 0.06f, 1f),
        new Color(1.00f, 0.08f, 0.72f, 1f),
        new Color(0.18f, 0.92f, 1.00f, 1f),
        new Color(0.32f, 1.00f, 0.16f, 1f)
    };

    private sealed class Spiral
    {
        public GameObject Object;
        public LineRenderer Line;
        public Vector3[] Points;
        public float BaseWidth;
        public float PulseSpeed;
        public float PulsePhase;
        public Color Color;
    }

    private readonly List<Spiral> activeSpirals = new();

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

        for (int i = activeSpirals.Count - 1; i >= 0; i--)
        {
            DestroySpiral(activeSpirals[i]);
        }

        activeSpirals.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            activeSpirals.RemoveAll(
                spiral => spiral == null || spiral.Object == null
            );

            int allowed = maximumActiveSpirals + (opening ? 3 : 0);

            if (activeSpirals.Count < allowed)
            {
                CreateSpiral();
            }

            float delay = Random.Range(
                minimumSpawnDelay,
                maximumSpawnDelay
            );

            delay /= 1f + motionEnergy * motionSpawnBoost;

            yield return new WaitForSeconds(delay);
        }
    }

    private void CreateSpiral()
    {
        if (spiralMaterial == null)
        {
            Debug.LogError(
                "FibonacciRingSpirals needs a Spiral Material assigned.",
                this
            );

            enabled = false;
            return;
        }

        GameObject go = new GameObject("FibonacciRingSpiral");
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();

        float width = Random.Range(
            minimumWidth,
            maximumWidth
        );

        line.useWorldSpace = false;
        line.loop = false;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = spiralMaterial;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 5;
        line.numCornerVertices = 5;
        line.sortingOrder = 130;
        line.widthMultiplier = width;

        line.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.20f),
            new Keyframe(0.15f, 0.90f),
            new Keyframe(0.72f, 1.00f),
            new Keyframe(1f, 0f)
        );

        Color color = GetRandomColor();

        line.startColor = new Color(
            color.r,
            color.g,
            color.b,
            0.95f
        );

        line.endColor = new Color(
            color.r,
            color.g,
            color.b,
            0f
        );

        Spiral spiral = new Spiral
        {
            Object = go,
            Line = line,
            Points = GenerateSpiralPoints(),
            BaseWidth = width,
            PulseSpeed = Random.Range(
                minimumPulseSpeed,
                maximumPulseSpeed
            ),
            PulsePhase = Random.Range(
                0f,
                Mathf.PI * 2f
            ),
            Color = color
        };

        activeSpirals.Add(spiral);
        StartCoroutine(AnimateSpiral(spiral));
    }

    private Vector3[] GenerateSpiralPoints()
    {
        Vector3[] points = new Vector3[pointsPerSpiral];

        float majorAngle = Random.Range(
            0f,
            Mathf.PI * 2f
        );

        float tubeAngle = Random.Range(
            0f,
            Mathf.PI * 2f
        );

        float direction = Random.value < 0.5f ? -1f : 1f;

        float turns = Random.Range(
            minimumTurns,
            maximumTurns
        );

        float startRadius = Random.Range(
            minimumStartRadius,
            maximumStartRadius
        );

        float endRadius = Random.Range(
            minimumEndRadius,
            maximumEndRadius
        );

        float seed = Random.Range(0f, 1000f);

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

        Vector3 center =
            radial * majorRadius +
            tubeOut * (tubeRadius + surfaceOffset);

        const float goldenGrowth = 1.61803398875f;

        for (int i = 0; i < pointsPerSpiral; i++)
        {
            float t = i / (float)(pointsPerSpiral - 1);

            float theta =
                direction *
                t *
                turns *
                Mathf.PI *
                2f;

            float logarithmic =
                (
                    Mathf.Pow(goldenGrowth, t) -
                    1f
                ) /
                (goldenGrowth - 1f);

            float radius = Mathf.Lerp(
                startRadius,
                endRadius,
                logarithmic
            );

            float wobble =
                Mathf.Sin(
                    theta * 2.7f +
                    seed
                ) *
                irregularity *
                radius *
                0.22f;

            float lifted =
                Mathf.Sin(t * Mathf.PI) *
                radius *
                liftAmount;

            Vector3 planarOffset =
                tangent *
                Mathf.Cos(theta) *
                (radius + wobble) +
                tubeTangent *
                Mathf.Sin(theta) *
                (radius + wobble);

            points[i] =
                center +
                planarOffset +
                tubeOut * lifted;
        }

        return points;
    }

    private IEnumerator AnimateSpiral(Spiral spiral)
    {
        float growTime = Random.Range(
            minimumGrowTime,
            maximumGrowTime
        );

        float holdTime = Random.Range(
            minimumHoldTime,
            maximumHoldTime
        );

        float retractTime = Random.Range(
            minimumRetractTime,
            maximumRetractTime
        );

        float elapsed = 0f;

        while (elapsed < growTime && spiral.Object != null)
        {
            elapsed += Time.deltaTime;

            float progress = Smooth01(
                elapsed / growTime
            );

            ApplyLivingWidth(spiral);
            SetVisiblePath(
                spiral.Line,
                spiral.Points,
                progress
            );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < holdTime && spiral.Object != null)
        {
            elapsed += Time.deltaTime;

            ApplyLivingWidth(spiral);

            spiral.Line.positionCount =
                spiral.Points.Length;

            spiral.Line.SetPositions(
                spiral.Points
            );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < retractTime && spiral.Object != null)
        {
            elapsed += Time.deltaTime;

            float progress = Smooth01(
                elapsed / retractTime
            );

            ApplyLivingWidth(spiral);

            SetVisiblePath(
                spiral.Line,
                spiral.Points,
                1f - progress
            );

            float alpha = 1f - progress;

            spiral.Line.startColor = new Color(
                spiral.Color.r,
                spiral.Color.g,
                spiral.Color.b,
                alpha
            );

            spiral.Line.endColor = new Color(
                spiral.Color.r,
                spiral.Color.g,
                spiral.Color.b,
                0f
            );

            yield return null;
        }

        activeSpirals.Remove(spiral);
        DestroySpiral(spiral);
    }

    private void ApplyLivingWidth(Spiral spiral)
    {
        if (spiral.Line == null)
        {
            return;
        }

        float pulse =
            1f +
            Mathf.Sin(
                Time.time *
                spiral.PulseSpeed *
                Mathf.PI *
                2f +
                spiral.PulsePhase
            ) *
            pulseAmount;

        float selectedMultiplier =
            selected ? 0.92f : 1f;

        float openingMultiplier =
            opening ? 1.15f : 1f;

        float motionMultiplier =
            1f +
            motionEnergy *
            motionScaleBoost;

        spiral.Line.widthMultiplier =
            spiral.BaseWidth *
            pulse *
            selectedMultiplier *
            openingMultiplier *
            motionMultiplier;
    }

    private static void SetVisiblePath(
        LineRenderer line,
        Vector3[] points,
        float progress
    )
    {
        if (line == null)
        {
            return;
        }

        progress = Mathf.Clamp01(progress);

        int visiblePoints = Mathf.Clamp(
            Mathf.CeilToInt(
                progress *
                points.Length
            ),
            progress > 0f ? 2 : 0,
            points.Length
        );

        line.positionCount = visiblePoints;

        for (int i = 0; i < visiblePoints; i++)
        {
            line.SetPosition(i, points[i]);
        }
    }

    private Color GetRandomColor()
    {
        if (
            spiralColors == null ||
            spiralColors.Length == 0
        )
        {
            return new Color(
                1f,
                0.82f,
                0.04f,
                1f
            );
        }

        return spiralColors[
            Random.Range(
                0,
                spiralColors.Length
            )
        ];
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static void DestroySpiral(Spiral spiral)
    {
        if (
            spiral != null &&
            spiral.Object != null
        )
        {
            Destroy(spiral.Object);
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

        maximumTurns =
            Mathf.Max(
                maximumTurns,
                minimumTurns
            );

        maximumStartRadius =
            Mathf.Max(
                maximumStartRadius,
                minimumStartRadius
            );

        maximumEndRadius =
            Mathf.Max(
                maximumEndRadius,
                minimumEndRadius
            );

        maximumWidth =
            Mathf.Max(
                maximumWidth,
                minimumWidth
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

        maximumRetractTime =
            Mathf.Max(
                maximumRetractTime,
                minimumRetractTime
            );

        maximumPulseSpeed =
            Mathf.Max(
                maximumPulseSpeed,
                minimumPulseSpeed
            );
    }
#endif
}
