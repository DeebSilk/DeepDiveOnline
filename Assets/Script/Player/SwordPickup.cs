using UnityEngine;

/// <summary>
/// Legacy weapon pickup wrapper. Forwards to InventoryPickupService.
/// Kept for backward compatibility with existing prefabs and scenes.
/// </summary>
public class SwordPickup : MonoBehaviour
{
    [Header("=== ITEM ===")]
    [SerializeField] private ItemData itemData;

    public ItemData ItemData => itemData;

    private void Awake()
    {
        ResolveItemData();
    }

    private void ResolveItemData()
    {
        if (itemData != null)
            return;

        ItemPickup pickup = GetComponent<ItemPickup>();
        if (pickup != null && pickup.ItemData != null)
        {
            itemData = pickup.ItemData;
            return;
        }

        PickupItem legacy = GetComponent<PickupItem>();
        if (legacy != null && legacy.itemData != null)
            itemData = legacy.itemData;
    }

    public bool TryPickup(PlayerInteraction interaction)
    {
        if (interaction == null)
        {
            Debug.LogWarning("[SwordPickup] PlayerInteraction is null.", this);
            return false;
        }

        ItemPickup pickup = GetComponent<ItemPickup>();
        if (pickup != null)
            return pickup.TryPickup(true);

        ResolveItemData();

        if (itemData == null)
        {
            Debug.LogWarning(
                $"[SwordPickup] ItemData is not assigned on '{gameObject.name}'.",
                this
            );

            return false;
        }

        if (!itemData.IsWeapon)
        {
            Debug.LogWarning(
                $"[SwordPickup] Item '{itemData.itemName}' is not marked as weapon.",
                this
            );

            return false;
        }

        int amount = 1;
        PickupItem legacy = GetComponent<PickupItem>();
        if (legacy != null && legacy.quantity > 0)
            amount = legacy.quantity;

        return InventoryPickupService.TryPickupFromWorld(
            itemData,
            amount,
            gameObject,
            autoEquipWeapon: true,
            PickupCleanupMode.Destroy
        );
    }
}
