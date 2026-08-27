using UnityEngine;

[CreateAssetMenu(menuName = "2D Dungeon/Boss Visual Database")]
public sealed class BossVisualDatabase : ScriptableObject
{
    public GameObject easyBossPrefab;
    [Header("Boss Attack Effects")]
    public Sprite darkShockwave;
    public Sprite groundWarning;
    public Sprite groundImpact;
    public Sprite spinSlash;
    public Sprite summonCircle;
    public Sprite shadowMinion;
    [Header("Reusable UI Art")]
    public Sprite bossBarFrame;
    public Sprite bossBarFill;
    public Sprite playerManaFill;
}
