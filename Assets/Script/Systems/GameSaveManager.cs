using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Stores the current game in a JSON file. F5 saves and F9 restores the last
/// save. The data includes player position, facing direction, health, inventory,
/// selected hotbar slot and equipped item.
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    private const string SaveFileName = "savegame.json";

    [SerializeField] private bool loadOnStart = true;
    [SerializeField, Min(10f)] private float autoSaveInterval = 60f;

    private PlayerHealth playerHealth;
    private float nextAutoSaveTime;

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private IEnumerator Start()
    {
        yield return null;

        if (loadOnStart)
            LoadGame();

        nextAutoSaveTime = Time.unscaledTime + autoSaveInterval;
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.f5Key.wasPressedThisFrame)
                SaveGame();
            else if (Keyboard.current.f9Key.wasPressedThisFrame)
                LoadGame();
        }

        if (Time.unscaledTime >= nextAutoSaveTime)
        {
            SaveGame();
            nextAutoSaveTime = Time.unscaledTime + autoSaveInterval;
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData
        {
            playerPosition = transform.position,
            playerRotationY = transform.eulerAngles.y,
            health = playerHealth != null ? playerHealth.CurrentHealth : 0f,
            selectedSlotIndex = InventoryManager.Instance != null ? InventoryManager.Instance.SelectedSlotIndex : 0,
            equippedItemId = InventoryManager.Instance != null && InventoryManager.Instance.EquippedItem != null
                ? InventoryManager.Instance.EquippedItem.itemId
                : string.Empty,
            inventory = BuildInventoryData()
        };

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"Game saved to {SavePath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not save the game: {exception.Message}", this);
        }
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath))
            return;

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null)
                return;

            transform.SetPositionAndRotation(data.playerPosition, Quaternion.Euler(0f, data.playerRotationY, 0f));
            playerHealth?.SetHealth(data.health);
            RestoreInventory(data.inventory, data.selectedSlotIndex, data.equippedItemId);
            Debug.Log("Game loaded.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not load the game: {exception.Message}", this);
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    private static List<SavedInventoryItem> BuildInventoryData()
    {
        List<SavedInventoryItem> savedItems = new List<SavedInventoryItem>();
        if (InventoryManager.Instance == null)
            return savedItems;

        foreach (InventorySlot slot in InventoryManager.Instance.Slots)
        {
            if (!slot.IsEmpty && !string.IsNullOrWhiteSpace(slot.Item.itemId))
                savedItems.Add(new SavedInventoryItem { itemId = slot.Item.itemId, amount = slot.Amount });
        }

        return savedItems;
    }

    private static void RestoreInventory(List<SavedInventoryItem> savedItems, int selectedSlotIndex, string equippedItemId)
    {
        if (InventoryManager.Instance == null || savedItems == null)
            return;

        InventoryManager.Instance.ClearInventory();
        foreach (SavedInventoryItem savedItem in savedItems)
        {
            ItemData item = ItemData.FindById(savedItem.itemId);
            if (item == null)
            {
                Debug.LogWarning($"Saved item '{savedItem.itemId}' was not found and was skipped.");
                continue;
            }

            InventoryManager.Instance.AddItem(item, savedItem.amount);
        }

        if (selectedSlotIndex >= 0 && selectedSlotIndex < InventoryManager.Instance.TotalSlots)
            InventoryManager.Instance.SelectSlot(selectedSlotIndex);

        if (!string.IsNullOrWhiteSpace(equippedItemId))
        {
            ItemData equippedItem = ItemData.FindById(equippedItemId);
            if (equippedItem != null)
                InventoryManager.Instance.EquipItem(equippedItem);
        }
    }

    [Serializable]
    private class SaveData
    {
        public Vector3 playerPosition;
        public float playerRotationY;
        public float health;
        public int selectedSlotIndex;
        public string equippedItemId = string.Empty;
        public List<SavedInventoryItem> inventory = new List<SavedInventoryItem>();
    }

    [Serializable]
    private class SavedInventoryItem
    {
        public string itemId;
        public int amount;
    }
}
