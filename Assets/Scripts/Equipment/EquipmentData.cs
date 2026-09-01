using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentSlot { Weapon, Helmet, Armor, Boots, Accessory }
public enum WeaponClass { Warrior, Archer, Mage }
public enum EquipmentRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum EquipmentStat
{
    AttackPower,
    Defense,
    MaxHealth,
    CriticalChance,
    CriticalDamage,
    AttackSpeed,
    MoveSpeed,
    MaxMana
}

[Serializable]
public sealed class EquipmentAffix
{
    public EquipmentStat stat;
    public float value;

    public string Description => Describe(stat, value);

    public static string Describe(EquipmentStat targetStat, float targetValue) => targetStat switch
    {
        EquipmentStat.CriticalChance => $"치명타 확률 +{targetValue * 100f:0.#}%",
        EquipmentStat.CriticalDamage => $"치명타 피해 +{targetValue * 100f:0.#}%",
        EquipmentStat.AttackSpeed => $"공격 속도 +{targetValue * 100f:0.#}%",
        EquipmentStat.MoveSpeed => $"이동 속도 +{targetValue * 100f:0.#}%",
        _ => $"{KoreanName(targetStat)} +{targetValue:0}"
    };

    private static string KoreanName(EquipmentStat stat) => stat switch
    {
        EquipmentStat.AttackPower => "공격력",
        EquipmentStat.Defense => "방어력",
        EquipmentStat.MaxHealth => "체력",
        EquipmentStat.MaxMana => "마나",
        _ => stat.ToString()
    };
}

[Serializable]
public sealed class EquipmentItem
{
    public EquipmentData data;
    public EquipmentRarity rarity;
    public List<EquipmentAffix> affixes = new();
    public string instanceId = Guid.NewGuid().ToString("N");
    public string DisplayName => $"{EquipmentRarityUtility.KoreanName(rarity)} {data.DisplayName}";

    public float GetBonus(EquipmentStat stat)
    {
        float total = data.GetBaseStat(stat);
        foreach (EquipmentAffix affix in affixes)
            if (affix.stat == stat) total += affix.value;
        return total;
    }
}

public static class EquipmentRarityUtility
{
    public static int AffixCount(EquipmentRarity rarity) => (int)rarity;
    public static float Power(EquipmentRarity rarity) => 1f + (int)rarity * 0.38f;
    public static string KoreanName(EquipmentRarity rarity) => rarity switch
    {
        EquipmentRarity.Common => "일반",
        EquipmentRarity.Uncommon => "고급",
        EquipmentRarity.Rare => "희귀",
        EquipmentRarity.Epic => "영웅",
        _ => "전설"
    };

    public static Color Color(EquipmentRarity rarity) => rarity switch
    {
        EquipmentRarity.Uncommon => new Color(0.3f, 0.95f, 0.4f),
        EquipmentRarity.Rare => new Color(0.3f, 0.65f, 1f),
        EquipmentRarity.Epic => new Color(0.75f, 0.35f, 1f),
        EquipmentRarity.Legendary => new Color(1f, 0.62f, 0.12f),
        _ => UnityEngine.Color.white
    };
}

[CreateAssetMenu(menuName = "2D Dungeon/Equipment", fileName = "Equipment")]
public class EquipmentData : ScriptableObject
{
    [SerializeField] private string itemId = "equipment_id";
    [SerializeField] private string displayName = "Equipment";
    [SerializeField] private EquipmentSlot slot;
    [SerializeField] private EquipmentRarity rarity;
    [Header("Class Restriction")]
    [SerializeField] private WeaponClass weaponClass;
    [Header("Weapon Type")]
    [SerializeField] private AttackController.WeaponType warriorWeaponType;
    [SerializeField] private ArcherController.RangedWeapon archerWeaponType;
    [SerializeField] private MageController.MagicWeapon mageWeaponType;
    [SerializeField, Min(0)] private int attackPower;
    [SerializeField, Min(0)] private int defense;
    [SerializeField, Min(0)] private int maxHealth;
    [SerializeField, Range(0f, 1f)] private float criticalChance;
    [SerializeField, Min(0f)] private float criticalDamage;
    [SerializeField, Min(0f)] private float attackSpeed;
    [SerializeField, Min(0f)] private float moveSpeed;
    [SerializeField, Min(0)] private int maxMana;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite equippedSprite;
    [SerializeField] private Sprite attackSprite;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public EquipmentSlot Slot => slot;
    public EquipmentRarity Rarity => rarity;
    public AttackController.WeaponType WarriorWeaponType => warriorWeaponType;
    public ArcherController.RangedWeapon ArcherWeaponType => archerWeaponType;
    public MageController.MagicWeapon MageWeaponType => mageWeaponType;
    public WeaponClass WeaponClass => weaponClass;
    public bool IsUsableBy(PlayerStats.PlayerClass playerClass) => (int)weaponClass == (int)playerClass;
    public Sprite Icon => icon;
    public Sprite EquippedSprite => equippedSprite;
    public Sprite AttackSprite => attackSprite != null ? attackSprite : equippedSprite;

    public float GetBaseStat(EquipmentStat stat) => stat switch
    {
        EquipmentStat.AttackPower => attackPower,
        EquipmentStat.Defense => defense,
        EquipmentStat.MaxHealth => maxHealth,
        EquipmentStat.CriticalChance => criticalChance,
        EquipmentStat.CriticalDamage => criticalDamage,
        EquipmentStat.AttackSpeed => attackSpeed,
        EquipmentStat.MoveSpeed => moveSpeed,
        EquipmentStat.MaxMana => maxMana,
        _ => 0f
    };

    public EquipmentItem Roll(EquipmentRarity? forcedRarity = null)
    {
        EquipmentRarity rolled = forcedRarity ?? rarity;
        var item = new EquipmentItem { data = this, rarity = rolled };
        List<EquipmentStat> available = GetAffixPool();
        int affixCount = EquipmentRarityUtility.AffixCount(rolled);
        for (int i = 0; i < affixCount; i++)
        {
            if (available.Count == 0) available = GetAffixPool();
            int index = UnityEngine.Random.Range(0, available.Count);
            EquipmentStat stat = available[index];
            available.RemoveAt(index);
            float power = EquipmentRarityUtility.Power(rolled);
            float value = stat switch
            {
                EquipmentStat.CriticalChance => UnityEngine.Random.Range(0.01f, 0.025f) * power,
                EquipmentStat.CriticalDamage => UnityEngine.Random.Range(0.06f, 0.14f) * power,
                EquipmentStat.AttackSpeed or EquipmentStat.MoveSpeed =>
                    UnityEngine.Random.Range(0.02f, 0.05f) * power,
                EquipmentStat.MaxHealth => Mathf.Round(UnityEngine.Random.Range(6f, 14f) * power),
                EquipmentStat.MaxMana => Mathf.Round(UnityEngine.Random.Range(4f, 10f) * power),
                _ => Mathf.Round(UnityEngine.Random.Range(1f, 4f) * power)
            };
            item.affixes.Add(new EquipmentAffix { stat = stat, value = value });
        }
        return item;
    }

    private List<EquipmentStat> GetAffixPool()
    {
        if (slot == EquipmentSlot.Weapon)
            return new List<EquipmentStat>((EquipmentStat[])Enum.GetValues(typeof(EquipmentStat)));

        return weaponClass switch
        {
            WeaponClass.Archer => new List<EquipmentStat>
                { EquipmentStat.AttackSpeed, EquipmentStat.AttackPower, EquipmentStat.MoveSpeed, EquipmentStat.MaxHealth },
            WeaponClass.Mage => new List<EquipmentStat>
                { EquipmentStat.MaxMana, EquipmentStat.MaxHealth, EquipmentStat.AttackPower },
            _ => new List<EquipmentStat>
                { EquipmentStat.AttackPower, EquipmentStat.Defense, EquipmentStat.MaxHealth }
        };
    }
}
