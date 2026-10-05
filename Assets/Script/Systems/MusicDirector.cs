using UnityEngine;

public class MusicDirector : MonoBehaviour
{
    [Header("Gameplay Music")]
    [SerializeField] private MusicManager.MusicState gameplayState =
        MusicManager.MusicState.Gameplay;

    [Header("Startup")]
    [SerializeField] private bool playOnStart = true;

    [SerializeField, Min(0f)]
    private float startupFadeDuration = 2f;

    private void Start()
    {
        if (!playOnStart)
            return;

        PlayGameplayMusic();
    }

    public void PlayGameplayMusic()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "MusicDirector: AudioManager was not found in the scene.",
                this
            );

            return;
        }

        MusicManager music =
            AudioManager.Instance.Music;

        if (music == null)
        {
            Debug.LogWarning(
                "MusicDirector: MusicManager was not found.",
                this
            );

            return;
        }

        music.SetState(
            gameplayState,
            startupFadeDuration
        );
    }

    public void PlayState(
        MusicManager.MusicState state
    )
    {
        PlayState(
            state,
            1.5f
        );
    }

    public void PlayState(
        MusicManager.MusicState state,
        float fadeDuration
    )
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "MusicDirector: AudioManager was not found.",
                this
            );

            return;
        }

        MusicManager music =
            AudioManager.Instance.Music;

        if (music == null)
            return;

        music.SetState(
            state,
            fadeDuration
        );
    }

    public void PlayBattleMusic()
    {
        PlayState(
            MusicManager.MusicState.Battle
        );
    }

    public void PlayBossMusic()
    {
        PlayState(
            MusicManager.MusicState.Boss
        );
    }

    public void PlayTownMusic()
    {
        PlayState(
            MusicManager.MusicState.Town
        );
    }

    public void PlayDungeonMusic()
    {
        PlayState(
            MusicManager.MusicState.Dungeon
        );
    }

    public void PlayDeathMusic()
    {
        PlayState(
            MusicManager.MusicState.Death
        );
    }

    public void PlayVictoryMusic()
    {
        PlayState(
            MusicManager.MusicState.Victory
        );
    }

    public void StopMusic()
    {
        if (AudioManager.Instance == null)
            return;

        MusicManager music =
            AudioManager.Instance.Music;

        if (music == null)
            return;

        music.Stop();
    }
}