using System;
using UnityEngine;

public static class ColorVisionAccessibility
{
    public enum ColorVisionMode
    {
        Off = 0,

        // Red-deficient vision
        Protan = 1,

        // Green-deficient vision
        Deutan = 2,

        // Blue-yellow deficient vision
        Tritan = 3
    }

    private const string PlayerPrefsKey =
        "FidgetFlow_ColorVisionMode";

    private static bool loaded;

    private static ColorVisionMode currentMode =
        ColorVisionMode.Off;

    public static event Action<ColorVisionMode> OnChanged;

    public static ColorVisionMode CurrentMode
    {
        get
        {
            EnsureLoaded();
            return currentMode;
        }
    }

    public static bool IsEnabled
    {
        get
        {
            return CurrentMode !=
                   ColorVisionMode.Off;
        }
    }

    public static void SetMode(
        ColorVisionMode mode
    )
    {
        EnsureLoaded();

        if (currentMode == mode)
        {
            return;
        }

        currentMode = mode;

        PlayerPrefs.SetInt(
            PlayerPrefsKey,
            (int)currentMode
        );

        PlayerPrefs.Save();

        OnChanged?.Invoke(
            currentMode
        );

        Debug.Log(
            $"[ColorVisionAccessibility] Mode changed to {currentMode}"
        );
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;

        int savedValue =
            PlayerPrefs.GetInt(
                PlayerPrefsKey,
                (int)ColorVisionMode.Off
            );

        if (
            !Enum.IsDefined(
                typeof(ColorVisionMode),
                savedValue
            )
        )
        {
            savedValue =
                (int)ColorVisionMode.Off;
        }

        currentMode =
            (ColorVisionMode)savedValue;
    }
}