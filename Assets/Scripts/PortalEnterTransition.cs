using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PortalEnterTransition : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Button enterButton;
    [SerializeField] private GameObject rainbowVortexOverlay;
    [SerializeField] private Image rainbowVortexImage;
    [SerializeField] private Transform portalRoot;

    [Header("Optional Portal Effects")]
    [SerializeField] private MonoBehaviour portalGlowPulse;
    [SerializeField] private Graphic portalGlow;
    [SerializeField] private Graphic lightSpill;

    [Header("Transition Timing")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 1.25f;
    [SerializeField, Min(0f)] private float holdDuration = 0.35f;

    [Header("Portal Scale")]
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale = new Vector3(1.45f, 1.45f, 1.45f);

    [Header("Glow Boost")]
    [SerializeField, Range(0f, 1f)] private float portalGlowTargetAlpha = 0.58f;
    [SerializeField, Range(0f, 1f)] private float lightSpillTargetAlpha = 0.28f;

    [Header("Animation Curves")]
    [SerializeField] private AnimationCurve progressCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private AnimationCurve scaleCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private AnimationCurve glowCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int OverlayAlphaId = Shader.PropertyToID("_OverlayAlpha");

    private Material runtimeMaterial;
    private bool isTransitioning;

    private Color portalGlowStartColor;
    private Color lightSpillStartColor;

    private void Awake()
    {
        if (enterButton != null)
        {
            enterButton.onClick.AddListener(BeginTransition);
        }

        if (rainbowVortexImage != null && rainbowVortexImage.material != null)
        {
            runtimeMaterial = new Material(rainbowVortexImage.material);
            rainbowVortexImage.material = runtimeMaterial;
            runtimeMaterial.SetFloat(ProgressId, 0f);
            runtimeMaterial.SetFloat(OverlayAlphaId, 0f);
        }

        if (rainbowVortexOverlay != null)
        {
            rainbowVortexOverlay.SetActive(false);
        }

        if (portalRoot != null)
        {
            startScale = portalRoot.localScale;
        }

        if (portalGlow != null)
        {
            portalGlowStartColor = portalGlow.color;
        }

        if (lightSpill != null)
        {
            lightSpillStartColor = lightSpill.color;
        }
    }

    private void OnDestroy()
    {
        if (enterButton != null)
        {
            enterButton.onClick.RemoveListener(BeginTransition);
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    public void BeginTransition()
    {
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(PlayTransition());
    }

    private IEnumerator PlayTransition()
    {
        isTransitioning = true;

        if (enterButton != null)
        {
            enterButton.interactable = false;
        }

        if (portalGlowPulse != null)
        {
            portalGlowPulse.enabled = false;
        }

        if (rainbowVortexOverlay != null)
        {
            rainbowVortexOverlay.SetActive(true);
        }

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(ProgressId, 1f);
            runtimeMaterial.SetFloat(OverlayAlphaId, 1f);
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / transitionDuration);

            // Let the portal breathe first, then slowly allow the tunnel
            // to consume the screen.
            float tunnelTime = normalizedTime;

            float progressValue =
                progressCurve.Evaluate(tunnelTime);

            // Slow swell that finishes before the tunnel fully covers us.
            float portalTime = Mathf.InverseLerp(
                0f,
                0.75f,
                normalizedTime
            );

            float scaleValue =
                scaleCurve.Evaluate(portalTime);

            float glowValue =
                glowCurve.Evaluate(normalizedTime);

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat(
                    ProgressId,
                    progressValue
                );

                // Fade the rainbow over the Home screen as the tunnel grows.
                float overlayAlpha = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        0.15f,
                        0.75f,
                        normalizedTime
                    )
                );

                runtimeMaterial.SetFloat(
                    OverlayAlphaId,
                    overlayAlpha
                );
            }

            if (portalRoot != null)
            {
                portalRoot.localScale = Vector3.LerpUnclamped(
                    startScale,
                    endScale,
                    scaleValue
                );
            }

            if (portalGlow != null)
            {
                Color color = portalGlowStartColor;

                color.a = Mathf.Lerp(
                    portalGlowStartColor.a,
                    portalGlowTargetAlpha,
                    glowValue
                );

                portalGlow.color = color;
            }

            if (lightSpill != null)
            {
                Color color = lightSpillStartColor;

                color.a = Mathf.Lerp(
                    lightSpillStartColor.a,
                    lightSpillTargetAlpha,
                    glowValue
                );

                lightSpill.color = color;
            }

            yield return null;
        }

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(ProgressId, 1f);
        }

        if (portalRoot != null)
        {
            portalRoot.localScale = endScale;
        }

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        // Intentionally stops here for the current test.
        // Scene loading gets added after this animation is approved.
    }
}