using System.Collections;
using UnityEngine;

public class CombatMusicController : MonoBehaviour
{
    [Header("Music States")]
    [SerializeField]
    private MusicManager.MusicState battleState =
        MusicManager.MusicState.Battle;

    [SerializeField]
    private MusicManager.MusicState gameplayState =
        MusicManager.MusicState.Gameplay;

    [Header("Crossfade")]
    [SerializeField, Min(0f)]
    private float battleFadeDuration = 1.5f;

    [SerializeField, Min(0f)]
    private float gameplayFadeDuration = 2f;

    [Header("Combat Exit")]
    [SerializeField, Min(0f)]
    private float combatExitDelay = 5f;

    private Coroutine returnToGameplayCoroutine;

    private bool battleMusicActive;

    private void OnEnable()
    {
        EnemyController.CombatStateChanged +=
            OnEnemyCombatStateChanged;
    }

    private void OnDisable()
    {
        EnemyController.CombatStateChanged -=
            OnEnemyCombatStateChanged;

        if (returnToGameplayCoroutine != null)
        {
            StopCoroutine(
                returnToGameplayCoroutine
            );

            returnToGameplayCoroutine = null;
        }
    }

    private void Start()
    {
        RefreshMusicState();
    }

    private void OnEnemyCombatStateChanged(
        EnemyController enemy,
        bool enteredCombat)
    {
        RefreshMusicState();
    }

    private void RefreshMusicState()
    {
        int activeEnemies =
            EnemyController.ActiveCombatEnemyCount;

        if (activeEnemies > 0)
        {
            EnterBattleMusic();
        }
        else
        {
            ScheduleReturnToGameplay();
        }
    }

    private void EnterBattleMusic()
    {
        if (returnToGameplayCoroutine != null)
        {
            StopCoroutine(
                returnToGameplayCoroutine
            );

            returnToGameplayCoroutine = null;
        }

        if (battleMusicActive)
            return;

        if (AudioManager.Instance == null)
            return;

        MusicManager music =
            AudioManager.Instance.Music;

        if (music == null)
            return;

        if (!music.HasState(
                battleState
            ))
        {
            Debug.LogWarning(
                $"CombatMusicController: No music configured for state '{battleState}'.",
                this
            );

            return;
        }

        battleMusicActive = true;

        music.SetState(
            battleState,
            battleFadeDuration
        );
    }

    private void ScheduleReturnToGameplay()
    {
        if (!battleMusicActive)
            return;

        if (returnToGameplayCoroutine != null)
            return;

        if (combatExitDelay <= 0f)
        {
            ReturnToGameplay();

            return;
        }

        returnToGameplayCoroutine =
            StartCoroutine(
                ReturnToGameplayAfterDelay()
            );
    }

    private IEnumerator ReturnToGameplayAfterDelay()
    {
        yield return new WaitForSeconds(
            combatExitDelay
        );

        returnToGameplayCoroutine = null;

        // За время ожидания мог появиться
        // новый враг в бою.
        if (EnemyController.ActiveCombatEnemyCount > 0)
            yield break;

        ReturnToGameplay();
    }

    private void ReturnToGameplay()
    {
        if (EnemyController.ActiveCombatEnemyCount > 0)
            return;

        battleMusicActive = false;

        if (AudioManager.Instance == null)
            return;

        MusicManager music =
            AudioManager.Instance.Music;

        if (music == null)
            return;

        if (!music.HasState(
                gameplayState
            ))
        {
            Debug.LogWarning(
                $"CombatMusicController: No music configured for state '{gameplayState}'.",
                this
            );

            return;
        }

        music.SetState(
            gameplayState,
            gameplayFadeDuration
        );
    }
}