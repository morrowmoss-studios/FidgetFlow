using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ModeMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform drawerRoot;

    [Header("Direct Input Buttons")]
    [SerializeField] private RectTransform hamburgerButton;
    [SerializeField] private RectTransform backButton;

    [Header("Drawer Positions")]
    [SerializeField] private Vector2 closedPosition = new Vector2(0f, 200f);
    [SerializeField] private Vector2 openPosition = new Vector2(0f, -2f);

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.25f;

    private bool isOpen = false;
    private Coroutine moveCoroutine;

    // Prevents one physical press from being processed more than once.
    private bool pointerWasPressed = false;

    private void Start()
    {
        isOpen = false;

        if (drawerRoot == null)
        {
            Debug.LogError(
                "[ModeMenuController] DrawerRoot is not assigned.",
                this
            );

            return;
        }

        StartCoroutine(ForceClosedAfterLayout());
    }

    private void Update()
    {
        Vector2 screenPosition;
        bool pointerPressed;

        // ------------------------------------------------------------
        // TOUCH INPUT
        // iOS + Android
        // ------------------------------------------------------------

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            pointerPressed = touch.press.isPressed;
            screenPosition = touch.position.ReadValue();
        }

        // ------------------------------------------------------------
        // MOUSE INPUT
        // Unity Editor
        // ------------------------------------------------------------

        else if (Mouse.current != null)
        {
            pointerPressed = Mouse.current.leftButton.isPressed;
            screenPosition = Mouse.current.position.ReadValue();
        }

        else
        {
            pointerPressed = false;
            screenPosition = Vector2.zero;
        }

        // We only want the INITIAL press.
        if (pointerPressed && !pointerWasPressed)
        {
            HandlePointerDown(screenPosition);
        }

        pointerWasPressed = pointerPressed;
    }

    private void HandlePointerDown(Vector2 screenPosition)
    {
        // ------------------------------------------------------------
        // HAMBURGER
        // ------------------------------------------------------------

        if (
            hamburgerButton != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                hamburgerButton,
                screenPosition,
                null
            )
        )
        {
            Debug.Log(
                $"[ModeMenuController] Hamburger direct input detected at {screenPosition}"
            );

            ToggleMenu();
            return;
        }

        // ------------------------------------------------------------
        // BACK
        //
        // Only allow the back button while the drawer is open.
        // ------------------------------------------------------------

        if (
            isOpen &&
            backButton != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                backButton,
                screenPosition,
                null
            )
        )
        {
            Debug.Log(
                $"[ModeMenuController] Back direct input detected at {screenPosition}"
            );

            GoBackToPortalHub();
        }
    }

    private IEnumerator ForceClosedAfterLayout()
    {
        // Let Unity finish rebuilding/layouting the UI first.
        yield return null;
        yield return new WaitForEndOfFrame();

        drawerRoot.anchoredPosition = closedPosition;
        isOpen = false;

        Debug.Log(
            $"[ModeMenuController] Forced CLOSED at {drawerRoot.anchoredPosition}"
        );
    }

    public void ToggleMenu()
    {
        if (drawerRoot == null)
            return;

        isOpen = !isOpen;

        Vector2 target =
            isOpen
                ? openPosition
                : closedPosition;

        MoveDrawer(target);
    }

    public void OpenMenu()
    {
        if (drawerRoot == null || isOpen)
            return;

        isOpen = true;
        MoveDrawer(openPosition);
    }

    public void CloseMenu()
    {
        if (drawerRoot == null || !isOpen)
            return;

        isOpen = false;
        MoveDrawer(closedPosition);
    }

    private void MoveDrawer(Vector2 target)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine =
            StartCoroutine(
                AnimateDrawer(target)
            );
    }

    private IEnumerator AnimateDrawer(Vector2 target)
    {
        Vector2 start =
            drawerRoot.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / slideDuration
                );

            // Smooth ease in/out.
            t = t * t * (3f - 2f * t);

            drawerRoot.anchoredPosition =
                Vector2.Lerp(
                    start,
                    target,
                    t
                );

            yield return null;
        }

        drawerRoot.anchoredPosition = target;
        moveCoroutine = null;
    }

    public void GoBackToPortalHub()
    {
        Debug.Log(
            "[ModeMenuController] Loading PortalHub."
        );

        SceneManager.LoadScene("PortalHub");
    }
}