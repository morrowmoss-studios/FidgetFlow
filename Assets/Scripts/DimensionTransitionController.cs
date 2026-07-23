using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DimensionTransitionController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Button enterButton;
    [SerializeField] private GameObject rainbowVortexOverlay;
    [SerializeField] private Image rainbowVortexImage;
    [SerializeField] private Transform portalRoot;
    [SerializeField] private Canvas transitionCanvas;

    [Header("Optional Portal Effects")]
    [SerializeField] private MonoBehaviour portalGlowPulse;
    [SerializeField] private Graphic portalGlow;
    [SerializeField] private Graphic lightSpill;

    [Header("Transition Timing")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 1.25f;
    [SerializeField, Min(0f)] private float holdDuration = 0.35f;
    [SerializeField, Min(0f)] private float destinationSettleTime = 0.1f;

    [Header("Portal Scale")]
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale =
        new Vector3(1.45f, 1.45f, 1.45f);

    [Header("Glow Boost")]
    [SerializeField, Range(0f, 1f)]
    private float portalGlowTargetAlpha = 0.58f;

    [SerializeField, Range(0f, 1f)]
    private float lightSpillTargetAlpha = 0.28f;

    [Header("Animation Curves")]
    [SerializeField] private AnimationCurve progressCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private AnimationCurve scaleCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private AnimationCurve glowCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Destination")]
    [SerializeField] private string destinationScene = "PortalHub";

    private static readonly int ProgressId =
        Shader.PropertyToID("_Progress");

    private static readonly int OverlayAlphaId =
        Shader.PropertyToID("_OverlayAlpha");

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

        if (transitionCanvas == null)
        {
            transitionCanvas = GetComponentInChildren<Canvas>(true);
        }

        /*
            Keep the persistent transition canvas above every canvas
            created by the destination scene.
        */
        if (transitionCanvas != null)
        {
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.overrideSorting = true;
            transitionCanvas.sortingOrder = short.MaxValue;
        }

        if (rainbowVortexImage != null &&
            rainbowVortexImage.material != null)
        {
            runtimeMaterial =
                new Material(rainbowVortexImage.material);

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

        DontDestroyOnLoad(gameObject);
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
            runtimeMaterial.SetFloat(ProgressId, 0f);
            runtimeMaterial.SetFloat(OverlayAlphaId, 0f);
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / transitionDuration);

            float progressValue =
                progressCurve.Evaluate(normalizedTime);

            float portalTime =
                Mathf.InverseLerp(0f, 0.75f, normalizedTime);

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

                runtimeMaterial.SetFloat(
                    OverlayAlphaId,
                    EvaluateTunnelAlpha(normalizedTime)
                );
            }

            if (portalRoot != null)
            {
                portalRoot.localScale =
                    Vector3.LerpUnclamped(
                        startScale,
                        endScale,
                        scaleValue
                    );
            }

            if (portalGlow != null)
            {
                Color color = portalGlowStartColor;

                color.a =
                    Mathf.Lerp(
                        portalGlowStartColor.a,
                        portalGlowTargetAlpha,
                        glowValue
                    );

                portalGlow.color = color;
            }

            if (lightSpill != null)
            {
                Color color = lightSpillStartColor;

                color.a =
                    Mathf.Lerp(
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
            runtimeMaterial.SetFloat(OverlayAlphaId, 1f);
        }

        if (portalRoot != null)
        {
            portalRoot.localScale = endScale;
        }

        if (holdDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(holdDuration);
        }

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(destinationScene);

        loadOperation.allowSceneActivation = false;

        float minimumTunnelTime = 1.75f;
        float timer = 0f;

        while (
            timer < minimumTunnelTime ||
            loadOperation.progress < 0.9f
        )
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        loadOperation.allowSceneActivation = true;

        yield return
            new WaitUntil(() => loadOperation.isDone);

        /*
            Allow the destination scene to render underneath the
            persistent tunnel before beginning the matching fade-out.
        */
        yield return null;
        yield return new WaitForEndOfFrame();

        if (destinationSettleTime > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    destinationSettleTime
                );
        }

        /*
            Run the exact same alpha timing backwards.

            The entry alpha grows between 15% and 75% of the main
            transition, so the matching exit lasts that same 60%
            of transitionDuration.
        */
        float exitDuration = transitionDuration * 0.60f;
        float fadeTimer = 0f;

        while (fadeTimer < exitDuration)
        {
            fadeTimer += Time.unscaledDeltaTime;

            float normalizedFade =
                Mathf.Clamp01(fadeTimer / exitDuration);

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat(
                    OverlayAlphaId,
                    EvaluateTunnelAlpha(1f - normalizedFade)
                );
            }

            yield return null;
        }

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(OverlayAlphaId, 0f);
        }

        Destroy(gameObject);
    }

    private float EvaluateTunnelAlpha(float normalizedTime)
    {
        return Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(
                0.15f,
                0.75f,
                normalizedTime
            )
        );
    }
}