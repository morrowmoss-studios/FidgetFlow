using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsSceneController : MonoBehaviour
{
    [Header("Settings Universe Canvases")]
    [SerializeField] private GameObject universeCanvas;
    [SerializeField] private GameObject accessibilityCanvas;
    [SerializeField] private GameObject audioCanvas;
    [SerializeField] private GameObject legalCanvas;

    [Header("Accessibility Value Text")]
    [SerializeField] private TMP_Text colorVisionValueText;
    [SerializeField] private TMP_Text visualIntensityValueText;
    [SerializeField] private TMP_Text motionIntensityValueText;
    [SerializeField] private TMP_Text hapticsValueText;

    private const string HapticsPrefsKey =
        "FidgetFlow_HapticsEnabled";

    private bool hapticsEnabled = true;

    private void Start()
    {
        hapticsEnabled =
            PlayerPrefs.GetInt(
                HapticsPrefsKey,
                1
            ) == 1;

        ShowUniverse();
        RefreshAllAccessibilityLabels();
    }

    // =========================================================
    // CANVAS NAVIGATION
    // =========================================================

    public void ShowUniverse()
    {
        SetCanvasState(
            universe: true,
            accessibility: false,
            audio: false,
            legal: false
        );
    }

    public void ShowAccessibility()
    {
        SetCanvasState(
            universe: false,
            accessibility: true,
            audio: false,
            legal: false
        );

        RefreshAllAccessibilityLabels();
    }

    public void ShowAudio()
    {
        SetCanvasState(
            universe: false,
            accessibility: false,
            audio: true,
            legal: false
        );
    }

    public void ShowLegal()
    {
        SetCanvasState(
            universe: false,
            accessibility: false,
            audio: false,
            legal: true
        );
    }

    private void SetCanvasState(
        bool universe,
        bool accessibility,
        bool audio,
        bool legal
    )
    {
        if (universeCanvas != null)
        {
            universeCanvas.SetActive(universe);
        }

        if (accessibilityCanvas != null)
        {
            accessibilityCanvas.SetActive(accessibility);
        }

        if (audioCanvas != null)
        {
            audioCanvas.SetActive(audio);
        }

        if (legalCanvas != null)
        {
            legalCanvas.SetActive(legal);
        }
    }

    public void BackToPortalHub()
    {
        SceneManager.LoadScene("PortalHub");
    }

    // =========================================================
    // COLOR VISION
    // =========================================================

    public void PreviousColorVision()
    {
        int current =
            (int)ColorVisionAccessibility.CurrentMode;

        int count =
            System.Enum.GetValues(
                typeof(
                    ColorVisionAccessibility.ColorVisionMode
                )
            ).Length;

        current--;

        if (current < 0)
        {
            current = count - 1;
        }

        ColorVisionAccessibility.SetMode(
            (ColorVisionAccessibility.ColorVisionMode)current
        );

        RefreshColorVisionLabel();
    }

    public void NextColorVision()
    {
        int current =
            (int)ColorVisionAccessibility.CurrentMode;

        int count =
            System.Enum.GetValues(
                typeof(
                    ColorVisionAccessibility.ColorVisionMode
                )
            ).Length;

        current++;

        if (current >= count)
        {
            current = 0;
        }

        ColorVisionAccessibility.SetMode(
            (ColorVisionAccessibility.ColorVisionMode)current
        );

        RefreshColorVisionLabel();
    }

    // =========================================================
    // VISUAL INTENSITY
    // =========================================================

    public void PreviousVisualIntensity()
    {
        int current =
            (int)VisualIntensityAccessibility.CurrentLevel;

        int count =
            System.Enum.GetValues(
                typeof(
                    VisualIntensityAccessibility.VisualLevel
                )
            ).Length;

        current--;

        if (current < 0)
        {
            current = count - 1;
        }

        VisualIntensityAccessibility.SetLevel(
            (VisualIntensityAccessibility.VisualLevel)current
        );

        RefreshVisualIntensityLabel();
    }

    public void NextVisualIntensity()
    {
        int current =
            (int)VisualIntensityAccessibility.CurrentLevel;

        int count =
            System.Enum.GetValues(
                typeof(
                    VisualIntensityAccessibility.VisualLevel
                )
            ).Length;

        current++;

        if (current >= count)
        {
            current = 0;
        }

        VisualIntensityAccessibility.SetLevel(
            (VisualIntensityAccessibility.VisualLevel)current
        );

        RefreshVisualIntensityLabel();
    }

    // =========================================================
    // MOTION INTENSITY
    // =========================================================

    public void PreviousMotionIntensity()
    {
        int current =
            (int)MotionAccessibility.CurrentLevel;

        int count =
            System.Enum.GetValues(
                typeof(
                    MotionAccessibility.MotionLevel
                )
            ).Length;

        current--;

        if (current < 0)
        {
            current = count - 1;
        }

        MotionAccessibility.SetLevel(
            (MotionAccessibility.MotionLevel)current
        );

        RefreshMotionIntensityLabel();
    }

    public void NextMotionIntensity()
    {
        int current =
            (int)MotionAccessibility.CurrentLevel;

        int count =
            System.Enum.GetValues(
                typeof(
                    MotionAccessibility.MotionLevel
                )
            ).Length;

        current++;

        if (current >= count)
        {
            current = 0;
        }

        MotionAccessibility.SetLevel(
            (MotionAccessibility.MotionLevel)current
        );

        RefreshMotionIntensityLabel();
    }

    // =========================================================
    // HAPTICS
    // =========================================================

    public void ToggleHaptics()
    {
        hapticsEnabled =
            !hapticsEnabled;

        PlayerPrefs.SetInt(
            HapticsPrefsKey,
            hapticsEnabled ? 1 : 0
        );

        PlayerPrefs.Save();

        RefreshHapticsLabel();
    }

    public bool HapticsEnabled()
    {
        return hapticsEnabled;
    }

    // =========================================================
    // LABEL REFRESH
    // =========================================================

    private void RefreshAllAccessibilityLabels()
    {
        RefreshColorVisionLabel();
        RefreshVisualIntensityLabel();
        RefreshMotionIntensityLabel();
        RefreshHapticsLabel();
    }

    private void RefreshColorVisionLabel()
    {
        if (colorVisionValueText == null)
        {
            return;
        }

        colorVisionValueText.text =
            ColorVisionAccessibility.CurrentMode.ToString();
    }

    private void RefreshVisualIntensityLabel()
    {
        if (visualIntensityValueText == null)
        {
            return;
        }

        visualIntensityValueText.text =
            VisualIntensityAccessibility.CurrentLevel.ToString();
    }

    private void RefreshMotionIntensityLabel()
    {
        if (motionIntensityValueText == null)
        {
            return;
        }

        motionIntensityValueText.text =
            MotionAccessibility.CurrentLevel.ToString();
    }

    private void RefreshHapticsLabel()
    {
        if (hapticsValueText == null)
        {
            return;
        }

        hapticsValueText.text =
            hapticsEnabled
                ? "On"
                : "Off";
    }
}