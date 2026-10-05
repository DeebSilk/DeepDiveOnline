using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Mixer Parameters")]
    [SerializeField] private string masterParameter = "MasterVolume";
    [SerializeField] private string musicParameter = "MusicVolume";
    [SerializeField] private string sfxParameter = "SfxVolume";
    [SerializeField] private string ambientParameter = "AmbientVolume";

    [Header("Default Values")]
    [SerializeField, Range(0f, 1f)]
    private float defaultMasterVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float defaultMusicVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float defaultSfxVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float defaultAmbientVolume = 1f;

    private const string MasterKey = "Audio.Master";
    private const string MusicKey = "Audio.Music";
    private const string SFXKey = "Audio.SFX";
    private const string AmbientKey = "Audio.Ambient";

    private const float MinLinearVolume = 0.0001f;

    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    private float ambientVolume;

    private bool mixerWarningShown;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;
    public float AmbientVolume => ambientVolume;

    private void Awake()
    {
        Load();
        ApplyAll();
    }

    private void Load()
    {
        masterVolume = PlayerPrefs.GetFloat(
            MasterKey,
            Mathf.Clamp01(defaultMasterVolume)
        );

        musicVolume = PlayerPrefs.GetFloat(
            MusicKey,
            Mathf.Clamp01(defaultMusicVolume)
        );

        sfxVolume = PlayerPrefs.GetFloat(
            SFXKey,
            Mathf.Clamp01(defaultSfxVolume)
        );

        ambientVolume = PlayerPrefs.GetFloat(
            AmbientKey,
            Mathf.Clamp01(defaultAmbientVolume)
        );

        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        ambientVolume = Mathf.Clamp01(ambientVolume);
    }

    private void ApplyAll()
    {
        SetMixerValue(
            masterParameter,
            masterVolume
        );

        SetMixerValue(
            musicParameter,
            musicVolume
        );

        SetMixerValue(
            sfxParameter,
            sfxVolume
        );

        SetMixerValue(
            ambientParameter,
            ambientVolume
        );
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);

        Save(
            MasterKey,
            masterVolume
        );

        SetMixerValue(
            masterParameter,
            masterVolume
        );
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        Save(
            MusicKey,
            musicVolume
        );

        SetMixerValue(
            musicParameter,
            musicVolume
        );
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);

        Save(
            SFXKey,
            sfxVolume
        );

        SetMixerValue(
            sfxParameter,
            sfxVolume
        );
    }

    public void SetAmbientVolume(float value)
    {
        ambientVolume = Mathf.Clamp01(value);

        Save(
            AmbientKey,
            ambientVolume
        );

        SetMixerValue(
            ambientParameter,
            ambientVolume
        );
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    public float GetAmbientVolume()
    {
        return ambientVolume;
    }

    private void SetMixerValue(
        string parameter,
        float value
    )
    {
        if (audioMixer == null)
        {
            ShowMixerWarning(
                "AudioSettingsManager: Audio Mixer is not assigned."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(parameter))
        {
            ShowMixerWarning(
                "AudioSettingsManager: Mixer parameter name is empty."
            );

            return;
        }

        if (!audioMixer.GetFloat(
                parameter,
                out _))
        {
            ShowMixerWarning(
                $"AudioSettingsManager: Mixer parameter '{parameter}' was not found. " +
                "Make sure the parameter is Exposed in the Audio Mixer."
            );

            return;
        }

        float safeValue = Mathf.Clamp(
            value,
            MinLinearVolume,
            1f
        );

        float decibels =
            Mathf.Log10(safeValue) * 20f;

        audioMixer.SetFloat(
            parameter,
            decibels
        );
    }

    private void ShowMixerWarning(string message)
    {
        if (mixerWarningShown)
            return;

        mixerWarningShown = true;

        Debug.LogWarning(
            message,
            this
        );
    }

    private void Save(
        string key,
        float value
    )
    {
        PlayerPrefs.SetFloat(
            key,
            Mathf.Clamp01(value)
        );

        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        masterVolume =
            Mathf.Clamp01(defaultMasterVolume);

        musicVolume =
            Mathf.Clamp01(defaultMusicVolume);

        sfxVolume =
            Mathf.Clamp01(defaultSfxVolume);

        ambientVolume =
            Mathf.Clamp01(defaultAmbientVolume);

        Save(
            MasterKey,
            masterVolume
        );

        Save(
            MusicKey,
            musicVolume
        );

        Save(
            SFXKey,
            sfxVolume
        );

        Save(
            AmbientKey,
            ambientVolume
        );

        ApplyAll();
    }

    public void ReloadFromPlayerPrefs()
    {
        Load();
        ApplyAll();
    }
}