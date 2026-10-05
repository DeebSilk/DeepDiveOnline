using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Creates the runtime HUD: health, pause control and a nine-slot hotbar.
/// Attach it to Player to change its colors, sizes and layout in the Inspector.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Style")]
    [SerializeField] private Color panelColor = new Color(0.035f, 0.05f, 0.08f, 0.9f);
    [SerializeField] private Color borderColor = new Color(0.24f, 0.55f, 0.8f, 0.95f);
    [SerializeField] private Color selectedColor = new Color(0.95f, 0.68f, 0.16f, 0.95f);
    [SerializeField] private Color healthColor = new Color(0.15f, 0.82f, 0.34f, 1f);
    [SerializeField, Range(42f, 96f)] private float slotSize = 64f;
    [SerializeField, Range(0f, 20f)] private float slotSpacing = 6f;

    public int SelectedSlotIndex { get; private set; }

    private Image healthFill;
    private Text healthText;
    private Image[] slotBackgrounds;
    private Image[] slotIcons;
    private Text[] slotAmounts;
    private PlayerHealth playerHealth;
    private InventoryManager inventory;
    private InventoryUI inventoryUi;
    private bool inventorySubscribed;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        BuildHud();
    }

    private void Start()
    {
        SubscribeToSystems();
        RefreshHealth();
        RefreshHotbar();
    }

    private void Update()
    {
        SubscribeToSystems();

        if (Keyboard.current == null)
            return;

        // Выбор слота по цифрам
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        else if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
        else if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectSlot(5);
        else if (Keyboard.current.digit7Key.wasPressedThisFrame) SelectSlot(6);
        else if (Keyboard.current.digit8Key.wasPressedThisFrame) SelectSlot(7);
        else if (Keyboard.current.digit9Key.wasPressedThisFrame) SelectSlot(8);

        // Использование предмета из выбранного слота (E)
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            inventory?.UseSelectedItem();
        }

        // Выброс предмета из выбранного слота (G - выбросить 1, Shift+G - выбросить весь стак)
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            bool dropAll = Keyboard.current.shiftKey.isPressed;
            inventory?.DropSelectedItem(dropAll);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;

        if (inventory != null && inventorySubscribed)
        {
            inventory.OnInventoryChanged -= RefreshHotbar;
            inventory.OnSelectedSlotChanged -= OnInventorySelectedSlotChanged;
        }
    }

    private void SubscribeToSystems()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= OnHealthChanged;
                playerHealth.HealthChanged += OnHealthChanged;
                RefreshHealth();
            }
        }

        if (!inventorySubscribed && InventoryManager.Instance != null)
        {
            inventory = InventoryManager.Instance;
            inventory.OnInventoryChanged += RefreshHotbar;
            inventory.OnSelectedSlotChanged += OnInventorySelectedSlotChanged;
            inventorySubscribed = true;
            RefreshHotbar();

            // Синхронизируем выбранный слот
            SelectedSlotIndex = inventory.SelectedSlotIndex;
            UpdateSlotHighlights();
        }
    }

    private void OnInventorySelectedSlotChanged(int index)
    {
        SelectedSlotIndex = index;
        UpdateSlotHighlights();
    }

    private void BuildHud()
    {
        GameObject root = new GameObject("Player HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        BuildHealth(root.transform);
        BuildInventoryButton(root.transform);
        BuildPauseButton(root.transform);
        BuildHotbar(root.transform);
    }

    private void BuildHealth(Transform parent)
    {
        Image background = CreateImage("Health", parent, panelColor);
        RectTransform rect = background.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(28f, -28f);
        rect.sizeDelta = new Vector2(300f, 42f);

        healthFill = CreateImage("Fill", background.transform, healthColor);
        RectTransform fill = healthFill.rectTransform;
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(0f, 1f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.anchoredPosition = new Vector2(4f, 0f);
        fill.sizeDelta = new Vector2(292f, -8f);

        healthText = CreateText("Label", background.transform, 18, TextAnchor.MiddleCenter);
        Stretch(healthText.rectTransform, 6f);
    }

    private void BuildPauseButton(Transform parent)
    {
        Image background = CreateImage("Pause", parent, panelColor);
        RectTransform rect = background.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(138f, 44f);

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(TogglePause);

        Text label = CreateText("Label", background.transform, 17, TextAnchor.MiddleCenter);
        label.text = "PAUSE";
        Stretch(label.rectTransform, 4f);
    }

    private void BuildInventoryButton(Transform parent)
    {
        Image background = CreateImage("Inventory", parent, panelColor);
        RectTransform rect = background.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-176f, -28f);
        rect.sizeDelta = new Vector2(138f, 44f);

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(ToggleInventory);

        Text label = CreateText("Label", background.transform, 17, TextAnchor.MiddleCenter);
        label.text = "INVENTORY";
        Stretch(label.rectTransform, 4f);
    }

    private void BuildHotbar(Transform parent)
    {
        slotBackgrounds = new Image[9];
        slotIcons = new Image[9];
        slotAmounts = new Text[9];
        float width = slotSize * 9f + slotSpacing * 8f;

        for (int i = 0; i < 9; i++)
        {
            Image slot = CreateImage($"Slot {i + 1}", parent, panelColor);
            RectTransform rect = slot.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(-width * 0.5f + i * (slotSize + slotSpacing), 28f);
            rect.sizeDelta = new Vector2(slotSize, slotSize);
            slotBackgrounds[i] = slot;

            int slotIndex = i;
            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slot;
            button.onClick.AddListener(() => SelectSlot(slotIndex));

            Image icon = CreateImage("Icon", slot.transform, Color.white);
            icon.preserveAspect = true;
            Stretch(icon.rectTransform, 8f);
            icon.enabled = false;
            icon.raycastTarget = false;
            slotIcons[i] = icon;

            Text number = CreateText("Number", slot.transform, 15, TextAnchor.UpperLeft);
            number.text = (i + 1).ToString();
            number.rectTransform.anchorMin = new Vector2(0f, 1f);
            number.rectTransform.anchorMax = new Vector2(0f, 1f);
            number.rectTransform.pivot = new Vector2(0f, 1f);
            number.rectTransform.anchoredPosition = new Vector2(6f, -4f);
            number.rectTransform.sizeDelta = new Vector2(24f, 20f);
            number.raycastTarget = false;

            Text amount = CreateText("Amount", slot.transform, 15, TextAnchor.LowerRight);
            amount.rectTransform.anchorMin = new Vector2(1f, 0f);
            amount.rectTransform.anchorMax = new Vector2(1f, 0f);
            amount.rectTransform.pivot = new Vector2(1f, 0f);
            amount.rectTransform.anchoredPosition = new Vector2(-6f, 4f);
            amount.rectTransform.sizeDelta = new Vector2(36f, 20f);
            amount.raycastTarget = false;
            slotAmounts[i] = amount;
        }

        SelectSlot(0);
    }

    private void OnHealthChanged(float _, float __) => RefreshHealth();

    private void RefreshHealth()
    {
        if (playerHealth == null || healthFill == null || healthText == null)
            return;

        healthFill.rectTransform.sizeDelta = new Vector2(292f * playerHealth.NormalizedHealth, -8f);
        healthText.text = $"HP  {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {Mathf.CeilToInt(playerHealth.MaxHealth)}";
    }

    private void RefreshHotbar()
    {
        if (slotIcons == null)
            return;

        for (int i = 0; i < slotIcons.Length; i++)
        {
            InventorySlot slot = inventory != null && i < inventory.Slots.Count ? inventory.Slots[i] : null;
            bool hasItem = slot != null && !slot.IsEmpty;
            slotIcons[i].enabled = hasItem && slot.Item.icon != null;
            slotIcons[i].sprite = hasItem ? slot.Item.icon : null;
            slotAmounts[i].text = hasItem && slot.Item.stackable && slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
        }

        UpdateSlotHighlights();
    }

    private void UpdateSlotHighlights()
    {
        if (slotBackgrounds == null)
            return;

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            slotBackgrounds[i].color = i == SelectedSlotIndex ? selectedColor : panelColor;
        }
    }

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= 9 || slotBackgrounds == null)
            return;

        SelectedSlotIndex = index;

        // Синхронизируем с InventoryManager
        if (inventory != null)
            inventory.SelectSlot(index);

        UpdateSlotHighlights();
    }

    private void TogglePause()
    {
        PauseMenu pauseMenu = FindAnyObjectByType<PauseMenu>();
        if (pauseMenu != null)
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsPaused)
                pauseMenu.Resume();
            else
                pauseMenu.Pause();
            return;
        }

        GameStateManager.Instance?.TogglePause();
    }

    private void ToggleInventory()
    {
        if (inventoryUi == null)
            inventoryUi = FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);

        if (inventoryUi == null)
            return;

        bool show = !inventoryUi.gameObject.activeSelf;
        inventoryUi.gameObject.SetActive(show);
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        Text text = gameObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }
}