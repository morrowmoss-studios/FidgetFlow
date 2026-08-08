using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform panel;
    Rect lastSafeArea = new Rect();

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            ApplySafeArea();
    }

    void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;
        lastSafeArea = safe;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;

        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        panel.anchorMin = min;
        panel.anchorMax = max;
    }
}