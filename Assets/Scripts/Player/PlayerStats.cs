using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    public enum PlayerClass { Warrior, Archer, Mage }

    [Header("Survival")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int maxMana = 50;
    [SerializeField, Min(0f)] private float manaRegenerationPerSecond = 2f;

    [Header("Combat Stats")]
    [SerializeField, Range(0f, 1f)] private float criticalChance = 0.1f;
    [SerializeField, Min(1f)] private float criticalDamageMultiplier = 1.5f;
    [SerializeField, Min(0.1f)] private float attackSpeedMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float moveSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float criticalChancePerLevel = 0.005f;
    [SerializeField, Min(0f)] private float attackSpeedPerLevel = 0.02f;
    [SerializeField, Min(0f)] private float moveSpeedPerLevel = 0.01f;

    [Header("Warrior Growth")]
    [SerializeField, Min(1)] private int strength = 10;
    [SerializeField, Min(0)] private int defense = 5;
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(1)] private int baseExperienceRequirement = 100;
    [SerializeField, Min(1f)] private float experienceGrowth = 1.5f;
    [SerializeField, Min(0)] private int healthPerLevel = 15;
    [SerializeField, Min(0)] private int manaPerLevel = 5;
    [SerializeField, Min(0)] private int strengthPerLevel = 2;
    [SerializeField, Min(0)] private int defensePerLevel = 1;

    [Header("Class")]
    [SerializeField] private PlayerClass currentClass = PlayerClass.Warrior;
    [SerializeField, Min(1)] private int dexterity = 12;
    [SerializeField, Min(0)] private int dexterityPerLevel = 3;
    [SerializeField, Min(1)] private int intelligence = 14;
    [SerializeField, Min(0)] private int intelligencePerLevel = 3;

    public int CurrentHealth { get; private set; }
    public int CurrentMana { get; private set; }
    private EquipmentInventory equipment;
    private int EquipmentInt(EquipmentStat stat) => equipment != null ? Mathf.RoundToInt(equipment.Sum(stat)) : 0;
    private float EquipmentFloat(EquipmentStat stat) => equipment != null ? equipment.Sum(stat) : 0f;
    public int MaxHealth => maxHealth + EquipmentInt(EquipmentStat.MaxHealth);
    public int MaxMana => maxMana + EquipmentInt(EquipmentStat.MaxMana);
    public bool IsDead { get; private set; }
    public int Strength => strength;
    public int Defense => defense + EquipmentInt(EquipmentStat.Defense);
    public int Dexterity => dexterity;
    public int Intelligence => intelligence;
    public int AttackPowerBonus => EquipmentInt(EquipmentStat.AttackPower);
    public float CriticalChance => Mathf.Clamp01(criticalChance + EquipmentFloat(EquipmentStat.CriticalChance));
    public float CriticalDamageMultiplier => criticalDamageMultiplier + EquipmentFloat(EquipmentStat.CriticalDamage);
    public float AttackSpeedMultiplier => attackSpeedMultiplier + EquipmentFloat(EquipmentStat.AttackSpeed);
    public float MoveSpeedMultiplier => moveSpeedMultiplier + EquipmentFloat(EquipmentStat.MoveSpeed);
    public PlayerClass CurrentClass => currentClass;
    public int Level => level;
    public int CurrentExperience { get; private set; }
    public int Gold { get; private set; }
    public int BaseMaxHealth => maxHealth;
    public int BaseMaxMana => maxMana;
    public int BaseDefense => defense;
    public float BaseCriticalChance => criticalChance;
    public float BaseCriticalDamage => criticalDamageMultiplier;
    public float BaseAttackSpeed => attackSpeedMultiplier;
    public float BaseMoveSpeed => moveSpeedMultiplier;
    public int ExperienceToNextLevel => GetExperienceRequirement(level);

    public event Action<int, int> HealthChanged;
    public event Action<int, int> ManaChanged;
    public event Action Died;
    public event Action<int, int> ExperienceChanged;
    public event Action<int> LeveledUp;
    public event Action<PlayerClass> ClassChanged;
    public event Action<int> GoldChanged;
    private float manaRegenerationBuffer;
    private int initialMaxHealth, initialMaxMana, initialStrength, initialDefense, initialDexterity, initialIntelligence;
    private float initialCriticalChance, initialCriticalDamage, initialAttackSpeed, initialMoveSpeed;

    private void Awake()
    {
        initialMaxHealth = maxHealth; initialMaxMana = maxMana; initialStrength = strength;
        initialDefense = defense; initialDexterity = dexterity; initialIntelligence = intelligence;
        initialCriticalChance = criticalChance; initialCriticalDamage = criticalDamageMultiplier;
        initialAttackSpeed = attackSpeedMultiplier; initialMoveSpeed = moveSpeedMultiplier;
        equipment = GetComponent<EquipmentInventory>();
        if (equipment == null) equipment = gameObject.AddComponent<EquipmentInventory>();
        equipment.EquipmentChanged += OnEquipmentChanged;
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
    }

    private void Start()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        ManaChanged?.Invoke(CurrentMana, MaxMana);
        ExperienceChanged?.Invoke(CurrentExperience, ExperienceToNextLevel);
    }

    private void Update()
    {
        if (IsDead || CurrentMana >= MaxMana || manaRegenerationPerSecond <= 0f) return;
        manaRegenerationBuffer += manaRegenerationPerSecond * Time.deltaTime;
        int restored = Mathf.FloorToInt(manaRegenerationBuffer);
        if (restored <= 0) return;
        manaRegenerationBuffer -= restored;
        RestoreMana(restored);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;
        int finalDamage = Mathf.Max(1, damage - Defense);
        CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
        DamagePopup.Spawn(transform, finalDamage);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if (CurrentHealth == 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public bool UseMana(int cost)
    {
        if (IsDead || cost < 0 || CurrentMana < cost) return false;
        CurrentMana -= cost;
        ManaChanged?.Invoke(CurrentMana, MaxMana);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
        ManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        CurrentExperience += amount;
        while (CurrentExperience >= ExperienceToNextLevel)
        {
            CurrentExperience -= ExperienceToNextLevel;
            LevelUp();
        }
        ExperienceChanged?.Invoke(CurrentExperience, ExperienceToNextLevel);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        GoldChanged?.Invoke(Gold);
    }

    public bool TrySpendGold(int amount)
    {
        int price = Mathf.Max(0, amount);
        if (Gold < price) return false;
        Gold -= price;
        GoldChanged?.Invoke(Gold);
        return true;
    }

    public int GetExperienceRequirement(int targetLevel)
    {
        int safeLevel = Mathf.Max(1, targetLevel);
        return Mathf.Max(1, Mathf.RoundToInt(baseExperienceRequirement *
            Mathf.Pow(experienceGrowth, safeLevel - 1)));
    }

    public void SelectClass(PlayerClass playerClass)
    {
        if (currentClass == playerClass) return;
        currentClass = playerClass;
        ClassChanged?.Invoke(currentClass);
    }

    public void ResetForNewGame(PlayerClass playerClass)
    {
        currentClass = playerClass;
        level = 1;
        CurrentExperience = 0;
        Gold = 0;
        maxHealth = initialMaxHealth; maxMana = initialMaxMana; strength = initialStrength;
        defense = initialDefense; dexterity = initialDexterity; intelligence = initialIntelligence;
        criticalChance = initialCriticalChance; criticalDamageMultiplier = initialCriticalDamage;
        attackSpeedMultiplier = initialAttackSpeed; moveSpeedMultiplier = initialMoveSpeed;
        IsDead = false;
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        NotifyAllStats();
    }

    public void ApplySave(GameSaveData data)
    {
        if (data == null) return;
        currentClass = (PlayerClass)Mathf.Clamp(data.playerClass, 0, 2);
        level = Mathf.Max(1, data.level);
        CurrentExperience = Mathf.Max(0, data.experience);
        Gold = Mathf.Max(0, data.gold);
        maxHealth = Mathf.Max(1, data.maxHealth);
        maxMana = Mathf.Max(0, data.maxMana);
        strength = Mathf.Max(1, data.strength);
        defense = Mathf.Max(0, data.defense);
        dexterity = Mathf.Max(1, data.dexterity);
        intelligence = Mathf.Max(1, data.intelligence);
        criticalChance = Mathf.Clamp01(data.criticalChance);
        criticalDamageMultiplier = Mathf.Max(1f, data.criticalDamage);
        attackSpeedMultiplier = Mathf.Max(0.1f, data.attackSpeed);
        moveSpeedMultiplier = Mathf.Max(0.1f, data.moveSpeed);
        IsDead = false;
        CurrentHealth = Mathf.Clamp(data.health, 1, MaxHealth);
        CurrentMana = Mathf.Clamp(data.mana, 0, MaxMana);
        NotifyAllStats();
    }

    private void NotifyAllStats()
    {
        ClassChanged?.Invoke(currentClass);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        ManaChanged?.Invoke(CurrentMana, MaxMana);
        ExperienceChanged?.Invoke(CurrentExperience, ExperienceToNextLevel);
        GoldChanged?.Invoke(Gold);
    }

    private void LevelUp()
    {
        level++;
        maxHealth += healthPerLevel;
        maxMana += manaPerLevel;
        strength += strengthPerLevel;
        dexterity += dexterityPerLevel;
        intelligence += intelligencePerLevel;
        criticalChance = Mathf.Clamp01(criticalChance + criticalChancePerLevel);
        attackSpeedMultiplier += attackSpeedPerLevel;
        moveSpeedMultiplier += moveSpeedPerLevel;
        defense += defensePerLevel;
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        ManaChanged?.Invoke(CurrentMana, MaxMana);
        LeveledUp?.Invoke(level);
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        CurrentHealth = 0;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        Died?.Invoke();
    }

    private void OnEquipmentChanged()
    {
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        ManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    private void OnDestroy()
    {
        if (equipment != null) equipment.EquipmentChanged -= OnEquipmentChanged;
    }
}
