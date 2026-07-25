using UnityEngine;

[RequireComponent(typeof(GalaxyObject))]
public class PortalController : MonoBehaviour
{
    [Header("Portal Identity")]
    [Tooltip("Name shown to the player.")]
    [SerializeField] private string portalName;

    [Tooltip("The mode this portal opens inside the shared Modes scene.")]
    [SerializeField] private string modeId;

    [Header("Portal Parts")]
    [Tooltip("Renderer using the live visualization material.")]
    [SerializeField] private Renderer portalSurface;

    [Tooltip("Renderer using the portal ring material.")]
    [SerializeField] private Renderer portalRing;

    [Header("Idle Motion")]
    [Tooltip("Optional child object that rotates. Usually the ring.")]
    [SerializeField] private Transform rotationTarget;

    [SerializeField] private Vector3 idleRotationAxis = Vector3.forward;

    [Min(0f)]
    [SerializeField] private float idleRotationSpeed = 4f;

    [Header("Selection")]
    [Min(1f)]
    [SerializeField] private float selectedScaleMultiplier = 1.08f;

    [Min(0.01f)]
    [SerializeField] private float scaleAnimationSpeed = 6f;

    private GalaxyObject galaxyObject;
    private Vector3 baseScale;
    private bool isSelected;
    private bool isOpening;

    public string PortalName => portalName;
    public string ModeId => modeId;
    public Renderer PortalSurface => portalSurface;
    public Renderer PortalRing => portalRing;
    public bool IsSelected => isSelected;
    public bool IsOpening => isOpening;
    public GalaxyObject GalaxyObject => galaxyObject;

    private void Awake()
    {
        galaxyObject = GetComponent<GalaxyObject>();
        baseScale = transform.localScale;

        if (rotationTarget == null)
        {
            rotationTarget = portalRing != null
                ? portalRing.transform
                : transform;
        }
    }

    private void Update()
    {
        UpdateIdleRotation();
        UpdateSelectionScale();
    }

    private void UpdateIdleRotation()
    {
        if (isOpening || idleRotationSpeed <= 0f)
        {
            return;
        }

        rotationTarget.Rotate(
            idleRotationAxis.normalized,
            idleRotationSpeed * Time.deltaTime,
            Space.Self
        );
    }

    private void UpdateSelectionScale()
    {
        Vector3 targetScale =
            isSelected
                ? baseScale * selectedScaleMultiplier
                : baseScale;

        float interpolation =
            1f -
            Mathf.Exp(
                -scaleAnimationSpeed * Time.deltaTime
            );

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            interpolation
        );
    }

    public void SetSelected(bool selected)
    {
        if (isOpening)
        {
            return;
        }

        isSelected = selected;
    }

    public void OpenPortal()
    {
        if (isOpening)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(modeId))
        {
            Debug.LogError(
                $"Portal '{name}' has no Mode ID assigned.",
                this
            );

            return;
        }

        isOpening = true;

        /*
         * We will connect this to your existing Modes-scene manager
         * once we use the actual class and method names from your project.
         */
        Debug.Log(
            $"Open shared Modes scene using mode '{modeId}'.",
            this
        );
    }

    public void FinishOpening()
    {
        isOpening = false;
    }

    public void ResetToBaseScale()
    {
        isSelected = false;
        isOpening = false;
        transform.localScale = baseScale;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(portalName))
        {
            portalName = gameObject.name;
        }

        if (
            rotationTarget == null &&
            portalRing != null
        )
        {
            rotationTarget = portalRing.transform;
        }
    }
#endif
}