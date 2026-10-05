using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public static InventoryManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("InventoryManager");
        return managerObject.AddComponent<InventoryManager>();
    }

    [Header("Inventory Settings")]
    [SerializeField] private int hotbarSlots = 9;
    [SerializeField] private int backpackSlots = 32;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    public event Action OnInventoryChanged;
    public event Action<int> OnSelectedSlotChanged;
    public IReadOnlyList<InventorySlot> Slots => slots;
    public int SelectedSlotIndex { get; private set; } = -1;
    public ItemData EquippedItem { get; private set; }

    public int HotbarSlots => Mathf.Min(hotbarSlots, slots.Count);
    public int TotalSlots => slots.Count;
    public bool IsFull => slots.FindAll(s => s.IsEmpty).Count == 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EquipmentManager.GetOrCreate();

        if (slots == null)
            slots = new List<InventorySlot>();

        while (slots.Count < backpackSlots)
            slots.Add(new InventorySlot());

        SelectSlot(0);
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return;

        if (SelectedSlotIndex != index)
        {
            SelectedSlotIndex = index;
            OnSelectedSlotChanged?.Invoke(index);
            OnInventoryChanged?.Invoke();
        }

        if (index >= 0 && index < HotbarSlots)
            TryEquipHotbarSlot(index);
    }

    public InventorySlot GetSelectedSlot()
    {
        if (SelectedSlotIndex < 0 || SelectedSlotIndex >= slots.Count)
            return null;

        return slots[SelectedSlotIndex];
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetTotalItemCount(item) >= amount;
    }

    public void UseSelectedItem()
    {
        InventorySlot selected = GetSelectedSlot();
        if (selected == null || selected.IsEmpty)
            return;

        if (selected.Item.itemType == ItemType.Consumable)
        {
            RemoveItem(selected.Item, 1);
            return;
        }

        if (selected.Item.IsWeapon)
            EquipItem(selected.Item);
    }

    public bool EquipItem(ItemData item)
    {
        if (item == null || !item.IsWeapon)
            return false;

        EquipmentManager equipment = EquipmentManager.Instance;
        if (equipment == null)
            equipment = EquipmentManager.GetOrCreate();

        if (!equipment.EquipWeapon(item))
            return false;

        EquippedItem = item;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void UnequipItem()
    {
        EquipmentManager equipment = EquipmentManager.Instance;
        equipment?.UnequipWeapon();

        EquippedItem = null;
        OnInventoryChanged?.Invoke();
    }

    private void TryEquipHotbarSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return;

        InventorySlot slot = slots[index];
        if (!slot.IsEmpty && slot.Item.IsWeapon)
            EquipItem(slot.Item);
    }

    public void DropItem(int slotIndex, bool dropAll = false)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty)
            return;

        int dropAmount = dropAll ? slot.Amount : 1;

        Transform playerTransform = FindPlayerTransform();
        if (playerTransform != null && slot.Item.dropPrefab != null)
        {
            Vector3 dropPos =
                playerTransform.position +
                playerTransform.forward * 2f +
                Vector3.up * 0.5f;

            GameObject dropped =
                Instantiate(slot.Item.dropPrefab, dropPos, Quaternion.identity);

            ItemPickup itemPickup = dropped.GetComponent<ItemPickup>();
            if (itemPickup == null)
                itemPickup = dropped.AddComponent<ItemPickup>();

            PickupItem pickup = dropped.GetComponent<PickupItem>();
            if (pickup != null)
                pickup.quantity = dropAmount;

            if (EquippedItem == slot.Item)
                UnequipItem();

            RemoveItem(slot.Item, dropAmount);
            return;
        }

        if (EquippedItem == slot.Item)
            UnequipItem();

        RemoveItem(slot.Item, dropAmount);
    }

    public void DropSelectedItem(bool dropAll = false)
    {
        if (SelectedSlotIndex >= 0)
            DropItem(SelectedSlotIndex, dropAll);
    }

    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= slots.Count ||
            toIndex < 0 || toIndex >= slots.Count ||
            fromIndex == toIndex)
            return;

        InventorySlot fromSlot = slots[fromIndex];
        InventorySlot toSlot = slots[toIndex];

        if (!fromSlot.IsEmpty && !toSlot.IsEmpty &&
            toSlot.Item == fromSlot.Item &&
            toSlot.Item.stackable)
        {
            int maxAdd = toSlot.Item.maxStack - toSlot.Amount;
            int addAmount = Mathf.Min(maxAdd, fromSlot.Amount);
            if (addAmount > 0)
            {
                toSlot.Amount += addAmount;
                fromSlot.Amount -= addAmount;
                if (fromSlot.Amount <= 0)
                    fromSlot.Clear();

                OnInventoryChanged?.Invoke();
                return;
            }
        }

        InventorySlot temp = slots[fromIndex];
        slots[fromIndex] = slots[toIndex];
        slots[toIndex] = temp;

        if (SelectedSlotIndex == fromIndex)
            SelectedSlotIndex = toIndex;
        else if (SelectedSlotIndex == toIndex)
            SelectedSlotIndex = fromIndex;

        OnInventoryChanged?.Invoke();
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (item.stackable)
        {
            int freeSpace = 0;
            foreach (InventorySlot slot in slots)
            {
                if (!slot.IsEmpty && slot.Item == item)
                    freeSpace += Mathf.Max(0, item.maxStack - slot.Amount);
            }

            int emptySlots = slots.FindAll(s => s.IsEmpty).Count;
            if (freeSpace + emptySlots * item.maxStack < amount)
                return false;

            foreach (InventorySlot slot in slots)
            {
                if (amount <= 0 || slot.IsEmpty || slot.Item != item || slot.Amount >= item.maxStack)
                    continue;

                int addAmount = Mathf.Min(item.maxStack - slot.Amount, amount);
                slot.Amount += addAmount;
                amount -= addAmount;
            }
        }
        else if (slots.FindAll(s => s.IsEmpty).Count < amount)
        {
            return false;
        }

        while (amount > 0)
        {
            InventorySlot emptySlot = slots.Find(s => s.IsEmpty);
            if (emptySlot == null)
                return false;

            int fill = item.stackable ? Mathf.Min(amount, item.maxStack) : 1;
            emptySlot.Set(item, fill);
            amount -= fill;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (GetTotalItemCount(item) < amount)
            return false;

        int remaining = amount;
        List<InventorySlot> slotsWithItem = slots.FindAll(s => !s.IsEmpty && s.Item == item);

        foreach (InventorySlot slot in slotsWithItem)
        {
            if (slot.Amount > remaining)
            {
                slot.Amount -= remaining;
                remaining = 0;
                break;
            }

            remaining -= slot.Amount;
            slot.Clear();
        }

        if (remaining > 0)
            return false;

        if (EquippedItem == item && GetTotalItemCount(item) <= 0)
            UnequipItem();

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetTotalItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        int count = 0;
        foreach (InventorySlot slot in slots)
        {
            if (!slot.IsEmpty && slot.Item == item)
                count += slot.Amount;
        }

        return count;
    }

    public void ClearInventory()
    {
        UnequipItem();

        foreach (InventorySlot slot in slots)
            slot.Clear();

        SelectedSlotIndex = 0;
        EquippedItem = null;
        OnInventoryChanged?.Invoke();
        OnSelectedSlotChanged?.Invoke(0);
    }

    private static Transform FindPlayerTransform()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
            return taggedPlayer.transform;

        PlayerCombat combat = FindAnyObjectByType<PlayerCombat>();
        return combat != null ? combat.transform : null;
    }
}

[Serializable]
public class InventorySlot
{
    public ItemData Item;
    public int Amount;

    public bool IsEmpty => Item == null;

    public void Set(ItemData item, int amount)
    {
        Item = item;
        Amount = amount;
    }

    public void Clear()
    {
        Item = null;
        Amount = 0;
    }
}
