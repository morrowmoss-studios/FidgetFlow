using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PlasmaRingCorona :
    MonoBehaviour,
    IPortalAccessory
{
    [Header("Required")]
    [Tooltip("Material using FidgetFlow/PlasmaCorona.")]
    [SerializeField] private Material coronaMaterial;

    [Header("Ring Dimensions")]
    [Tooltip("Distance from portal center to the center of the torus tube.")]
    [Min(0.01f)]
    [SerializeField] private float majorRadius = 1f;

    [Tooltip("Radius of the torus tube.")]
    [Min(0.001f)]
    [SerializeField] private float tubeRadius = 0.10f;

    [Tooltip("Keeps corona tongues slightly above the ring surface.")]
    [Min(0f)]
    [SerializeField] private float surfaceOffset = 0.025f;

    [Header("Population")]
    [Range(1, 16)]
    [SerializeField] private int maximumActiveTongues = 7;

    [Min(0.02f)]
    [SerializeField] private float minimumSpawnDelay = 0.12f;

    [Min(0.02f)]
    [SerializeField] private float maximumSpawnDelay = 0.40f;

    [Header("Tongue Shape")]
    [Range(4, 24)]
    [SerializeField] private int pointCount = 10;

    [Tooltip("Minimum length around the ring.")]
    [Range(2f, 45f)]
    [SerializeField] private float minimumTravelDegrees = 6f;

    [Tooltip("Maximum length around the ring.")]
    [Range(2f, 90f)]
    [SerializeField] private float maximumTravelDegrees = 18f;

    [Tooltip("How far the corona rises away from the ring.")]
    [Min(0f)]
    [SerializeField] private float minimumLift = 0.025f;

    [Min(0f)]
    [SerializeField] private float maximumLift = 0.10f;

    [Tooltip("Sideways curling amount.")]
    [Min(0f)]
    [SerializeField] private float minimumCurl = 0.008f;

    [Min(0f)]
    [SerializeField] private float maximumCurl = 0.035f;

    [Header("Thickness")]
    [Min(0.0005f)]
    [SerializeField] private float minimumWidth = 0.004f;

    [Min(0.0005f)]
    [SerializeField] private float maximumWidth = 0.012f;

    [Header("Animation")]
    [Min(0.03f)]
    [SerializeField] private float minimumGrowTime = 0.18f;

    [Min(0.03f)]
    [SerializeField] private float maximumGrowTime = 0.42f;

    [Min(0f)]
    [SerializeField] private float minimumHoldTime = 0.08f;

    [Min(0f)]
    [SerializeField] private float maximumHoldTime = 0.28f;

    [Min(0.03f)]
    [SerializeField] private float minimumRetractTime = 0.16f;

    [Min(0.03f)]
    [SerializeField] private float maximumRetractTime = 0.38f;

    [Header("Flicker")]
    [Range(0f, 0.8f)]
    [SerializeField] private float flickerAmount = 0.18f;

    [Min(0f)]
    [SerializeField] private float minimumFlickerSpeed = 5f;

    [Min(0f)]
    [SerializeField] private float maximumFlickerSpeed = 11f;

    [Header("Motion Reaction")]
    [Tooltip("Galaxy movement increases corona activity.")]
    [Range(0f, 2f)]
    [SerializeField] private float motionSpawnBoost = 0.75f;

    [Range(0f, 2f)]
    [SerializeField] private float motionLiftBoost = 0.65f;

    [Range(0f, 2f)]
    [SerializeField] private float motionWidthBoost = 0.35f;

    [Header("Colors")]
    [SerializeField] private Color[] coronaColors =
    {
        new Color(0.05f, 0.90f, 1.00f, 1f),
        new Color(0.20f, 0.45f, 1.00f, 1f),
        new Color(0.95f, 0.12f, 0.75f, 1f),
        new Color(1.00f, 0.32f, 0.48f, 1f),
        new Color(1.00f, 0.82f, 0.10f, 1f),
        new Color(0.42f, 1.00f, 0.18f, 1f)
    };

    private sealed class CoronaTongue
    {
        public LineRenderer Line;
        public float BaseWidth;
        public float FlickerSpeed;
        public float FlickerPhase;
    }

    private readonly List<CoronaTongue> activeTongues =
        new List<CoronaTongue>();

    private Coroutine spawnRoutine;

    private float motionEnergy;
    private bool selected;
    private bool opening;

    private void OnEnable()
    {
        spawnRoutine =
            StartCoroutine(
                SpawnLoop()
            );
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(
                spawnRoutine
            );

            spawnRoutine = null;
        }

        for (
            int i = activeTongues.Count - 1;
            i >= 0;
            i--
        )
        {
            DestroyTongue(
                activeTongues[i]
            );
        }

        activeTongues.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            activeTongues.RemoveAll(
                tongue =>
                    tongue == null ||
                    tongue.Line == null
            );

            int allowedTongues =
                maximumActiveTongues;

            if (opening)
            {
                allowedTongues += 3;
            }

            if (
                activeTongues.Count <
                allowedTongues
            )
            {
                CreateTongue();
            }

            float delay =
                Random.Range(
                    minimumSpawnDelay,
                    maximumSpawnDelay
                );

            float activity =
                motionEnergy *
                motionSpawnBoost;

            if (opening)
            {
                activity += 0.75f;
            }

            delay /=
                1f +
                activity;

            yield return new WaitForSeconds(
                delay
            );
        }
    }

    private void CreateTongue()
    {
        if (coronaMaterial == null)
        {
            Debug.LogError(
                "PlasmaRingCorona needs a Corona Material.",
                this
            );

            enabled = false;
            return;
        }

        GameObject tongueObject =
            new GameObject(
                "PlasmaCoronaTongue"
            );

        tongueObject.transform.SetParent(
            transform,
            false
        );

        LineRenderer line =
            tongueObject.AddComponent<LineRenderer>();

        float baseWidth =
            Random.Range(
                minimumWidth,
                maximumWidth
            );

        ConfigureLine(
            line,
            baseWidth
        );

        CoronaTongue tongue =
            new CoronaTongue
            {
                Line = line,
                BaseWidth = baseWidth,
                FlickerSpeed = Random.Range(
                    minimumFlickerSpeed,
                    maximumFlickerSpeed
                ),
                FlickerPhase = Random.Range(
                    0f,
                    Mathf.PI * 2f
                )
            };

        Vector3[] points =
            GenerateTonguePoints();

        Color color =
            GetRandomColor();

        activeTongues.Add(
            tongue
        );

        StartCoroutine(
            AnimateTongue(
                tongue,
                points,
                color
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

        line.alignment =
            LineAlignment.View;

        line.textureMode =
            LineTextureMode.Stretch;

        line.sharedMaterial =
            coronaMaterial;

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows = false;

        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.sortingOrder = 120;

        line.widthMultiplier =
            width;

        line.widthCurve =
            new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.18f, 1f),
                new Keyframe(0.58f, 0.82f),
                new Keyframe(1f, 0f)
            );

        line.positionCount = 0;
    }

    private Vector3[] GenerateTonguePoints()
    {
        Vector3[] points =
            new Vector3[pointCount];

        float startingAngle =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );

        float direction =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float travel =
            Mathf.Deg2Rad *
            Random.Range(
                minimumTravelDegrees,
                maximumTravelDegrees
            ) *
            direction;

        float lift =
            Random.Range(
                minimumLift,
                maximumLift
            );

        lift *=
            1f +
            motionEnergy *
            motionLiftBoost;

        float curl =
            Random.Range(
                minimumCurl,
                maximumCurl
            );

        float curlDirection =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float raisedTubeRadius =
            tubeRadius +
            surfaceOffset;

        for (
            int i = 0;
            i < pointCount;
            i++
        )
        {
            float progress =
                i /
                (float)(pointCount - 1);

            float angle =
                startingAngle +
                travel *
                progress;

            Vector3 radial =
                new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)
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

            /*
             * The base follows the outer surface of the torus.
             */
            Vector3 surfacePoint =
                tubeCenter +
                radial *
                raisedTubeRadius;

            /*
             * Makes the middle rise like a tiny solar flare.
             * Both ends remain attached to the ring.
             */
            float arch =
                Mathf.Sin(
                    progress *
                    Mathf.PI
                );

            float secondaryWave =
                Mathf.Sin(
                    progress *
                    Mathf.PI *
                    2f
                );

            Vector3 point =
                surfacePoint;

            point +=
                radial *
                arch *
                lift;

            point +=
                tangent *
                secondaryWave *
                curl *
                curlDirection;

            points[i] =
                point;
        }

        return points;
    }

    private IEnumerator AnimateTongue(
        CoronaTongue tongue,
        Vector3[] points,
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

        tongue.Line.startColor =
            WithAlpha(
                baseColor,
                0.95f
            );

        tongue.Line.endColor =
            WithAlpha(
                baseColor,
                0f
            );

        float elapsed = 0f;

        while (
            elapsed < growTime &&
            tongue.Line != null
        )
        {
            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    growTime
                );

            ApplyLivingWidth(
                tongue
            );

            SetVisiblePath(
                tongue.Line,
                points,
                progress
            );

            yield return null;
        }

        if (tongue.Line == null)
        {
            yield break;
        }

        tongue.Line.positionCount =
            points.Length;

        tongue.Line.SetPositions(
            points
        );

        elapsed = 0f;

        while (
            elapsed < holdTime &&
            tongue.Line != null
        )
        {
            elapsed +=
                Time.deltaTime;

            ApplyLivingWidth(
                tongue
            );

            yield return null;
        }

        elapsed = 0f;

        while (
            elapsed < retractTime &&
            tongue.Line != null
        )
        {
            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    retractTime
                );

            ApplyLivingWidth(
                tongue
            );

            SetVisiblePath(
                tongue.Line,
                points,
                1f - progress
            );

            float alpha =
                1f -
                progress;

            tongue.Line.startColor =
                WithAlpha(
                    baseColor,
                    alpha
                );

            tongue.Line.endColor =
                WithAlpha(
                    baseColor,
                    0f
                );

            yield return null;
        }

        activeTongues.Remove(
            tongue
        );

        DestroyTongue(
            tongue
        );
    }

    private void ApplyLivingWidth(
        CoronaTongue tongue
    )
    {
        if (tongue.Line == null)
        {
            return;
        }

        float flicker =
            1f +
            Mathf.Sin(
                Time.time *
                tongue.FlickerSpeed +
                tongue.FlickerPhase
            ) *
            flickerAmount;

        float motionWidth =
            1f +
            motionEnergy *
            motionWidthBoost;

        if (selected)
        {
            motionWidth *= 0.9f;
        }

        if (opening)
        {
            motionWidth *= 1.25f;
        }

        tongue.Line.widthMultiplier =
            tongue.BaseWidth *
            flicker *
            motionWidth;
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

        progress =
            Mathf.Clamp01(
                progress
            );

        int visiblePoints =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    progress *
                    points.Length
                ),
                progress > 0f
                    ? 2
                    : 0,
                points.Length
            );

        line.positionCount =
            visiblePoints;

        for (
            int i = 0;
            i < visiblePoints;
            i++
        )
        {
            line.SetPosition(
                i,
                points[i]
            );
        }
    }

    private Color GetRandomColor()
    {
        if (
            coronaColors == null ||
            coronaColors.Length == 0
        )
        {
            return Color.cyan;
        }

        return coronaColors[
            Random.Range(
                0,
                coronaColors.Length
            )
        ];
    }

    private static Color WithAlpha(
        Color color,
        float alpha
    )
    {
        color.a =
            Mathf.Clamp01(
                alpha
            );

        return color;
    }

    private static void DestroyTongue(
        CoronaTongue tongue
    )
    {
        if (
            tongue != null &&
            tongue.Line != null
        )
        {
            Destroy(
                tongue.Line.gameObject
            );
        }
    }

    public void SetMotionEnergy(
        float value
    )
    {
        motionEnergy =
            Mathf.Clamp01(
                value
            );
    }

    public void SetSelected(
        bool value
    )
    {
        selected =
            value;
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

        maximumTravelDegrees =
            Mathf.Max(
                maximumTravelDegrees,
                minimumTravelDegrees
            );

        maximumLift =
            Mathf.Max(
                maximumLift,
                minimumLift
            );

        maximumCurl =
            Mathf.Max(
                maximumCurl,
                minimumCurl
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

        maximumFlickerSpeed =
            Mathf.Max(
                maximumFlickerSpeed,
                minimumFlickerSpeed
            );
    }
#endif
}