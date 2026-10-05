using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerDeathController : MonoBehaviour
{
    [Header("=== DEATH ===")]

    [SerializeField]
    private float deathScreenDelay = 1.5f;

    [Header("=== RESPAWN ===")]

    [SerializeField]
    private bool canRespawn = true;

    [SerializeField]
    private Transform respawnPoint;

    [SerializeField]
    private float respawnDelay = 0.5f;

    [SerializeField]
    private bool restoreFullHealth = true;

    [Header("=== MAIN MENU ===")]

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    [Header("=== PLAYER COMPONENTS ===")]

    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private PlayerCombat playerCombat;

    [SerializeField]
    private CharacterController characterController;

    [Header("=== DEATH UI ===")]

    [SerializeField]
    private DeathScreenUI deathUI;

    [Header("=== CURSOR ===")]

    [SerializeField]
    private bool lockCursorAfterRespawn = true;

    [SerializeField]
    private bool hideCursorAfterRespawn = true;

    private PlayerHealth playerHealth;

    private bool isDead;
    private bool respawnRequested;

    private float deathTimer;
    private float respawnTimer;

    public bool IsDead => isDead;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        playerHealth =
            GetComponent<PlayerHealth>();

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        if (playerCombat == null)
        {
            playerCombat =
                GetComponent<PlayerCombat>();
        }

        if (characterController == null)
        {
            characterController =
                GetComponent<CharacterController>();
        }

        if (deathUI == null)
        {
            deathUI =
                FindAnyObjectByType<DeathScreenUI>();
        }

        if (deathUI != null)
        {
            deathUI.Initialize(this);
        }

        ValidateComponents();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        LockGameplayCursor();
    }

    // =========================================================
    // ENABLE
    // =========================================================

    private void OnEnable()
    {
        if (playerHealth == null)
        {
            playerHealth =
                GetComponent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerHealthDied;
        }
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerHealthDied;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!isDead)
            return;

        deathTimer +=
            Time.unscaledDeltaTime;

        if (deathTimer >= deathScreenDelay)
        {
            if (deathUI != null)
            {
                deathUI.Show();
            }
        }

        if (respawnRequested)
        {
            respawnTimer +=
                Time.unscaledDeltaTime;

            if (respawnTimer >= respawnDelay)
            {
                respawnRequested = false;

                RespawnInternal();
            }
        }
    }

    // =========================================================
    // HEALTH EVENT
    // =========================================================

    private void OnPlayerHealthDied()
    {
        Die();
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private void ValidateComponents()
    {
        if (playerHealth == null)
        {
            Debug.LogError(
                "[PlayerDeathController] PlayerHealth not found!",
                this
            );
        }

        if (playerMovement == null)
        {
            Debug.LogWarning(
                "[PlayerDeathController] PlayerMovement not found.",
                this
            );
        }

        if (playerCombat == null)
        {
            Debug.LogWarning(
                "[PlayerDeathController] PlayerCombat not found.",
                this
            );
        }

        if (characterController == null)
        {
            Debug.LogWarning(
                "[PlayerDeathController] CharacterController not found.",
                this
            );
        }

        if (respawnPoint == null)
        {
            Debug.LogWarning(
                "[PlayerDeathController] Respawn Point is not assigned.",
                this
            );
        }

        if (deathUI == null)
        {
            Debug.LogWarning(
                "[PlayerDeathController] DeathScreenUI is not assigned or found.",
                this
            );
        }
    }

    // =========================================================
    // DIE
    // =========================================================

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        deathTimer = 0f;
        respawnTimer = 0f;
        respawnRequested = false;

        Debug.Log(
            "[PlayerDeathController] Player died."
        );

        DisablePlayerControl();

        ShowDeathCursor();

        if (deathUI != null)
        {
            deathUI.Prepare();
        }

        Time.timeScale = 0f;
    }

    // =========================================================
    // RESPAWN REQUEST
    // =========================================================

    public void Respawn()
    {
        if (!isDead)
            return;

        if (!canRespawn)
        {
            Debug.LogWarning(
                "[PlayerDeathController] Respawn is disabled."
            );

            return;
        }

        if (respawnPoint == null)
        {
            Debug.LogError(
                "[PlayerDeathController] Respawn Point is not assigned.",
                this
            );

            return;
        }

        if (respawnRequested)
            return;

        Debug.Log(
            "[PlayerDeathController] Respawn requested."
        );

        respawnRequested = true;
        respawnTimer = 0f;

        Time.timeScale = 1f;

        if (deathUI != null)
        {
            deathUI.Hide();
        }
    }

    // =========================================================
    // ACTUAL RESPAWN
    // =========================================================

    private void RespawnInternal()
    {
        if (!isDead)
            return;

        Debug.Log(
            "[PlayerDeathController] Respawning player."
        );

        // -----------------------------------------------------
        // 1. MOVE PLAYER
        // -----------------------------------------------------

        MoveToRespawnPoint();

        // -----------------------------------------------------
        // 2. RESTORE HEALTH
        // -----------------------------------------------------

        if (restoreFullHealth)
        {
            RestorePlayerHealth();
        }

        // -----------------------------------------------------
        // 3. RESET MOVEMENT
        // -----------------------------------------------------

        ResetMovementState();

        // -----------------------------------------------------
        // 4. ENABLE PLAYER CONTROL
        // -----------------------------------------------------

        EnablePlayerControl();

        // -----------------------------------------------------
        // 5. RESET DEATH STATE
        // -----------------------------------------------------

        isDead = false;
        respawnRequested = false;

        deathTimer = 0f;
        respawnTimer = 0f;

        // -----------------------------------------------------
        // 6. HIDE DEATH SCREEN COMPLETELY
        // -----------------------------------------------------

        if (deathUI != null)
        {
            deathUI.Hide();
        }

        // -----------------------------------------------------
        // 7. RESTORE GAME TIME
        // -----------------------------------------------------

        Time.timeScale = 1f;

        // -----------------------------------------------------
        // 8. LOCK MOUSE BACK INTO GAME
        // -----------------------------------------------------

        LockGameplayCursor();

        Debug.Log(
            "[PlayerDeathController] Player respawned."
        );
    }

    // =========================================================
    // MOVE TO RESPAWN
    // =========================================================

    private void MoveToRespawnPoint()
    {
        if (respawnPoint == null)
        {
            Debug.LogError(
                "[PlayerDeathController] Respawn Point is missing.",
                this
            );

            return;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(
            respawnPoint.position,
            respawnPoint.rotation
        );

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    // =========================================================
    // HEALTH
    // =========================================================

    private void RestorePlayerHealth()
    {
        if (playerHealth == null)
        {
            playerHealth =
                GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "[PlayerDeathController] PlayerHealth is missing!",
                this
            );

            return;
        }

        playerHealth.RestoreFullHealth();
    }

    // =========================================================
    // MOVEMENT RESET
    // =========================================================

    private void ResetMovementState()
    {
        if (playerMovement != null)
        {
            playerMovement.ResetMovementState();
        }
    }

    // =========================================================
    // PLAYER CONTROL
    // =========================================================

    private void DisablePlayerControl()
    {
        if (playerCombat != null)
        {
            playerCombat.CancelAttack();
            playerCombat.enabled = false;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }

    private void EnablePlayerControl()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (playerCombat != null)
        {
            playerCombat.enabled = true;
        }
    }

    // =========================================================
    // CURSOR
    // =========================================================

    private void ShowDeathCursor()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }

    private void LockGameplayCursor()
    {
        if (lockCursorAfterRespawn)
        {
            Cursor.lockState =
                CursorLockMode.Locked;
        }

        if (hideCursorAfterRespawn)
        {
            Cursor.visible = false;
        }
    }

    // =========================================================
    // MAIN MENU
    // =========================================================

    public void GoToMainMenu()
    {
        Debug.Log(
            "[PlayerDeathController] Loading main menu: "
            + mainMenuSceneName
        );

        Time.timeScale = 1f;

        if (deathUI != null)
        {
            deathUI.Hide();
        }

        if (string.IsNullOrWhiteSpace(
            mainMenuSceneName))
        {
            Debug.LogError(
                "[PlayerDeathController] Main Menu Scene Name is empty!",
                this
            );

            return;
        }

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }
}