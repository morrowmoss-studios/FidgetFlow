using UnityEngine;
using System.Collections;

public class AudioReactivityManager : MonoBehaviour
{
    public enum AudioMode
    {
        Bundled,
        LocalDevice,
        Mic
    }

    [Header("Materials")]
    public Material[] reactableMaterials;

    [Header("Audio Sources")]
    public AudioSource bundledAudioSource;
    public AudioSource localAudioSource;

    [Header("Sensitivity")]
    public float bassSensitivity = 2.8f;
    public float midSensitivity = 2.2f;
    public float highSensitivity = 1.8f;

    [Header("Response Speed")]
    [Tooltip("How quickly values jump upward when sound gets louder.")]
    public float attackSpeed = 22.0f;

    [Tooltip("How quickly values fall after the sound gets quieter.")]
    public float decaySpeed = 9.0f;

    [Tooltip("Adds extra punch when a band suddenly increases.")]
    public float transientBoost = 0.35f;

    [Header("Beat Detection")]
    public float bpmMin = 60f;
    public float bpmMax = 180f;
    public float blobSizeMin = 0.3f;
    public float blobSizeMax = 1.2f;
    public float beatThreshold = 0.3f;
    public float bpmSmoothing = 0.5f;

    public AudioMode CurrentMode { get; private set; } = AudioMode.Bundled;

    public float _bass;
    public float _mid;
    public float _high;
    public float _energy;

    private float _previousTargetBass;
    private float _previousTargetMid;
    private float _previousTargetHigh;

    private float _detectedBPM = 120f;
    private float _smoothedBPM = 120f;
    private float _smoothedBlobSize = 0.7f;
    private float _lastBeatTime;
    private bool _wasAboveThreshold;

    private AudioClip _micClip;
    private string _micDevice;

    private bool _iosMicRestarting;

    private readonly float[] _spectrumData = new float[1024];
    private readonly float[] _micSamples = new float[1024];

    private void Start()
    {
        StartCoroutine(MonitorAudioRoute());
    }

    public void PlayBundledTrack(AudioClip clip)
    {
        if (clip == null || bundledAudioSource == null)
        {
            return;
        }

        StopMic();

        bundledAudioSource.clip = clip;
        bundledAudioSource.Play();

        CurrentMode = AudioMode.Bundled;
    }

    public void PlayLocalTrack(AudioClip clip)
    {
        if (clip == null || localAudioSource == null)
        {
            return;
        }

        StopMic();

        localAudioSource.clip = clip;
        localAudioSource.Play();

        CurrentMode = AudioMode.LocalDevice;
    }

    public void StartMicMode()
    {
        if (CurrentMode == AudioMode.Mic)
        {
            return;
        }

        CurrentMode = AudioMode.Mic;

        StopAllMusic();
        StartMic();
    }

    public void StopAllMusic()
    {
        if (bundledAudioSource != null)
        {
            bundledAudioSource.Stop();
        }

        if (localAudioSource != null)
        {
            localAudioSource.Stop();
        }
    }

    private void StartMic()
    {
#if UNITY_IOS && !UNITY_EDITOR

        bool started =
            IOSAudioSession.StartMicrophone();

        if (started)
        {
            Debug.Log(
                "[AudioReactivityManager] Native iOS microphone started."
            );
        }
        else
        {
            Debug.LogError(
                "[AudioReactivityManager] Native iOS microphone failed to start."
            );
        }

#else

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning(
                "No microphone device found."
            );

            return;
        }

        _micDevice =
            Microphone.devices[0];

        _micClip =
            Microphone.Start(
                _micDevice,
                true,
                1,
                AudioSettings.outputSampleRate
            );

#endif
    }

    private void StopMic()
    {
#if UNITY_IOS && !UNITY_EDITOR

        IOSAudioSession.StopMicrophone();

#else

        if (string.IsNullOrEmpty(_micDevice))
        {
            return;
        }

        Microphone.End(_micDevice);

        _micDevice = null;
        _micClip = null;

#endif
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (CurrentMode != AudioMode.Mic)
        {
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR

        if (pauseStatus)
        {
            Debug.Log(
                "[AudioReactivityManager] App paused — stopping native iOS microphone."
            );

            IOSAudioSession.StopMicrophone();
        }
        else
        {
            Debug.Log(
                "[AudioReactivityManager] App resumed — scheduling iOS microphone restart."
            );

            StartCoroutine(
                RestartIOSMicrophoneAfterResume()
            );
        }

#endif
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (
            !hasFocus ||
            CurrentMode != AudioMode.Mic
        )
        {
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR

        Debug.Log(
            "[AudioReactivityManager] App regained focus — checking iOS microphone."
        );

        StartCoroutine(
            RestartIOSMicrophoneAfterResume()
        );

#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    private IEnumerator RestartIOSMicrophoneAfterResume()
    {
        if (_iosMicRestarting)
        {
            yield break;
        }

        _iosMicRestarting = true;

        // Give iOS time to finish restoring the app/audio session.
        yield return null;
        yield return new WaitForSecondsRealtime(0.25f);

        IOSAudioSession.StopMicrophone();

        yield return null;

        bool started =
            IOSAudioSession.StartMicrophone();

        if (started)
        {
            Debug.Log(
                "[AudioReactivityManager] iOS microphone restarted after resume."
            );
        }
        else
        {
            Debug.LogError(
                "[AudioReactivityManager] Failed to restart iOS microphone after resume."
            );
        }

        _iosMicRestarting = false;
    }
#endif

    private IEnumerator MonitorAudioRoute()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);

            bool bundledPlaying =
                bundledAudioSource != null &&
                bundledAudioSource.isPlaying;

            bool localPlaying =
                localAudioSource != null &&
                localAudioSource.isPlaying;

            if (
                !bundledPlaying &&
                !localPlaying &&
                CurrentMode != AudioMode.Mic
            )
            {
                StartMicMode();
            }
        }
    }

    private void Update()
    {
        switch (CurrentMode)
        {
            case AudioMode.Bundled:
                AnalyzeAudioSource(bundledAudioSource);
                break;

            case AudioMode.LocalDevice:
                AnalyzeAudioSource(localAudioSource);
                break;

            case AudioMode.Mic:
                AnalyzeMic();
                break;
        }

        PushToShaders();
    }

    private void AnalyzeAudioSource(AudioSource source)
    {
        if (source == null || !source.isPlaying)
        {
            DecayTowardSilence();
            return;
        }

        source.GetSpectrumData(
            _spectrumData,
            0,
            FFTWindow.BlackmanHarris
        );

        ProcessSpectrum();
    }

    private void AnalyzeMic()
    {
#if UNITY_IOS && !UNITY_EDITOR

        int samplesRead =
            IOSAudioSession.GetMicrophoneSamples(
                _micSamples
            );

        if (samplesRead <= 0)
        {
            DecayTowardSilence();
            return;
        }

        for (int i = 0; i < _micSamples.Length; i++)
        {
            _spectrumData[i] =
                Mathf.Abs(
                    _micSamples[i]
                );
        }

        ProcessSpectrum();

#else

        if (
            _micClip == null ||
            string.IsNullOrEmpty(_micDevice)
        )
        {
            DecayTowardSilence();
            return;
        }

        int micPosition =
            Microphone.GetPosition(
                _micDevice
            );

        if (micPosition < _micSamples.Length)
        {
            return;
        }

        _micClip.GetData(
            _micSamples,
            micPosition - _micSamples.Length
        );

        for (int i = 0; i < _micSamples.Length; i++)
        {
            _spectrumData[i] =
                Mathf.Abs(
                    _micSamples[i]
                );
        }

        ProcessSpectrum();

#endif
    }

    private void ProcessSpectrum()
    {
        float rawBass = 0f;
        float rawMid = 0f;
        float rawHigh = 0f;

        for (int i = 0; i < 10; i++)
        {
            rawBass += _spectrumData[i];
        }

        for (int i = 10; i < 100; i++)
        {
            rawMid += _spectrumData[i];
        }

        for (int i = 100; i < 512; i++)
        {
            rawHigh += _spectrumData[i];
        }

        rawBass /= 10f;
        rawMid /= 90f;
        rawHigh /= 412f;

        float targetBass =
            Mathf.Clamp01(
                rawBass *
                bassSensitivity
            );

        float targetMid =
            Mathf.Clamp01(
                rawMid *
                midSensitivity
            );

        float targetHigh =
            Mathf.Clamp01(
                rawHigh *
                highSensitivity
            );

        float bassTransient =
            Mathf.Max(
                0f,
                targetBass -
                _previousTargetBass
            );

        float midTransient =
            Mathf.Max(
                0f,
                targetMid -
                _previousTargetMid
            );

        float highTransient =
            Mathf.Max(
                0f,
                targetHigh -
                _previousTargetHigh
            );

        targetBass =
            Mathf.Clamp01(
                targetBass +
                bassTransient *
                transientBoost
            );

        targetMid =
            Mathf.Clamp01(
                targetMid +
                midTransient *
                transientBoost
            );

        targetHigh =
            Mathf.Clamp01(
                targetHigh +
                highTransient *
                transientBoost
            );

        _previousTargetBass =
            targetBass;

        _previousTargetMid =
            targetMid;

        _previousTargetHigh =
            targetHigh;

        _bass =
            SmoothBand(
                _bass,
                targetBass
            );

        _mid =
            SmoothBand(
                _mid,
                targetMid
            );

        _high =
            SmoothBand(
                _high,
                targetHigh
            );

        float targetEnergy =
            Mathf.Clamp01(
                _bass * 0.50f +
                _mid * 0.30f +
                _high * 0.20f
            );

        _energy =
            SmoothBand(
                _energy,
                targetEnergy
            );

        DetectBeat();
    }

    private float SmoothBand(
        float current,
        float target
    )
    {
        float speed =
            target > current
                ? attackSpeed
                : decaySpeed;

        float interpolation =
            1f -
            Mathf.Exp(
                -speed *
                Time.unscaledDeltaTime
            );

        return Mathf.Lerp(
            current,
            target,
            interpolation
        );
    }

    private void DecayTowardSilence()
    {
        _bass =
            SmoothBand(
                _bass,
                0f
            );

        _mid =
            SmoothBand(
                _mid,
                0f
            );

        _high =
            SmoothBand(
                _high,
                0f
            );

        _energy =
            SmoothBand(
                _energy,
                0f
            );
    }

    private void DetectBeat()
    {
        float combinedSignal =
            _bass * 0.45f +
            _mid * 0.35f +
            _energy * 0.20f;

        bool aboveThreshold =
            combinedSignal >
            beatThreshold;

        if (
            aboveThreshold &&
            !_wasAboveThreshold
        )
        {
            float now =
                Time.time;

            float interval =
                now -
                _lastBeatTime;

            if (
                interval > 0.25f &&
                interval < 1.5f
            )
            {
                float measuredBPM =
                    60f /
                    interval;

                _detectedBPM =
                    Mathf.Lerp(
                        _detectedBPM,
                        measuredBPM,
                        0.25f
                    );
            }

            _lastBeatTime =
                now;
        }

        _wasAboveThreshold =
            aboveThreshold;

        float bpmInterpolation =
            1f -
            Mathf.Exp(
                -bpmSmoothing *
                Time.unscaledDeltaTime
            );

        _smoothedBPM =
            Mathf.Lerp(
                _smoothedBPM,
                _detectedBPM,
                bpmInterpolation
            );

        float bpmNormalized =
            Mathf.InverseLerp(
                bpmMin,
                bpmMax,
                _smoothedBPM
            );

        float targetBlobSize =
            Mathf.Lerp(
                blobSizeMin,
                blobSizeMax,
                bpmNormalized
            );

        float blobInterpolation =
            1f -
            Mathf.Exp(
                -4f *
                Time.unscaledDeltaTime
            );

        _smoothedBlobSize =
            Mathf.Lerp(
                _smoothedBlobSize,
                targetBlobSize,
                blobInterpolation
            );
    }

    private void PushToShaders()
    {
        if (reactableMaterials == null)
        {
            return;
        }

        foreach (
            Material material
            in reactableMaterials
        )
        {
            if (material == null)
            {
                continue;
            }

            material.SetFloat(
                "_AudioBass",
                _bass
            );

            material.SetFloat(
                "_AudioMid",
                _mid
            );

            material.SetFloat(
                "_AudioHigh",
                _high
            );

            material.SetFloat(
                "_AudioEnergy",
                _energy
            );

            material.SetFloat(
                "_BlobSize",
                _smoothedBlobSize
            );
        }
    }
}