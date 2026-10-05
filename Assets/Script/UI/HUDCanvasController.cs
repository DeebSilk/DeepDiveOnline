using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Connects an editable Canvas HUD to gameplay data. Its visual hierarchy is
/// created in the scene and may be freely redesigned in the Inspector.
/// </summary>
public class HUDCanvasController : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthText;

    [Header("Hotbar UI")]
    [SerializeField] private GameObject hotbarRoot;
    [SerializeField] private Image[] slotIcons = new Image[9];
    [SerializeField] private Text[] slotAmounts = new Text[9];

    [Header("Optional panels")]
    [SerializeField] private InventoryUI inventoryUi;
    [SerializeField] private PauseMenu pauseMenu;

    private PlayerHealth playerHealth;
    private InventoryManager inventory;

    private void Start()
    {
        if (hotbarRoot == null)
            hotbarRoot = transform.Find("Hotbar")?.gameObject;

        SetHotbarVisible(false);
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += OnHealthChanged;
            RefreshHealth();
        }

        TrySubscribeInventory();
    }

    private void Update()
    {
        TrySubscribeInventory();
        SetHotbarVisible(Keyboard.current != null && Keyboard.current.tabKey.isPressed);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;

        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshHotbar;
    }

    public void TogglePause()
    {
        pauseMenu ??= FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pauseMenu == null)
        {
            GameStateManager.Instance?.TogglePause();
            return;
        }

        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
            pauseMenu.Resume();
        else
            pauseMenu.Pause();
    }

    public void ToggleInventory()
    {
        inventoryUi ??= FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (inventoryUi == null)
            return;

        bool shouldShow = !inventoryUi.gameObject.activeSelf;
        inventoryUi.gameObject.SetActive(shouldShow);
        Cursor.visible = shouldShow;
        Cursor.lockState = shouldShow ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void Configure(Image fill, Text label, GameObject hotbar, Image[] icons, Text[] amounts, InventoryUI linkedInventory, PauseMenu linkedPause)
    {
        healthFill = fill;
        healthText = label;
        hotbarRoot = hotbar;
        slotIcons = icons;
        slotAmounts = amounts;
        inventoryUi = linkedInventory;
        pauseMenu = linkedPause;
    }

    private void TrySubscribeInventory()
    {
        if (inventory != null || InventoryManager.Instance == null)
            return;

        inventory = InventoryManager.Instance;
        inventory.OnInventoryChanged += RefreshHotbar;
        RefreshHotbar();
    }

    private void OnHealthChanged(float _, float __) => RefreshHealth();

    private void RefreshHealth()
    {
        if (playerHealth == null)
            return;

        if (healthFill != null)
            healthFill.fillAmount = playerHealth.NormalizedHealth;

        if (healthText != null)
            healthText.text = $"HP  {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {Mathf.CeilToInt(playerHealth.MaxHealth)}";
    }

    private void RefreshHotbar()
    {
        if (inventory == null)
            return;

        for (int i = 0; i < slotIcons.Length; i++)
        {
            InventorySlot slot = i < inventory.Slots.Count ? inventory.Slots[i] : null;
            bool hasItem = slot != null && !slot.IsEmpty;

            if (slotIcons[i] != null)
            {
                slotIcons[i].enabled = hasItem && slot.Item.icon != null;
                slotIcons[i].sprite = hasItem ? slot.Item.icon : null;
            }

            if (i < slotAmounts.Length && slotAmounts[i] != null)
            {
                if (!hasItem)
                    slotAmounts[i].text = string.Empty;
                else if (slot.Item.icon == null)
                    slotAmounts[i].text = slot.Item.itemName;
                else
                    slotAmounts[i].text = slot.Item.stackable && slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
            }
        }
    }

    private void SetHotbarVisible(bool visible)
    {
        if (hotbarRoot != null && hotbarRoot.activeSelf != visible)
            hotbarRoot.SetActive(visible);
    }
}
