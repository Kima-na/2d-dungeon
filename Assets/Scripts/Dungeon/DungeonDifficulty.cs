using System;
using UnityEngine;

public enum DungeonDifficulty { Easy, Normal, Hard, Nightmare }

[Serializable]
public readonly struct DifficultyModifiers
{
    public readonly int MinimumRooms;
    public readonly int MaximumRooms;
    public readonly float EnemyHealth;
    public readonly float EnemyDamage;
    public readonly float EnemySpeed;
    public readonly float Reward;
    public readonly int MinimumEnemies;
    public readonly int MaximumEnemies;

    public DifficultyModifiers(int minRooms, int maxRooms, float health, float damage,
        float speed, float reward, int minEnemies, int maxEnemies)
    {
        MinimumRooms = minRooms; MaximumRooms = maxRooms; EnemyHealth = health;
        EnemyDamage = damage; EnemySpeed = speed; Reward = reward;
        MinimumEnemies = minEnemies; MaximumEnemies = maxEnemies;
    }
}

public static class DungeonDifficultyTable
{
    public static DifficultyModifiers Get(DungeonDifficulty difficulty) => difficulty switch
    {
        DungeonDifficulty.Easy => new(10, 14, 1.15f, 1.1f, 0.9f, 1f, 6, 9),
        DungeonDifficulty.Normal => new(14, 20, 1.8f, 1.7f, 1f, 1.5f, 8, 12),
        DungeonDifficulty.Hard => new(18, 24, 2.7f, 2.5f, 1.1f, 2.25f, 10, 15),
        DungeonDifficulty.Nightmare => new(22, 30, 4.2f, 3.8f, 1.2f, 3.5f, 13, 18),
        _ => new(10, 14, 1.15f, 1.1f, 1f, 1f, 6, 9)
    };
}
