using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SFXManager : MonoBehaviour
{
    [Serializable]
    public class SFXClip
    {
        public string id;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("SFX Library")]
    [SerializeField] private List<SFXClip> clips =
        new List<SFXClip>();

    [Header("Audio Pool")]
    [SerializeField, Min(1)]
    private int sourcePoolSize = 12;

    private Dictionary<string, SFXClip> clipDictionary =
        new Dictionary<string, SFXClip>(
            StringComparer.OrdinalIgnoreCase
        );

    private readonly List<AudioSource> sources =
        new List<AudioSource>();

    private float volume = 1f;

    private void Awake()
    {
        BuildLibrary();
        CreateSourcePool();
    }

    private void BuildLibrary()
    {
        clipDictionary.Clear();

        foreach (SFXClip entry in clips)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.id))
            {
                Debug.LogWarning(
                    "SFXManager: SFX has empty ID.",
                    this
                );

                continue;
            }

            if (entry.clip == null)
            {
                Debug.LogWarning(
                    $"SFXManager: SFX '{entry.id}' has no AudioClip.",
                    this
                );

                continue;
            }

            if (clipDictionary.ContainsKey(entry.id))
            {
                Debug.LogWarning(
                    $"SFXManager: Duplicate SFX ID '{entry.id}'.",
                    this
                );

                continue;
            }

            clipDictionary.Add(entry.id, entry);
        }
    }

    private void CreateSourcePool()
    {
        sources.Clear();

        for (int i = 0; i < sourcePoolSize; i++)
        {
            GameObject sourceObject =
                new GameObject($"SFXSource_{i + 1:00}");

            sourceObject.transform.SetParent(transform);

            AudioSource source =
                sourceObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            if (sfxMixerGroup != null)
                source.outputAudioMixerGroup = sfxMixerGroup;

            sources.Add(source);
        }
    }

    public void Play(string id)
    {
        Play(id, 1f);
    }

    public void Play(
        string id,
        float volumeMultiplier
    )
    {
        if (!clipDictionary.TryGetValue(
                id,
                out SFXClip entry))
        {
            Debug.LogWarning(
                $"SFXManager: SFX '{id}' was not found.",
                this
            );

            return;
        }

        AudioSource source = GetFreeSource();

        if (source == null)
        {
            Debug.LogWarning(
                "SFXManager: No free AudioSource available.",
                this
            );

            return;
        }

        float finalVolume =
            Mathf.Clamp01(entry.volume) *
            Mathf.Clamp01(volumeMultiplier) *
            volume;

        source.PlayOneShot(
            entry.clip,
            finalVolume
        );
    }

    private AudioSource GetFreeSource()
    {
        foreach (AudioSource source in sources)
        {
            if (!source.isPlaying)
                return source;
        }

        return null;
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
    }

    public float GetVolume()
    {
        return volume;
    }
}