using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AudioReactivityManager : MonoBehaviour
{
    public enum AudioMode { Bundled, LocalDevice, Mic }

    [Header("Materials")]
    public Material[] reactableMaterials;

    [Header("Audio Sources")]
    public AudioSource bundledAudioSource;    // for your bundled tracks
    public AudioSource localAudioSource;      // for device library tracks

    [Header("Sensitivity")]
    public float bassSensitivity = 2.0f;
    public float midSensitivity = 1.5f;
    public float highSensitivity = 1.0f;
    public float smoothing = 5.0f;

    // public so UI can read current mode
    public AudioMode CurrentMode { get; private set; } = AudioMode.Bundled;

    public float _bass, _mid, _high, _energy;

    // mic
    AudioClip _micClip;
    string _micDevice;

    // spectrum data
    float[] _spectrumData = new float[1024];

    void Start()
    {
        StartCoroutine(MonitorAudioRoute());
    }

    // called by UI when user picks a bundled track
    public void PlayBundledTrack(AudioClip clip)
    {
        StopMic();
        bundledAudioSource.clip = clip;
        bundledAudioSource.Play();
        CurrentMode = AudioMode.Bundled;
    }

    // called by UI when user picks a local device track
    public void PlayLocalTrack(AudioClip clip)
    {
        StopMic();
        localAudioSource.clip = clip;
        localAudioSource.Play();
        CurrentMode = AudioMode.LocalDevice;
    }

    // called when no music is playing through Unity — fall back to mic
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

    // auto switch to mic if Unity audio stops playing
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

        // get spectrum data directly from AudioSource — perfect accuracy
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
        // bass: bins 0-10 (~0-430hz) — kick, bass guitar, low synth
        // mid: bins 10-100 (~430hz-4.3khz) — snare, vocals, melody
        // high: bins 100-512 (~4.3khz+) — hi-hats, cymbals, air
        float rawBass = 0, rawMid = 0, rawHigh = 0;

        for (int i = 0; i < 10; i++) rawBass += _spectrumData[i];
        for (int i = 10; i < 100; i++) rawMid += _spectrumData[i];
        for (int i = 100; i < 512; i++) rawHigh += _spectrumData[i];

        rawBass /= 10f;
        rawMid /= 90f;
        rawHigh /= 412f;

        float dt = Time.deltaTime * smoothing;
        _bass = Mathf.Lerp(_bass, Mathf.Clamp01(rawBass * bassSensitivity), dt);
        _mid = Mathf.Lerp(_mid, Mathf.Clamp01(rawMid * midSensitivity), dt);
        _high = Mathf.Lerp(_high, Mathf.Clamp01(rawHigh * highSensitivity), dt);
        _energy = (_bass + _mid * 0.6f + _high * 0.4f) / 2f;
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
        }
    }
}