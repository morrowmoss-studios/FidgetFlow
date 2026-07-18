using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WarningMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle warningToggle;
    [SerializeField] private Button continueButton;

    [Header("Scenes")]
    [SerializeField] private string homeScene = "HomeMenu";

    private void Start()
    {
        // Every launch starts unchecked.
        warningToggle.isOn = false;

        // Continue starts disabled.
        continueButton.interactable = false;

        // Listen for the toggle changing.
        warningToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        warningToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
    {
        continueButton.interactable = value;
    }

    // Continue Button
    public void Continue()
    {
        SceneManager.LoadScene(homeScene);
    }

    // Cancel Button
    public void Cancel()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}