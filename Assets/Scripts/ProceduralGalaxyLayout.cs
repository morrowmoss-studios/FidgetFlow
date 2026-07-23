using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProceduralGalaxyLayout : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private int galaxySeed = 13742;

    [Tooltip("Generate the layout automatically when the scene starts.")]
    [SerializeField] private bool generateOnStart = true;

    [Header("Galaxy Size")]
    [Tooltip("Minimum outer radius when only a few objects exist.")]
    [Min(1f)]
    [SerializeField] private float baseRadius = 7f;

    [Tooltip("How quickly the galaxy expands as more objects are added.")]
    [Min(0f)]
    [SerializeField] private float radiusGrowth = 1.25f;

    [Tooltip("Keeps the center clear around the player/camera.")]
    [Min(0f)]
    [SerializeField] private float centerClearRadius = 2.5f;

    [Header("Galaxy Shape")]
    [Tooltip("Maximum height above or below the galaxy's middle plane.")]
    [Min(0f)]
    [SerializeField] private float verticalThickness = 2.25f;

    [Range(0f, 1f)]
    [Tooltip("0 creates a flat disc. 1 uses the full vertical thickness.")]
    [SerializeField] private float thicknessAtOuterEdge = 0.65f;

    [Header("Spacing")]
    [Tooltip("Preferred minimum distance between objects.")]
    [Min(0.1f)]
    [SerializeField] private float preferredSpacing = 3.25f;

    [Tooltip("The generator may reduce spacing to this value if the galaxy becomes crowded.")]
    [Min(0.1f)]
    [SerializeField] private float minimumAllowedSpacing = 1.75f;

    [Tooltip("Attempts made before the galaxy expands and retries.")]
    [Min(10)]
    [SerializeField] private int attemptsPerObject = 200;

    [Tooltip("How many complete retries are allowed.")]
    [Min(1)]
    [SerializeField] private int layoutRetries = 8;

    [Tooltip("Amount the galaxy expands after a failed generation attempt.")]
    [Min(1.01f)]
    [SerializeField] private float retryExpansion = 1.12f;

    [Header("Spiral Structure")]
    [Tooltip("Adds subtle galactic-arm structure without creating obvious rings.")]
    [SerializeField] private bool useSpiralInfluence = true;

    [Range(2, 6)]
    [SerializeField] private int spiralArmCount = 3;

    [Tooltip("How much the arms twist from the center toward the edge.")]
    [SerializeField] private float spiralTwist = 3.25f;

    [Range(0f, 1f)]
    [Tooltip("0 is a random disc. 1 tightly follows spiral arms.")]
    [SerializeField] private float spiralStrength = 0.35f;

    [Tooltip("Random angular variation around each spiral arm.")]
    [Range(0f, 180f)]
    [SerializeField] private float armScatterDegrees = 45f;

    [Header("Editor")]
    [SerializeField] private bool showGizmos = true;

    private readonly List<PlacedGalaxyObject> placedObjects = new();

    private struct PlacedGalaxyObject
    {
        public GalaxyObject GalaxyObject;
        public Vector3 Position;

        public PlacedGalaxyObject(GalaxyObject galaxyObject, Vector3 position)
        {
            GalaxyObject = galaxyObject;
            Position = position;
        }
    }

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateLayout();
        }
    }

    [ContextMenu("Generate Galaxy Layout")]
    public void GenerateLayout()
    {
        GalaxyObject[] galaxyObjects = GetComponentsInChildren<GalaxyObject>(true)
            .Where(item => !item.LockPosition)
            .OrderBy(item => item.StableId, StringComparer.Ordinal)
            .ToArray();

        if (galaxyObjects.Length == 0)
        {
            Debug.LogWarning("No unlocked GalaxyObject components were found.", this);
            return;
        }

        float calculatedRadius = CalculateInitialRadius(galaxyObjects.Length);

        for (int retry = 0; retry < layoutRetries; retry++)
        {
            float currentRadius = calculatedRadius * Mathf.Pow(retryExpansion, retry);

            if (TryGenerateLayout(galaxyObjects, currentRadius))
            {
                ApplyGeneratedPositions();

                Debug.Log(
                    $"Generated galaxy with {galaxyObjects.Length} objects. " +
                    $"Radius: {currentRadius:F2}. Retry: {retry}.",
                    this
                );

                return;
            }
        }

        Debug.LogError(
            $"Galaxy generation failed after {layoutRetries} retries. " +
            "Increase the radius, reduce preferred spacing, or increase attempts per object.",
            this
        );
    }

    private float CalculateInitialRadius(int objectCount)
    {
        /*
         * Square-root growth gives additional objects more area
         * without making a six-object galaxy absurdly enormous.
         */
        float countGrowth = Mathf.Sqrt(Mathf.Max(0, objectCount - 1));
        return baseRadius + countGrowth * radiusGrowth;
    }

    private bool TryGenerateLayout(
        IReadOnlyList<GalaxyObject> galaxyObjects,
        float galaxyRadius
    )
    {
        placedObjects.Clear();

        System.Random random = new System.Random(galaxySeed);

        foreach (GalaxyObject galaxyObject in galaxyObjects)
        {
            bool foundPosition = false;

            for (int attempt = 0; attempt < attemptsPerObject; attempt++)
            {
                Vector3 candidate = GenerateCandidatePosition(random, galaxyRadius);

                if (IsPositionValid(candidate, galaxyObject, galaxyRadius))
                {
                    placedObjects.Add(
                        new PlacedGalaxyObject(galaxyObject, candidate)
                    );

                    foundPosition = true;
                    break;
                }
            }

            if (!foundPosition)
            {
                placedObjects.Clear();
                return false;
            }
        }

        return true;
    }

    private Vector3 GenerateCandidatePosition(
        System.Random random,
        float galaxyRadius
    )
    {
        float random01 = NextFloat(random);

        /*
         * sqrt(random) distributes points evenly by area.
         * Using plain random would crowd too many objects near the center.
         */
        float radialFraction = Mathf.Sqrt(random01);

        float radius = Mathf.Lerp(
            centerClearRadius,
            galaxyRadius,
            radialFraction
        );

        float angle;

        if (useSpiralInfluence)
        {
            int armIndex = random.Next(0, spiralArmCount);

            float armBaseAngle =
                armIndex * (Mathf.PI * 2f / spiralArmCount);

            float spiralAngle =
                armBaseAngle +
                radialFraction * spiralTwist;

            float completelyRandomAngle =
                NextFloat(random) * Mathf.PI * 2f;

            float armScatterRadians = Mathf.Lerp(
                -armScatterDegrees,
                armScatterDegrees,
                NextFloat(random)
            ) * Mathf.Deg2Rad;

            /*
             * Blend between a fully random disc angle and a spiral-arm angle.
             */
            angle = Mathf.LerpAngle(
                completelyRandomAngle * Mathf.Rad2Deg,
                (spiralAngle + armScatterRadians) * Mathf.Rad2Deg,
                spiralStrength
            ) * Mathf.Deg2Rad;
        }
        else
        {
            angle = NextFloat(random) * Mathf.PI * 2f;
        }

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        /*
         * The disc gets slightly thicker toward the outside,
         * like a loose 3D galaxy rather than a flat menu.
         */
        float edgeThickness = Mathf.Lerp(
            1f - thicknessAtOuterEdge,
            1f,
            radialFraction
        );

        float verticalRange = verticalThickness * edgeThickness;

        /*
         * Averaging random values biases most objects toward the middle plane
         * while still allowing occasional higher and lower objects.
         */
        float verticalRandom =
            (NextFloat(random) + NextFloat(random) + NextFloat(random)) / 3f;

        float y = Mathf.Lerp(
            -verticalRange,
            verticalRange,
            verticalRandom
        );

        return new Vector3(x, y, z);
    }

    private bool IsPositionValid(
        Vector3 candidate,
        GalaxyObject candidateObject,
        float galaxyRadius
    )
    {
        Vector2 flatPosition = new Vector2(candidate.x, candidate.z);

        if (flatPosition.magnitude < centerClearRadius)
        {
            return false;
        }

        if (flatPosition.magnitude > galaxyRadius)
        {
            return false;
        }

        foreach (PlacedGalaxyObject placed in placedObjects)
        {
            float combinedMultiplier =
                (candidateObject.SpacingMultiplier +
                 placed.GalaxyObject.SpacingMultiplier) * 0.5f;

            float requiredSpacing = Mathf.Max(
                minimumAllowedSpacing,
                preferredSpacing * combinedMultiplier
            );

            float distance = Vector3.Distance(
                candidate,
                placed.Position
            );

            if (distance < requiredSpacing)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyGeneratedPositions()
    {
        foreach (PlacedGalaxyObject placed in placedObjects)
        {
            placed.GalaxyObject.transform.localPosition = placed.Position;
        }
    }

    private static float NextFloat(System.Random random)
    {
        return (float)random.NextDouble();
    }

    [ContextMenu("Clear Generated Positions")]
    public void ClearGeneratedPositions()
    {
        foreach (GalaxyObject galaxyObject in
                 GetComponentsInChildren<GalaxyObject>(true))
        {
            if (!galaxyObject.LockPosition)
            {
                galaxyObject.transform.localPosition = Vector3.zero;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
        {
            return;
        }

        GalaxyObject[] galaxyObjects =
            GetComponentsInChildren<GalaxyObject>(true);

        float radius = CalculateInitialRadius(
            Mathf.Max(1, galaxyObjects.Length)
        );

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireSphere(
            Vector3.zero,
            centerClearRadius
        );

        const int segments = 96;

        Vector3 previousPoint = new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 nextPoint = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        foreach (GalaxyObject galaxyObject in galaxyObjects)
        {
            Gizmos.DrawWireSphere(
                galaxyObject.transform.localPosition,
                0.3f * galaxyObject.SpacingMultiplier
            );
        }
    }
}