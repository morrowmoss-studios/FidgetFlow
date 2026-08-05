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

    [Min(1f)]
    [SerializeField] private float selectedPortalScaleMultiplier = 1.45f;

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

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private static readonly int ProgressId =
        Shader.PropertyToID("_Progress");

    private static readonly int OverlayAlphaId =
        Shader.PropertyToID("_OverlayAlpha");

    private Material runtimeMaterial;
    private bool isTransitioning;

    private Color portalGlowStartColor;
    private Color lightSpillStartColor;

    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        Log("AWAKE ENTERED");

        if (enterButton != null)
        {
            enterButton.onClick.AddListener(BeginTransition);
            Log("Enter button listener attached");
        }
        else
        {
            Log("Enter button is NULL");
        }

        if (transitionCanvas == null)
        {
            transitionCanvas =
                GetComponentInChildren<Canvas>(true);

            Log(
                $"Auto-found TransitionCanvas: " +
                $"{(transitionCanvas != null ? transitionCanvas.name : "NULL")}"
            );
        }

        if (transitionCanvas != null)
        {
            transitionCanvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            transitionCanvas.overrideSorting = true;
            transitionCanvas.sortingOrder = short.MaxValue;

            Log("TransitionCanvas configured");
        }
        else
        {
            LogError("TransitionCanvas is NULL");
        }

        if (
            rainbowVortexImage != null &&
            rainbowVortexImage.material != null
        )
        {
            runtimeMaterial =
                new Material(
                    rainbowVortexImage.material
                );

            rainbowVortexImage.material =
                runtimeMaterial;

            runtimeMaterial.SetFloat(ProgressId, 0f);
            runtimeMaterial.SetFloat(OverlayAlphaId, 0f);

            Log(
                $"Runtime material created from '{runtimeMaterial.shader.name}'"
            );
        }
        else
        {
            LogError(
                $"Rainbow image/material invalid | Image=" +
                $"{(rainbowVortexImage != null ? rainbowVortexImage.name : "NULL")} | " +
                $"Material={(rainbowVortexImage != null && rainbowVortexImage.material != null ? rainbowVortexImage.material.name : "NULL")}"
            );
        }

        if (rainbowVortexOverlay != null)
        {
            rainbowVortexOverlay.SetActive(false);
            Log("Rainbow overlay disabled at startup");
        }
        else
        {
            LogError("RainbowVortexOverlay is NULL");
        }

        if (portalRoot != null)
        {
            startScale = portalRoot.localScale;
            Log($"Initial PortalRoot='{portalRoot.name}' | StartScale={startScale}");
        }
        else
        {
            Log("PortalRoot is NULL at startup | This is valid in PortalHub");
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

        Log(
            $"AWAKE COMPLETE | ActiveScene='{SceneManager.GetActiveScene().name}' | " +
            $"Destination='{destinationScene}'"
        );
    }

    private void OnDestroy()
    {
        Log("ON DESTROY");

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
        Log(
            $"BeginTransition() no-arg called | Destination='{destinationScene}'"
        );

        BeginTransition(
            destinationScene,
            null
        );
    }

    public void BeginTransition(
        string destinationSceneName
    )
    {
        Log(
            $"BeginTransition(scene) called | Destination='{destinationSceneName}'"
        );

        BeginTransition(
            destinationSceneName,
            null
        );
    }

    public void BeginTransition(
        string destinationSceneName,
        Transform animatedPortalRoot
    )
    {
        Log(
            $"BeginTransition(scene, root) ENTERED | " +
            $"RequestedScene='{destinationSceneName}' | " +
            $"Root={(animatedPortalRoot != null ? animatedPortalRoot.name : "NULL")} | " +
            $"IsTransitioning={isTransitioning}"
        );

        if (isTransitioning)
        {
            Log("BeginTransition ABORTED | Already transitioning");
            return;
        }

        if (!string.IsNullOrWhiteSpace(destinationSceneName))
        {
            destinationScene = destinationSceneName;
            Log($"Destination updated to '{destinationScene}'");
        }
        else
        {
            Log("Destination argument was empty | Keeping inspector value");
        }

        if (animatedPortalRoot != null)
        {
            portalRoot = animatedPortalRoot;
            startScale = portalRoot.localScale;
            endScale =
                startScale *
                selectedPortalScaleMultiplier;

            Log(
                $"Portal root assigned | Root='{portalRoot.name}' | " +
                $"StartScale={startScale} | EndScale={endScale}"
            );
        }
        else
        {
            Log(
                $"No animated root passed | ExistingRoot=" +
                $"{(portalRoot != null ? portalRoot.name : "NULL")}"
            );
        }

        Log("Starting PlayTransition coroutine");
        StartCoroutine(PlayTransition());
    }

    private IEnumerator PlayTransition()
    {
        Log("PlayTransition COROUTINE ENTERED");
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
            Log("Rainbow overlay activated");
        }
        else
        {
            LogError("Cannot activate overlay | Reference is NULL");
        }

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(ProgressId, 0f);
            runtimeMaterial.SetFloat(OverlayAlphaId, 0f);
            Log("Runtime material reset");
        }
        else
        {
            LogError("Runtime material is NULL");
        }

        float elapsed = 0f;
        Log($"Beginning transition animation | Duration={transitionDuration:F2}");

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    transitionDuration
                );

            float progressValue =
                progressCurve.Evaluate(normalizedTime);

            float portalTime =
                Mathf.InverseLerp(
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

        Log("Transition animation loop complete");

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
            Log($"Holding tunnel for {holdDuration:F2}s");
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        Log(
            $"Attempting LoadSceneAsync('{destinationScene}') | " +
            $"CanStreamedLevelBeLoaded={Application.CanStreamedLevelBeLoaded(destinationScene)}"
        );

        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            LogError(
                $"Scene '{destinationScene}' is not available to load. " +
                $"Check Build Profiles / Scene List."
            );

            isTransitioning = false;
            yield break;
        }

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(destinationScene);

        if (loadOperation == null)
        {
            LogError(
                $"LoadSceneAsync returned NULL for '{destinationScene}'"
            );

            isTransitioning = false;
            yield break;
        }

        Log("LoadSceneAsync returned a valid operation");

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

        Log(
            $"Scene ready | Progress={loadOperation.progress:F2} | Activating"
        );

        loadOperation.allowSceneActivation = true;

        yield return new WaitUntil(
            () => loadOperation.isDone
        );

        Log(
            $"Scene load completed | ActiveScene='{SceneManager.GetActiveScene().name}'"
        );

        yield return null;
        yield return new WaitForEndOfFrame();

        if (destinationSettleTime > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    destinationSettleTime
                );
        }

        float exitDuration =
            transitionDuration *
            0.60f;

        float fadeTimer = 0f;

        Log($"Beginning tunnel fade-out | Duration={exitDuration:F2}");

        while (fadeTimer < exitDuration)
        {
            fadeTimer += Time.unscaledDeltaTime;

            float normalizedFade =
                Mathf.Clamp01(
                    fadeTimer /
                    exitDuration
                );

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat(
                    OverlayAlphaId,
                    EvaluateTunnelAlpha(
                        1f -
                        normalizedFade
                    )
                );
            }

            yield return null;
        }

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(OverlayAlphaId, 0f);
        }

        Log("Transition complete | Destroying controller object");
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

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[TRANSITION DEBUG] {name} | {message}",
                this
            );
        }
    }

    private void LogError(string message)
    {
        Debug.LogError(
            $"[TRANSITION DEBUG] {name} | {message}",
            this
        );
    }
}
