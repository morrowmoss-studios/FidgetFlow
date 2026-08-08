using System.Collections;
using UnityEngine;

public class ModeMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform sideMenu;

    [Header("Menu Positions")]
    [SerializeField] private Vector2 closedPosition = new Vector2(-500f, 0f);
    [SerializeField] private Vector2 openPosition = Vector2.zero;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.3f;

    private bool isOpen;
    private Coroutine slideCoroutine;

    private void Start()
    {
        if (sideMenu == null)
        {
            Debug.LogError("[ModeMenuController] SideMenu has not been assigned.");
            return;
        }

        // Always begin hidden.
        sideMenu.anchoredPosition = closedPosition;
        isOpen = false;
    }

    public void ToggleMenu()
    {
        if (sideMenu == null)
            return;

        isOpen = !isOpen;

        Vector2 targetPosition = isOpen
            ? openPosition
            : closedPosition;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(
            SlideMenu(sideMenu.anchoredPosition, targetPosition)
        );
    }

    public void OpenMenu()
    {
        if (sideMenu == null || isOpen)
            return;

        isOpen = true;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(
            SlideMenu(sideMenu.anchoredPosition, openPosition)
        );
    }

    public void CloseMenu()
    {
        if (sideMenu == null || !isOpen)
            return;

        isOpen = false;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(
            SlideMenu(sideMenu.anchoredPosition, closedPosition)
        );
    }

    private IEnumerator SlideMenu(Vector2 start, Vector2 target)
    {
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / slideDuration);

            // Smooth acceleration/deceleration instead of robotic linear movement.
            float smoothT = t * t * (3f - 2f * t);

            sideMenu.anchoredPosition =
                Vector2.LerpUnclamped(start, target, smoothT);

            yield return null;
        }

        sideMenu.anchoredPosition = target;
        slideCoroutine = null;
    }
}