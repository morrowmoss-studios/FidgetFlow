using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class StarloomRingVines : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Material using the FidgetFlow/StarloomVine shader.")]
    [SerializeField] private Material vineMaterial;

    [Tooltip("Camera viewing the portal. Leave empty to use Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Torus Dimensions")]
    [Tooltip("Distance from the ring center to the center of the torus tube.")]
    [Min(0.01f)]
    [SerializeField] private float majorRadius = 1f;

    [Tooltip("Radius of the torus tube.")]
    [Min(0.001f)]
    [SerializeField] private float tubeRadius = 0.10f;

    [Tooltip("Raises vines slightly above the torus surface.")]
    [Min(0f)]
    [SerializeField] private float surfaceOffset = 0.025f;

    [Tooltip("Additional lift toward the camera to prevent clipping.")]
    [Min(0f)]
    [SerializeField] private float cameraSurfaceLift = 0.035f;

    [Header("Population")]
    [Range(1, 16)]
    [SerializeField] private int maximumActiveVines = 6;

    [Min(0.05f)]
    [SerializeField] private float minimumSpawnDelay = 0.20f;

    [Min(0.05f)]
    [SerializeField] private float maximumSpawnDelay = 0.55f;

    [Header("Inside Portal")]
    [Range(0f, 0.98f)]
    [SerializeField] private float minimumStartRadius = 0.78f;

    [Range(0.05f, 0.99f)]
    [SerializeField] private float maximumStartRadius = 0.94f;

    [Min(0f)]
    [SerializeField] private float escapeCurveAmount = 0.05f;

    [Range(0f, 1f)]
    [SerializeField] private float angularity = 0.75f;

    [Header("Ring Crawl")]
    [Range(2f, 120f)]
    [SerializeField] private float minimumRingTravelDegrees = 16f;

    [Range(2f, 180f)]
    [SerializeField] private float maximumRingTravelDegrees = 42f;

    [Tooltip("Chance that a vine travels substantially farther around the ring.")]
    [Range(0f, 1f)]
    [SerializeField] private float longVineChance = 0.12f;

    [Range(20f, 180f)]
    [SerializeField] private float minimumLongVineDegrees = 55f;

    [Range(20f, 240f)]
    [SerializeField] private float maximumLongVineDegrees = 95f;

    [Range(0f, 180f)]
    [SerializeField] private float tubeWrapDegrees = 95f;

    [Min(0f)]
    [SerializeField] private float ringWobbleAmount = 0.012f;

    [Header("Branching")]
    [Range(0f, 1f)]
    [SerializeField] private float branchChance = 0.35f;

    [Range(0, 3)]
    [SerializeField] private int maximumBranchesPerVine = 2;

    [Range(0.2f, 0.9f)]
    [SerializeField] private float minimumBranchStart = 0.35f;

    [Range(0.2f, 0.95f)]
    [SerializeField] private float maximumBranchStart = 0.72f;

    [Range(4f, 60f)]
    [SerializeField] private float minimumBranchTravelDegrees = 8f;

    [Range(4f, 90f)]
    [SerializeField] private float maximumBranchTravelDegrees = 22f;

    [Range(4, 32)]
    [SerializeField] private int branchPointCount = 10;

    [Range(0.1f, 1f)]
    [SerializeField] private float branchWidthMultiplier = 0.55f;

    [Header("Path Resolution")]
    [Range(4, 24)]
    [SerializeField] private int escapePointCount = 6;

    [Range(8, 64)]
    [SerializeField] private int ringPointCount = 24;

    [Header("Thickness")]
    [Min(0.0005f)]
    [SerializeField] private float minimumWidth = 0.003f;

    [Min(0.0005f)]
    [SerializeField] private float maximumWidth = 0.014f;

    [Tooltip("How much each vine gently changes thickness while alive.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float breathingAmount = 0.12f;

    [Min(0f)]
    [SerializeField] private float minimumBreathingSpeed = 0.45f;

    [Min(0f)]
    [SerializeField] private float maximumBreathingSpeed = 0.90f;

    [Header("Animation")]
    [Min(0.05f)]
    [SerializeField] private float minimumGrowTime = 0.45f;

    [Min(0.05f)]
    [SerializeField] private float maximumGrowTime = 0.85f;

    [Min(0f)]
    [SerializeField] private float minimumHoldTime = 0.55f;

    [Min(0f)]
    [SerializeField] private float maximumHoldTime = 1.25f;

    [Min(0.05f)]
    [SerializeField] private float minimumRetractTime = 0.45f;

    [Min(0.05f)]
    [SerializeField] private float maximumRetractTime = 0.85f;

    [Header("Audio Reactivity")]
    [Tooltip("Allows the global StarLoom audio values to affect the vines.")]
    [SerializeField] private bool useAudioReactivity = true;

    [Tooltip("Bass increases vine thickness.")]
    [Range(0f, 2f)]
    [SerializeField] private float bassWidthBoost = 0.45f;

    [Tooltip("High frequencies slightly increase spawn activity.")]
    [Range(0f, 1f)]
    [SerializeField] private float highSpawnBoost = 0.30f;

    [Header("Colors")]
    [SerializeField] private Color[] vineColors =
    {
        new Color(0.10f, 0.85f, 1.00f, 1f),
        new Color(0.18f, 0.40f, 1.00f, 1f),
        new Color(0.55f, 0.22f, 1.00f, 1f),
        new Color(1.00f, 0.18f, 0.72f, 1f),
        new Color(0.28f, 1.00f, 0.35f, 1f),
        new Color(1.00f, 0.72f, 0.12f, 1f)
    };

    private sealed class VineInstance
    {
        public LineRenderer MainLine;
        public readonly List<LineRenderer> BranchLines = new();

        public float BaseWidth;
        public float BreathingSpeed;
        public float BreathingPhase;
    }

    private sealed class BranchPath
    {
        public LineRenderer Line;
        public Vector3[] Points;
        public float StartProgress;
    }

    private readonly List<VineInstance> activeVines = new();

    private Coroutine spawnRoutine;

    private static readonly int AudioBassId =
        Shader.PropertyToID("_AudioBass");

    private static readonly int AudioHighId =
        Shader.PropertyToID("_AudioHigh");

    private void Awake()
    {
        ResolveCamera();
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

        for (int i = activeVines.Count - 1; i >= 0; i--)
        {
            DestroyVine(activeVines[i]);
        }

        activeVines.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            activeVines.RemoveAll(
                vine =>
                    vine == null ||
                    vine.MainLine == null
            );

            if (activeVines.Count < maximumActiveVines)
            {
                CreateVine();
            }

            float delay = Random.Range(
                minimumSpawnDelay,
                maximumSpawnDelay
            );

            if (useAudioReactivity)
            {
                float high =
                    Mathf.Clamp01(
                        Shader.GetGlobalFloat(AudioHighId)
                    );

                delay *= Mathf.Lerp(
                    1f,
                    0.45f,
                    high * highSpawnBoost
                );
            }

            yield return new WaitForSeconds(delay);
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

        GameObject vineObject =
            new GameObject("StarloomRingVine");

        vineObject.transform.SetParent(transform, false);

        LineRenderer mainLine =
            vineObject.AddComponent<LineRenderer>();

        float baseWidth =
            Random.Range(
                minimumWidth,
                maximumWidth
            );

        ConfigureLine(
            mainLine,
            baseWidth
        );

        Vector3[] mainPoints =
            GenerateMainVinePoints();

        Color vineColor =
            GetRandomColor();

        VineInstance vine =
            new VineInstance
            {
                MainLine = mainLine,
                BaseWidth = baseWidth,
                BreathingSpeed = Random.Range(
                    minimumBreathingSpeed,
                    maximumBreathingSpeed
                ),
                BreathingPhase = Random.Range(
                    0f,
                    Mathf.PI * 2f
                )
            };

        List<BranchPath> branches =
            CreateBranches(
                vineObject.transform,
                mainPoints,
                vineColor,
                baseWidth
            );

        foreach (BranchPath branch in branches)
        {
            vine.BranchLines.Add(branch.Line);
        }

        activeVines.Add(vine);

        StartCoroutine(
            AnimateVine(
                vine,
                mainPoints,
                branches,
                vineColor
            )
        );
    }

    private void ConfigureLine(
        LineRenderer line,
        float width
    )
    {
        line.useWorldSpace = false;
        line.loop = false;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;

        line.sharedMaterial = vineMaterial;

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows = false;

        line.numCapVertices = 4;
        line.numCornerVertices = 3;
        line.sortingOrder = 110;

        line.widthMultiplier = width;

        line.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.18f),
            new Keyframe(0.10f, 1f),
            new Keyframe(0.72f, 0.72f),
            new Keyframe(1f, 0f)
        );

        line.positionCount = 0;
    }

    private Vector3[] GenerateMainVinePoints()
    {
        ResolveCamera();

        int totalPointCount =
            escapePointCount +
            ringPointCount;

        Vector3[] points =
            new Vector3[totalPointCount];

        float innerLipRadius =
            majorRadius -
            tubeRadius;

        float startingAngle =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );

        float startingRadius =
            innerLipRadius *
            Random.Range(
                minimumStartRadius,
                maximumStartRadius
            );

        float ringDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float tubeDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float travelDegrees;

        if (Random.value < longVineChance)
        {
            travelDegrees =
                Random.Range(
                    minimumLongVineDegrees,
                    maximumLongVineDegrees
                );
        }
        else
        {
            travelDegrees =
                Random.Range(
                    minimumRingTravelDegrees,
                    maximumRingTravelDegrees
                );
        }

        float ringTravelRadians =
            Mathf.Deg2Rad *
            travelDegrees *
            ringDirection;

        Vector3 localCameraDirection =
            GetLocalCameraDirection();

        Vector3 startingRadial =
            GetRadialDirection(startingAngle);

        Vector3 startingTangent =
            GetTangentDirection(startingRadial);

        float escapeCurveDirection =
            Random.Range(
                -escapeCurveAmount,
                escapeCurveAmount
            );

        float angularKickDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;

        for (int i = 0; i < escapePointCount; i++)
        {
            float progress =
                i /
                (float)(escapePointCount - 1);

            float smoothedProgress =
                SmoothStep01(progress);

            float radius =
                Mathf.Lerp(
                    startingRadius,
                    innerLipRadius,
                    smoothedProgress
                );

            Vector3 point =
                startingRadial *
                radius;

            float smoothCurve =
                Mathf.Sin(
                    progress *
                    Mathf.PI
                ) *
                escapeCurveDirection;

            float angularSegment =
                GetAngularSegmentOffset(
                    progress,
                    angularKickDirection
                );

            point +=
                startingTangent *
                Mathf.Lerp(
                    smoothCurve,
                    angularSegment,
                    angularity
                );

            point +=
                localCameraDirection *
                cameraSurfaceLift;

            points[i] = point;
        }

        float wobbleFrequency =
            Random.Range(1.5f, 3.5f);

        float wobblePhase =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );

        for (int i = 0; i < ringPointCount; i++)
        {
            float progress =
                i /
                (float)(ringPointCount - 1);

            float mainAngle =
                startingAngle +
                ringTravelRadians *
                progress;

            points[escapePointCount + i] =
                GetPointOnRingSurface(
                    mainAngle,
                    progress,
                    tubeDirection,
                    wobbleFrequency,
                    wobblePhase,
                    localCameraDirection
                );
        }

        return points;
    }

    private List<BranchPath> CreateBranches(
        Transform parent,
        Vector3[] mainPoints,
        Color mainColor,
        float mainWidth
    )
    {
        List<BranchPath> branches =
            new List<BranchPath>();

        if (
            maximumBranchesPerVine <= 0 ||
            Random.value > branchChance
        )
        {
            return branches;
        }

        int branchCount =
            Random.Range(
                1,
                maximumBranchesPerVine + 1
            );

        for (int branchIndex = 0;
             branchIndex < branchCount;
             branchIndex++)
        {
            float branchStartProgress =
                Random.Range(
                    minimumBranchStart,
                    maximumBranchStart
                );

            int mainIndex =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        branchStartProgress *
                        (mainPoints.Length - 1)
                    ),
                    escapePointCount,
                    mainPoints.Length - 2
                );

            Vector3 startPoint =
                mainPoints[mainIndex];

            float branchAngle =
                Mathf.Atan2(
                    startPoint.z,
                    startPoint.x
                );

            float branchDirection =
                Random.value < 0.5f
                    ? -1f
                    : 1f;

            float branchTravel =
                Mathf.Deg2Rad *
                Random.Range(
                    minimumBranchTravelDegrees,
                    maximumBranchTravelDegrees
                ) *
                branchDirection;

            Vector3[] branchPoints =
                GenerateBranchPoints(
                    startPoint,
                    branchAngle,
                    branchTravel
                );

            GameObject branchObject =
                new GameObject(
                    "StarloomRingVine_Branch"
                );

            branchObject.transform.SetParent(
                parent,
                false
            );

            LineRenderer branchLine =
                branchObject.AddComponent<LineRenderer>();

            ConfigureLine(
                branchLine,
                mainWidth *
                branchWidthMultiplier
            );

            Color branchColor =
                Color.Lerp(
                    mainColor,
                    GetRandomColor(),
                    0.25f
                );

            branchLine.startColor =
                WithAlpha(
                    branchColor,
                    0.9f
                );

            branchLine.endColor =
                WithAlpha(
                    branchColor,
                    0f
                );

            branches.Add(
                new BranchPath
                {
                    Line = branchLine,
                    Points = branchPoints,
                    StartProgress = branchStartProgress
                }
            );
        }

        return branches;
    }

    private Vector3[] GenerateBranchPoints(
        Vector3 startPoint,
        float startingAngle,
        float branchTravel
    )
    {
        Vector3[] points =
            new Vector3[branchPointCount];

        Vector3 localCameraDirection =
            GetLocalCameraDirection();

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

        for (int i = 0; i < branchPointCount; i++)
        {
            float progress =
                i /
                (float)(branchPointCount - 1);

            float angle =
                startingAngle +
                branchTravel *
                progress;

            Vector3 ringPoint =
                GetPointOnRingSurface(
                    angle,
                    progress,
                    tubeDirection,
                    wobbleFrequency,
                    wobblePhase,
                    localCameraDirection
                );

            float connectionBlend =
                SmoothStep01(
                    Mathf.Clamp01(
                        progress / 0.22f
                    )
                );

            points[i] =
                Vector3.Lerp(
                    startPoint,
                    ringPoint,
                    connectionBlend
                );
        }

        return points;
    }

    private Vector3 GetPointOnRingSurface(
        float mainAngle,
        float progress,
        float tubeDirection,
        float wobbleFrequency,
        float wobblePhase,
        Vector3 localCameraDirection
    )
    {
        Vector3 radial =
            GetRadialDirection(mainAngle);

        Vector3 tangent =
            GetTangentDirection(radial);

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
                    progress /
                    0.28f
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
                progress *
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
                progress *
                Mathf.PI *
                2f *
                wobbleFrequency +
                wobblePhase
            ) *
            ringWobbleAmount *
            Mathf.Sin(
                progress *
                Mathf.PI
            );

        point +=
            tangent *
            wobble;

        point +=
            localCameraDirection *
            cameraSurfaceLift;

        return point;
    }

    private IEnumerator AnimateVine(
        VineInstance vine,
        Vector3[] mainPoints,
        List<BranchPath> branches,
        Color baseColor
    )
    {
        float growTime =
            Random.Range(
                minimumGrowTime,
                maximumGrowTime
            );

        float holdTime =
            Random.Range(
                minimumHoldTime,
                maximumHoldTime
            );

        float retractTime =
            Random.Range(
                minimumRetractTime,
                maximumRetractTime
            );

        vine.MainLine.startColor =
            WithAlpha(
                baseColor,
                1f
            );

        vine.MainLine.endColor =
            WithAlpha(
                baseColor,
                0.22f
            );

        float elapsed = 0f;

        while (
            elapsed < growTime &&
            vine.MainLine != null
        )
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    growTime
                );

            ApplyLivingWidth(vine);

            SetVisiblePath(
                vine.MainLine,
                mainPoints,
                progress
            );

            UpdateGrowingBranches(
                branches,
                progress
            );

            yield return null;
        }

        if (vine.MainLine == null)
        {
            yield break;
        }

        vine.MainLine.positionCount =
            mainPoints.Length;

        vine.MainLine.SetPositions(
            mainPoints
        );

        foreach (BranchPath branch in branches)
        {
            if (branch.Line == null)
            {
                continue;
            }

            branch.Line.positionCount =
                branch.Points.Length;

            branch.Line.SetPositions(
                branch.Points
            );
        }

        elapsed = 0f;

        while (
            elapsed < holdTime &&
            vine.MainLine != null
        )
        {
            elapsed += Time.deltaTime;

            ApplyLivingWidth(vine);

            yield return null;
        }

        elapsed = 0f;

        while (
            elapsed < retractTime &&
            vine.MainLine != null
        )
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    retractTime
                );

            ApplyLivingWidth(vine);

            float remainingProgress =
                1f -
                progress;

            SetVisiblePath(
                vine.MainLine,
                mainPoints,
                remainingProgress
            );

            foreach (BranchPath branch in branches)
            {
                if (branch.Line == null)
                {
                    continue;
                }

                float branchRemaining =
                    Mathf.InverseLerp(
                        branch.StartProgress,
                        1f,
                        remainingProgress
                    );

                SetVisiblePath(
                    branch.Line,
                    branch.Points,
                    branchRemaining
                );
            }

            float alpha =
                1f -
                progress;

            vine.MainLine.startColor =
                WithAlpha(
                    baseColor,
                    alpha
                );

            vine.MainLine.endColor =
                WithAlpha(
                    baseColor,
                    alpha * 0.22f
                );

            yield return null;
        }

        activeVines.Remove(vine);
        DestroyVine(vine);
    }

    private void UpdateGrowingBranches(
        List<BranchPath> branches,
        float mainProgress
    )
    {
        foreach (BranchPath branch in branches)
        {
            if (branch.Line == null)
            {
                continue;
            }

            float branchProgress =
                Mathf.InverseLerp(
                    branch.StartProgress,
                    1f,
                    mainProgress
                );

            SetVisiblePath(
                branch.Line,
                branch.Points,
                branchProgress
            );
        }
    }

    private void SetVisiblePath(
        LineRenderer line,
        Vector3[] points,
        float progress
    )
    {
        if (line == null)
        {
            return;
        }

        progress =
            Mathf.Clamp01(progress);

        int visiblePointCount =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    progress *
                    points.Length
                ),
                progress > 0f ? 2 : 0,
                points.Length
            );

        line.positionCount =
            visiblePointCount;

        for (int i = 0;
             i < visiblePointCount;
             i++)
        {
            line.SetPosition(
                i,
                points[i]
            );
        }
    }

    private void ApplyLivingWidth(
        VineInstance vine
    )
    {
        float breathing =
            1f +
            Mathf.Sin(
                Time.time *
                vine.BreathingSpeed *
                Mathf.PI *
                2f +
                vine.BreathingPhase
            ) *
            breathingAmount;

        float bassMultiplier =
            1f;

        if (useAudioReactivity)
        {
            float bass =
                Mathf.Clamp01(
                    Shader.GetGlobalFloat(AudioBassId)
                );

            bassMultiplier +=
                bass *
                bassWidthBoost;
        }

        vine.MainLine.widthMultiplier =
            vine.BaseWidth *
            breathing *
            bassMultiplier;

        foreach (LineRenderer branch in vine.BranchLines)
        {
            if (branch == null)
            {
                continue;
            }

            branch.widthMultiplier =
                vine.BaseWidth *
                branchWidthMultiplier *
                breathing *
                bassMultiplier;
        }
    }

    private float GetAngularSegmentOffset(
        float progress,
        float direction
    )
    {
        float segmentValue;

        if (progress < 0.34f)
        {
            segmentValue =
                progress /
                0.34f;
        }
        else if (progress < 0.68f)
        {
            segmentValue =
                1f -
                (
                    progress -
                    0.34f
                ) /
                0.34f;
        }
        else
        {
            segmentValue =
                (
                    progress -
                    0.68f
                ) /
                0.32f;
        }

        return
            (
                segmentValue -
                0.5f
            ) *
            escapeCurveAmount *
            direction;
    }

    private Vector3 GetRadialDirection(
        float angle
    )
    {
        return new Vector3(
            Mathf.Cos(angle),
            0f,
            Mathf.Sin(angle)
        );
    }

    private Vector3 GetTangentDirection(
        Vector3 radialDirection
    )
    {
        return new Vector3(
            -radialDirection.z,
            0f,
            radialDirection.x
        );
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private Vector3 GetLocalCameraDirection()
    {
        ResolveCamera();

        if (targetCamera == null)
        {
            return Vector3.up;
        }

        Vector3 worldDirection =
            (
                targetCamera.transform.position -
                transform.position
            ).normalized;

        return transform
            .InverseTransformDirection(
                worldDirection
            )
            .normalized;
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
        color.a =
            Mathf.Clamp01(alpha);

        return color;
    }

    private static float SmoothStep01(
        float value
    )
    {
        value =
            Mathf.Clamp01(value);

        return
            value *
            value *
            (
                3f -
                2f *
                value
            );
    }

    private static void DestroyVine(
        VineInstance vine
    )
    {
        if (vine == null)
        {
            return;
        }

        if (vine.MainLine != null)
        {
            Destroy(
                vine.MainLine.gameObject
            );
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maximumSpawnDelay =
            Mathf.Max(
                maximumSpawnDelay,
                minimumSpawnDelay
            );

        maximumStartRadius =
            Mathf.Max(
                maximumStartRadius,
                minimumStartRadius
            );

        maximumRingTravelDegrees =
            Mathf.Max(
                maximumRingTravelDegrees,
                minimumRingTravelDegrees
            );

        maximumLongVineDegrees =
            Mathf.Max(
                maximumLongVineDegrees,
                minimumLongVineDegrees
            );

        maximumBranchStart =
            Mathf.Max(
                maximumBranchStart,
                minimumBranchStart
            );

        maximumBranchTravelDegrees =
            Mathf.Max(
                maximumBranchTravelDegrees,
                minimumBranchTravelDegrees
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

        maximumBreathingSpeed =
            Mathf.Max(
                maximumBreathingSpeed,
                minimumBreathingSpeed
            );
    }
#endif
}