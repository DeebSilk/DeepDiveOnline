using UnityEngine;

/// <summary>
/// Unified world pickup for any item type (weapons, consumables, materials, etc.).
/// Replaces duplicate per-type pickup scripts while keeping legacy components compatible.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity = 1;

    [Header("Behaviour")]
    [SerializeField] private bool autoEquipWeaponOnPickup = true;
    [SerializeField] private PickupCleanupMode cleanupMode = PickupCleanupMode.Destroy;

    public ItemData ItemData => itemData;
    public int Quantity => quantity;

    private void Awake()
    {
        SyncLegacyReferences();
    }

    private void OnValidate()
    {
        if (quantity < 1)
            quantity = 1;
    }

    /// <summary>Sync ItemData from legacy PickupItem / SwordPickup if not set in Inspector.</summary>
    public void SyncLegacyReferences()
    {
        if (itemData != null)
            return;

        PickupItem legacyPickup = GetComponent<PickupItem>();
        if (legacyPickup != null && legacyPickup.itemData != null)
        {
            itemData = legacyPickup.itemData;
            if (legacyPickup.quantity > 0)
                quantity = legacyPickup.quantity;

            return;
        }

        SwordPickup legacySword = GetComponent<SwordPickup>();
        if (legacySword != null && legacySword.ItemData != null)
            itemData = legacySword.ItemData;
    }

    public bool TryPickup(bool autoEquipOverride = true)
    {
        SyncLegacyReferences();

        return InventoryPickupService.TryPickupFromWorld(
            itemData,
            quantity,
            gameObject,
            autoEquipOverride && autoEquipWeaponOnPickup,
            cleanupMode
        );
    }
}
