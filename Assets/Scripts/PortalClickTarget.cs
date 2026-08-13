using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class PortalClickTarget : MonoBehaviour
{
    [Header("Portal")]
    [SerializeField] private PortalController portalController;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    public PortalController PortalController => portalController;

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

    public bool TryOpenPortal()
    {
        if (portalController == null)
        {
            LogError("OPEN ABORTED | PortalController is NULL");
            return false;
        }

        if (portalController.IsOpening)
        {
            Log("OPEN ABORTED | Portal is already opening");
            return false;
        }

        Log("OPEN ACCEPTED | Calling PortalController.OpenPortal()");

        portalController.OpenPortal();

        return true;
    }

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log(
            $"[PORTAL CLICK DEBUG] {name} | {message}",
            this
        );
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