using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    public enum PlayerClass { Warrior, Archer }

    [Header("Survival")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int maxMana = 50;

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

    public int CurrentHealth { get; private set; }
    public int CurrentMana { get; private set; }
    public int MaxHealth => maxHealth;
    public int MaxMana => maxMana;
    public bool IsDead { get; private set; }
    public int Strength => strength;
    public int Defense => defense;
    public int Dexterity => dexterity;
    public PlayerClass CurrentClass => currentClass;
    public int Level => level;
    public int CurrentExperience { get; private set; }
    public int ExperienceToNextLevel => GetExperienceRequirement(level);

    public event Action<int, int> HealthChanged;
    public event Action<int, int> ManaChanged;
    public event Action Died;
    public event Action<int, int> ExperienceChanged;
    public event Action<int> LeveledUp;
    public event Action<PlayerClass> ClassChanged;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentMana = maxMana;
    }

    private void Start()
    {
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        ManaChanged?.Invoke(CurrentMana, maxMana);
        ExperienceChanged?.Invoke(CurrentExperience, ExperienceToNextLevel);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;
        int finalDamage = Mathf.Max(1, damage - defense);
        CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (CurrentHealth == 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public bool UseMana(int cost)
    {
        if (IsDead || cost < 0 || CurrentMana < cost) return false;
        CurrentMana -= cost;
        ManaChanged?.Invoke(CurrentMana, maxMana);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
        ManaChanged?.Invoke(CurrentMana, maxMana);
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

    private void LevelUp()
    {
        level++;
        maxHealth += healthPerLevel;
        maxMana += manaPerLevel;
        strength += strengthPerLevel;
        dexterity += dexterityPerLevel;
        defense += defensePerLevel;
        CurrentHealth = maxHealth;
        CurrentMana = maxMana;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        ManaChanged?.Invoke(CurrentMana, maxMana);
        LeveledUp?.Invoke(level);
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        CurrentHealth = 0;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Died?.Invoke();
    }
}
