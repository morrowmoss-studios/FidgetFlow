using System;
using System.Linq;
using UnityEngine;

public class ModeManager : MonoBehaviour
{
    private const string SelectedModeKey =
        "FidgetFlow.SelectedMode";

    [Header("Fallback")]
    [SerializeField] private string defaultModeId = "starloom";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private ModeTarget[] modeTargets;

    private void Awake()
    {
        Log(
            $"AWAKE | ActiveObject={gameObject.activeInHierarchy}"
        );

        modeTargets =
            FindObjectsByType<ModeTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        Log($"Found {modeTargets.Length} ModeTarget objects");

        foreach (ModeTarget target in modeTargets)
        {
            Log(
                $"ModeTarget | Object='{target.name}' | ModeId='{target.ModeId}'"
            );
        }

        ActivateSelectedMode();
    }

    private void ActivateSelectedMode()
    {
        string selectedModeId =
            PlayerPrefs.GetString(
                SelectedModeKey,
                defaultModeId
            );

        Log(
            $"ActivateSelectedMode | Selected='{selectedModeId}' | Default='{defaultModeId}'"
        );

        ModeTarget selectedTarget =
            modeTargets.FirstOrDefault(
                target =>
                    string.Equals(
                        target.ModeId,
                        selectedModeId,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        if (selectedTarget == null)
        {
            Debug.LogError(
                $"[MODE MANAGER DEBUG] No ModeTarget found for mode ID '{selectedModeId}'.",
                this
            );

            selectedTarget =
                modeTargets.FirstOrDefault(
                    target =>
                        string.Equals(
                            target.ModeId,
                            defaultModeId,
                            StringComparison.OrdinalIgnoreCase
                        )
                );
        }

        foreach (ModeTarget target in modeTargets)
        {
            bool active =
                target == selectedTarget;

            target.gameObject.SetActive(active);

            Log(
                $"SetActive({active}) | Object='{target.name}' | ModeId='{target.ModeId}'"
            );
        }

        Log(
            $"Activation complete | SelectedTarget=" +
            $"{(selectedTarget != null ? selectedTarget.name : "NULL")}"
        );
    }

    public static void SetSelectedMode(string modeId)
    {
        if (string.IsNullOrWhiteSpace(modeId))
        {
            Debug.LogError(
                "[MODE MANAGER DEBUG] Cannot select an empty mode ID."
            );
            return;
        }

        Debug.Log(
            $"[MODE MANAGER DEBUG] SetSelectedMode('{modeId}')"
        );

        PlayerPrefs.SetString(
            SelectedModeKey,
            modeId
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"[MODE MANAGER DEBUG] Saved value is now " +
            $"'{PlayerPrefs.GetString(SelectedModeKey, "<MISSING>")}'"
        );
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[MODE MANAGER DEBUG] {name} | {message}",
                this
            );
        }
    }
}
