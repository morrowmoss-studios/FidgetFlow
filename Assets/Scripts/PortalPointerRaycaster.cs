using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

    private readonly RaycastHit[] hits =
        new RaycastHit[32];

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
        if (!ReadPointer(
                out bool down,
                out bool up,
                out Vector2 position))
        {
            return;
        }

        if (down)
        {
            pointerHeld = true;

            pointerDownPosition = position;
            pointerDownTime = Time.unscaledTime;

            pointerStartedOverUI =
                ignoreInputOverUI &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(-1);
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
            pointerStartedOverUI = false;
            return;
        }

        pointerStartedOverUI = false;

        OpenPortalAt(position);
    }

    private void OpenPortalAt(Vector2 screenPosition)
    {
        if (targetCamera == null)
        {
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
                portalLayer,
                QueryTriggerInteraction.Collide
            );

        PortalClickTarget closestTarget = null;
        PortalController fallbackPortal = null;

        float closestDistance =
            float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
            {
                continue;
            }

            if (hit.distance >= closestDistance)
            {
                continue;
            }

            PortalClickTarget clickTarget =
                hit.collider.GetComponent<PortalClickTarget>();

            if (clickTarget == null)
            {
                clickTarget =
                    hit.collider.GetComponentInParent<PortalClickTarget>();
            }

            if (
                clickTarget != null &&
                clickTarget.PortalController != null &&
                !clickTarget.PortalController.IsOpening
            )
            {
                closestTarget = clickTarget;
                fallbackPortal = null;
                closestDistance = hit.distance;

                continue;
            }

            // Keeps compatibility with portal colliders that may not
            // have PortalClickTarget attached yet.
            PortalController portal =
                hit.collider.GetComponent<PortalController>();

            if (portal == null)
            {
                portal =
                    hit.collider.GetComponentInParent<PortalController>();
            }

            if (
                portal != null &&
                !portal.IsOpening
            )
            {
                closestTarget = null;
                fallbackPortal = portal;
                closestDistance = hit.distance;
            }
        }

        if (closestTarget != null)
        {
            closestTarget.TryOpenPortal();
            return;
        }

        if (fallbackPortal != null)
        {
            fallbackPortal.OpenPortal();
        }
    }

    private static bool ReadPointer(
        out bool down,
        out bool up,
        out Vector2 position
    )
    {
        down = false;
        up = false;
        position = Vector2.zero;

        // ---------------------------------------------------------
        // TOUCH
        // iOS + Android
        // ---------------------------------------------------------

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            bool pressed =
                touch.press.isPressed;

            bool pressedThisFrame =
                touch.press.wasPressedThisFrame;

            bool releasedThisFrame =
                touch.press.wasReleasedThisFrame;

            if (
                pressed ||
                pressedThisFrame ||
                releasedThisFrame
            )
            {
                position =
                    touch.position.ReadValue();

                down = pressedThisFrame;
                up = releasedThisFrame;

                return true;
            }
        }

        // ---------------------------------------------------------
        // MOUSE
        // Unity Editor / desktop testing
        // ---------------------------------------------------------

        if (Mouse.current != null)
        {
            bool pressed =
                Mouse.current.leftButton.isPressed;

            bool pressedThisFrame =
                Mouse.current.leftButton.wasPressedThisFrame;

            bool releasedThisFrame =
                Mouse.current.leftButton.wasReleasedThisFrame;

            if (
                pressed ||
                pressedThisFrame ||
                releasedThisFrame
            )
            {
                position =
                    Mouse.current.position.ReadValue();

                down = pressedThisFrame;
                up = releasedThisFrame;

                return true;
            }
        }

        return false;
    }
}