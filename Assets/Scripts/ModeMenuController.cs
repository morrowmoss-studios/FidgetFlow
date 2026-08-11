using System.Collections;
using UnityEngine;

public class ModeMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform dropMenu;

    [Header("Drawer Travel")]
    [Tooltip("How far upward the entire drawer moves when closed.")]
    [SerializeField] private float closedOffset = 100f;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.3f;

    private Vector2 openPosition;
    private Vector2 closedPosition;

    private bool isOpen;
    private Coroutine slideCoroutine;

    private void Start()
    {
        if (dropMenu == null)
        {
            Debug.LogError(
                "[ModeMenuController] DropMenu is not assigned.",
                this
            );

            return;
        }

        // Wherever DropMenu is positioned in the editor
        // is considered the fully OPEN position.
        openPosition = dropMenu.anchoredPosition;

        CalculateClosedPosition();

        // Start closed.
        dropMenu.anchoredPosition = closedPosition;
        isOpen = false;
    }

    private void CalculateClosedPosition()
    {
        // Positive Y moves a top-anchored UI object upward.
        closedPosition =
            openPosition +
            new Vector2(0f, closedOffset);
    }

    public void ToggleMenu()
    {
        if (dropMenu == null)
            return;

        isOpen = !isOpen;

        StartSlide(
            isOpen
                ? openPosition
                : closedPosition
        );
    }

    public void OpenMenu()
    {
        if (dropMenu == null || isOpen)
            return;

        isOpen = true;
        StartSlide(openPosition);
    }

    public void CloseMenu()
    {
        if (dropMenu == null || !isOpen)
            return;

        isOpen = false;
        StartSlide(closedPosition);
    }

    private void StartSlide(Vector2 target)
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        slideCoroutine =
            StartCoroutine(
                SlideMenu(target)
            );
    }

    private IEnumerator SlideMenu(Vector2 target)
    {
        Vector2 start = dropMenu.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / slideDuration
                );

            // Smooth ease in / ease out.
            t = t * t * (3f - 2f * t);

            dropMenu.anchoredPosition =
                Vector2.Lerp(
                    start,
                    target,
                    t
                );

            yield return null;
        }

        dropMenu.anchoredPosition = target;
        slideCoroutine = null;
    }
}