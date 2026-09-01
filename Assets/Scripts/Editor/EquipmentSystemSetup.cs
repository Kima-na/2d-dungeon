#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EquipmentSystemSetup
{
    private const string Folder = "Assets/Resources/Equipment";
    private const string WeaponFolder = Folder + "/Warrior Weapons";
    private const string ArcherWeaponFolder = Folder + "/Archer Weapons";
    private const string MageWeaponFolder = Folder + "/Mage Weapons";

    [MenuItem("Tools/2D Dungeon/Setup Equipment System")]
    public static void Setup()
    {
        EnsureFolder("Assets/Resources", "Equipment");
        EnsureFolder(Folder, "Warrior Weapons");
        EnsureFolder(Folder, "Archer Weapons");
        EnsureFolder(Folder, "Mage Weapons");

        var helmets = CreateArmorSet(Folder, "", "Helmet", "전사 투구", EquipmentSlot.Helmet,
            WeaponClass.Warrior, 2, 2, 8, 0f, 0f, 0f, 0f, 0);
        var armors = CreateArmorSet(Folder, "", "Armor", "전사 갑옷", EquipmentSlot.Armor,
            WeaponClass.Warrior, 3, 5, 18, 0f, 0f, 0f, 0f, 0);
        var boots = CreateArmorSet(Folder, "", "Boots", "전사 장화", EquipmentSlot.Boots,
            WeaponClass.Warrior, 2, 1, 8, 0f, 0f, 0f, 0f, 0);
        var accessories = CreateArmorSet(Folder, "", "Accessory", "전사 장신구", EquipmentSlot.Accessory,
            WeaponClass.Warrior, 3, 1, 6, 0f, 0f, 0f, 0f, 0);

        string archerArmorRoot = Folder + "/Archer Armor";
        string mageArmorRoot = Folder + "/Mage Armor";
        EnsureFolder(Folder, "Archer Armor");
        EnsureFolder(Folder, "Mage Armor");
        var archerHelmets = CreateArmorSet(archerArmorRoot, "archer_", "Helmet", "궁수 투구", EquipmentSlot.Helmet,
            WeaponClass.Archer, 2, 0, 7, 0f, 0f, 0.02f, 0.02f, 0);
        var archerArmors = CreateArmorSet(archerArmorRoot, "archer_", "Armor", "궁수 갑옷", EquipmentSlot.Armor,
            WeaponClass.Archer, 3, 0, 15, 0f, 0f, 0.02f, 0.02f, 0);
        var archerBoots = CreateArmorSet(archerArmorRoot, "archer_", "Boots", "궁수 장화", EquipmentSlot.Boots,
            WeaponClass.Archer, 2, 0, 7, 0f, 0f, 0.02f, 0.04f, 0);
        var archerAccessories = CreateArmorSet(archerArmorRoot, "archer_", "Accessory", "궁수 장신구", EquipmentSlot.Accessory,
            WeaponClass.Archer, 3, 0, 6, 0f, 0f, 0.02f, 0.02f, 0);
        var mageHelmets = CreateArmorSet(mageArmorRoot, "mage_", "Helmet", "마법사 모자", EquipmentSlot.Helmet,
            WeaponClass.Mage, 2, 0, 7, 0f, 0f, 0f, 0f, 6);
        var mageArmors = CreateArmorSet(mageArmorRoot, "mage_", "Armor", "마법사 로브", EquipmentSlot.Armor,
            WeaponClass.Mage, 3, 0, 14, 0f, 0f, 0f, 0f, 12);
        var mageBoots = CreateArmorSet(mageArmorRoot, "mage_", "Boots", "마법사 장화", EquipmentSlot.Boots,
            WeaponClass.Mage, 2, 0, 7, 0f, 0f, 0f, 0f, 6);
        var mageAccessories = CreateArmorSet(mageArmorRoot, "mage_", "Accessory", "마법사 장신구", EquipmentSlot.Accessory,
            WeaponClass.Mage, 3, 0, 6, 0f, 0f, 0f, 0f, 8);

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
        var bows = CreateClassWeaponSet("Bow", WeaponClass.Archer, 7, 0.035f,
            ArcherController.RangedWeapon.Bow, MageController.MagicWeapon.Staff);
        var crossbows = CreateClassWeaponSet("Crossbow", WeaponClass.Archer, 11, 0f,
            ArcherController.RangedWeapon.Crossbow, MageController.MagicWeapon.Staff);
        var staffs = CreateClassWeaponSet("Staff", WeaponClass.Mage, 8, 0.025f,
            ArcherController.RangedWeapon.Bow, MageController.MagicWeapon.Staff);
        var spellbooks = CreateClassWeaponSet("Spellbook", WeaponClass.Mage, 12, 0f,
            ArcherController.RangedWeapon.Bow, MageController.MagicWeapon.Spellbook);

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("Equipment setup: Player not found.");
            return;
        }

        EquipmentInventory inventory = player.GetComponent<EquipmentInventory>();
        if (inventory == null) inventory = player.AddComponent<EquipmentInventory>();
        SerializedObject serialized = new(inventory);
        var starting = new List<EquipmentData>
            { weapons[0], bows[0], crossbows[0], staffs[0], spellbooks[0],
              helmets[0], armors[0], boots[0], accessories[0],
              archerHelmets[0], archerArmors[0], archerBoots[0], archerAccessories[0],
              mageHelmets[0], mageArmors[0], mageBoots[0], mageAccessories[0] };
        var loot = new List<EquipmentData>(weapons);
        loot.AddRange(bows); loot.AddRange(crossbows); loot.AddRange(staffs); loot.AddRange(spellbooks);
        loot.AddRange(helmets); loot.AddRange(armors); loot.AddRange(boots); loot.AddRange(accessories);
        loot.AddRange(archerHelmets); loot.AddRange(archerArmors);
        loot.AddRange(archerBoots); loot.AddRange(archerAccessories);
        loot.AddRange(mageHelmets); loot.AddRange(mageArmors);
        loot.AddRange(mageBoots); loot.AddRange(mageAccessories);
        SetArray(serialized.FindProperty("startingEquipment"), starting);
        SetArray(serialized.FindProperty("lootTemplates"), loot);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(inventory);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Equipment system setup complete: {weapons.Count} warrior and " +
                  $"{bows.Count + crossbows.Count + staffs.Count + spellbooks.Count} class weapon assets connected.");
    }

    private static List<EquipmentData> CreateArmorSet(string rootFolder, string idPrefix,
        string folderName, string displayName, EquipmentSlot slot, WeaponClass itemClass,
        int attackPower, int defense, int health, float criticalChance,
        float criticalDamage, float attackSpeed, float moveSpeed, int mana)
    {
        EnsureFolder(rootFolder, folderName);
        var output = new List<EquipmentData>();
        for (int i = 0; i < 5; i++)
        {
            EquipmentRarity rarity = (EquipmentRarity)i;
            float power = EquipmentRarityUtility.Power(rarity);
            string id = $"{idPrefix}{slot.ToString().ToLowerInvariant()}_{i + 1:00}";
            EquipmentData data = LoadOrCreate($"{rootFolder}/{folderName}/{folderName}_{i + 1:00}.asset");
            SerializedObject serialized = new(data);
            SetCommon(serialized, id, $"{displayName} {i + 1:00}", slot, rarity,
                Mathf.RoundToInt(attackPower * power), Mathf.RoundToInt(defense * power),
                Mathf.RoundToInt(health * power), criticalChance * power, criticalDamage * power,
                attackSpeed * power, moveSpeed * power, Mathf.RoundToInt(mana * power));
            serialized.FindProperty("weaponClass").enumValueIndex = (int)itemClass;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            output.Add(data);
        }
        return output;
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
            serialized.FindProperty("weaponClass").enumValueIndex = (int)WeaponClass.Warrior;
            serialized.FindProperty("equippedSprite").objectReferenceValue = sprites[i];
            serialized.FindProperty("attackSprite").objectReferenceValue = sprites[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            output.Add(data);
        }
    }

    private static List<EquipmentData> CreateClassWeaponSet(string name, WeaponClass weaponClass,
        int baseAttack, float attackSpeed, ArcherController.RangedWeapon archerType,
        MageController.MagicWeapon mageType)
    {
        string root = weaponClass == WeaponClass.Archer ? ArcherWeaponFolder : MageWeaponFolder;
        EnsureFolder(root, name);
        var output = new List<EquipmentData>();
        for (int i = 0; i < 5; i++)
        {
            EquipmentRarity rarity = (EquipmentRarity)i;
            float power = EquipmentRarityUtility.Power(rarity);
            string id = $"{weaponClass.ToString().ToLowerInvariant()}_{name.ToLowerInvariant()}_{i + 1:00}";
            EquipmentData data = LoadOrCreate($"{root}/{name}/{name}_{i + 1:00}.asset");
            SerializedObject serialized = new(data);
            float scaledAttackSpeed = weaponClass == WeaponClass.Archer
                ? attackSpeed + i * 0.06f
                : attackSpeed * power;
            SetCommon(serialized, id, $"{name} {i + 1:00}", EquipmentSlot.Weapon, rarity,
                Mathf.RoundToInt((baseAttack + i * 2) * power), 0, 0, 0f, 0f,
                scaledAttackSpeed, 0f, weaponClass == WeaponClass.Mage ? 5 + i * 3 : 0);
            serialized.FindProperty("weaponClass").enumValueIndex = (int)weaponClass;
            serialized.FindProperty("archerWeaponType").enumValueIndex = (int)archerType;
            serialized.FindProperty("mageWeaponType").enumValueIndex = (int)mageType;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            output.Add(data);
        }
        return output;
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
