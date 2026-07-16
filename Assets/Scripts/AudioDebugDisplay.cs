using UnityEngine;

public class AudioDebugDisplay : MonoBehaviour
{
    public AudioReactivityManager audioManager;

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 120));
        GUILayout.Label($"Mode: {audioManager.CurrentMode}");
        GUILayout.Label($"Bass: {audioManager._bass:F3}");
        GUILayout.Label($"Mid: {audioManager._mid:F3}");
        GUILayout.Label($"High: {audioManager._high:F3}");
        GUILayout.Label($"Energy: {audioManager._energy:F3}");
        GUILayout.EndArea();
    }
}