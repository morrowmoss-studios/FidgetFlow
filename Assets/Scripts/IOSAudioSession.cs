using System.Runtime.InteropServices;
using UnityEngine;

public static class IOSAudioSession
{
#if UNITY_IOS && !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern bool FidgetFlow_StartMicrophone();

    [DllImport("__Internal")]
    private static extern void FidgetFlow_StopMicrophone();

    [DllImport("__Internal")]
    private static extern int FidgetFlow_GetMicrophoneSamples(
        [Out] float[] destination,
        int sampleCount
    );

#endif

    public static bool StartMicrophone()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return FidgetFlow_StartMicrophone();
#else
        return false;
#endif
    }

    public static void StopMicrophone()
    {
#if UNITY_IOS && !UNITY_EDITOR
        FidgetFlow_StopMicrophone();
#endif
    }

    public static int GetMicrophoneSamples(
        float[] destination
    )
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (
            destination == null ||
            destination.Length == 0
        )
        {
            return 0;
        }

        return FidgetFlow_GetMicrophoneSamples(
            destination,
            destination.Length
        );
#else
        return 0;
#endif
    }
}