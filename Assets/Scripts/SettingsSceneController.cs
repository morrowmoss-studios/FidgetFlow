using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsSceneController : MonoBehaviour
{
    [Header("Canvases")]
    [SerializeField] private GameObject settingsUniverseCanvas;
    [SerializeField] private GameObject accessibilityCanvas;
    [SerializeField] private GameObject audioCanvas;
    [SerializeField] private GameObject legalCanvas;

    [Header("Accessibility Values")]
    [SerializeField] private TMP_Text colorVisionValue;
    [SerializeField] private TMP_Text visualIntensityValue;
    [SerializeField] private TMP_Text motionIntensityValue;
    [SerializeField] private TMP_Text hapticsValue;

    private const string HapticsKey = "FidgetFlow_Haptics";
    private bool hapticsEnabled = true;

    private void Start()
    {
        hapticsEnabled =
            PlayerPrefs.GetInt(
                HapticsKey,
                1
            ) == 1;

        ShowSettingsUniverse();
        RefreshAccessibilityValues();
    }

    // ---------------------------------------------------------
    // CANVAS NAVIGATION
    // ---------------------------------------------------------

    public void ShowSettingsUniverse()
    {
        SetCanvasState(
            settingsUniverse: true,
            accessibility: false,
            audio: false,
            legal: false
        );
    }

    public void ShowAccessibility()
    {
        SetCanvasState(
            settingsUniverse: false,
            accessibility: true,
            audio: false,
            legal: false
        );

        RefreshAccessibilityValues();
    }

    public void ShowAudio()
    {
        SetCanvasState(
            settingsUniverse: false,
            accessibility: false,
            audio: true,
            legal: false
        );
    }

    public void ShowLegal()
    {
        SetCanvasState(
            settingsUniverse: false,
            accessibility: false,
            audio: false,
            legal: true
        );
    }

    private void SetCanvasState(
        bool settingsUniverse,
        bool accessibility,
        bool audio,
        bool legal
    )
    {
        if (settingsUniverseCanvas != null)
        {
            settingsUniverseCanvas.SetActive(settingsUniverse);
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

    // ---------------------------------------------------------
    // COLOR VISION
    // ---------------------------------------------------------

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

        RefreshColorVision();
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

        RefreshColorVision();
    }

    // ---------------------------------------------------------
    // VISUAL INTENSITY
    // ---------------------------------------------------------

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

        RefreshVisualIntensity();
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

        RefreshVisualIntensity();
    }

    // ---------------------------------------------------------
    // MOTION INTENSITY
    // ---------------------------------------------------------

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

        RefreshMotionIntensity();
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

        RefreshMotionIntensity();
    }

    // ---------------------------------------------------------
    // HAPTICS
    // ---------------------------------------------------------

    public void ToggleHaptics()
    {
        hapticsEnabled =
            !hapticsEnabled;

        PlayerPrefs.SetInt(
            HapticsKey,
            hapticsEnabled ? 1 : 0
        );

        PlayerPrefs.Save();

        RefreshHaptics();
    }

    // ---------------------------------------------------------
    // LABEL REFRESH
    // ---------------------------------------------------------

    private void RefreshAccessibilityValues()
    {
        RefreshColorVision();
        RefreshVisualIntensity();
        RefreshMotionIntensity();
        RefreshHaptics();
    }

    private void RefreshColorVision()
    {
        if (colorVisionValue == null)
        {
            return;
        }

        colorVisionValue.text =
            ColorVisionAccessibility.CurrentMode.ToString();
    }

    private void RefreshVisualIntensity()
    {
        if (visualIntensityValue == null)
        {
            return;
        }

        visualIntensityValue.text =
            VisualIntensityAccessibility.CurrentLevel.ToString();
    }

    private void RefreshMotionIntensity()
    {
        if (motionIntensityValue == null)
        {
            return;
        }

        motionIntensityValue.text =
            MotionAccessibility.CurrentLevel.ToString();
    }

    private void RefreshHaptics()
    {
        if (hapticsValue == null)
        {
            return;
        }

        hapticsValue.text =
            hapticsEnabled
                ? "On"
                : "Off";
    }
}