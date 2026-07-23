using UnityEngine;
using UnityEngine.UI;

public class PortalGlowPulse : MonoBehaviour
{
    [Header("Glow Images")]
    [SerializeField] private Image portalGlow;
    [SerializeField] private Image lightSpill;

    [Header("Pulse Timing")]
    [SerializeField] private float pulseDuration = 3f;

    [Header("Portal Glow Alpha")]
    [SerializeField, Range(0f, 1f)] private float portalGlowMinAlpha = 0.38f;
    [SerializeField, Range(0f, 1f)] private float portalGlowMaxAlpha = 0.44f;

    [Header("Light Spill Alpha")]
    [SerializeField, Range(0f, 1f)] private float lightSpillMinAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] private float lightSpillMaxAlpha = 0.24f;

    [Header("Scale")]
    [SerializeField] private float portalGlowMinScale = 1f;
    [SerializeField] private float portalGlowMaxScale = 1.03f;

    [SerializeField] private float lightSpillMinScale = 1f;
    [SerializeField] private float lightSpillMaxScale = 1.04f;

    private Vector3 portalGlowBaseScale;
    private Vector3 lightSpillBaseScale;
    private float pulseTimer;

    private void Awake()
    {
        if (portalGlow == null || lightSpill == null)
        {
            Debug.LogError(
                "PortalGlowPulse is missing one or both Image references.",
                this
            );

            enabled = false;
            return;
        }

        portalGlowBaseScale = portalGlow.rectTransform.localScale;
        lightSpillBaseScale = lightSpill.rectTransform.localScale;
    }

    private void Update()
    {
        pulseTimer += Time.unscaledDeltaTime;

        float cycle = Mathf.PingPong(
            pulseTimer / Mathf.Max(pulseDuration, 0.01f),
            1f
        );

        float easedPulse = cycle * cycle * (3f - 2f * cycle);

        SetImageAlpha(
            portalGlow,
            Mathf.Lerp(
                portalGlowMinAlpha,
                portalGlowMaxAlpha,
                easedPulse
            )
        );

        SetImageAlpha(
            lightSpill,
            Mathf.Lerp(
                lightSpillMinAlpha,
                lightSpillMaxAlpha,
                easedPulse
            )
        );

        portalGlow.rectTransform.localScale =
            portalGlowBaseScale *
            Mathf.Lerp(
                portalGlowMinScale,
                portalGlowMaxScale,
                easedPulse
            );

        lightSpill.rectTransform.localScale =
            lightSpillBaseScale *
            Mathf.Lerp(
                lightSpillMinScale,
                lightSpillMaxScale,
                easedPulse
            );
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}