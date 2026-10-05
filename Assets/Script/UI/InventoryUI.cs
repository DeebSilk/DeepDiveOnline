using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private Image detailsIcon;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button useButton;

    [Header("Layout")]
    [SerializeField] private int slotsPerRow = 8;
    [SerializeField] private float slotSize = 64f;
    [SerializeField] private float slotSpacing = 6f;

    private InventorySlotUI[] slotUIs;
    private InventoryManager inventory;

    private void Awake()
    {
        if (slotContainer == null)
            Debug.LogError("InventoryUI: slotContainer is not assigned.", this);

        if (slotPrefab == null)
            Debug.LogError("InventoryUI: slotPrefab is not assigned.", this);

        if (detailsPanel != null)
            detailsPanel.SetActive(false);

        // Подписка на кнопки
        if (dropButton != null)
            dropButton.onClick.AddListener(DropSelectedItem);

        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedItem);
    }

    private void Start()
    {
        inventory = InventoryManager.Instance;
        if (inventory != null)
        {
            inventory.OnInventoryChanged += Refresh;
            inventory.OnSelectedSlotChanged += OnSelectedSlotChanged;
            Refresh();
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= Refresh;
            inventory.OnSelectedSlotChanged -= OnSelectedSlotChanged;
        }
    }

    private void OnSelectedSlotChanged(int index)
    {
        // Обновляем подсветку выбранного слота
        if (slotUIs == null)
            return;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].SetSelected(i == index);
        }

        // Обновляем панель деталей для выбранного слота
        if (index >= 0 && index < inventory.Slots.Count)
            ShowItemDetails(inventory.Slots[index]);
        else
            HideDetails();
    }

    public void Refresh()
    {
        if (inventory == null || slotContainer == null || slotPrefab == null)
            return;

        // Создаём слоты если их нет или количество изменилось
        if (slotUIs == null || slotUIs.Length != inventory.TotalSlots)
        {
            foreach (Transform child in slotContainer)
                Destroy(child.gameObject);

            slotUIs = new InventorySlotUI[inventory.TotalSlots];

            // Создаём сетку с переносом по строкам
            for (int i = 0; i < slotUIs.Length; i++)
            {
                var slotUI = Instantiate(slotPrefab, slotContainer);
                RectTransform rect = slotUI.GetComponent<RectTransform>();

                int row = i / slotsPerRow;
                int col = i % slotsPerRow;
                float xPos = col * (slotSize + slotSpacing);
                float yPos = -row * (slotSize + slotSpacing);

                rect.anchoredPosition = new Vector2(xPos, yPos);
                rect.sizeDelta = new Vector2(slotSize, slotSize);

                slotUIs[i] = slotUI;
            }
        }

        // Обновляем все слоты (создаём локальную копию i для лямбды)
        for (int i = 0; i < slotUIs.Length; i++)
        {
            int slotIndex = i; // Локальная копия для замыкания
            slotUIs[i].Setup(slotIndex, inventory.Slots[slotIndex], (slot) => OnSlotClicked(slotIndex), OnDropBetweenSlots);
            slotUIs[i].SetSelected(slotIndex == inventory.SelectedSlotIndex);
        }

        // Обновляем панель деталей
        if (inventory.SelectedSlotIndex >= 0)
            ShowItemDetails(inventory.Slots[inventory.SelectedSlotIndex]);
        else
            HideDetails();
    }

    private void OnSlotClicked(int index)
    {
        if (inventory == null)
            return;

        InventorySlot slot = inventory.Slots[index];
        if (slot == null || slot.IsEmpty)
            return;

        // Выбираем слот
        inventory.SelectSlot(index);
        ShowItemDetails(slot);
    }

    private void OnDropBetweenSlots(int fromIndex, int toIndex)
    {
        inventory?.SwapSlots(fromIndex, toIndex);
    }

    public void ShowItemDetails(InventorySlot slot)
    {
        if (detailsPanel == null || slot == null || slot.IsEmpty)
        {
            HideDetails();
            return;
        }

        if (itemNameText != null)
            itemNameText.text = slot.Item.itemName;

        if (itemDescriptionText != null)
            itemDescriptionText.text = slot.Item.description;

        if (detailsIcon != null)
        {
            detailsIcon.sprite = slot.Item.icon;
            detailsIcon.enabled = slot.Item.icon != null;
        }

        detailsPanel.SetActive(true);
    }

    public void HideDetails()
    {
        if (detailsPanel != null)
            detailsPanel.SetActive(false);
    }

    private void DropSelectedItem()
    {
        inventory?.DropSelectedItem(true);
    }

    private void UseSelectedItem()
    {
        inventory?.UseSelectedItem();
    }
}