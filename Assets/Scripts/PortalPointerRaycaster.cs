using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class PortalPointerRaycaster : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Set this to only the PortalInteractable layer.")]
    [SerializeField] private LayerMask portalLayer;

    [Min(0.1f)]
    [SerializeField] private float maximumRayDistance = 1000f;

    [Header("Click Filtering")]
    [Min(0f)]
    [SerializeField] private float maximumClickMovementPixels = 14f;

    [Min(0f)]
    [SerializeField] private float maximumClickDuration = 0.45f;

    [SerializeField] private bool ignoreInputOverUI = true;

    private Vector2 pointerDownPosition;
    private float pointerDownTime;
    private bool pointerHeld;
    private bool pointerStartedOverUI;

    private readonly RaycastHit[] hits = new RaycastHit[32];

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
    }

    private void Update()
    {
        ReadPointer(
            out bool down,
            out bool up,
            out Vector2 position,
            out int pointerId
        );

        if (down)
        {
            pointerHeld = true;
            pointerDownPosition = position;
            pointerDownTime = Time.unscaledTime;

            pointerStartedOverUI =
                ignoreInputOverUI &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        if (!up || !pointerHeld)
        {
            return;
        }

        pointerHeld = false;

        float movement =
            Vector2.Distance(
                pointerDownPosition,
                position
            );

        float duration =
            Time.unscaledTime -
            pointerDownTime;

        if (
            pointerStartedOverUI ||
            movement > maximumClickMovementPixels ||
            duration > maximumClickDuration
        )
        {
            return;
        }

        OpenPortalAt(position);
    }

    private void OpenPortalAt(Vector2 screenPosition)
    {
        if (targetCamera == null)
        {
            return;
        }

        Ray ray =
            targetCamera.ScreenPointToRay(screenPosition);

        int hitCount =
            Physics.RaycastNonAlloc(
                ray,
                hits,
                maximumRayDistance,
                portalLayer,
                QueryTriggerInteraction.Collide
            );

        PortalController closestPortal = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
            {
                continue;
            }

            PortalController portal =
                hit.collider.GetComponent<PortalController>();

            if (portal == null)
            {
                portal =
                    hit.collider.GetComponentInParent<PortalController>();
            }

            if (
                portal != null &&
                !portal.IsOpening &&
                hit.distance < closestDistance
            )
            {
                closestPortal = portal;
                closestDistance = hit.distance;
            }
        }

        if (closestPortal != null)
        {
            closestPortal.OpenPortal();
        }
    }

    private static void ReadPointer(
        out bool down,
        out bool up,
        out Vector2 position,
        out int pointerId
    )
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            position = touch.position;
            pointerId = touch.fingerId;
            down = touch.phase == TouchPhase.Began;

            up =
                touch.phase == TouchPhase.Ended ||
                touch.phase == TouchPhase.Canceled;

            return;
        }

        position = Input.mousePosition;
        pointerId = -1;
        down = Input.GetMouseButtonDown(0);
        up = Input.GetMouseButtonUp(0);
    }
}
