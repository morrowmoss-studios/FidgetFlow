using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BinauralBeatGenerator : MonoBehaviour
{
    public enum BeatPreset
    {
        Delta_2hz_DeepSleep,
        Theta_6hz_Meditation,
        Alpha_10hz_Relaxed,
        Theta_8hz_Creativity,
        Beta_20hz_Focus,
        Gamma_40hz_HighFocus,
        SolfeggioHeal_528hz,
        SolfeggioLove_639hz,
        SolfeggioAwaken_852hz,
        Custom
    }

    [Header("Preset")]
    public BeatPreset preset = BeatPreset.Alpha_10hz_Relaxed;

    [Header("Custom (only used if preset = Custom)")]
    public float baseFrequency = 200f;
    public float beatFrequency = 10f;

    [Header("Settings")]
    public float volume = 0.5f;
    public float fadeInSeconds = 3.0f;

    AudioSource _audioSource;
    float _sampleRate;
    float _phase;
    float _currentVolume = 0f;
    bool _playing = false;

    // preset definitions: base freq, beat freq
    static readonly (float baseHz, float beatHz)[] PresetValues = new[]
    {
        (100f, 2f),   // delta deep sleep
        (150f, 6f),   // theta meditation
        (200f, 10f),  // alpha relaxed
        (180f, 8f),   // theta creativity
        (220f, 20f),  // beta focus
        (200f, 40f),  // gamma high focus
        (528f, 0f),   // solfeggio heal — pure 528hz tone (no beat)
        (639f, 0f),   // solfeggio love
        (852f, 0f),   // solfeggio awaken
        (0f, 0f),     // custom placeholder
    };

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.spatialBlend = 0f;
        _sampleRate = AudioSettings.outputSampleRate;
        ApplyPreset();
        // don't auto start — user triggers this from UI
        _playing = false;
    }

    public void ApplyPreset()
    {
        if (preset != BeatPreset.Custom)
        {
            int idx = (int)preset;
            baseFrequency = PresetValues[idx].baseHz;
            beatFrequency = PresetValues[idx].beatHz;
        }
    }

    public void SetPreset(BeatPreset newPreset)
    {
        preset = newPreset;
        ApplyPreset();
    }

    public void StartPlaying()
    {
        _playing = true;
        _currentVolume = 0f;
    }

    public void StopPlaying()
    {
        _playing = false;
    }

    // Unity calls this automatically to fill audio buffer
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!_playing) return;

        float fadeSpeed = 1f / (fadeInSeconds * _sampleRate);

        for (int i = 0; i < data.Length; i += channels)
        {
            // fade in smoothly
            _currentVolume = Mathf.MoveTowards(_currentVolume, volume, fadeSpeed);

            _phase += 1f / _sampleRate;
            if (_phase > 1f) _phase -= 1f;

            // left channel: base frequency
            float left = Mathf.Sin(2f * Mathf.PI * baseFrequency * _phase);

            // right channel: base + beat frequency (creates the binaural effect)
            float rightFreq = beatFrequency > 0f
                ? baseFrequency + beatFrequency
                : baseFrequency;
            float right = Mathf.Sin(2f * Mathf.PI * rightFreq * _phase);

            if (channels >= 1) data[i] = left * _currentVolume;
            if (channels >= 2) data[i + 1] = right * _currentVolume;
        }
    }
}