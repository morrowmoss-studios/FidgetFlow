using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class SettingsPlanetClickTarget : MonoBehaviour
{
    public enum SettingsDestination
    {
        Accessibility,
        Audio,
        Legal
    }

    [Header("Settings")]
    [SerializeField] private SettingsSceneController settingsSceneController;

    [Header("Destination")]
    [SerializeField] private SettingsDestination destination;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    public SettingsDestination Destination => destination;

    private void Awake()
    {
        if (settingsSceneController == null)
        {
            settingsSceneController =
                FindFirstObjectByType<SettingsSceneController>();
        }

        Log(
            $"AWAKE | Collider={GetComponent<Collider>() != null} | " +
            $"SettingsSceneController=" +
            $"{(settingsSceneController != null ? settingsSceneController.name : "NULL")} | " +
            $"Destination={destination} | " +
            $"Active={isActiveAndEnabled}"
        );
    }

    public bool TryOpenDestination()
    {
        if (settingsSceneController == null)
        {
            LogError(
                "OPEN ABORTED | SettingsSceneController is NULL"
            );

            return false;
        }

        Log(
            $"OPEN ACCEPTED | Destination={destination}"
        );

        switch (destination)
        {
            case SettingsDestination.Accessibility:
                settingsSceneController.ShowAccessibility();
                break;

            case SettingsDestination.Audio:
                settingsSceneController.ShowAudio();
                break;

            case SettingsDestination.Legal:
                settingsSceneController.ShowLegal();
                break;

            default:
                LogError(
                    $"OPEN ABORTED | Unknown destination {destination}"
                );

                return false;
        }

        return true;
    }

    private void Log(string message)
    {
        if (!debugLogs)
        {
            return;
        }

        Debug.Log(
            $"[SETTINGS PLANET DEBUG] {name} | {message}",
            this
        );
    }

    private void LogError(string message)
    {
        Debug.LogError(
            $"[SETTINGS PLANET DEBUG] {name} | {message}",
            this
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (settingsSceneController == null)
        {
            settingsSceneController =
                FindFirstObjectByType<SettingsSceneController>();
        }
    }
#endif
}