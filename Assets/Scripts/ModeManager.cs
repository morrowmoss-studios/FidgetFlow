using System;
using System.Linq;
using UnityEngine;

public class ModeManager : MonoBehaviour
{
    private const string SelectedModeKey = "FidgetFlow.SelectedMode";

    [Header("Fallback")]
    [Tooltip("Used when no portal has selected a mode yet.")]
    [SerializeField] private string defaultModeId = "starloom";

    private ModeTarget[] modeTargets;

    private void Awake()
    {
        modeTargets = FindObjectsByType<ModeTarget>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        ActivateSelectedMode();
    }

    private void ActivateSelectedMode()
    {
        string selectedModeId = PlayerPrefs.GetString(
            SelectedModeKey,
            defaultModeId
        );

        ModeTarget selectedTarget = modeTargets.FirstOrDefault(
            target => string.Equals(
                target.ModeId,
                selectedModeId,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (selectedTarget == null)
        {
            Debug.LogError(
                $"No ModeTarget found for mode ID '{selectedModeId}'.",
                this
            );

            selectedTarget = modeTargets.FirstOrDefault(
                target => string.Equals(
                    target.ModeId,
                    defaultModeId,
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }

        foreach (ModeTarget target in modeTargets)
        {
            target.gameObject.SetActive(target == selectedTarget);
        }
    }

    public static void SetSelectedMode(string modeId)
    {
        if (string.IsNullOrWhiteSpace(modeId))
        {
            Debug.LogError("Cannot select an empty mode ID.");
            return;
        }

        PlayerPrefs.SetString(SelectedModeKey, modeId);
        PlayerPrefs.Save();
    }
}