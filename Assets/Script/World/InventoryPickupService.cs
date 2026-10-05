using UnityEngine;

/// <summary>
/// Shared pickup logic used by ItemPickup, SwordPickup and PlayerInteraction.
/// </summary>
public static class InventoryPickupService
{
    public static bool TryPickupFromWorld(
        ItemData itemData,
        int quantity,
        GameObject worldObject,
        bool autoEquipWeapon,
        PickupCleanupMode cleanupMode = PickupCleanupMode.Destroy)
    {
        if (itemData == null)
        {
            Debug.LogWarning(
                $"[Pickup] ItemData is not assigned on '{worldObject.name}'.",
                worldObject
            );

            return false;
        }

        if (quantity <= 0)
            quantity = 1;

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
            inventory = InventoryManager.GetOrCreate();

        if (!inventory.AddItem(itemData, quantity))
        {
            Debug.LogWarning(
                $"[Pickup] Inventory full — '{itemData.itemName}' was not picked up.",
                worldObject
            );

            return false;
        }

        if (autoEquipWeapon && itemData.IsWeapon)
        {
            bool equipped = inventory.EquipItem(itemData);
            if (!equipped)
            {
                Debug.LogWarning(
                    $"[Pickup] '{itemData.itemName}' added to inventory but could not be equipped.",
                    worldObject
                );
            }
        }

        ApplyCleanup(worldObject, cleanupMode);

        Debug.Log(
            $"[Pickup] Picked up: {itemData.itemName} x{quantity}",
            worldObject
        );

        return true;
    }

    private static void ApplyCleanup(
        GameObject worldObject,
        PickupCleanupMode cleanupMode)
    {
        if (worldObject == null)
            return;

        switch (cleanupMode)
        {
            case PickupCleanupMode.Destroy:
                Object.Destroy(worldObject);
                break;

            case PickupCleanupMode.Disable:
                worldObject.SetActive(false);
                break;

            case PickupCleanupMode.None:
                break;
        }
    }
}

public enum PickupCleanupMode
{
    Destroy,
    Disable,
    None
}
