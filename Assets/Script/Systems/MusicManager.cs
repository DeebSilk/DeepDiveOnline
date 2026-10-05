using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public enum MusicState
    {
        None,
        Menu,
        Gameplay,
        Battle,
        Boss,
        Dungeon,
        Town,
        Death,
        Victory
    }

    [Serializable]
    public class MusicTrack
    {
        [Header("Track")]
        public string id;

        public MusicState state = MusicState.None;

        public AudioClip clip;

        [Header("Playback")]
        [Range(0f, 1f)]
        public float volume = 1f;

        public bool loop = true;

        [Header("Selection")]
        [Min(0.01f)]
        public float weight = 1f;
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Header("Music Library")]
    [SerializeField] private List<MusicTrack> tracks =
        new List<MusicTrack>();

    [Header("Default State")]
    [SerializeField]
    private MusicState defaultState = MusicState.None;

    [Header("Crossfade")]
    [SerializeField, Min(0f)]
    private float defaultCrossfadeDuration = 1.5f;

    [Header("Random Selection")]
    [SerializeField]
    private bool avoidImmediateRepeat = true;

    private static MusicManager instance;

    private readonly Dictionary<string, MusicTrack> trackById =
        new Dictionary<string, MusicTrack>(
            StringComparer.OrdinalIgnoreCase
        );

    private readonly Dictionary<MusicState, List<MusicTrack>>
        tracksByState =
            new Dictionary<MusicState, List<MusicTrack>>();

    private readonly Dictionary<MusicState, int>
        lastSelectedIndex =
            new Dictionary<MusicState, int>();

    private Coroutine transitionCoroutine;

    private string currentTrackId = string.Empty;

    private MusicState currentState = MusicState.None;

    private float trackLocalVolume = 1f;

    public static MusicManager Instance => instance;

    public string CurrentTrackId => currentTrackId;

    public MusicState CurrentState => currentState;

    public bool IsPlaying =>
        musicSource != null &&
        musicSource.isPlaying;

    public AudioSource MusicSource => musicSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        SetupAudioSource();
        BuildLibrary();

        // Автоматически запускаем музыку,
        // если в Inspector выбран Default State.
        if (defaultState != MusicState.None)
        {
            SetState(
                defaultState,
                0f
            );
        }
    }

    private void SetupAudioSource()
    {
        if (musicSource == null)
        {
            musicSource =
                GetComponent<AudioSource>();

            if (musicSource == null)
            {
                musicSource =
                    gameObject.AddComponent<AudioSource>();
            }
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0f;

        if (musicMixerGroup != null)
        {
            musicSource.outputAudioMixerGroup =
                musicMixerGroup;
        }
    }

    private void BuildLibrary()
    {
        trackById.Clear();
        tracksByState.Clear();
        lastSelectedIndex.Clear();

        foreach (MusicTrack track in tracks)
        {
            if (track == null)
                continue;

            if (string.IsNullOrWhiteSpace(track.id))
            {
                Debug.LogWarning(
                    "MusicManager: A track has an empty ID.",
                    this
                );

                continue;
            }

            if (track.clip == null)
            {
                Debug.LogWarning(
                    $"MusicManager: Track '{track.id}' has no AudioClip.",
                    this
                );

                continue;
            }

            if (track.state == MusicState.None)
            {
                Debug.LogWarning(
                    $"MusicManager: Track '{track.id}' has state None.",
                    this
                );

                continue;
            }

            if (trackById.ContainsKey(track.id))
            {
                Debug.LogWarning(
                    $"MusicManager: Duplicate track ID '{track.id}'.",
                    this
                );

                continue;
            }

            track.volume =
                Mathf.Clamp01(track.volume);

            track.weight =
                Mathf.Max(
                    0.01f,
                    track.weight
                );

            trackById.Add(
                track.id,
                track
            );

            if (!tracksByState.TryGetValue(
                    track.state,
                    out List<MusicTrack> stateTracks))
            {
                stateTracks =
                    new List<MusicTrack>();

                tracksByState.Add(
                    track.state,
                    stateTracks
                );
            }

            stateTracks.Add(track);
        }
    }

    /// <summary>
    /// Переключает музыкальное состояние.
    /// Например: Gameplay, Battle, Boss.
    /// </summary>
    public void SetState(MusicState state)
    {
        SetState(
            state,
            defaultCrossfadeDuration
        );
    }

    /// <summary>
    /// Переключает музыкальное состояние
    /// с указанной длительностью crossfade.
    /// </summary>
    public void SetState(
        MusicState state,
        float crossfadeDuration
    )
    {
        if (state == MusicState.None)
        {
            Stop(
                crossfadeDuration
            );

            return;
        }

        if (currentState == state &&
            musicSource != null &&
            musicSource.isPlaying)
        {
            return;
        }

        if (!tracksByState.TryGetValue(
                state,
                out List<MusicTrack> stateTracks) ||
            stateTracks == null ||
            stateTracks.Count == 0)
        {
            Debug.LogWarning(
                $"MusicManager: No music configured for state '{state}'.",
                this
            );

            return;
        }

        MusicTrack selectedTrack =
            SelectTrack(
                state,
                stateTracks
            );

        if (selectedTrack == null)
            return;

        currentState = state;

        PlayTrack(
            selectedTrack,
            crossfadeDuration
        );
    }

    /// <summary>
    /// Запускает конкретный трек по ID.
    /// </summary>
    public void Play(string id)
    {
        Play(
            id,
            defaultCrossfadeDuration
        );
    }

    /// <summary>
    /// Запускает конкретный трек по ID
    /// с указанным crossfade.
    /// </summary>
    public void Play(
        string id,
        float crossfadeDuration
    )
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning(
                "MusicManager: Cannot play music with an empty ID.",
                this
            );

            return;
        }

        if (!trackById.TryGetValue(
                id,
                out MusicTrack track))
        {
            Debug.LogWarning(
                $"MusicManager: Track '{id}' was not found.",
                this
            );

            return;
        }

        currentState =
            track.state;

        PlayTrack(
            track,
            crossfadeDuration
        );
    }

    private void PlayTrack(
        MusicTrack track,
        float crossfadeDuration
    )
    {
        if (track == null ||
            track.clip == null ||
            musicSource == null)
        {
            return;
        }

        crossfadeDuration =
            Mathf.Max(
                0f,
                crossfadeDuration
            );

        if (currentTrackId.Equals(
                track.id,
                StringComparison.OrdinalIgnoreCase) &&
            musicSource.isPlaying)
        {
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(
                transitionCoroutine
            );

            transitionCoroutine = null;
        }

        transitionCoroutine =
            StartCoroutine(
                ChangeTrackRoutine(
                    track,
                    crossfadeDuration
                )
            );
    }

    private IEnumerator ChangeTrackRoutine(
        MusicTrack newTrack,
        float duration
    )
    {
        if (musicSource == null)
        {
            transitionCoroutine = null;
            yield break;
        }

        float startVolume =
            musicSource.isPlaying
                ? musicSource.volume
                : 0f;

        // Плавно убираем текущий трек.
        if (musicSource.isPlaying)
        {
            if (duration > 0f)
            {
                float timer = 0f;

                while (timer < duration)
                {
                    timer +=
                        Time.unscaledDeltaTime;

                    float progress =
                        Mathf.Clamp01(
                            timer / duration
                        );

                    musicSource.volume =
                        Mathf.Lerp(
                            startVolume,
                            0f,
                            progress
                        );

                    yield return null;
                }
            }

            musicSource.Stop();
        }

        // Устанавливаем новый трек.
        musicSource.clip =
            newTrack.clip;

        musicSource.loop =
            newTrack.loop;

        musicSource.volume =
            0f;

        musicSource.Play();

        currentTrackId =
            newTrack.id;

        float targetVolume =
            Mathf.Clamp01(
                newTrack.volume
            ) * trackLocalVolume;

        // Запуск без fade.
        if (duration <= 0f)
        {
            musicSource.volume =
                targetVolume;

            transitionCoroutine = null;
            yield break;
        }

        // Плавно увеличиваем громкость.
        float fadeTimer = 0f;

        while (fadeTimer < duration)
        {
            fadeTimer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    fadeTimer / duration
                );

            musicSource.volume =
                Mathf.Lerp(
                    0f,
                    targetVolume,
                    progress
                );

            yield return null;
        }

        musicSource.volume =
            targetVolume;

        transitionCoroutine = null;
    }

    /// <summary>
    /// Останавливает текущую музыку.
    /// </summary>
    public void Stop()
    {
        Stop(
            defaultCrossfadeDuration
        );
    }

    /// <summary>
    /// Останавливает музыку с указанным fade-out.
    /// </summary>
    public void Stop(float fadeDuration)
    {
        fadeDuration =
            Mathf.Max(
                0f,
                fadeDuration
            );

        if (transitionCoroutine != null)
        {
            StopCoroutine(
                transitionCoroutine
            );

            transitionCoroutine = null;
        }

        transitionCoroutine =
            StartCoroutine(
                FadeOutRoutine(
                    fadeDuration
                )
            );
    }

    private IEnumerator FadeOutRoutine(
        float duration
    )
    {
        if (musicSource == null)
        {
            ClearCurrentMusic();
            yield break;
        }

        if (!musicSource.isPlaying)
        {
            ClearCurrentMusic();
            yield break;
        }

        float startVolume =
            musicSource.volume;

        if (duration <= 0f)
        {
            musicSource.Stop();
            musicSource.volume = 0f;

            ClearCurrentMusic();
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / duration
                );

            musicSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0f,
                    progress
                );

            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = 0f;

        ClearCurrentMusic();
    }

    private void ClearCurrentMusic()
    {
        currentTrackId =
            string.Empty;

        currentState =
            MusicState.None;

        transitionCoroutine = null;
    }

    /// <summary>
    /// Локальная громкость MusicManager.
    ///
    /// Глобальная громкость музыки регулируется
    /// через AudioMixer / AudioSettingsManager.
    /// </summary>
    public void SetVolume(float value)
    {
        trackLocalVolume =
            Mathf.Clamp01(value);

        if (musicSource == null ||
            !musicSource.isPlaying)
        {
            return;
        }

        if (trackById.TryGetValue(
                currentTrackId,
                out MusicTrack track))
        {
            musicSource.volume =
                Mathf.Clamp01(
                    track.volume
                ) * trackLocalVolume;
        }
        else
        {
            musicSource.volume =
                trackLocalVolume;
        }
    }

    public float GetVolume()
    {
        return trackLocalVolume;
    }

    /// <summary>
    /// Проверяет, есть ли музыка
    /// для указанного состояния.
    /// </summary>
    public bool HasState(
        MusicState state
    )
    {
        return
            tracksByState.TryGetValue(
                state,
                out List<MusicTrack> stateTracks
            ) &&
            stateTracks != null &&
            stateTracks.Count > 0;
    }

    /// <summary>
    /// Проверяет наличие конкретного трека.
    /// </summary>
    public bool HasTrack(
        string id
    )
    {
        return
            !string.IsNullOrWhiteSpace(id) &&
            trackById.ContainsKey(id);
    }

    /// <summary>
    /// Перестраивает музыкальную библиотеку.
    /// </summary>
    public void RebuildLibrary()
    {
        BuildLibrary();
    }

    private MusicTrack SelectTrack(
        MusicState state,
        List<MusicTrack> availableTracks
    )
    {
        if (availableTracks == null ||
            availableTracks.Count == 0)
        {
            return null;
        }

        if (availableTracks.Count == 1)
        {
            lastSelectedIndex[state] = 0;

            return availableTracks[0];
        }

        int previousIndex = -1;

        lastSelectedIndex.TryGetValue(
            state,
            out previousIndex
        );

        float totalWeight = 0f;

        for (int i = 0;
             i < availableTracks.Count;
             i++)
        {
            if (avoidImmediateRepeat &&
                i == previousIndex)
            {
                continue;
            }

            totalWeight +=
                Mathf.Max(
                    0.01f,
                    availableTracks[i].weight
                );
        }

        if (totalWeight <= 0f)
        {
            return availableTracks[0];
        }

        float randomValue =
            UnityEngine.Random.Range(
                0f,
                totalWeight
            );

        float accumulated = 0f;

        for (int i = 0;
             i < availableTracks.Count;
             i++)
        {
            if (avoidImmediateRepeat &&
                i == previousIndex)
            {
                continue;
            }

            accumulated +=
                Mathf.Max(
                    0.01f,
                    availableTracks[i].weight
                );

            if (randomValue <= accumulated)
            {
                lastSelectedIndex[state] = i;

                return availableTracks[i];
            }
        }

        return availableTracks[0];
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}