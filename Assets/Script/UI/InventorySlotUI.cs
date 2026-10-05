using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image selectionHighlight;
    [SerializeField] private Image hoverHighlight;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.035f, 0.05f, 0.08f, 0.9f);
    [SerializeField] private Color selectedColor = new Color(0.95f, 0.68f, 0.16f, 0.95f);
    [SerializeField] private Color hoverColor = new Color(0.24f, 0.55f, 0.8f, 0.5f);
    [SerializeField] private Color emptyColor = new Color(0.035f, 0.05f, 0.08f, 0.5f);

    private InventorySlot slot;
    private Action<InventorySlot> clickCallback;
    private Action<int, int> onDropCallback; // fromIndex, toIndex
    private Image background;
    private int slotIndex;
    private bool isSelected;

    public int SlotIndex => slotIndex;
    public InventorySlot Slot => slot;

    public void Setup(int index, InventorySlot inventorySlot, Action<InventorySlot> callback, Action<int, int> dropCallback)
    {
        slotIndex = index;
        slot = inventorySlot;
        clickCallback = callback;
        onDropCallback = dropCallback;

        Refresh();
    }

    public void Refresh()
    {
        if (background == null)
            background = GetComponent<Image>();

        if (slot == null || slot.IsEmpty)
        {
            iconImage.gameObject.SetActive(false);
            amountText.text = string.Empty;
            background.color = isSelected ? selectedColor : emptyColor;
        }
        else
        {
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = slot.Item.icon;
            amountText.text = slot.Item.stackable && slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
            background.color = isSelected ? selectedColor : normalColor;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        Refresh();
    }

    private void Awake()
    {
        background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();

        if (selectionHighlight != null)
            selectionHighlight.gameObject.SetActive(false);

        if (hoverHighlight != null)
            hoverHighlight.gameObject.SetActive(false);
    }

    // ====== Клик ======
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            clickCallback?.Invoke(slot);
        }
        else if (eventData.button == PointerEventData.InputButton.Right && slot != null && !slot.IsEmpty)
        {
            // ПКМ — выбросить 1 предмет
            InventoryManager.Instance?.DropItem(slotIndex, false);
        }
    }

    // ====== Hover ======
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverHighlight != null)
            hoverHighlight.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverHighlight != null)
            hoverHighlight.gameObject.SetActive(false);
    }

    // ====== Drag & Drop ======
    private GameObject dragIcon;
    private int dragFromIndex = -1;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slot == null || slot.IsEmpty)
            return;

        dragFromIndex = slotIndex;

        // Создаём иконку перетаскивания
        dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(Image));
        dragIcon.transform.SetParent(transform.root, false);
        dragIcon.transform.SetAsLastSibling();

        Image dragImage = dragIcon.GetComponent<Image>();
        dragImage.sprite = slot.Item.icon;
        dragImage.raycastTarget = false;
        dragImage.preserveAspect = true;

        RectTransform rect = dragIcon.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(64f, 64f);

        // Делаем полупрозрачным оригинал
        if (background != null)
            background.color = new Color(background.color.r, background.color.g, background.color.b, 0.3f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            Destroy(dragIcon);

        // Возвращаем цвет
        if (background != null)
            Refresh();

        // Если дропнули не на слот — выбрасываем предмет
        if (dragFromIndex >= 0)
        {
            // pointerCurrentRaycast - это структура RaycastResult, проверяем через isValid
            bool droppedOnSlot = eventData.pointerCurrentRaycast.isValid &&
                                 eventData.pointerCurrentRaycast.gameObject != null &&
                                 eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<InventorySlotUI>() != null;

            if (!droppedOnSlot)
            {
                // Выбросить предмет
                InventoryManager.Instance?.DropItem(dragFromIndex, true);
            }
        }

        dragFromIndex = -1;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
        if (draggedSlot != null && draggedSlot.slotIndex != slotIndex)
        {
            onDropCallback?.Invoke(draggedSlot.slotIndex, slotIndex);
        }
    }
}