using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class LavaLampBlobs : MonoBehaviour, IPortalAccessory
{
    [Header("Required")]
    [SerializeField] private Material cellMaterial;

    [Header("Torus Dimensions")]
    [Min(0.01f)] [SerializeField] private float majorRadius = 1f;
    [Min(0.001f)] [SerializeField] private float tubeRadius = 0.10f;
    [Min(0f)] [SerializeField] private float surfaceOffset = 0.018f;

    [Header("Population")]
    [Range(1, 24)] [SerializeField] private int maximumActiveCells = 9;
    [Min(0.03f)] [SerializeField] private float minimumSpawnDelay = 0.16f;
    [Min(0.03f)] [SerializeField] private float maximumSpawnDelay = 0.42f;

    [Header("Cell Size")]
    [Min(0.005f)] [SerializeField] private float minimumSize = 0.035f;
    [Min(0.005f)] [SerializeField] private float maximumSize = 0.085f;
    [Range(0.2f, 2f)] [SerializeField] private float minimumFlattening = 0.45f;
    [Range(0.2f, 2f)] [SerializeField] private float maximumFlattening = 0.85f;
    [Range(0f, 0.8f)] [SerializeField] private float irregularity = 0.28f;

    [Header("Cell Motion")]
    [Range(0f, 90f)] [SerializeField] private float minimumRingTravelDegrees = 5f;
    [Range(0f, 180f)] [SerializeField] private float maximumRingTravelDegrees = 20f;
    [Range(0f, 120f)] [SerializeField] private float minimumTubeTravelDegrees = 8f;
    [Range(0f, 240f)] [SerializeField] private float maximumTubeTravelDegrees = 35f;
    [Range(0f, 0.5f)] [SerializeField] private float driftWobble = 0.08f;

    [Header("Animation")]
    [Min(0.05f)] [SerializeField] private float minimumGrowTime = 0.30f;
    [Min(0.05f)] [SerializeField] private float maximumGrowTime = 0.65f;
    [Min(0f)] [SerializeField] private float minimumHoldTime = 0.45f;
    [Min(0f)] [SerializeField] private float maximumHoldTime = 1.15f;
    [Min(0.05f)] [SerializeField] private float minimumFadeTime = 0.35f;
    [Min(0.05f)] [SerializeField] private float maximumFadeTime = 0.75f;
    [Range(0f, 0.5f)] [SerializeField] private float pulseAmount = 0.16f;
    [Min(0f)] [SerializeField] private float minimumPulseSpeed = 1.0f;
    [Min(0f)] [SerializeField] private float maximumPulseSpeed = 2.4f;

    [Header("Motion Reaction")]
    [Range(0f, 2f)] [SerializeField] private float motionSpawnBoost = 0.75f;
    [Range(0f, 2f)] [SerializeField] private float motionSizeBoost = 0.30f;
    [Range(0f, 2f)] [SerializeField] private float motionTravelBoost = 0.50f;

    [Header("Colors")]
    [SerializeField] private Color[] cellColors =
    {
        new Color(0.05f, 0.90f, 1.00f, 1f),
        new Color(0.18f, 0.42f, 1.00f, 1f),
        new Color(0.92f, 0.12f, 0.78f, 1f),
        new Color(1.00f, 0.32f, 0.46f, 1f),
        new Color(1.00f, 0.78f, 0.08f, 1f),
        new Color(0.35f, 1.00f, 0.20f, 1f)
    };

    private static readonly int CellColorId = Shader.PropertyToID("_CellColor");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int WobbleId = Shader.PropertyToID("_Wobble");

    private sealed class PlasmaCell
    {
        public GameObject Object;
        public MeshRenderer Renderer;
        public MaterialPropertyBlock Properties;
        public float StartMajorAngle;
        public float StartTubeAngle;
        public float MajorTravel;
        public float TubeTravel;
        public float BaseSize;
        public float Flattening;
        public float PulseSpeed;
        public float PulsePhase;
        public float Seed;
        public Color Color;
    }

    private readonly List<PlasmaCell> activeCells = new();
    private Mesh sharedCellMesh;
    private Coroutine spawnRoutine;
    private float motionEnergy;
    private bool selected;
    private bool opening;

    private void Awake()
    {
        sharedCellMesh = CreateLowPolySphereMesh(12, 8);
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

        for (int i = activeCells.Count - 1; i >= 0; i--)
        {
            DestroyCell(activeCells[i]);
        }

        activeCells.Clear();
    }

    private void OnDestroy()
    {
        if (sharedCellMesh != null)
        {
            Destroy(sharedCellMesh);
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            activeCells.RemoveAll(cell => cell == null || cell.Object == null);

            int allowedCells = maximumActiveCells + (opening ? 3 : 0);
            if (activeCells.Count < allowedCells)
            {
                CreateCell();
            }

            float delay = Random.Range(minimumSpawnDelay, maximumSpawnDelay);
            delay /= 1f + motionEnergy * motionSpawnBoost;
            yield return new WaitForSeconds(delay);
        }
    }

    private void CreateCell()
    {
        if (cellMaterial == null)
        {
            Debug.LogError("PlasmaRingCells needs a Cell Material assigned.", this);
            enabled = false;
            return;
        }

        GameObject cellObject = new GameObject("PlasmaRingCell");
        cellObject.transform.SetParent(transform, false);

        MeshFilter filter = cellObject.AddComponent<MeshFilter>();
        filter.sharedMesh = sharedCellMesh;

        MeshRenderer renderer = cellObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = cellMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        PlasmaCell cell = new PlasmaCell
        {
            Object = cellObject,
            Renderer = renderer,
            Properties = new MaterialPropertyBlock(),
            StartMajorAngle = Random.Range(0f, Mathf.PI * 2f),
            StartTubeAngle = Random.Range(0f, Mathf.PI * 2f),
            MajorTravel = Mathf.Deg2Rad * Random.Range(minimumRingTravelDegrees, maximumRingTravelDegrees) * RandomSign(),
            TubeTravel = Mathf.Deg2Rad * Random.Range(minimumTubeTravelDegrees, maximumTubeTravelDegrees) * RandomSign(),
            BaseSize = Random.Range(minimumSize, maximumSize),
            Flattening = Random.Range(minimumFlattening, maximumFlattening),
            PulseSpeed = Random.Range(minimumPulseSpeed, maximumPulseSpeed),
            PulsePhase = Random.Range(0f, Mathf.PI * 2f),
            Seed = Random.Range(0f, 1000f),
            Color = GetRandomColor()
        };

        activeCells.Add(cell);
        StartCoroutine(AnimateCell(cell));
    }

    private IEnumerator AnimateCell(PlasmaCell cell)
    {
        float growTime = Random.Range(minimumGrowTime, maximumGrowTime);
        float holdTime = Random.Range(minimumHoldTime, maximumHoldTime);
        float fadeTime = Random.Range(minimumFadeTime, maximumFadeTime);

        float elapsed = 0f;
        while (elapsed < growTime && cell.Object != null)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / growTime);
            float eased = SmoothStep01(p);
            UpdateCell(cell, eased, eased, eased);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < holdTime && cell.Object != null)
        {
            elapsed += Time.deltaTime;
            float p = holdTime <= 0f ? 1f : Mathf.Clamp01(elapsed / holdTime);
            UpdateCell(cell, p, 1f, 1f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeTime && cell.Object != null)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / fadeTime);
            UpdateCell(cell, 1f, 1f - p, 1f - p);
            yield return null;
        }

        activeCells.Remove(cell);
        DestroyCell(cell);
    }

    private void UpdateCell(PlasmaCell cell, float pathProgress, float scaleAlpha, float opacity)
    {
        if (cell.Object == null)
        {
            return;
        }

        float travelMultiplier = 1f + motionEnergy * motionTravelBoost;
        float majorAngle = cell.StartMajorAngle + cell.MajorTravel * pathProgress * travelMultiplier;
        float tubeAngle = cell.StartTubeAngle + cell.TubeTravel * pathProgress * travelMultiplier;
        tubeAngle += Mathf.Sin(Time.time * 1.7f + cell.PulsePhase) * driftWobble;

        Vector3 radial = new Vector3(Mathf.Cos(majorAngle), 0f, Mathf.Sin(majorAngle));
        Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
        Vector3 tubeOut = radial * Mathf.Cos(tubeAngle) + Vector3.up * Mathf.Sin(tubeAngle);

        cell.Object.transform.localPosition =
            radial * majorRadius + tubeOut * (tubeRadius + surfaceOffset);

        cell.Object.transform.localRotation =
            Quaternion.LookRotation(tubeOut, tangent);

        float pulse = 1f + Mathf.Sin(Time.time * cell.PulseSpeed * Mathf.PI * 2f + cell.PulsePhase) * pulseAmount;
        float selectedMultiplier = selected ? 0.92f : 1f;
        float openingMultiplier = opening ? 1.20f : 1f;
        float motionMultiplier = 1f + motionEnergy * motionSizeBoost;

        float size = cell.BaseSize * scaleAlpha * pulse * selectedMultiplier * openingMultiplier * motionMultiplier;
        float irregularX = 1f + Mathf.Sin(cell.Seed * 1.37f) * irregularity;
        float irregularY = 1f + Mathf.Sin(cell.Seed * 2.11f) * irregularity;

        cell.Object.transform.localScale =
            new Vector3(size * irregularX, size * irregularY, size * cell.Flattening);

        cell.Renderer.GetPropertyBlock(cell.Properties);
        cell.Properties.SetColor(CellColorId, cell.Color);
        cell.Properties.SetFloat(OpacityId, Mathf.Clamp01(opacity));
        cell.Properties.SetFloat(SeedId, cell.Seed);
        cell.Properties.SetFloat(WobbleId, irregularity);
        cell.Renderer.SetPropertyBlock(cell.Properties);
    }

    private static Mesh CreateLowPolySphereMesh(int longitudeSegments, int latitudeSegments)
    {
        Mesh mesh = new Mesh { name = "PlasmaCellMesh" };
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int latitude = 0; latitude <= latitudeSegments; latitude++)
        {
            float v = latitude / (float)latitudeSegments;
            float phi = v * Mathf.PI;
            float sinPhi = Mathf.Sin(phi);
            float cosPhi = Mathf.Cos(phi);

            for (int longitude = 0; longitude <= longitudeSegments; longitude++)
            {
                float u = longitude / (float)longitudeSegments;
                float theta = u * Mathf.PI * 2f;
                Vector3 normal = new Vector3(Mathf.Cos(theta) * sinPhi, cosPhi, Mathf.Sin(theta) * sinPhi);
                vertices.Add(normal * 0.5f);
                normals.Add(normal);
                uvs.Add(new Vector2(u, v));
            }
        }

        int rowLength = longitudeSegments + 1;
        for (int latitude = 0; latitude < latitudeSegments; latitude++)
        {
            for (int longitude = 0; longitude < longitudeSegments; longitude++)
            {
                int current = latitude * rowLength + longitude;
                int next = current + rowLength;
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);
                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private Color GetRandomColor()
    {
        if (cellColors == null || cellColors.Length == 0)
        {
            return Color.cyan;
        }

        return cellColors[Random.Range(0, cellColors.Length)];
    }

    private static float RandomSign()
    {
        return Random.value < 0.5f ? -1f : 1f;
    }

    private static float SmoothStep01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static void DestroyCell(PlasmaCell cell)
    {
        if (cell != null && cell.Object != null)
        {
            Destroy(cell.Object);
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
        maximumSize = Mathf.Max(maximumSize, minimumSize);
        maximumFlattening = Mathf.Max(maximumFlattening, minimumFlattening);
        maximumRingTravelDegrees = Mathf.Max(maximumRingTravelDegrees, minimumRingTravelDegrees);
        maximumTubeTravelDegrees = Mathf.Max(maximumTubeTravelDegrees, minimumTubeTravelDegrees);
        maximumGrowTime = Mathf.Max(maximumGrowTime, minimumGrowTime);
        maximumHoldTime = Mathf.Max(maximumHoldTime, minimumHoldTime);
        maximumFadeTime = Mathf.Max(maximumFadeTime, minimumFadeTime);
        maximumPulseSpeed = Mathf.Max(maximumPulseSpeed, minimumPulseSpeed);
    }
#endif
}
