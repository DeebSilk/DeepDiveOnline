using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("PLAYER")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("HP BAR")]
    [SerializeField] private Image hpFill;
    [SerializeField] private Image delayedHpFill;

    [Header("TEXT")]
    [SerializeField] private Text hpText;

    [Header("DAMAGE EFFECT")]
    [SerializeField] private Image damageFlash;

    [Header("SMOOTH HP")]
    [SerializeField] private float hpSmoothTime = 0.12f;

    [Header("DELAYED BAR")]
    [SerializeField] private float delayedSmoothTime = 0.45f;

    [Header("DAMAGE FLASH")]
    [SerializeField] private float damageFlashDuration = 0.3f;
    [SerializeField, Range(0f, 1f)]
    private float damageFlashAlpha = 0.45f;

    [Header("LOW HP")]
    [SerializeField, Range(0f, 1f)]
    private float lowHealthThreshold = 0.25f;

    [SerializeField]
    private float lowHealthPulseSpeed = 5f;

    [SerializeField]
    private float lowHealthPulseAmount = 0.25f;

    [Header("COLORS")]
    [SerializeField]
    private Color normalHealthColor =
        new Color(0.1f, 1f, 0.3f, 1f);

    [SerializeField]
    private Color mediumHealthColor =
        new Color(1f, 0.85f, 0.05f, 1f);

    [SerializeField]
    private Color lowHealthColor =
        new Color(1f, 0.08f, 0.08f, 1f);

    private float targetHealth;
    private float delayedTargetHealth;

    private float hpVelocity;
    private float delayedVelocity;

    private Coroutine damageFlashCoroutine;

    private bool subscribed;

    private void Awake()
    {
        FindPlayerHealth();

        ConfigureHPImage(hpFill);
        ConfigureHPImage(delayedHpFill);

        if (damageFlash != null)
        {
            Color color = damageFlash.color;
            color.a = 0f;
            damageFlash.color = color;
        }
    }

    private void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogError(
                "[PlayerHealthUI] PlayerHealth not found.",
                this
            );

            return;
        }

        Subscribe();

        SetHealthImmediately(
            playerHealth.CurrentHealth,
            playerHealth.MaxHealth
        );
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    // =========================================================
    // FIND PLAYER HEALTH
    // =========================================================

    private void FindPlayerHealth()
    {
        if (playerHealth != null)
            return;

        playerHealth =
            GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
            return;

        playerHealth =
            FindAnyObjectByType<PlayerHealth>();
    }

    // =========================================================
    // SUBSCRIBE
    // =========================================================

    private void Subscribe()
    {
        if (playerHealth == null || subscribed)
            return;

        playerHealth.HealthChanged +=
            OnHealthChanged;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (playerHealth == null || !subscribed)
            return;

        playerHealth.HealthChanged -=
            OnHealthChanged;

        subscribed = false;
    }

    // =========================================================
    // IMAGE CONFIGURATION
    // =========================================================

    private void ConfigureHPImage(Image image)
    {
        if (image == null)
            return;

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillClockwise = true;
        image.fillAmount = 1f;
    }

    // =========================================================
    // HEALTH EVENT
    // =========================================================

    private void OnHealthChanged(
        float currentHealth,
        float maxHealth)
    {
        if (maxHealth <= 0f)
            return;

        float normalized =
            Mathf.Clamp01(
                currentHealth / maxHealth
            );

        float previousHealth =
            targetHealth;

        targetHealth = normalized;

        /*
         * Запаздывающая полоса должна начинаться
         * с предыдущего значения, а не мгновенно
         * прыгать к новому HP.
         */
        if (normalized < previousHealth)
        {
            delayedTargetHealth =
                previousHealth;

            PlayDamageFlash();
        }
        else
        {
            delayedTargetHealth =
                normalized;
        }

        UpdateHPColor(normalized);

        UpdateHPText(
            currentHealth,
            maxHealth
        );

        Debug.Log(
            $"[PlayerHealthUI] HP updated: " +
            $"{currentHealth}/{maxHealth} " +
            $"({normalized * 100f:0}%)"
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdateMainHP();
        UpdateDelayedHP();
        UpdateLowHealthEffect();
    }

    // =========================================================
    // MAIN HP
    // =========================================================

    private void UpdateMainHP()
    {
        if (hpFill == null)
            return;

        hpFill.fillAmount =
            Mathf.SmoothDamp(
                hpFill.fillAmount,
                targetHealth,
                ref hpVelocity,
                hpSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );
    }

    // =========================================================
    // DELAYED HP
    // =========================================================

    private void UpdateDelayedHP()
    {
        if (delayedHpFill == null)
            return;

        /*
         * После получения урона сначала остаётся
         * старая полоса, затем постепенно догоняет HP.
         */
        delayedHpFill.fillAmount =
            Mathf.SmoothDamp(
                delayedHpFill.fillAmount,
                delayedTargetHealth,
                ref delayedVelocity,
                delayedSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

        /*
         * Когда основная полоса уже дошла до нового HP,
         * разрешаем серой полосе идти за ней.
         */
        if (
            Mathf.Abs(
                hpFill != null
                    ? hpFill.fillAmount - targetHealth
                    : 0f
            ) < 0.005f
        )
        {
            delayedTargetHealth =
                targetHealth;
        }
    }

    // =========================================================
    // COLOR
    // =========================================================

    private void UpdateHPColor(
        float normalized)
    {
        if (hpFill == null)
            return;

        Color targetColor;

        if (normalized > 0.5f)
        {
            float t =
                Mathf.InverseLerp(
                    0.5f,
                    1f,
                    normalized
                );

            targetColor =
                Color.Lerp(
                    mediumHealthColor,
                    normalHealthColor,
                    t
                );
        }
        else
        {
            float t =
                Mathf.InverseLerp(
                    0f,
                    0.5f,
                    normalized
                );

            targetColor =
                Color.Lerp(
                    lowHealthColor,
                    mediumHealthColor,
                    t
                );
        }

        targetColor.a = 1f;

        hpFill.color = targetColor;
    }

    // =========================================================
    // HP TEXT
    // =========================================================

    private void UpdateHPText(
        float currentHealth,
        float maxHealth)
    {
        if (hpText == null)
            return;

        hpText.text =
            $"{Mathf.CeilToInt(currentHealth)} / " +
            $"{Mathf.CeilToInt(maxHealth)}";
    }

    // =========================================================
    // INITIAL VALUE
    // =========================================================

    private void SetHealthImmediately(
        float currentHealth,
        float maxHealth)
    {
        if (maxHealth <= 0f)
            return;

        float normalized =
            Mathf.Clamp01(
                currentHealth / maxHealth
            );

        targetHealth = normalized;
        delayedTargetHealth = normalized;

        hpVelocity = 0f;
        delayedVelocity = 0f;

        if (hpFill != null)
            hpFill.fillAmount = normalized;

        if (delayedHpFill != null)
            delayedHpFill.fillAmount = normalized;

        UpdateHPColor(normalized);

        UpdateHPText(
            currentHealth,
            maxHealth
        );
    }

    // =========================================================
    // DAMAGE FLASH
    // =========================================================

    private void PlayDamageFlash()
    {
        if (damageFlash == null)
            return;

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(
                damageFlashCoroutine
            );
        }

        damageFlashCoroutine =
            StartCoroutine(
                DamageFlashRoutine()
            );
    }

    private IEnumerator DamageFlashRoutine()
    {
        Color color =
            damageFlash.color;

        color.a =
            damageFlashAlpha;

        damageFlash.color =
            color;

        float elapsed = 0f;

        while (
            elapsed <
            damageFlashDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    damageFlashDuration
                );

            color.a =
                Mathf.Lerp(
                    damageFlashAlpha,
                    0f,
                    t
                );

            damageFlash.color =
                color;

            yield return null;
        }

        color.a = 0f;

        damageFlash.color =
            color;

        damageFlashCoroutine = null;
    }

    // =========================================================
    // LOW HP
    // =========================================================

    private void UpdateLowHealthEffect()
    {
        if (hpFill == null)
            return;

        if (targetHealth > lowHealthThreshold)
            return;

        float pulse =
            (Mathf.Sin(
                Time.unscaledTime *
                lowHealthPulseSpeed
            ) + 1f) * 0.5f;

        Color color =
            lowHealthColor;

        float alpha =
            Mathf.Lerp(
                0.75f,
                1f,
                pulse * lowHealthPulseAmount
            );

        color.a = alpha;

        hpFill.color = color;
    }
}