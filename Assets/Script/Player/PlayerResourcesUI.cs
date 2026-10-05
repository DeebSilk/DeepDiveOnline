using UnityEngine;
using UnityEngine.UI;

public class PlayerResourcesUI : MonoBehaviour
{
    [Header("PLAYER")]
    [SerializeField] private PlayerResources playerResources;

    [Header("RESOURCE BARS")]
    [SerializeField] private Image mpFill;
    [SerializeField] private Image staminaFill;

    private bool subscribed;
    private bool warnedAboutMissingResources;

    private void Awake()
    {
        ConfigureFillImage(mpFill);
        ConfigureFillImage(staminaFill);
    }

    private void Start()
    {
        FindPlayerResources();

        if (playerResources == null)
        {
            WarnAboutMissingResources();
            return;
        }

        Subscribe();

        // Первичная установка значений.
        RefreshAll();
    }

    private void Update()
    {
        if (playerResources == null)
        {
            FindPlayerResources();

            if (playerResources == null)
                return;

            Subscribe();
        }

        // Дополнительная синхронизация HUD.
        // События остаются основным способом обновления,
        // но это гарантирует, что полоски всегда соответствуют ресурсам.
        RefreshAll();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    // =========================================================
    // PLAYER RESOURCES
    // =========================================================

    private void FindPlayerResources()
    {
        if (playerResources != null)
            return;

        // Если UI находится внутри PlayerRoot.
        playerResources =
            GetComponentInParent<PlayerResources>();

        if (playerResources != null)
            return;

        // Если Canvas находится отдельно от PlayerRoot.
        playerResources =
            FindAnyObjectByType<PlayerResources>();
    }

    private void Subscribe()
    {
        if (playerResources == null || subscribed)
            return;

        playerResources.MPChanged += OnMPChanged;
        playerResources.StaminaChanged += OnStaminaChanged;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (playerResources == null || !subscribed)
            return;

        playerResources.MPChanged -= OnMPChanged;
        playerResources.StaminaChanged -= OnStaminaChanged;

        subscribed = false;
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void OnMPChanged(
        float current,
        float maximum)
    {
        RefreshMP(current, maximum);
    }

    private void OnStaminaChanged(
        float current,
        float maximum)
    {
        RefreshStamina(current, maximum);
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshAll()
    {
        if (playerResources == null)
            return;

        RefreshMP(
            playerResources.CurrentMP,
            playerResources.MaxMP
        );

        RefreshStamina(
            playerResources.CurrentStamina,
            playerResources.MaxStamina
        );
    }

    private void RefreshMP(
        float current,
        float maximum)
    {
        if (mpFill == null)
            return;

        if (maximum <= 0f)
        {
            mpFill.fillAmount = 0f;
            return;
        }

        mpFill.fillAmount =
            Mathf.Clamp01(current / maximum);
    }

    private void RefreshStamina(
        float current,
        float maximum)
    {
        if (staminaFill == null)
            return;

        if (maximum <= 0f)
        {
            staminaFill.fillAmount = 0f;
            return;
        }

        staminaFill.fillAmount =
            Mathf.Clamp01(current / maximum);
    }

    // =========================================================
    // IMAGE CONFIGURATION
    // =========================================================

    private static void ConfigureFillImage(
        Image fill)
    {
        if (fill == null)
            return;

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillClockwise = true;
    }

    // =========================================================
    // WARNING
    // =========================================================

    private void WarnAboutMissingResources()
    {
        if (warnedAboutMissingResources)
            return;

        warnedAboutMissingResources = true;

        Debug.LogWarning(
            "[PlayerResourcesUI] PlayerResources not found. " +
            "MP and Stamina bars will not be updated.",
            this
        );
    }
}

