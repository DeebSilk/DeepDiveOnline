using System;
using UnityEngine;

/// <summary>
/// Manages equipped gear (weapons now; armor/accessories later).
/// Delegates visual/combat equip to PlayerCombat without replacing it.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    public event Action OnEquipmentChanged;

    public ItemData EquippedWeapon { get; private set; }

    public static EquipmentManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        EquipmentManager existing = FindAnyObjectByType<EquipmentManager>();
        if (existing != null)
            return existing;

        GameObject managerObject = new GameObject("EquipmentManager");
        return managerObject.AddComponent<EquipmentManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public ItemData GetEquippedWeapon() => EquippedWeapon;

    public bool EquipWeapon(ItemData weapon)
    {
        if (weapon == null || !weapon.IsWeapon)
            return false;

        PlayerCombat playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (playerCombat == null)
        {
            Debug.LogWarning("[EquipmentManager] PlayerCombat not found.", this);
            return false;
        }

        if (!playerCombat.TryEquipItem(weapon))
            return false;

        EquippedWeapon = weapon;
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public void UnequipWeapon()
    {
        PlayerCombat playerCombat = FindAnyObjectByType<PlayerCombat>();
        playerCombat?.Unequip();

        EquippedWeapon = null;
        OnEquipmentChanged?.Invoke();
    }

    /// <summary>Future hook for armor slots.</summary>
    public bool HasWeaponEquipped => EquippedWeapon != null;
}
