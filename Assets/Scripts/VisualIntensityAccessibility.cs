using System;
using UnityEngine;

public static class VisualIntensityAccessibility
{
    public enum VisualLevel
    {
        Reduced = 0,
        Standard = 1,
        Full = 2
    }

    private const string PlayerPrefsKey =
        "FidgetFlow_VisualIntensity";

    private static bool loaded;

    private static VisualLevel currentLevel =
        VisualLevel.Standard;

    public static event Action<VisualLevel> OnChanged;

    public static VisualLevel CurrentLevel
    {
        get
        {
            EnsureLoaded();
            return currentLevel;
        }
    }

    // ---------------------------------------------------------
    // SHARED MULTIPLIERS
    //
    // Starter values only.
    // Final values come from beta feedback.
    // ---------------------------------------------------------

    public static float BrightnessMultiplier
    {
        get
        {
            EnsureLoaded();

            return currentLevel switch
            {
                VisualLevel.Reduced => 0.70f,
                VisualLevel.Standard => 1.00f,
                VisualLevel.Full => 1.15f,
                _ => 1.00f
            };
        }
    }

    public static float PulseMultiplier
    {
        get
        {
            EnsureLoaded();

            return currentLevel switch
            {
                VisualLevel.Reduced => 0.45f,
                VisualLevel.Standard => 1.00f,
                VisualLevel.Full => 1.20f,
                _ => 1.00f
            };
        }
    }

    public static float ColorCycleMultiplier
    {
        get
        {
            EnsureLoaded();

            return currentLevel switch
            {
                VisualLevel.Reduced => 0.55f,
                VisualLevel.Standard => 1.00f,
                VisualLevel.Full => 1.15f,
                _ => 1.00f
            };
        }
    }

    public static float FlashMultiplier
    {
        get
        {
            EnsureLoaded();

            return currentLevel switch
            {
                VisualLevel.Reduced => 0.25f,
                VisualLevel.Standard => 1.00f,
                VisualLevel.Full => 1.00f,
                _ => 1.00f
            };
        }
    }

    // ---------------------------------------------------------
    // SETTING
    // ---------------------------------------------------------

    public static void SetLevel(
        VisualLevel level
    )
    {
        EnsureLoaded();

        if (currentLevel == level)
        {
            return;
        }

        currentLevel = level;

        PlayerPrefs.SetInt(
            PlayerPrefsKey,
            (int)currentLevel
        );

        PlayerPrefs.Save();

        OnChanged?.Invoke(
            currentLevel
        );

        Debug.Log(
            $"[VisualIntensityAccessibility] Level changed to {currentLevel}"
        );
    }

    // ---------------------------------------------------------
    // LOAD
    // ---------------------------------------------------------

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
                (int)VisualLevel.Standard
            );

        if (
            !Enum.IsDefined(
                typeof(VisualLevel),
                savedValue
            )
        )
        {
            savedValue =
                (int)VisualLevel.Standard;
        }

        currentLevel =
            (VisualLevel)savedValue;
    }
}