using System;
using UnityEngine;

public static class MotionAccessibility
{
    public enum MotionLevel
    {
        Reduced = 0,
        Standard = 1,
        Full = 2
    }

    private const string PlayerPrefsKey =
        "FidgetFlow_MotionIntensity";

    private static bool loaded;

    private static MotionLevel currentLevel =
        MotionLevel.Standard;

    public static event Action<MotionLevel> OnChanged;

    public static MotionLevel CurrentLevel
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
    // These are intentionally starter values.
    // We will tune them during beta testing.
    // ---------------------------------------------------------

    public static float RotationMultiplier
    {
        get
        {
            EnsureLoaded();

            return currentLevel switch
            {
                MotionLevel.Reduced => 0.45f,
                MotionLevel.Standard => 1.00f,
                MotionLevel.Full => 1.25f,
                _ => 1.00f
            };
        }
    }

    public static float ZoomMultiplier
    {
        get
        {
            EnsureLoaded();

            return currentLevel switch
            {
                MotionLevel.Reduced => 0.40f,
                MotionLevel.Standard => 1.00f,
                MotionLevel.Full => 1.25f,
                _ => 1.00f
            };
        }
    }

    public static float WarpMultiplier
    {
        get
        {
            EnsureLoaded();

            return currentLevel switch
            {
                MotionLevel.Reduced => 0.50f,
                MotionLevel.Standard => 1.00f,
                MotionLevel.Full => 1.20f,
                _ => 1.00f
            };
        }
    }

    public static float TransitionMultiplier
    {
        get
        {
            EnsureLoaded();

            return currentLevel switch
            {
                MotionLevel.Reduced => 0.55f,
                MotionLevel.Standard => 1.00f,
                MotionLevel.Full => 1.15f,
                _ => 1.00f
            };
        }
    }

    // ---------------------------------------------------------
    // SETTING
    // ---------------------------------------------------------

    public static void SetLevel(
        MotionLevel level
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
            $"[MotionAccessibility] Level changed to {currentLevel}"
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
                (int)MotionLevel.Standard
            );

        if (
            !Enum.IsDefined(
                typeof(MotionLevel),
                savedValue
            )
        )
        {
            savedValue =
                (int)MotionLevel.Standard;
        }

        currentLevel =
            (MotionLevel)savedValue;
    }
}