using UnityEngine;

public class SceneAudioController : MonoBehaviour
{
    public enum SceneMusicState
    {
        None,
        Menu,
        Gameplay
    }

    [Header("Music State")]
    [SerializeField]
    private SceneMusicState musicState = SceneMusicState.None;

    private void Start()
    {
        ApplyMusicState();
    }

    private void ApplyMusicState()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "SceneAudioController: AudioManager not found.",
                this
            );

            return;
        }

        if (AudioManager.Instance.Music == null)
        {
            Debug.LogWarning(
                "SceneAudioController: MusicManager not found.",
                this
            );

            return;
        }

        switch (musicState)
        {
            case SceneMusicState.Menu:

                AudioManager.Instance.Music.SetState(
                    MusicManager.MusicState.Menu
                );

                break;

            case SceneMusicState.Gameplay:

                AudioManager.Instance.Music.SetState(
                    MusicManager.MusicState.Gameplay
                );

                break;

            case SceneMusicState.None:
                break;
        }
    }
}