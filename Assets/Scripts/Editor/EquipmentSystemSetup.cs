#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EquipmentSystemSetup
{
    private const string Folder = "Assets/Resources/Equipment";
    private const string WeaponFolder = Folder + "/Warrior Weapons";

    [MenuItem("Tools/2D Dungeon/Setup Equipment System")]
    public static void Setup()
    {
        EnsureFolder("Assets/Resources", "Equipment");
        EnsureFolder(Folder, "Warrior Weapons");

        EquipmentData helmet = CreateArmor("starter_helmet", "철제 투구", EquipmentSlot.Helmet,
            0, 2, 8, 0f, 0f, 0f, 0f, 0);
        EquipmentData armor = CreateArmor("starter_armor", "기사의 갑옷", EquipmentSlot.Armor,
            0, 5, 18, 0f, 0f, 0f, 0f, 0);
        EquipmentData boots = CreateArmor("starter_boots", "가죽 장화", EquipmentSlot.Boots,
            0, 1, 0, 0f, 0f, 0f, 0.06f, 0);
        EquipmentData ring = CreateArmor("starter_ring", "붉은 수정 반지", EquipmentSlot.Accessory,
            2, 0, 0, 0.03f, 0.08f, 0f, 0f, 10);

        var weapons = new List<EquipmentData>();
        CreateWeaponSet(weapons, "Shortsword", AttackController.WeaponType.OneHandedSword,
            WarriorWeaponSpriteSetup.LoadSprites("Shortsword"), 5, 0.03f);
        CreateWeaponSet(weapons, "Greatsword", AttackController.WeaponType.Greatsword,
            WarriorWeaponSpriteSetup.LoadSprites("Greatsword"), 10, 0f);
        CreateWeaponSet(weapons, "Spear", AttackController.WeaponType.Spear,
            WarriorWeaponSpriteSetup.LoadSprites("Spear"), 7, 0.015f);
        if (weapons.Count != 24)
        {
            Debug.LogError($"Equipment setup stopped: expected 24 warrior weapons, found {weapons.Count}.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("Equipment setup: Player not found.");
            return;
        }

        EquipmentInventory inventory = player.GetComponent<EquipmentInventory>();
        if (inventory == null) inventory = player.AddComponent<EquipmentInventory>();
        SerializedObject serialized = new(inventory);
        var starting = new List<EquipmentData> { weapons[0], helmet, armor, boots, ring };
        var loot = new List<EquipmentData>(weapons) { helmet, armor, boots, ring };
        SetArray(serialized.FindProperty("startingEquipment"), starting);
        SetArray(serialized.FindProperty("lootTemplates"), loot);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(inventory);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Equipment system setup complete: {weapons.Count} warrior weapon assets connected.");
    }

    private static void CreateWeaponSet(List<EquipmentData> output, string name,
        AttackController.WeaponType weaponType, Sprite[] sprites, int baseAttack, float attackSpeed)
    {
        if (sprites.Length != 8)
        {
            Debug.LogError($"{name}: expected 8 sprites, found {sprites.Length}.");
            return;
        }

        EnsureFolder(WeaponFolder, name);
        for (int i = 0; i < sprites.Length; i++)
        {
            EquipmentRarity rarity = i switch
            {
                0 => EquipmentRarity.Common,
                1 => EquipmentRarity.Uncommon,
                2 or 3 => EquipmentRarity.Rare,
                4 or 5 => EquipmentRarity.Epic,
                _ => EquipmentRarity.Legendary
            };
            string id = $"warrior_{name.ToLowerInvariant()}_{i + 1:00}";
            string path = $"{WeaponFolder}/{name}/{name}_{i + 1:00}.asset";
            EquipmentData data = LoadOrCreate(path);
            SerializedObject serialized = new(data);
            SetCommon(serialized, id, $"{name} {i + 1:00}", EquipmentSlot.Weapon, rarity,
                baseAttack + i * 2, 0, 0, 0f, 0f, attackSpeed, 0f, 0);
            serialized.FindProperty("warriorWeaponType").enumValueIndex = (int)weaponType;
            serialized.FindProperty("equippedSprite").objectReferenceValue = sprites[i];
            serialized.FindProperty("attackSprite").objectReferenceValue = sprites[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            output.Add(data);
        }
    }

    private static EquipmentData CreateArmor(string id, string displayName, EquipmentSlot slot,
        int attackPower, int defense, int health, float criticalChance, float criticalDamage,
        float attackSpeed, float moveSpeed, int mana)
    {
        EquipmentData data = LoadOrCreate($"{Folder}/{id}.asset");
        SerializedObject serialized = new(data);
        SetCommon(serialized, id, displayName, slot, EquipmentRarity.Common, attackPower, defense,
            health, criticalChance, criticalDamage, attackSpeed, moveSpeed, mana);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void SetCommon(SerializedObject serialized, string id, string displayName,
        EquipmentSlot slot, EquipmentRarity rarity, int attackPower, int defense, int health,
        float criticalChance, float criticalDamage, float attackSpeed, float moveSpeed, int mana)
    {
        serialized.FindProperty("itemId").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("slot").enumValueIndex = (int)slot;
        serialized.FindProperty("rarity").enumValueIndex = (int)rarity;
        serialized.FindProperty("attackPower").intValue = attackPower;
        serialized.FindProperty("defense").intValue = defense;
        serialized.FindProperty("maxHealth").intValue = health;
        serialized.FindProperty("criticalChance").floatValue = criticalChance;
        serialized.FindProperty("criticalDamage").floatValue = criticalDamage;
        serialized.FindProperty("attackSpeed").floatValue = attackSpeed;
        serialized.FindProperty("moveSpeed").floatValue = moveSpeed;
        serialized.FindProperty("maxMana").intValue = mana;
    }

    private static EquipmentData LoadOrCreate(string path)
    {
        EquipmentData data = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
        if (data != null) return data;
        data = ScriptableObject.CreateInstance<EquipmentData>();
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static void SetArray(SerializedProperty property, List<EquipmentData> items)
    {
        property.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
