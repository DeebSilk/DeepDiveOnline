using System;
using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("=== MP ===")]
    [SerializeField, Min(0f)]
    private float maxMP = 100f;

    [SerializeField, Min(0f)]
    private float mpRegenerationRate = 5f;

    [SerializeField, Min(0f)]
    private float mpRegenerationDelay = 0f;

    [SerializeField]
    private bool enableMPRegeneration = true;

    [Header("=== STAMINA ===")]
    [SerializeField, Min(0f)]
    private float maxStamina = 100f;

    [SerializeField, Min(0f)]
    private float staminaRegenerationRate = 15f;

    [SerializeField, Min(0f)]
    private float staminaRegenerationDelay = 2f;

    [SerializeField]
    private bool enableStaminaRegeneration = true;

    // =========================================================
    // PUBLIC PROPERTIES
    // =========================================================

    public float CurrentMP
    {
        get;
        private set;
    }

    public float MaxMP =>
        maxMP;

    public float NormalizedMP
    {
        get
        {
            if (MaxMP <= 0f)
                return 0f;

            return Mathf.Clamp01(
                CurrentMP / MaxMP
            );
        }
    }

    public float CurrentStamina
    {
        get;
        private set;
    }

    public float MaxStamina =>
        maxStamina;

    public float NormalizedStamina
    {
        get
        {
            if (MaxStamina <= 0f)
                return 0f;

            return Mathf.Clamp01(
                CurrentStamina / MaxStamina
            );
        }
    }

    // =========================================================
    // EVENTS
    // =========================================================

    /// <summary>
    /// Вызывается при изменении MP.
    /// float 1 = текущее MP.
    /// float 2 = максимальное MP.
    /// </summary>
    public event Action<float, float> MPChanged;

    /// <summary>
    /// Вызывается при изменении Stamina.
    /// float 1 = текущая Stamina.
    /// float 2 = максимальная Stamina.
    /// </summary>
    public event Action<float, float> StaminaChanged;

    // =========================================================
    // PRIVATE
    // =========================================================

    private float mpRegenerationBlockedUntil;
    private float staminaRegenerationBlockedUntil;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ClampConfiguration();

        CurrentMP = MaxMP;
        CurrentStamina = MaxStamina;

        mpRegenerationBlockedUntil = 0f;
        staminaRegenerationBlockedUntil = 0f;

        MPChanged?.Invoke(
            CurrentMP,
            MaxMP
        );

        StaminaChanged?.Invoke(
            CurrentStamina,
            MaxStamina
        );
    }

    private void Update()
    {
        bool valuesChanged =
            ClampConfiguration() |
            ClampCurrentValues();

        if (valuesChanged)
        {
            MPChanged?.Invoke(
                CurrentMP,
                MaxMP
            );

            StaminaChanged?.Invoke(
                CurrentStamina,
                MaxStamina
            );
        }

        RegenerateMP();
        RegenerateStamina();
    }

    private void OnValidate()
    {
        ClampConfiguration();
    }

    // =========================================================
    // MP
    // =========================================================

    public bool ConsumeMP(
        float amount
    )
    {
        if (amount <= 0f || CurrentMP < amount)
            return false;

        CurrentMP = Mathf.Clamp(
            CurrentMP - amount,
            0f,
            MaxMP
        );

        mpRegenerationBlockedUntil =
            Time.time + mpRegenerationDelay;

        MPChanged?.Invoke(
            CurrentMP,
            MaxMP
        );

        return true;
    }

    public void RestoreMP(
        float amount
    )
    {
        if (amount <= 0f)
            return;

        float oldMP =
            CurrentMP;

        CurrentMP = Mathf.Clamp(
            CurrentMP + amount,
            0f,
            MaxMP
        );

        if (Mathf.Approximately(oldMP, CurrentMP))
            return;

        MPChanged?.Invoke(
            CurrentMP,
            MaxMP
        );
    }

    public void RestoreFullMP()
    {
        SetMP(MaxMP);
    }

    public void SetMP(
        float value
    )
    {
        float newMP = Mathf.Clamp(
            value,
            0f,
            MaxMP
        );

        if (Mathf.Approximately(CurrentMP, newMP))
            return;

        CurrentMP = newMP;

        MPChanged?.Invoke(
            CurrentMP,
            MaxMP
        );
    }

    // =========================================================
    // STAMINA
    // =========================================================

    public bool ConsumeStamina(
        float amount
    )
    {
        if (amount <= 0f || CurrentStamina < amount)
            return false;

        CurrentStamina = Mathf.Clamp(
            CurrentStamina - amount,
            0f,
            MaxStamina
        );

        staminaRegenerationBlockedUntil =
            Time.time + staminaRegenerationDelay;

        StaminaChanged?.Invoke(
            CurrentStamina,
            MaxStamina
        );

        return true;
    }

    public void RestoreStamina(
        float amount
    )
    {
        if (amount <= 0f)
            return;

        float oldStamina =
            CurrentStamina;

        CurrentStamina = Mathf.Clamp(
            CurrentStamina + amount,
            0f,
            MaxStamina
        );

        if (Mathf.Approximately(oldStamina, CurrentStamina))
            return;

        StaminaChanged?.Invoke(
            CurrentStamina,
            MaxStamina
        );
    }

    public void RestoreFullStamina()
    {
        SetStamina(MaxStamina);
    }

    public void SetStamina(
        float value
    )
    {
        float newStamina = Mathf.Clamp(
            value,
            0f,
            MaxStamina
        );

        if (Mathf.Approximately(CurrentStamina, newStamina))
            return;

        CurrentStamina = newStamina;

        StaminaChanged?.Invoke(
            CurrentStamina,
            MaxStamina
        );
    }

    // =========================================================
    // REGENERATION
    // =========================================================

    private void RegenerateMP()
    {
        if (!enableMPRegeneration ||
            mpRegenerationRate <= 0f ||
            CurrentMP >= MaxMP ||
            Time.time < mpRegenerationBlockedUntil)
        {
            return;
        }

        RestoreMP(
            mpRegenerationRate * Time.deltaTime
        );
    }

    private void RegenerateStamina()
    {
        if (!enableStaminaRegeneration ||
            staminaRegenerationRate <= 0f ||
            CurrentStamina >= MaxStamina ||
            Time.time < staminaRegenerationBlockedUntil)
        {
            return;
        }

        RestoreStamina(
            staminaRegenerationRate * Time.deltaTime
        );
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool ClampConfiguration()
    {
        float oldMaxMP = maxMP;
        float oldMaxStamina = maxStamina;

        maxMP = Mathf.Max(maxMP, 0f);
        maxStamina = Mathf.Max(maxStamina, 0f);

        mpRegenerationRate =
            Mathf.Max(mpRegenerationRate, 0f);

        staminaRegenerationRate =
            Mathf.Max(staminaRegenerationRate, 0f);

        mpRegenerationDelay =
            Mathf.Max(mpRegenerationDelay, 0f);

        staminaRegenerationDelay =
            Mathf.Max(staminaRegenerationDelay, 0f);

        return !Mathf.Approximately(oldMaxMP, maxMP) ||
               !Mathf.Approximately(oldMaxStamina, maxStamina);
    }

    private bool ClampCurrentValues()
    {
        float oldMP =
            CurrentMP;

        float oldStamina =
            CurrentStamina;

        CurrentMP = Mathf.Clamp(
            CurrentMP,
            0f,
            MaxMP
        );

        CurrentStamina = Mathf.Clamp(
            CurrentStamina,
            0f,
            MaxStamina
        );

        return !Mathf.Approximately(oldMP, CurrentMP) ||
               !Mathf.Approximately(oldStamina, CurrentStamina);
    }
}