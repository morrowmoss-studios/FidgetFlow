using System.Runtime.InteropServices;
using UnityEngine;

public static class IOSAudioSession
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void FidgetFlow_EnableMixedRecording();
#endif

    public static void EnableMixedRecording()
    {
#if UNITY_IOS && !UNITY_EDITOR
        FidgetFlow_EnableMixedRecording();
        Debug.Log(
            "[IOSAudioSession] Mixed recording enabled."
        );
#endif
    }
}