using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AmbientManager : MonoBehaviour
{
    [Serializable]
    public class AmbientClip
    {
        public string id;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        public bool loop = true;
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource ambientSource;

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup ambientMixerGroup;

    [Header("Ambient Library")]
    [SerializeField] private List<AmbientClip> clips =
        new List<AmbientClip>();

    [Header("Crossfade")]
    [SerializeField, Min(0f)]
    private float crossfadeDuration = 2f;

    private Dictionary<string, AmbientClip> clipDictionary =
        new Dictionary<string, AmbientClip>(
            StringComparer.OrdinalIgnoreCase
        );

    private Coroutine fadeCoroutine;

    private string currentId = string.Empty;

    private float volume = 1f;

    private void Awake()
    {
        SetupSource();
        BuildLibrary();
    }

    private void SetupSource()
    {
        if (ambientSource == null)
        {
            ambientSource = GetComponent<AudioSource>();

            if (ambientSource == null)
                ambientSource =
                    gameObject.AddComponent<AudioSource>();
        }

        ambientSource.playOnAwake = false;
        ambientSource.spatialBlend = 0f;

        if (ambientMixerGroup != null)
            ambientSource.outputAudioMixerGroup =
                ambientMixerGroup;
    }

    private void BuildLibrary()
    {
        clipDictionary.Clear();

        foreach (AmbientClip entry in clips)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.id))
            {
                Debug.LogWarning(
                    "AmbientManager: Ambient has empty ID.",
                    this
                );

                continue;
            }

            if (entry.clip == null)
            {
                Debug.LogWarning(
                    $"AmbientManager: Ambient '{entry.id}' has no AudioClip.",
                    this
                );

                continue;
            }

            if (clipDictionary.ContainsKey(entry.id))
            {
                Debug.LogWarning(
                    $"AmbientManager: Duplicate ambient ID '{entry.id}'.",
                    this
                );

                continue;
            }

            clipDictionary.Add(entry.id, entry);
        }
    }

    public void Play(string id)
    {
        Play(id, crossfadeDuration);
    }

    public void Play(
        string id,
        float fadeDuration
    )
    {
        if (!clipDictionary.TryGetValue(
                id,
                out AmbientClip entry))
        {
            Debug.LogWarning(
                $"AmbientManager: Ambient '{id}' was not found.",
                this
            );

            return;
        }

        if (entry.clip == null)
            return;

        if (currentId.Equals(
                id,
                StringComparison.OrdinalIgnoreCase) &&
            ambientSource.isPlaying)
        {
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(
            ChangeAmbient(entry, fadeDuration)
        );
    }

    private IEnumerator ChangeAmbient(
        AmbientClip entry,
        float duration
    )
    {
        if (ambientSource.isPlaying)
        {
            float startVolume =
                ambientSource.volume;

            if (duration > 0f)
            {
                float timer = 0f;

                while (timer < duration)
                {
                    timer += Time.unscaledDeltaTime;

                    float progress =
                        Mathf.Clamp01(
                            timer / duration
                        );

                    ambientSource.volume =
                        Mathf.Lerp(
                            startVolume,
                            0f,
                            progress
                        );

                    yield return null;
                }
            }

            ambientSource.Stop();
        }

        ambientSource.clip = entry.clip;
        ambientSource.loop = entry.loop;
        ambientSource.volume = 0f;

        ambientSource.Play();

        currentId = entry.id;

        float targetVolume =
            Mathf.Clamp01(entry.volume) * volume;

        if (duration <= 0f)
        {
            ambientSource.volume =
                targetVolume;

            fadeCoroutine = null;
            yield break;
        }

        float fadeTimer = 0f;

        while (fadeTimer < duration)
        {
            fadeTimer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    fadeTimer / duration
                );

            ambientSource.volume =
                Mathf.Lerp(
                    0f,
                    targetVolume,
                    progress
                );

            yield return null;
        }

        ambientSource.volume =
            targetVolume;

        fadeCoroutine = null;
    }

    public void Stop()
    {
        Stop(crossfadeDuration);
    }

    public void Stop(float duration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        StartCoroutine(FadeOut(duration));
    }

    private IEnumerator FadeOut(float duration)
    {
        if (!ambientSource.isPlaying)
        {
            currentId = string.Empty;
            yield break;
        }

        float startVolume =
            ambientSource.volume;

        if (duration <= 0f)
        {
            ambientSource.Stop();
            ambientSource.volume = 0f;
            currentId = string.Empty;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / duration
                );

            ambientSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0f,
                    progress
                );

            yield return null;
        }

        ambientSource.Stop();
        ambientSource.volume = 0f;

        currentId = string.Empty;
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);

        if (!ambientSource.isPlaying)
            return;

        if (clipDictionary.TryGetValue(
                currentId,
                out AmbientClip entry))
        {
            ambientSource.volume =
                Mathf.Clamp01(entry.volume) *
                volume;
        }
        else
        {
            ambientSource.volume = volume;
        }
    }

    public float GetVolume()
    {
        return volume;
    }
}