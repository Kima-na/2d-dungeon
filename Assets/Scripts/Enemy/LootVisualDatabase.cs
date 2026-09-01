using UnityEngine;

[CreateAssetMenu(menuName = "2D Dungeon/Loot Visual Database", fileName = "LootVisualDatabase")]
public sealed class LootVisualDatabase : ScriptableObject
{
    public Sprite coin;
    public Sprite chestYellow;
    public Sprite chestBlue;
    public Sprite chestGreen;
    public Sprite chestRed;

    public Sprite GetRandomChest()
    {
        Sprite[] choices = { chestYellow, chestBlue, chestGreen, chestRed };
        Sprite selected = choices[Random.Range(0, choices.Length)];
        return selected != null ? selected : chestYellow;
    }

    public Sprite GetEquipmentChest(EquipmentRarity rarity) => rarity switch
    {
        EquipmentRarity.Common => chestGreen,
        EquipmentRarity.Uncommon => chestBlue,
        EquipmentRarity.Rare => chestYellow,
        EquipmentRarity.Epic => chestRed,
        EquipmentRarity.Legendary => chestRed,
        _ => chestYellow
    };

    public Color GetEquipmentChestColor(EquipmentRarity rarity) => rarity switch
    {
        EquipmentRarity.Common => Color.white,
        EquipmentRarity.Uncommon => new Color(0.55f, 0.9f, 1f),
        EquipmentRarity.Rare => new Color(1f, 0.9f, 0.35f),
        EquipmentRarity.Epic => new Color(0.9f, 0.48f, 1f),
        EquipmentRarity.Legendary => new Color(1f, 0.58f, 0.12f),
        _ => Color.white
    };
}
