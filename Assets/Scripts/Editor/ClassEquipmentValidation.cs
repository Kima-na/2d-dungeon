#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ClassEquipmentValidation
{
    [MenuItem("Tools/2D Dungeon/Validate Class Equipment")]
    public static void Validate()
    {
        GameObject player = new("Class Equipment Validation Player");
        var createdData = new List<EquipmentData>();
        try
        {
            EquipmentInventory inventory = player.AddComponent<EquipmentInventory>();
            PlayerStats stats = player.AddComponent<PlayerStats>();
            typeof(PlayerStats).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(stats, null);

            ValidateClass(stats, inventory, createdData, PlayerStats.PlayerClass.Warrior,
                new[] { EquipmentStat.AttackPower, EquipmentStat.Defense, EquipmentStat.MaxHealth });
            ValidateClass(stats, inventory, createdData, PlayerStats.PlayerClass.Archer,
                new[] { EquipmentStat.AttackSpeed, EquipmentStat.AttackPower,
                        EquipmentStat.MoveSpeed, EquipmentStat.MaxHealth });
            ValidateClass(stats, inventory, createdData, PlayerStats.PlayerClass.Mage,
                new[] { EquipmentStat.MaxMana, EquipmentStat.MaxHealth, EquipmentStat.AttackPower });
            ValidateManaDamage(stats);

            Debug.Log("CLASS_EQUIPMENT_VALIDATION_PASS: class affix pools, equip/unequip stats, " +
                      "mana clamping, and current-mana damage scaling passed.");
        }
        finally
        {
            foreach (EquipmentData data in createdData)
                if (data != null) UnityEngine.Object.DestroyImmediate(data);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    private static void ValidateClass(PlayerStats stats, EquipmentInventory inventory,
        List<EquipmentData> createdData, PlayerStats.PlayerClass playerClass, EquipmentStat[] allowed)
    {
        stats.ResetForNewGame(playerClass);
        EquipmentData data = CreateArmor(playerClass);
        createdData.Add(data);

        var allowedSet = new HashSet<EquipmentStat>(allowed);
        for (int roll = 0; roll < 30; roll++)
        {
            EquipmentItem rolled = data.Roll(EquipmentRarity.Legendary);
            Require(rolled.affixes.Count == EquipmentRarityUtility.AffixCount(EquipmentRarity.Legendary),
                $"{playerClass}: rarity affix count changed.");
            foreach (EquipmentAffix affix in rolled.affixes)
                Require(allowedSet.Contains(affix.stat), $"{playerClass}: invalid affix {affix.stat}.");
        }

        int baseAttack = stats.AttackPowerBonus;
        int baseDefense = stats.Defense;
        int baseHealth = stats.MaxHealth;
        int baseMana = stats.MaxMana;
        float baseAttackSpeed = stats.AttackSpeedMultiplier;
        float baseMoveSpeed = stats.MoveSpeedMultiplier;

        Require(inventory.Add(data), $"{playerClass}: could not add armor.");
        Require(inventory.Equip(data), $"{playerClass}: could not equip matching armor.");
        Require(stats.AttackPowerBonus > baseAttack, $"{playerClass}: attack did not increase.");
        Require(stats.MaxHealth > baseHealth, $"{playerClass}: max health did not increase.");
        if (playerClass == PlayerStats.PlayerClass.Warrior)
            Require(stats.Defense > baseDefense, "Warrior: defense did not increase.");
        if (playerClass == PlayerStats.PlayerClass.Archer)
        {
            Require(stats.AttackSpeedMultiplier > baseAttackSpeed, "Archer: attack speed did not increase.");
            Require(stats.MoveSpeedMultiplier > baseMoveSpeed, "Archer: move speed did not increase.");
        }
        if (playerClass == PlayerStats.PlayerClass.Mage)
            Require(stats.MaxMana > baseMana, "Mage: max mana did not increase.");

        Require(inventory.Unequip(EquipmentSlot.Helmet), $"{playerClass}: could not unequip armor.");
        Require(stats.AttackPowerBonus == baseAttack && stats.MaxHealth == baseHealth,
            $"{playerClass}: stats were duplicated or not removed after unequip.");
        Require(stats.CurrentMana <= stats.MaxMana, $"{playerClass}: current mana exceeded maximum.");
    }

    private static EquipmentData CreateArmor(PlayerStats.PlayerClass playerClass)
    {
        EquipmentData data = ScriptableObject.CreateInstance<EquipmentData>();
        SerializedObject serialized = new(data);
        serialized.FindProperty("displayName").stringValue = playerClass + " Validation Helmet";
        serialized.FindProperty("slot").enumValueIndex = (int)EquipmentSlot.Helmet;
        serialized.FindProperty("rarity").enumValueIndex = (int)EquipmentRarity.Legendary;
        serialized.FindProperty("weaponClass").enumValueIndex = (int)playerClass;
        serialized.FindProperty("attackPower").intValue = 7;
        serialized.FindProperty("maxHealth").intValue = 25;
        serialized.FindProperty("defense").intValue = playerClass == PlayerStats.PlayerClass.Warrior ? 5 : 0;
        serialized.FindProperty("attackSpeed").floatValue = playerClass == PlayerStats.PlayerClass.Archer ? 0.05f : 0f;
        serialized.FindProperty("moveSpeed").floatValue = playerClass == PlayerStats.PlayerClass.Archer ? 0.04f : 0f;
        serialized.FindProperty("maxMana").intValue = playerClass == PlayerStats.PlayerClass.Mage ? 20 : 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return data;
    }

    private static void ValidateManaDamage(PlayerStats stats)
    {
        stats.ResetForNewGame(PlayerStats.PlayerClass.Mage);
        SerializedObject serialized = new(stats);
        serialized.FindProperty("criticalChance").floatValue = 0f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        int highBonus = stats.ManaDamageBonus;
        int highDamage = CombatCalculator.RollDamage(stats, 20, out _);
        Require(stats.UseMana(Mathf.Max(1, stats.CurrentMana / 2)), "Mage: could not spend mana.");
        int lowBonus = stats.ManaDamageBonus;
        int lowDamage = CombatCalculator.RollDamage(stats, 20, out _);
        Require(highBonus > lowBonus && highDamage > lowDamage,
            "Mage: damage did not decrease with current mana.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
