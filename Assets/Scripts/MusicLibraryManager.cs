using UnityEngine;
using System.Collections;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class MusicLibraryManager : MonoBehaviour
{
    public AudioReactivityManager audioManager;

    [Header("Bundled Tracks")]
    public AudioClip[] bundledTracks;
    public string[] bundledTrackNames;
    public string[] bundledTrackGenres;

    int _currentBundledIndex = 0;

    // ---- BUNDLED MUSIC ----

    public void PlayBundledTrack(int index)
    {
        if (index < 0 || index >= bundledTracks.Length) return;
        _currentBundledIndex = index;
        audioManager.PlayBundledTrack(bundledTracks[index]);
    }

    public void NextBundledTrack()
    {
        _currentBundledIndex = (_currentBundledIndex + 1) % bundledTracks.Length;
        PlayBundledTrack(_currentBundledIndex);
    }

    public void PreviousBundledTrack()
    {
        _currentBundledIndex = (_currentBundledIndex - 1 + bundledTracks.Length)
            % bundledTracks.Length;
        PlayBundledTrack(_currentBundledIndex);
    }

    public void ShuffleBundled()
    {
        int random = Random.Range(0, bundledTracks.Length);
        PlayBundledTrack(random);
    }

    // ---- DEVICE LOCAL MUSIC ----
    // iOS: uses NativeFilePicker or MediaPlayer framework via plugin
    // Android: uses MediaStore via plugin
    // Both handled through a Unity plugin — recommended: "Native Audio" or
    // "Unity Native Audio" from Asset Store, or roll your own via native plugins

    public void OpenDeviceMusicPicker()
    {
#if UNITY_IOS
        StartCoroutine(PickIOSMusic());
#elif UNITY_ANDROID
        StartCoroutine(PickAndroidMusic());
#endif
    }

    IEnumerator PickIOSMusic()
    {
        // iOS MediaPlayer framework access requires a native plugin
        // placeholder for when native plugin is integrated
        // plugin will call back with an AudioClip via OnDeviceTrackLoaded()
        Debug.Log("iOS music picker — needs native plugin");
        yield return null;
    }

    IEnumerator PickAndroidMusic()
    {
        // Android MediaStore access requires a native plugin
        Debug.Log("Android music picker — needs native plugin");
        yield return null;
    }

    // called by native plugin callback when user picks a track
    public void OnDeviceTrackLoaded(AudioClip clip)
    {
        audioManager.PlayLocalTrack(clip);
    }

    // ---- MIC FALLBACK ----

    public void SwitchToMicMode()
    {
        audioManager.StartMicMode();
    }
}