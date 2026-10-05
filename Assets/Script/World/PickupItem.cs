using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public bool useTags = true;
    public string pickupTag = "Pickup";
    public ItemData itemData;
    public int quantity = 1;

    [Header("Editor Info")]
    public string itemName = "New Item";
    [TextArea(2, 4)] public string description;

    public bool CanPickup
    {
        get
        {
            if (!useTags)
                return true;

            if (itemData != null)
                return true;

            return CompareTag(pickupTag);
        }
    }
}
