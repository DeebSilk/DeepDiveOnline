using System;
using UnityEngine;

[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealth : MonoBehaviour
{
    [Header("=== HEALTH ===")]
    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private float damageInvulnerability = 0.2f;

    [Header("=== DEBUG ===")]
    [SerializeField]
    private bool debugLogs = true;

    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    public float CurrentHealth
    {
        get;
        private set;
    }

    public float MaxHealth =>
        maxHealth;

    public float NormalizedHealth
    {
        get
        {
            if (maxHealth <= 0f)
                return 0f;

            return Mathf.Clamp01(
                CurrentHealth / maxHealth
            );
        }
    }

    public bool IsDead
    {
        get;
        private set;
    }

    // =========================================================
    // EVENTS
    // =========================================================

    /// <summary>
    /// Вызывается при изменении HP.
    /// float 1 = текущее HP.
    /// float 2 = максимальное HP.
    /// </summary>
    public event Action<float, float> HealthChanged;

    /// <summary>
    /// Вызывается один раз в момент смерти.
    /// PlayerDeathController подписывается на это событие.
    /// </summary>
    public event Action Died;

    // =========================================================
    // PRIVATE
    // =========================================================

    private float nextDamageTime;

    private PlayerCombat combat;
    private PlayerMovement movement;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        combat =
            GetComponent<PlayerCombat>();

        movement =
            GetComponent<PlayerMovement>();

        maxHealth =
            Mathf.Max(
                maxHealth,
                1f
            );

        CurrentHealth =
            maxHealth;

        IsDead = false;

        nextDamageTime = 0f;

        // Первичное обновление UI.
        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public bool TakeDamage(
        float damage
    )
    {
        if (IsDead)
            return false;

        if (damage <= 0f)
            return false;

        if (Time.time < nextDamageTime)
            return false;

        nextDamageTime =
            Time.time +
            Mathf.Max(
                damageInvulnerability,
                0f
            );

        float oldHealth =
            CurrentHealth;

        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth - damage,
                0f,
                MaxHealth
            );

        if (debugLogs)
        {
            Debug.Log(
                $"[PlayerHealth] Damage: {damage:0.##} | " +
                $"HP: {CurrentHealth:0.##}/{MaxHealth:0.##}",
                this
            );
        }

        // Обновляем HP UI.
        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );

        // Проверяем смерть.
        if (CurrentHealth <= 0f)
        {
            Die();
        }

        return !Mathf.Approximately(
            oldHealth,
            CurrentHealth
        );
    }

    // =========================================================
    // HEAL
    // =========================================================

    public void Heal(
        float amount
    )
    {
        if (IsDead)
            return;

        if (amount <= 0f)
            return;

        float oldHealth =
            CurrentHealth;

        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth + amount,
                0f,
                MaxHealth
            );

        if (Mathf.Approximately(
                oldHealth,
                CurrentHealth))
        {
            return;
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[PlayerHealth] Heal: {amount:0.##} | " +
                $"HP: {CurrentHealth:0.##}/{MaxHealth:0.##}",
                this
            );
        }

        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );
    }

    // =========================================================
    // RESTORE FULL HEALTH
    // =========================================================

    public void RestoreFullHealth()
    {
        CurrentHealth =
            MaxHealth;

        IsDead = false;

        nextDamageTime = 0f;

        if (debugLogs)
        {
            Debug.Log(
                $"[PlayerHealth] Full health restored: " +
                $"{CurrentHealth:0.##}/{MaxHealth:0.##}",
                this
            );
        }

        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );
    }

    // =========================================================
    // SET HEALTH
    // =========================================================

    public void SetHealth(
        float value
    )
    {
        CurrentHealth =
            Mathf.Clamp(
                value,
                0f,
                MaxHealth
            );

        /*
         * Если HP устанавливается выше нуля,
         * игрок больше не считается мёртвым.
         */
        IsDead =
            CurrentHealth <= 0f;

        nextDamageTime = 0f;

        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );

        if (debugLogs)
        {
            Debug.Log(
                $"[PlayerHealth] Health set to: " +
                $"{CurrentHealth:0.##}/{MaxHealth:0.##}",
                this
            );
        }

        /*
         * Если через SetHealth установили 0 HP,
         * запускаем обычную систему смерти.
         */
        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        CurrentHealth = 0f;

        if (debugLogs)
        {
            Debug.Log(
                "[PlayerHealth] Player died.",
                this
            );
        }

        // Финальное обновление HP UI.
        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );

        /*
         * ВАЖНО:
         *
         * Здесь НЕ отключаем:
         *
         * playerCombat.enabled = false;
         * playerMovement.enabled = false;
         *
         * Этим занимается PlayerDeathController.
         *
         * Так смерть контролируется одним классом.
         */

        if (combat != null &&
            combat.IsAttacking)
        {
            combat.CancelAttack();
        }

        // Передаём событие контроллеру смерти.
        Died?.Invoke();
    }

    // =========================================================
    // REVIVE
    // =========================================================

    public void Revive()
    {
        if (!IsDead)
            return;

        RestoreFullHealth();
    }

    // =========================================================
    // CURRENT HEALTH PERCENT
    // =========================================================

    public float GetHealthPercent()
    {
        return NormalizedHealth;
    }

    // =========================================================
    // DAMAGE TEST
    // =========================================================

    [ContextMenu("Test Damage 10")]
    private void TestDamage()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Test Heal 10")]
    private void TestHeal()
    {
        Heal(10f);
    }

    [ContextMenu("Test Full Heal")]
    private void TestFullHeal()
    {
        RestoreFullHealth();
    }

    [ContextMenu("Test Kill")]
    private void TestKill()
    {
        TakeDamage(MaxHealth);
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(
                maxHealth,
                1f
            );

        damageInvulnerability =
            Mathf.Max(
                damageInvulnerability,
                0f
            );
    }
}