using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreenUI : MonoBehaviour
{
    // =========================================================
    // UI REFERENCES
    // =========================================================

    [Header("=== UI REFERENCES ===")]

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Text titleText;

    [SerializeField]
    private Text messageText;

    [SerializeField]
    private Button respawnButton;

    [SerializeField]
    private Button mainMenuButton;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("=== TEXT ===")]

    [SerializeField]
    private string title = "ВЫ УМЕРЛИ";

    [TextArea(2, 5)]
    [SerializeField]
    private string message =
        "ВЫ УМЕРЛИ\n" +
        "СИСТЕМА ЗАВЕРШИЛА ВАШ ТЕКУЩИЙ СОСТОЯНИЕ.";

    [SerializeField]
    private string respawnButtonText = "ВОЗРОДИТЬСЯ";

    [SerializeField]
    private string mainMenuButtonText = "ГЛАВНОЕ МЕНЮ";

    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("=== ANIMATION ===")]

    [SerializeField]
    [Min(0f)]
    private float fadeInDuration = 1.2f;

    [SerializeField]
    [Min(0f)]
    private float fadeOutDuration = 0.35f;

    [SerializeField]
    [Min(0f)]
    private float titlePulseSpeed = 1.8f;

    [SerializeField]
    [Range(0f, 0.5f)]
    private float titlePulseAmount = 0.08f;

    // =========================================================
    // CURSOR
    // =========================================================

    [Header("=== CURSOR ===")]

    [SerializeField]
    private bool showCursorOnDeath = true;

    [SerializeField]
    private bool lockCursorAfterHide = true;

    [SerializeField]
    private bool hideCursorAfterHide = true;

    // =========================================================
    // INTERNAL
    // =========================================================

    private PlayerDeathController deathController;

    private Coroutine fadeRoutine;
    private Coroutine pulseRoutine;

    private bool initialized;
    private bool isVisible;

    private Color originalTitleColor;

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(
        PlayerDeathController controller
    )
    {
        if (controller == null)
        {
            Debug.LogError(
                "[DeathScreenUI] PlayerDeathController is null.",
                this
            );

            return;
        }

        deathController = controller;

        if (initialized)
            return;

        initialized = true;

        SetupUI();
    }

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        /*
         * DeathScreen должен оставаться ACTIVE.
         *
         * Мы скрываем его через CanvasGroup.alpha,
         * а не через GameObject.SetActive(false).
         *
         * Это позволяет безопасно запускать Coroutine.
         */
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void SetupUI()
    {
        // -----------------------------------------------------
        // CANVAS GROUP
        // -----------------------------------------------------

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }

        // -----------------------------------------------------
        // TITLE COLOR
        // -----------------------------------------------------

        if (titleText != null)
        {
            originalTitleColor =
                titleText.color;
        }

        // -----------------------------------------------------
        // BUTTONS
        // -----------------------------------------------------

        if (respawnButton != null)
        {
            respawnButton.onClick.RemoveListener(
                OnRespawnClicked
            );

            respawnButton.onClick.AddListener(
                OnRespawnClicked
            );
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(
                OnMainMenuClicked
            );

            mainMenuButton.onClick.AddListener(
                OnMainMenuClicked
            );
        }

        UpdateTexts();

        HideImmediate();
    }

    // =========================================================
    // TEXT UPDATE
    // =========================================================

    private void UpdateTexts()
    {
        if (titleText != null)
        {
            titleText.text =
                title;
        }

        if (messageText != null)
        {
            messageText.text =
                message;
        }

        // -----------------------------------------------------
        // RESPAWN TEXT
        // -----------------------------------------------------

        if (respawnButton != null)
        {
            Text buttonText =
                respawnButton.GetComponentInChildren<Text>(
                    true
                );

            if (buttonText != null)
            {
                buttonText.text =
                    respawnButtonText;
            }
        }

        // -----------------------------------------------------
        // MAIN MENU TEXT
        // -----------------------------------------------------

        if (mainMenuButton != null)
        {
            Text buttonText =
                mainMenuButton.GetComponentInChildren<Text>(
                    true
                );

            if (buttonText != null)
            {
                buttonText.text =
                    mainMenuButtonText;
            }
        }
    }

    // =========================================================
    // PREPARE
    // =========================================================

    public void Prepare()
    {
        if (!initialized)
        {
            Debug.LogWarning(
                "[DeathScreenUI] Prepare() called before Initialize().",
                this
            );

            return;
        }

        StopAllUIAnimations();

        UpdateTexts();

        if (respawnButton != null)
        {
            respawnButton.interactable = true;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.interactable = true;
        }

        HideImmediate();
    }

    // =========================================================
    // SHOW
    // =========================================================

    public void Show()
    {
        if (!initialized)
            return;

        if (isVisible)
            return;

        isVisible = true;

        // -----------------------------------------------------
        // STOP OLD FADE
        // -----------------------------------------------------

        if (fadeRoutine != null)
        {
            StopCoroutine(
                fadeRoutine
            );

            fadeRoutine = null;
        }

        // -----------------------------------------------------
        // FADE IN
        // -----------------------------------------------------

        fadeRoutine =
            StartCoroutine(
                FadeRoutine(
                    canvasGroup.alpha,
                    1f,
                    fadeInDuration
                )
            );

        // -----------------------------------------------------
        // TITLE PULSE
        // -----------------------------------------------------

        if (pulseRoutine != null)
        {
            StopCoroutine(
                pulseRoutine
            );
        }

        pulseRoutine =
            StartCoroutine(
                PulseTitleRoutine()
            );

        // -----------------------------------------------------
        // INTERACTION
        // -----------------------------------------------------

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // -----------------------------------------------------
        // CURSOR
        // -----------------------------------------------------

        if (showCursorOnDeath)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
    }

    // =========================================================
    // HIDE
    // =========================================================

    public void Hide()
    {
        if (!initialized)
            return;

        if (!isVisible)
        {
            HideImmediate();
            return;
        }

        isVisible = false;

        // -----------------------------------------------------
        // STOP CURRENT FADE
        // -----------------------------------------------------

        if (fadeRoutine != null)
        {
            StopCoroutine(
                fadeRoutine
            );

            fadeRoutine = null;
        }

        // -----------------------------------------------------
        // STOP PULSE
        // -----------------------------------------------------

        if (pulseRoutine != null)
        {
            StopCoroutine(
                pulseRoutine
            );

            pulseRoutine = null;
        }

        // -----------------------------------------------------
        // FADE OUT
        // -----------------------------------------------------

        fadeRoutine =
            StartCoroutine(
                FadeRoutine(
                    canvasGroup.alpha,
                    0f,
                    fadeOutDuration
                )
            );
    }

    // =========================================================
    // HIDE IMMEDIATE
    // =========================================================

    private void HideImmediate()
    {
        isVisible = false;

        // -----------------------------------------------------
        // STOP ANIMATIONS
        // -----------------------------------------------------

        if (fadeRoutine != null)
        {
            StopCoroutine(
                fadeRoutine
            );

            fadeRoutine = null;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(
                pulseRoutine
            );

            pulseRoutine = null;
        }

        // -----------------------------------------------------
        // HIDE UI
        // -----------------------------------------------------

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;

            canvasGroup.interactable =
                false;

            canvasGroup.blocksRaycasts =
                false;
        }

        // -----------------------------------------------------
        // RESET TITLE
        // -----------------------------------------------------

        if (titleText != null)
        {
            titleText.color =
                originalTitleColor;
        }
    }

    // =========================================================
    // FADE ROUTINE
    // =========================================================

    private IEnumerator FadeRoutine(
        float startAlpha,
        float targetAlpha,
        float duration
    )
    {
        if (canvasGroup == null)
        {
            fadeRoutine = null;
            yield break;
        }

        // -----------------------------------------------------
        // ZERO DURATION
        // -----------------------------------------------------

        if (duration <= 0f)
        {
            canvasGroup.alpha =
                targetAlpha;

            FinishFade(
                targetAlpha
            );

            fadeRoutine = null;

            yield break;
        }

        // -----------------------------------------------------
        // FADE
        // -----------------------------------------------------

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            // SmoothStep
            t =
                t * t *
                (3f - 2f * t);

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            yield return null;
        }

        canvasGroup.alpha =
            targetAlpha;

        FinishFade(
            targetAlpha
        );

        fadeRoutine = null;
    }

    // =========================================================
    // FINISH FADE
    // =========================================================

    private void FinishFade(
        float alpha
    )
    {
        if (canvasGroup == null)
            return;

        bool visible =
            alpha > 0.001f;

        canvasGroup.interactable =
            visible;

        canvasGroup.blocksRaycasts =
            visible;

        // -----------------------------------------------------
        // GAMEPLAY CURSOR
        // -----------------------------------------------------

        if (!visible)
        {
            if (lockCursorAfterHide)
            {
                Cursor.lockState =
                    CursorLockMode.Locked;
            }

            if (hideCursorAfterHide)
            {
                Cursor.visible =
                    false;
            }
        }
    }

    // =========================================================
    // TITLE PULSE
    // =========================================================

    private IEnumerator PulseTitleRoutine()
    {
        if (titleText == null)
        {
            pulseRoutine = null;
            yield break;
        }

        while (isVisible)
        {
            float value =
                (
                    Mathf.Sin(
                        Time.unscaledTime *
                        titlePulseSpeed
                    )
                    + 1f
                ) * 0.5f;

            float intensity =
                Mathf.Lerp(
                    1f - titlePulseAmount,
                    1f,
                    value
                );

            Color color =
                originalTitleColor;

            color.r *= intensity;
            color.g *= intensity;
            color.b *= intensity;

            titleText.color =
                color;

            yield return null;
        }

        titleText.color =
            originalTitleColor;

        pulseRoutine = null;
    }

    // =========================================================
    // RESPAWN BUTTON
    // =========================================================

    private void OnRespawnClicked()
    {
        if (deathController == null)
        {
            Debug.LogError(
                "[DeathScreenUI] PlayerDeathController is missing.",
                this
            );

            return;
        }

        if (!deathController.IsDead)
            return;

        // -----------------------------------------------------
        // DISABLE BUTTONS
        // -----------------------------------------------------

        if (respawnButton != null)
        {
            respawnButton.interactable =
                false;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.interactable =
                false;
        }

        // -----------------------------------------------------
        // RESPAWN
        // -----------------------------------------------------

        deathController.Respawn();
    }

    // =========================================================
    // MAIN MENU BUTTON
    // =========================================================

    private void OnMainMenuClicked()
    {
        if (deathController == null)
        {
            Debug.LogError(
                "[DeathScreenUI] PlayerDeathController is missing.",
                this
            );

            return;
        }

        if (!deathController.IsDead)
            return;

        // -----------------------------------------------------
        // DISABLE BUTTONS
        // -----------------------------------------------------

        if (respawnButton != null)
        {
            respawnButton.interactable =
                false;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.interactable =
                false;
        }

        // -----------------------------------------------------
        // MAIN MENU
        // -----------------------------------------------------

        deathController.GoToMainMenu();
    }

    // =========================================================
    // STOP ANIMATIONS
    // =========================================================

    private void StopAllUIAnimations()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(
                fadeRoutine
            );

            fadeRoutine = null;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(
                pulseRoutine
            );

            pulseRoutine = null;
        }
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        StopAllUIAnimations();
    }
}