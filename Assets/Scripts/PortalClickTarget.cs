using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class PortalClickTarget : MonoBehaviour
{
    [Header("Portal")]
    [SerializeField] private PortalController portalController;

    [Header("Click Filtering")]
    [Min(0f)]
    [SerializeField] private float maximumClickMovementPixels = 14f;

    [Min(0f)]
    [SerializeField] private float maximumClickDuration = 0.45f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Vector2 pointerDownPosition;
    private float pointerDownTime;
    private bool pointerDown;

    private void Awake()
    {
        if (portalController == null)
        {
            portalController =
                GetComponentInParent<PortalController>();
        }

        Log(
            $"AWAKE | Collider={GetComponent<Collider>() != null} | " +
            $"PortalController={(portalController != null ? portalController.name : "NULL")} | " +
            $"Active={isActiveAndEnabled}"
        );
    }

    private void OnEnable()
    {
        Log("ON ENABLE");
    }

    private void OnMouseEnter()
    {
        Log("MOUSE ENTER");
    }

    private void OnMouseDown()
    {
        Log("MOUSE DOWN RECEIVED");

        if (portalController == null)
        {
            LogError("MOUSE DOWN ABORTED | PortalController is NULL");
            return;
        }

        if (portalController.IsOpening)
        {
            Log("MOUSE DOWN ABORTED | Portal is already opening");
            return;
        }

        pointerDown = true;
        pointerDownPosition = Input.mousePosition;
        pointerDownTime = Time.unscaledTime;

        Log(
            $"MOUSE DOWN ACCEPTED | Position={pointerDownPosition}"
        );
    }

    private void OnMouseUp()
    {
        Log("MOUSE UP RECEIVED");

        if (!pointerDown)
        {
            Log("MOUSE UP ABORTED | No matching pointer down");
            return;
        }

        pointerDown = false;

        if (portalController == null)
        {
            LogError("MOUSE UP ABORTED | PortalController is NULL");
            return;
        }

        if (portalController.IsOpening)
        {
            Log("MOUSE UP ABORTED | Portal is already opening");
            return;
        }

        float movement =
            Vector2.Distance(
                pointerDownPosition,
                Input.mousePosition
            );

        float duration =
            Time.unscaledTime -
            pointerDownTime;

        Log(
            $"CLICK FILTER | Movement={movement:F2}/{maximumClickMovementPixels:F2} | " +
            $"Duration={duration:F3}/{maximumClickDuration:F3}"
        );

        if (movement > maximumClickMovementPixels)
        {
            Log("CLICK REJECTED | Movement exceeded tolerance");
            return;
        }

        if (duration > maximumClickDuration)
        {
            Log("CLICK REJECTED | Duration exceeded tolerance");
            return;
        }

        Log("CLICK ACCEPTED | Calling PortalController.OpenPortal()");
        portalController.OpenPortal();
    }

    private void OnMouseExit()
    {
        Log("MOUSE EXIT");
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[PORTAL CLICK DEBUG] {name} | {message}",
                this
            );
        }
    }

    private void LogError(string message)
    {
        Debug.LogError(
            $"[PORTAL CLICK DEBUG] {name} | {message}",
            this
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (portalController == null)
        {
            portalController =
                GetComponentInParent<PortalController>();
        }
    }
#endif
}
