using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class PortalPointerRaycaster : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Leave as Everything unless you create a dedicated Portal layer.")]
    [SerializeField] private LayerMask portalLayers = ~0;

    [Min(0.1f)]
    [SerializeField] private float maximumRayDistance = 1000f;

    [Header("Click Filtering")]
    [Min(0f)]
    [SerializeField] private float maximumClickMovementPixels = 14f;

    [Min(0f)]
    [SerializeField] private float maximumClickDuration = 0.45f;

    [SerializeField] private bool ignoreInputOverUI = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Vector2 pointerDownPosition;
    private float pointerDownTime;
    private bool pointerHeld;
    private bool pointerStartedOverUI;

    private readonly RaycastHit[] hits = new RaycastHit[64];

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        Log(
            $"AWAKE | Camera={(targetCamera != null ? targetCamera.name : "NULL")} | " +
            $"LayerMask={portalLayers.value}"
        );
    }

    private void Update()
    {
        ReadPointer(
            out bool down,
            out bool held,
            out bool up,
            out Vector2 position
        );

        if (down)
        {
            pointerHeld = true;
            pointerDownPosition = position;
            pointerDownTime = Time.unscaledTime;

            pointerStartedOverUI =
                ignoreInputOverUI &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(
                    Input.touchCount > 0
                        ? Input.GetTouch(0).fingerId
                        : -1
                );

            Log(
                $"POINTER DOWN | Position={position} | OverUI={pointerStartedOverUI}"
            );
        }

        if (up && pointerHeld)
        {
            pointerHeld = false;

            float movement =
                Vector2.Distance(
                    pointerDownPosition,
                    position
                );

            float duration =
                Time.unscaledTime -
                pointerDownTime;

            Log(
                $"POINTER UP | Movement={movement:F2}/{maximumClickMovementPixels:F2} | " +
                $"Duration={duration:F3}/{maximumClickDuration:F3}"
            );

            if (pointerStartedOverUI)
            {
                Log("CLICK REJECTED | Started over UI");
                return;
            }

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

            TryOpenPortal(position);
        }
    }

    private void TryOpenPortal(Vector2 screenPosition)
    {
        if (targetCamera == null)
        {
            LogError("Cannot raycast because Target Camera is NULL");
            return;
        }

        Ray ray =
            targetCamera.ScreenPointToRay(
                screenPosition
            );

        int hitCount =
            Physics.RaycastNonAlloc(
                ray,
                hits,
                maximumRayDistance,
                portalLayers,
                QueryTriggerInteraction.Collide
            );

        Log($"RAYCAST | HitCount={hitCount}");

        if (hitCount <= 0)
        {
            Log("RAYCAST MISS | No colliders hit");
            return;
        }

        PortalController closestPortal = null;
        float closestDistance = float.PositiveInfinity;
        string hitSummary = string.Empty;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
            {
                continue;
            }

            PortalController portal =
                hit.collider.GetComponentInParent<PortalController>();

            hitSummary +=
                $"[{hit.collider.name}, distance={hit.distance:F2}, " +
                $"portal={(portal != null ? portal.name : "none")}] ";

            if (
                portal != null &&
                hit.distance < closestDistance
            )
            {
                closestPortal = portal;
                closestDistance = hit.distance;
            }
        }

        Log($"RAYCAST HITS | {hitSummary}");

        if (closestPortal == null)
        {
            Log(
                "NO PORTAL FOUND | Ray hit colliders, but none had a PortalController in their parent chain"
            );
            return;
        }

        Log(
            $"PORTAL FOUND | '{closestPortal.name}' at distance {closestDistance:F2} | Calling OpenPortal()"
        );

        closestPortal.OpenPortal();
    }

    private static void ReadPointer(
        out bool down,
        out bool held,
        out bool up,
        out Vector2 position
    )
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            position = touch.position;
            down = touch.phase == TouchPhase.Began;

            held =
                touch.phase == TouchPhase.Began ||
                touch.phase == TouchPhase.Moved ||
                touch.phase == TouchPhase.Stationary;

            up =
                touch.phase == TouchPhase.Ended ||
                touch.phase == TouchPhase.Canceled;

            return;
        }

        position = Input.mousePosition;
        down = Input.GetMouseButtonDown(0);
        held = Input.GetMouseButton(0);
        up = Input.GetMouseButtonUp(0);
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[PORTAL RAYCAST DEBUG] {name} | {message}",
                this
            );
        }
    }

    private void LogError(string message)
    {
        Debug.LogError(
            $"[PORTAL RAYCAST DEBUG] {name} | {message}",
            this
        );
    }
}