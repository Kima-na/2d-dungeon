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
}
