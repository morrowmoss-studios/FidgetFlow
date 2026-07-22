using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogoSplashController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private RectTransform logoContainer;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "Warning";

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.25f;
    [SerializeField] private float fadeInDuration = 1.25f;
    [SerializeField] private float holdDuration = 1.25f;
    [SerializeField] private float fadeOutDuration = 0.75f;

    [Header("Scale Animation")]
    [SerializeField] private float startingScale = 0.96f;
    [SerializeField] private float endingScale = 1.0f;
    

    private void Awake()
    {
        if (logoCanvasGroup == null)
        {
            Debug.LogError(
                "LogoSplashController is missing the Logo Canvas Group reference.",
                this
            );

            enabled = false;
            return;
        }

        if (logoContainer == null)
        {
            Debug.LogError(
                "LogoSplashController is missing the Logo Container reference.",
                this
            );

            enabled = false;
            return;
        }

        logoCanvasGroup.alpha = 0.0f;
        logoCanvasGroup.interactable = false;
        logoCanvasGroup.blocksRaycasts = false;

        logoContainer.localScale =
            Vector3.one * startingScale;
    }

    private void Start()
    {
        StartCoroutine(PlaySplashSequence());
    }

    private IEnumerator PlaySplashSequence()
    {
        if (initialDelay > 0.0f)
        {
            yield return new WaitForSecondsRealtime(initialDelay);
        }

        yield return AnimateLogo(
            0.0f,
            1.0f,
            startingScale,
            endingScale,
            fadeInDuration
        );

        if (holdDuration > 0.0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }
        
        yield return AnimateLogo(
            1.0f,
            0.0f,
            endingScale,
            endingScale * 1.015f,
            fadeOutDuration
        );

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "Next scene name is empty.",
                this
            );

            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }
    

    private IEnumerator AnimateLogo(
        float startingAlpha,
        float targetAlpha,
        float scaleFrom,
        float scaleTo,
        float duration
    )
    {
        if (duration <= 0.0f)
        {
            logoCanvasGroup.alpha = targetAlpha;

            logoContainer.localScale =
                Vector3.one * scaleTo;

            yield break;
        }

        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            float easedProgress = Mathf.SmoothStep(
                0.0f,
                1.0f,
                progress
            );

            logoCanvasGroup.alpha = Mathf.Lerp(
                startingAlpha,
                targetAlpha,
                easedProgress
            );

            float currentScale = Mathf.Lerp(
                scaleFrom,
                scaleTo,
                easedProgress
            );

            logoContainer.localScale =
                Vector3.one * currentScale;

            yield return null;
        }

        logoCanvasGroup.alpha = targetAlpha;

        logoContainer.localScale =
            Vector3.one * scaleTo;
    }

}