using UnityEngine;

public class GoblinWarriorVisualDatabase : ScriptableObject
{
    public GameObject warriorPrefab;
    public GameObject archerPrefab;
    public GameObject magePrefab;
    public GameObject arrowProjectilePrefab;
    public GameObject magicProjectilePrefab;

    public GameObject GetEnemyPrefab(EnemyAI.MonsterType type) => type switch
    {
        EnemyAI.MonsterType.GoblinWarrior => warriorPrefab,
        EnemyAI.MonsterType.GoblinArcher => archerPrefab,
        EnemyAI.MonsterType.GoblinMage => magePrefab,
        _ => null
    };

    public GameObject GetProjectilePrefab(bool magic) =>
        magic ? magicProjectilePrefab : arrowProjectilePrefab;
}
