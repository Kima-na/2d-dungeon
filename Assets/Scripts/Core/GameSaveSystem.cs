using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SavedEquipmentItem
{
    public string itemId;
    public string instanceId;
    public int rarity;
    public bool equipped;
    public List<EquipmentAffix> affixes = new();
}

[Serializable]
public sealed class GameSaveData
{
    public int version = 1;
    public long savedAtUtcTicks;
    public int playerClass;
    public int characterDesign;
    public int level = 1;
    public int experience;
    public int health;
    public int maxHealth;
    public int mana;
    public int maxMana;
    public int strength;
    public int defense;
    public int dexterity;
    public int intelligence;
    public float criticalChance;
    public float criticalDamage;
    public float attackSpeed;
    public float moveSpeed;
    public int gold;
    public int selectedDifficulty;
    public bool easyCleared;
    public bool normalCleared;
    public bool hardCleared;
    public bool nightmareCleared;
    public List<SavedEquipmentItem> equipment = new();
}

public static class GameSaveSystem
{
    private const string SaveKey = "DungeonRpg.SaveData.V1";
    public static bool HasSave => PlayerPrefs.HasKey(SaveKey) && !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(SaveKey));

    public static void Save(PlayerStats player, EquipmentInventory inventory, DungeonDifficulty difficulty)
    {
        if (player == null) return;
        var data = new GameSaveData
        {
            savedAtUtcTicks = DateTime.UtcNow.Ticks,
            playerClass = (int)player.CurrentClass,
            characterDesign = player.GetComponent<PlayerVisualController>()?.DesignIndex ?? 0,
            level = player.Level,
            experience = player.CurrentExperience, health = player.CurrentHealth,
            maxHealth = player.BaseMaxHealth, mana = player.CurrentMana, maxMana = player.BaseMaxMana,
            strength = player.Strength, defense = player.BaseDefense, dexterity = player.Dexterity,
            intelligence = player.Intelligence, criticalChance = player.BaseCriticalChance,
            criticalDamage = player.BaseCriticalDamage, attackSpeed = player.BaseAttackSpeed,
            moveSpeed = player.BaseMoveSpeed, gold = player.Gold, selectedDifficulty = (int)difficulty,
            easyCleared = DungeonProgress.EasyCleared, normalCleared = DungeonProgress.NormalCleared,
            hardCleared = DungeonProgress.HardCleared, nightmareCleared = DungeonProgress.NightmareCleared
        };
        if (inventory != null)
        {
            foreach (EquipmentItem item in inventory.OwnedItems)
            {
                if (item?.data == null) continue;
                data.equipment.Add(new SavedEquipmentItem
                {
                    itemId = item.data.ItemId, instanceId = item.instanceId, rarity = (int)item.rarity,
                    equipped = ContainsReference(inventory.EquippedItems, item),
                    affixes = new List<EquipmentAffix>(item.affixes)
                });
            }
        }
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static GameSaveData Load()
    {
        if (!HasSave) return null;
        try { return JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(SaveKey)); }
        catch (Exception exception) { Debug.LogWarning($"Save load failed: {exception.Message}"); return null; }
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    private static bool ContainsReference(IReadOnlyList<EquipmentItem> list, EquipmentItem target)
    {
        for (int i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], target)) return true;
        return false;
    }
}
