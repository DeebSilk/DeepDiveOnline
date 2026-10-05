using UnityEngine;

public enum ItemType
{
    Consumable,
    Weapon,
    Quest,
    Material,
    Equipment
}

public enum WeaponCategory
{
    Sword,
    Axe,
    Spear,
    Bow,
    Staff,
    Other
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic")]
    public string itemId = "new_item";
    public string itemName = "New Item";
    public ItemType itemType = ItemType.Consumable;
    public Sprite icon;

    [TextArea(2, 5)]
    public string description;

    [Header("Inventory")]
    public bool stackable = true;
    public int maxStack = 99;

    [Header("Economy")]
    [Tooltip("Цена продажи NPC. Если 0 — используется value.")]
    public int sellPrice = 0;

    [Tooltip("Цена покупки у NPC. Если 0 — используется value.")]
    public int buyPrice = 0;

    [Tooltip("Префаб, который появляется при выбрасывании предмета из инвентаря")]
    public GameObject dropPrefab;

    [Header("Equipment")]
    [Tooltip("Префаб, который экипируется на персонажа (оружие, броня и т.д.)")]
    public GameObject equipPrefab;

    [Header("Weapon Stats")]
    public int damage = 10;
    public float attackSpeed = 1f;
    public WeaponCategory weaponCategory = WeaponCategory.Sword;
    public ItemRarity rarity = ItemRarity.Common;
    public int value = 0;
    public bool twoHanded = false;

    [Tooltip("Дальность атаки. 0 = использовать значение по умолчанию из PlayerCombat")]
    public float attackRange = 0f;

    [Tooltip("Опциональный Animator Override Controller для анимаций этого оружия")]
    public RuntimeAnimatorController animatorOverride;

    public static ItemData FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        foreach (ItemData item in Resources.FindObjectsOfTypeAll<ItemData>())
        {
            if (item != null && item.itemId == id)
                return item;
        }

        return null;
    }

    public bool IsWeapon => itemType == ItemType.Weapon;

    public bool IsConsumable => itemType == ItemType.Consumable;

    public bool IsEquipment => itemType == ItemType.Equipment;

    public int EffectiveSellPrice => sellPrice > 0 ? sellPrice : value;

    public int EffectiveBuyPrice => buyPrice > 0 ? buyPrice : value;

    public string GetStatsText()
    {
        if (!IsWeapon)
            return string.Empty;

        string rangeText = attackRange > 0f
            ? $"\nДальность: {attackRange:0.##}"
            : string.Empty;

        return
            $"Урон: {damage}\n" +
            $"Скорость: {attackSpeed:0.##}\n" +
            $"Тип: {weaponCategory}\n" +
            $"Редкость: {rarity}" +
            rangeText;
    }
}

