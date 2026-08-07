using UnityEngine;

[RequireComponent(typeof(GalaxyObject))]
public class PortalController : MonoBehaviour
{
    [Header("Portal Identity")]
    [Tooltip("Name shown to the player.")]
    [SerializeField] private string portalName;

    [Tooltip("Must exactly match the ModeTarget ID in the Modes scene.")]
    [SerializeField] private string modeId;

    [Header("Portal Parts")]
    [SerializeField] private Renderer portalSurface;
    [SerializeField] private Renderer portalRing;

    [Header("Transition")]
    [SerializeField] private DimensionTransitionController transitionController;
    [SerializeField] private GalaxyRotationController galaxyRotationController;
    [SerializeField] private string destinationScene = "Modes";

    [Header("Idle Motion")]
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
            rotationTarget =
                portalRing != null
                    ? portalRing.transform
                    : transform;
        }

        ResolveSceneReferences();
    }

    private void Update()
    {
        UpdateIdleRotation();
        UpdateSelectionScale();
    }

    private void UpdateIdleRotation()
    {
        if (
            isOpening ||
            idleRotationSpeed <= 0f ||
            rotationTarget == null
        )
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
        if (isOpening)
        {
            return;
        }

        Vector3 targetScale =
            isSelected
                ? baseScale * selectedScaleMultiplier
                : baseScale;

        float interpolation =
            1f -
            Mathf.Exp(
                -scaleAnimationSpeed *
                Time.deltaTime
            );

        transform.localScale =
            Vector3.Lerp(
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

        ResolveSceneReferences();

        if (transitionController == null)
        {
            Debug.LogError(
                $"Portal '{name}' could not find a DimensionTransitionController.",
                this
            );

            return;
        }

        isOpening = true;
        isSelected = true;

        if (galaxyRotationController != null)
        {
            galaxyRotationController.enabled = false;
        }

        NotifyAccessoriesOpening();

        ModeManager.SetSelectedMode(modeId);

        transitionController.BeginTransition(
            destinationScene,
            transform
        );
    }

    private void ResolveSceneReferences()
    {
        if (transitionController == null)
        {
            transitionController =
                FindFirstObjectByType<DimensionTransitionController>();
        }

        if (galaxyRotationController == null)
        {
            galaxyRotationController =
                GetComponentInParent<GalaxyRotationController>();
        }
    }

    private void NotifyAccessoriesOpening()
    {
        MonoBehaviour[] behaviours =
            GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IPortalAccessory accessory)
            {
                accessory.OnPortalOpening();
            }
        }
    }

    public void FinishOpening()
    {
        isOpening = false;

        if (galaxyRotationController != null)
        {
            galaxyRotationController.enabled = true;
        }
    }

    public void ResetToBaseScale()
    {
        isSelected = false;
        isOpening = false;
        transform.localScale = baseScale;

        if (galaxyRotationController != null)
        {
            galaxyRotationController.enabled = true;
        }
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
