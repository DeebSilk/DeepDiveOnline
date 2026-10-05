using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    // =========================================================
    // CURRENT SETTINGS
    // =========================================================

    public static float CurrentVolume { get; private set; } = 1f;
    public static float CurrentMusicVolume { get; private set; } = 1f;
    public static float CurrentSfxVolume { get; private set; } = 1f;
    public static float CurrentSensitivity { get; private set; } = 2f;
    public static bool CurrentFullscreen { get; private set; } = true;
    public static int CurrentQualityLevel { get; private set; } = 2;
    public static int CurrentResolutionIndex { get; private set; }
    public static bool CurrentVSync { get; private set; } = true;

    // =========================================================
    // PLAYER PREFS KEYS
    // =========================================================

    private const string VolumeKey = "Volume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string SensitivityKey = "Sensitivity";
    private const string FullscreenKey = "Fullscreen";
    private const string QualityKey = "GraphicsQuality";
    private const string ResolutionKey = "ResolutionIndex";
    private const string VSyncKey = "VSync";

    // =========================================================
    // AUDIO
    // =========================================================

    /*
     * ВАЖНО:
     *
     * GameSettings больше НЕ управляет AudioMixer напрямую.
     *
     * Вся аудиосистема находится здесь:
     *
     * AudioManager
     *      └── Settings
     *              ├── Master
     *              ├── Music
     *              ├── SFX
     *              └── Ambient
     *
     * Поэтому AudioMixer здесь намеренно отсутствует.
     */

    // =========================================================
    // UI
    // =========================================================

    [Header("Audio UI")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Camera")]
    [SerializeField] private Slider sensitivitySlider;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Dropdown qualityDropdown;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Toggle vSyncToggle;

    // =========================================================
    // DEFAULTS
    // =========================================================

    [Header("Defaults")]
    [SerializeField, Range(0f, 1f)]
    private float defaultVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float defaultMusicVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float defaultSfxVolume = 1f;

    [SerializeField, Range(0.1f, 10f)]
    private float defaultSensitivity = 2f;

    [SerializeField]
    private bool defaultFullscreen = true;

    [SerializeField]
    private int defaultQualityLevel = 2;

    [SerializeField]
    private bool defaultVSync = true;

    // =========================================================
    // RESOLUTIONS
    // =========================================================

    private Resolution[] availableResolutions;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ValidateReferences();
        SetupUIListeners();
    }

    private void OnEnable()
    {
        SetupUIListeners();
    }

    private void OnDisable()
    {
        RemoveUIListeners();
    }

    private void Start()
    {
        LoadSettings();
    }

    private void OnDestroy()
    {
        RemoveUIListeners();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    // =========================================================
    // AUDIO
    // =========================================================

    public void SetVolume(float value)
    {
        float volume = Mathf.Clamp01(value);

        CurrentVolume = volume;

        PlayerPrefs.SetFloat(
            VolumeKey,
            volume
        );

        PlayerPrefs.Save();

        AudioSettingsManager audioSettings =
            GetAudioSettingsManager();

        if (audioSettings != null)
        {
            audioSettings.SetMasterVolume(volume);
        }
    }

    public void SetMusicVolume(float value)
    {
        float volume = Mathf.Clamp01(value);

        CurrentMusicVolume = volume;

        PlayerPrefs.SetFloat(
            MusicVolumeKey,
            volume
        );

        PlayerPrefs.Save();

        AudioSettingsManager audioSettings =
            GetAudioSettingsManager();

        if (audioSettings != null)
        {
            audioSettings.SetMusicVolume(volume);
        }
    }

    public void SetSfxVolume(float value)
    {
        float volume = Mathf.Clamp01(value);

        CurrentSfxVolume = volume;

        PlayerPrefs.SetFloat(
            SfxVolumeKey,
            volume
        );

        PlayerPrefs.Save();

        AudioSettingsManager audioSettings =
            GetAudioSettingsManager();

        if (audioSettings != null)
        {
            audioSettings.SetSFXVolume(volume);
        }
    }

    private AudioSettingsManager GetAudioSettingsManager()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "GameSettings: AudioManager is not available.",
                this
            );

            return null;
        }

        if (AudioManager.Instance.Settings == null)
        {
            Debug.LogWarning(
                "GameSettings: AudioSettingsManager is not available.",
                this
            );

            return null;
        }

        return AudioManager.Instance.Settings;
    }

    // =========================================================
    // CAMERA SENSITIVITY
    // =========================================================

    public void SetSensitivity(float value)
    {
        ApplySensitivity(value);
    }

    private void ApplySensitivity(float value)
    {
        float sensitivity =
            Mathf.Clamp(
                value,
                0.1f,
                10f
            );

        CurrentSensitivity = sensitivity;

        PlayerPrefs.SetFloat(
            SensitivityKey,
            sensitivity
        );

        PlayerPrefs.Save();

        GenshinCamera[] cameras =
            FindObjectsByType<GenshinCamera>(
                FindObjectsInactive.Exclude
            );

        foreach (GenshinCamera camera in cameras)
        {
            if (camera != null)
            {
                camera.ApplySensitivity(
                    sensitivity
                );
            }
        }
    }

    // =========================================================
    // FULLSCREEN
    // =========================================================

    public void SetFullscreen(bool isFullscreen)
    {
        CurrentFullscreen = isFullscreen;

        PlayerPrefs.SetInt(
            FullscreenKey,
            isFullscreen ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplyFullscreen(isFullscreen);
    }

    private void ApplyFullscreen(bool isFullscreen)
    {
        Resolution selectedResolution =
            Screen.currentResolution;

        if (availableResolutions != null &&
            availableResolutions.Length > 0 &&
            CurrentResolutionIndex >= 0 &&
            CurrentResolutionIndex < availableResolutions.Length)
        {
            selectedResolution =
                availableResolutions[
                    CurrentResolutionIndex
                ];
        }

        Screen.SetResolution(
            selectedResolution.width,
            selectedResolution.height,
            GetFullscreenMode(isFullscreen),
            GetRefreshRate(selectedResolution)
        );
    }

    // =========================================================
    // QUALITY
    // =========================================================

    public void SetQuality(int qualityIndex)
    {
        if (QualitySettings.names == null ||
            QualitySettings.names.Length == 0)
        {
            return;
        }

        qualityIndex =
            Mathf.Clamp(
                qualityIndex,
                0,
                QualitySettings.names.Length - 1
            );

        CurrentQualityLevel =
            qualityIndex;

        PlayerPrefs.SetInt(
            QualityKey,
            qualityIndex
        );

        PlayerPrefs.Save();

        QualitySettings.SetQualityLevel(
            qualityIndex
        );

        ApplyQualityPreset(
            qualityIndex
        );
    }

    // =========================================================
    // RESOLUTION
    // =========================================================

    public void SetResolution(int resolutionIndex)
    {
        if (availableResolutions == null ||
            availableResolutions.Length == 0)
        {
            return;
        }

        if (resolutionIndex < 0 ||
            resolutionIndex >= availableResolutions.Length)
        {
            return;
        }

        CurrentResolutionIndex =
            resolutionIndex;

        PlayerPrefs.SetInt(
            ResolutionKey,
            resolutionIndex
        );

        PlayerPrefs.Save();

        ApplyResolution(
            resolutionIndex
        );
    }

    private void ApplyResolution(
        int resolutionIndex
    )
    {
        if (availableResolutions == null ||
            availableResolutions.Length == 0)
        {
            return;
        }

        resolutionIndex =
            Mathf.Clamp(
                resolutionIndex,
                0,
                availableResolutions.Length - 1
            );

        Resolution selected =
            availableResolutions[
                resolutionIndex
            ];

        Screen.SetResolution(
            selected.width,
            selected.height,
            GetFullscreenMode(
                CurrentFullscreen
            ),
            GetRefreshRate(selected)
        );
    }

    // =========================================================
    // VSYNC
    // =========================================================

    public void SetVSync(bool isEnabled)
    {
        CurrentVSync = isEnabled;

        QualitySettings.vSyncCount =
            isEnabled ? 1 : 0;

        PlayerPrefs.SetInt(
            VSyncKey,
            isEnabled ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetToDefaults()
    {
        SetVolume(defaultVolume);

        SetMusicVolume(
            defaultMusicVolume
        );

        SetSfxVolume(
            defaultSfxVolume
        );

        SetSensitivity(
            defaultSensitivity
        );

        SetFullscreen(
            defaultFullscreen
        );

        SetQuality(
            Mathf.Clamp(
                defaultQualityLevel,
                0,
                Mathf.Max(
                    0,
                    QualitySettings.names.Length - 1
                )
            )
        );

        SetVSync(
            defaultVSync
        );

        ApplyDefaultResolution();

        UpdateUI();
    }

    // =========================================================
    // LOAD SETTINGS
    // =========================================================

    private void LoadSettings()
    {
        PopulateGraphicsOptions();

        // -----------------------------
        // AUDIO
        // -----------------------------

        CurrentVolume =
            Mathf.Clamp01(
                PlayerPrefs.GetFloat(
                    VolumeKey,
                    defaultVolume
                )
            );

        CurrentMusicVolume =
            Mathf.Clamp01(
                PlayerPrefs.GetFloat(
                    MusicVolumeKey,
                    defaultMusicVolume
                )
            );

        CurrentSfxVolume =
            Mathf.Clamp01(
                PlayerPrefs.GetFloat(
                    SfxVolumeKey,
                    defaultSfxVolume
                )
            );

        // -----------------------------
        // CAMERA
        // -----------------------------

        CurrentSensitivity =
            Mathf.Clamp(
                PlayerPrefs.GetFloat(
                    SensitivityKey,
                    defaultSensitivity
                ),
                0.1f,
                10f
            );

        // -----------------------------
        // DISPLAY
        // -----------------------------

        CurrentFullscreen =
            PlayerPrefs.GetInt(
                FullscreenKey,
                defaultFullscreen ? 1 : 0
            ) == 1;

        int quality =
            PlayerPrefs.GetInt(
                QualityKey,
                defaultQualityLevel
            );

        if (QualitySettings.names.Length > 0)
        {
            CurrentQualityLevel =
                Mathf.Clamp(
                    quality,
                    0,
                    QualitySettings.names.Length - 1
                );
        }
        else
        {
            CurrentQualityLevel = 0;
        }

        int defaultResolutionIndex =
            availableResolutions != null &&
            availableResolutions.Length > 0
                ? availableResolutions.Length - 1
                : 0;

        int resolutionIndex =
            PlayerPrefs.GetInt(
                ResolutionKey,
                defaultResolutionIndex
            );

        if (availableResolutions != null &&
            availableResolutions.Length > 0)
        {
            CurrentResolutionIndex =
                Mathf.Clamp(
                    resolutionIndex,
                    0,
                    availableResolutions.Length - 1
                );
        }
        else
        {
            CurrentResolutionIndex = 0;
        }

        CurrentVSync =
            PlayerPrefs.GetInt(
                VSyncKey,
                defaultVSync ? 1 : 0
            ) == 1;

        // =====================================================
        // APPLY AUDIO
        // =====================================================

        AudioSettingsManager audioSettings =
            GetAudioSettingsManager();

        if (audioSettings != null)
        {
            audioSettings.SetMasterVolume(
                CurrentVolume
            );

            audioSettings.SetMusicVolume(
                CurrentMusicVolume
            );

            audioSettings.SetSFXVolume(
                CurrentSfxVolume
            );
        }

        // =====================================================
        // APPLY DISPLAY
        // =====================================================

        QualitySettings.SetQualityLevel(
            CurrentQualityLevel
        );

        ApplyQualityPreset(
            CurrentQualityLevel
        );

        ApplyFullscreen(
            CurrentFullscreen
        );

        ApplyResolution(
            CurrentResolutionIndex
        );

        QualitySettings.vSyncCount =
            CurrentVSync ? 1 : 0;

        // =====================================================
        // APPLY CAMERA
        // =====================================================

        ApplySensitivity(
            CurrentSensitivity
        );

        // =====================================================
        // UPDATE UI
        // =====================================================

        UpdateUI();
    }

    // =========================================================
    // QUALITY PRESETS
    // =========================================================

    private void ApplyQualityPreset(
        int qualityIndex
    )
    {
        switch (qualityIndex)
        {
            case 0:

                QualitySettings.antiAliasing = 0;
                QualitySettings.shadowDistance = 25f;
                QualitySettings.globalTextureMipmapLimit = 2;
                QualitySettings.pixelLightCount = 1;
                QualitySettings.shadows =
                    ShadowQuality.HardOnly;

                break;

            case 1:

                QualitySettings.antiAliasing = 2;
                QualitySettings.shadowDistance = 50f;
                QualitySettings.globalTextureMipmapLimit = 1;
                QualitySettings.pixelLightCount = 2;
                QualitySettings.shadows =
                    ShadowQuality.HardOnly;

                break;

            case 2:

                QualitySettings.antiAliasing = 4;
                QualitySettings.shadowDistance = 80f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.pixelLightCount = 4;
                QualitySettings.shadows =
                    ShadowQuality.All;

                break;

            default:

                QualitySettings.antiAliasing = 8;
                QualitySettings.shadowDistance = 120f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.pixelLightCount = 6;
                QualitySettings.shadows =
                    ShadowQuality.All;

                break;
        }
    }

    // =========================================================
    // RESOLUTION OPTIONS
    // =========================================================

    private void PopulateGraphicsOptions()
    {
        availableResolutions =
            Screen.resolutions;

        // -----------------------------
        // RESOLUTION DROPDOWN
        // -----------------------------

        if (resolutionDropdown != null &&
            availableResolutions != null &&
            availableResolutions.Length > 0)
        {
            resolutionDropdown.ClearOptions();

            List<string> options =
                new List<string>();

            int currentIndex = 0;

            for (int i = 0;
                 i < availableResolutions.Length;
                 i++)
            {
                Resolution resolution =
                    availableResolutions[i];

                float refreshRate =
                    (float)
                    resolution.refreshRateRatio.numerator /
                    resolution.refreshRateRatio.denominator;

                options.Add(
                    $"{resolution.width} x {resolution.height} @{refreshRate:0.#}Hz"
                );

                if (
                    resolution.width ==
                    Screen.currentResolution.width &&

                    resolution.height ==
                    Screen.currentResolution.height &&

                    resolution.refreshRateRatio.numerator ==
                    Screen.currentResolution.refreshRateRatio.numerator &&

                    resolution.refreshRateRatio.denominator ==
                    Screen.currentResolution.refreshRateRatio.denominator
                )
                {
                    currentIndex = i;
                }
            }

            resolutionDropdown.AddOptions(
                options
            );

            resolutionDropdown.value =
                currentIndex;

            resolutionDropdown.RefreshShownValue();
        }

        // -----------------------------
        // QUALITY DROPDOWN
        // -----------------------------

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();

            qualityDropdown.AddOptions(
                new List<string>(
                    QualitySettings.names
                )
            );

            if (QualitySettings.names.Length > 0)
            {
                qualityDropdown.value =
                    QualitySettings.GetQualityLevel();
            }

            qualityDropdown.RefreshShownValue();
        }
    }

    // =========================================================
    // DEFAULT RESOLUTION
    // =========================================================

    private void ApplyDefaultResolution()
    {
        if (availableResolutions == null ||
            availableResolutions.Length == 0)
        {
            return;
        }

        int defaultIndex =
            availableResolutions.Length - 1;

        CurrentResolutionIndex =
            defaultIndex;

        ApplyResolution(
            defaultIndex
        );
    }

    // =========================================================
    // FULLSCREEN MODE
    // =========================================================

    private static FullScreenMode GetFullscreenMode(
        bool isFullscreen
    )
    {
        return isFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    // =========================================================
    // REFRESH RATE
    // =========================================================

    private static RefreshRate GetRefreshRate(
        Resolution resolution
    )
    {
        return resolution.refreshRateRatio;
    }

    // =========================================================
    // UPDATE UI
    // =========================================================

    private void UpdateUI()
    {
        if (volumeSlider != null)
            volumeSlider.value =
                CurrentVolume;

        if (musicVolumeSlider != null)
            musicVolumeSlider.value =
                CurrentMusicVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value =
                CurrentSfxVolume;

        if (sensitivitySlider != null)
            sensitivitySlider.value =
                CurrentSensitivity;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn =
                CurrentFullscreen;

        if (qualityDropdown != null)
        {
            qualityDropdown.value =
                CurrentQualityLevel;

            qualityDropdown.RefreshShownValue();
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.value =
                CurrentResolutionIndex;

            resolutionDropdown.RefreshShownValue();
        }

        if (vSyncToggle != null)
            vSyncToggle.isOn =
                CurrentVSync;
    }

    // =========================================================
    // UI LISTENERS
    // =========================================================

    private void SetupUIListeners()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged
                .RemoveListener(SetVolume);

            volumeSlider.onValueChanged
                .AddListener(SetVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged
                .RemoveListener(SetMusicVolume);

            musicVolumeSlider.onValueChanged
                .AddListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged
                .RemoveListener(SetSfxVolume);

            sfxVolumeSlider.onValueChanged
                .AddListener(SetSfxVolume);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged
                .RemoveListener(SetSensitivity);

            sensitivitySlider.onValueChanged
                .AddListener(SetSensitivity);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged
                .RemoveListener(SetFullscreen);

            fullscreenToggle.onValueChanged
                .AddListener(SetFullscreen);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged
                .RemoveListener(SetQuality);

            qualityDropdown.onValueChanged
                .AddListener(SetQuality);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged
                .RemoveListener(SetResolution);

            resolutionDropdown.onValueChanged
                .AddListener(SetResolution);
        }

        if (vSyncToggle != null)
        {
            vSyncToggle.onValueChanged
                .RemoveListener(SetVSync);

            vSyncToggle.onValueChanged
                .AddListener(SetVSync);
        }
    }

    private void RemoveUIListeners()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged
                .RemoveListener(SetVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged
                .RemoveListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged
                .RemoveListener(SetSfxVolume);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged
                .RemoveListener(SetSensitivity);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged
                .RemoveListener(SetFullscreen);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged
                .RemoveListener(SetQuality);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged
                .RemoveListener(SetResolution);
        }

        if (vSyncToggle != null)
        {
            vSyncToggle.onValueChanged
                .RemoveListener(SetVSync);
        }
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private void ValidateReferences()
    {
        if (volumeSlider == null)
        {
            Debug.LogWarning(
                "GameSettings: Volume slider is not assigned.",
                this
            );
        }

        if (musicVolumeSlider == null)
        {
            Debug.LogWarning(
                "GameSettings: Music volume slider is not assigned.",
                this
            );
        }

        if (sfxVolumeSlider == null)
        {
            Debug.LogWarning(
                "GameSettings: SFX volume slider is not assigned.",
                this
            );
        }

        if (sensitivitySlider == null)
        {
            Debug.LogWarning(
                "GameSettings: Sensitivity slider is not assigned.",
                this
            );
        }

        if (fullscreenToggle == null)
        {
            Debug.LogWarning(
                "GameSettings: Fullscreen toggle is not assigned.",
                this
            );
        }

        if (qualityDropdown == null)
        {
            Debug.LogWarning(
                "GameSettings: Quality dropdown is not assigned.",
                this
            );
        }

        if (resolutionDropdown == null)
        {
            Debug.LogWarning(
                "GameSettings: Resolution dropdown is not assigned.",
                this
            );
        }

        if (vSyncToggle == null)
        {
            Debug.LogWarning(
                "GameSettings: VSync toggle is not assigned.",
                this
            );
        }
    }
}