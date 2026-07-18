using UnityEngine;
using System.Collections;

public class AudioReactivityManager : MonoBehaviour
{
    public enum AudioMode { Bundled, LocalDevice, Mic }

    [Header("Materials")]
    public Material[] reactableMaterials;

    [Header("Audio Sources")]
    public AudioSource bundledAudioSource;
    public AudioSource localAudioSource;

    [Header("Sensitivity")]
    public float bassSensitivity = 2.0f;
    public float midSensitivity = 1.5f;
    public float highSensitivity = 1.0f;
    public float smoothing = 5.0f;

    [Header("Beat Detection")]
    public float bpmMin = 60f;
    public float bpmMax = 180f;
    public float blobSizeMin = 0.3f;
    public float blobSizeMax = 1.2f;
    public float beatThreshold = 0.3f;
    public float bpmSmoothing = 0.5f;

    public AudioMode CurrentMode { get; private set; } = AudioMode.Bundled;

    public float _bass, _mid, _high, _energy;

    float _detectedBPM = 120f;
    float _smoothedBPM = 120f;
    float _smoothedBlobSize = 0.7f;
    float _lastBeatTime = 0f;
    bool _wasAboveThreshold = false;

    AudioClip _micClip;
    string _micDevice;
    float[] _spectrumData = new float[1024];

    void Start()
    {
        StartCoroutine(MonitorAudioRoute());
    }

    public void PlayBundledTrack(AudioClip clip)
    {
        StopMic();
        bundledAudioSource.clip = clip;
        bundledAudioSource.Play();
        CurrentMode = AudioMode.Bundled;
    }

    public void PlayLocalTrack(AudioClip clip)
    {
        StopMic();
        localAudioSource.clip = clip;
        localAudioSource.Play();
        CurrentMode = AudioMode.LocalDevice;
    }

    public void StartMicMode()
    {
        if (CurrentMode == AudioMode.Mic) return;
        CurrentMode = AudioMode.Mic;
        StopAllMusic();
        StartMic();
    }

    public void StopAllMusic()
    {
        if (bundledAudioSource != null) bundledAudioSource.Stop();
        if (localAudioSource != null) localAudioSource.Stop();
    }

    void StartMic()
    {
        if (Microphone.devices.Length == 0) return;
        _micDevice = Microphone.devices[0];
        _micClip = Microphone.Start(_micDevice, true, 1, 44100);
    }

    void StopMic()
    {
        if (_micDevice != null)
        {
            Microphone.End(_micDevice);
            _micDevice = null;
            _micClip = null;
        }
    }

    IEnumerator MonitorAudioRoute()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);

            bool bundledPlaying = bundledAudioSource != null && bundledAudioSource.isPlaying;
            bool localPlaying = localAudioSource != null && localAudioSource.isPlaying;

            if (!bundledPlaying && !localPlaying && CurrentMode != AudioMode.Mic)
            {
                StartMicMode();
            }
        }
    }

    void Update()
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

    void AnalyzeAudioSource(AudioSource source)
    {
        if (source == null || !source.isPlaying) return;
        source.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);
        ProcessSpectrum();
    }

    void AnalyzeMic()
    {
        if (_micClip == null) return;

        int micPos = Microphone.GetPosition(_micDevice);
        if (micPos < _spectrumData.Length) return;

        float[] samples = new float[_spectrumData.Length];
        _micClip.GetData(samples, micPos - _spectrumData.Length);

        for (int i = 0; i < samples.Length; i++)
            _spectrumData[i] = Mathf.Abs(samples[i]);

        ProcessSpectrum();
    }

    void ProcessSpectrum()
    {
        float rawBass = 0, rawMid = 0, rawHigh = 0;

        for (int i = 0; i < 10; i++) rawBass += _spectrumData[i];
        for (int i = 10; i < 100; i++) rawMid += _spectrumData[i];
        for (int i = 100; i < 512; i++) rawHigh += _spectrumData[i];

        rawBass /= 10f;
        rawMid /= 90f;
        rawHigh /= 412f;

        float targetBass = Mathf.Clamp01(rawBass * bassSensitivity);
        float targetMid = Mathf.Clamp01(rawMid * midSensitivity);
        float targetHigh = Mathf.Clamp01(rawHigh * highSensitivity);

        // attack fast, decay slow — snappy response going up, smooth fade going down
        float attackDt = Time.deltaTime * smoothing;
        float decayDt = Time.deltaTime * (smoothing * 0.1f);

        _bass = Mathf.Lerp(_bass, targetBass, targetBass > _bass ? attackDt : decayDt);
        _mid = Mathf.Lerp(_mid, targetMid, targetMid > _mid ? attackDt : decayDt);
        _high = Mathf.Lerp(_high, targetHigh, targetHigh > _high ? attackDt : decayDt);
        _energy = (_bass + _mid * 0.6f + _high * 0.4f) / 2f;

        DetectBeat();
    }

    void DetectBeat()
    {
        // use energy instead of just bass so classical/ambient still triggers
        float combinedSignal = (_bass * 0.4f + _mid * 0.4f + _energy * 0.2f);
        bool aboveThreshold = combinedSignal > beatThreshold;

        if (aboveThreshold && !_wasAboveThreshold)
        {
            float now = Time.time;
            float interval = now - _lastBeatTime;

            if (interval > 0.25f && interval < 1.5f)
            {
                float measuredBPM = 60f / interval;
                // raw BPM snaps toward measurement
                _detectedBPM = Mathf.Lerp(_detectedBPM, measuredBPM, 0.25f);
            }

            _lastBeatTime = now;
        }

        _wasAboveThreshold = aboveThreshold;

        // smooth BPM separately so blob size changes are gradual not jumpy
        _smoothedBPM = Mathf.Lerp(_smoothedBPM, _detectedBPM, Time.deltaTime * bpmSmoothing);

        // map smoothed BPM to blob size
        float bpmNormalized = Mathf.InverseLerp(bpmMin, bpmMax, _smoothedBPM);
        float targetBlobSize = Mathf.Lerp(blobSizeMin, blobSizeMax, bpmNormalized);

        // smooth blob size change independently — this is what makes it fluid
        _smoothedBlobSize = Mathf.Lerp(_smoothedBlobSize, targetBlobSize, Time.deltaTime * 1.5f);
    }

    void PushToShaders()
    {
        foreach (Material mat in reactableMaterials)
        {
            if (mat == null) continue;
            mat.SetFloat("_AudioBass", _bass);
            mat.SetFloat("_AudioMid", _mid);
            mat.SetFloat("_AudioHigh", _high);
            mat.SetFloat("_AudioEnergy", _energy);
            mat.SetFloat("_BlobSize", _smoothedBlobSize);
        }
    }
}