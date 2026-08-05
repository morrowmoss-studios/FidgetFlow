using UnityEngine;

[RequireComponent(typeof(GalaxyObject))]
public class PortalController : MonoBehaviour
{
    [Header("Portal Identity")]
    [SerializeField] private string portalName;
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

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

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

        Log(
            $"AWAKE | ModeId='{modeId}' | Destination='{destinationScene}' | " +
            $"Transition={(transitionController != null ? transitionController.name : "NULL")} | " +
            $"GalaxyRotation={(galaxyRotationController != null ? galaxyRotationController.name : "NULL")}"
        );
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
        Log($"SetSelected({selected})");

        if (isOpening)
        {
            Log("SetSelected ignored | Portal is opening");
            return;
        }

        isSelected = selected;
    }

    public void OpenPortal()
    {
        Log("OpenPortal() ENTERED");

        if (isOpening)
        {
            Log("OpenPortal ABORTED | isOpening already true");
            return;
        }

        if (string.IsNullOrWhiteSpace(modeId))
        {
            LogError("OpenPortal ABORTED | Mode ID is empty");
            return;
        }

        ResolveSceneReferences();

        Log(
            $"REFERENCES | Transition={(transitionController != null ? transitionController.name : "NULL")} | " +
            $"GalaxyRotation={(galaxyRotationController != null ? galaxyRotationController.name : "NULL")}"
        );

        if (transitionController == null)
        {
            LogError(
                "OpenPortal ABORTED | Could not find DimensionTransitionController"
            );
            return;
        }

        isOpening = true;
        isSelected = true;
        Log("Portal marked as opening and selected");

        if (galaxyRotationController != null)
        {
            galaxyRotationController.enabled = false;
            Log("GalaxyRotationController disabled");
        }
        else
        {
            Log("GalaxyRotationController was NULL | Continuing anyway");
        }

        NotifyAccessoriesOpening();

        Log($"Saving selected mode '{modeId}'");
        ModeManager.SetSelectedMode(modeId);

        string storedMode =
            PlayerPrefs.GetString(
                "FidgetFlow.SelectedMode",
                "<NOT STORED>"
            );

        Log($"PlayerPrefs verification | StoredMode='{storedMode}'");

        Log(
            $"Calling BeginTransition('{destinationScene}', '{transform.name}')"
        );

        transitionController.BeginTransition(
            destinationScene,
            transform
        );

        Log("OpenPortal() FINISHED CALLING TRANSITION");
    }

    private void ResolveSceneReferences()
    {
        Log("ResolveSceneReferences()");

        if (transitionController == null)
        {
            transitionController =
                FindFirstObjectByType<DimensionTransitionController>();

            Log(
                $"Auto-found transition: " +
                $"{(transitionController != null ? transitionController.name : "NULL")}"
            );
        }

        if (galaxyRotationController == null)
        {
            galaxyRotationController =
                GetComponentInParent<GalaxyRotationController>();

            Log(
                $"Auto-found galaxy rotation: " +
                $"{(galaxyRotationController != null ? galaxyRotationController.name : "NULL")}"
            );
        }
    }

    private void NotifyAccessoriesOpening()
    {
        MonoBehaviour[] behaviours =
            GetComponentsInChildren<MonoBehaviour>(true);

        int notifiedCount = 0;

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IPortalAccessory accessory)
            {
                Log(
                    $"Notifying accessory: {behaviour.GetType().Name}"
                );

                accessory.OnPortalOpening();
                notifiedCount++;
            }
        }

        Log($"Accessory notification complete | Count={notifiedCount}");
    }

    public void FinishOpening()
    {
        Log("FinishOpening()");

        isOpening = false;

        if (galaxyRotationController != null)
        {
            galaxyRotationController.enabled = true;
        }
    }

    public void ResetToBaseScale()
    {
        Log("ResetToBaseScale()");

        isSelected = false;
        isOpening = false;
        transform.localScale = baseScale;

        if (galaxyRotationController != null)
        {
            galaxyRotationController.enabled = true;
        }
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[PORTAL CONTROLLER DEBUG] {name} | {message}",
                this
            );
        }
    }

    private void LogError(string message)
    {
        Debug.LogError(
            $"[PORTAL CONTROLLER DEBUG] {name} | {message}",
            this
        );
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
