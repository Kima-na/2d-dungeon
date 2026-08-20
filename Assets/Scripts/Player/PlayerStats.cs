using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Survival")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(0)] private int maxMana = 50;

    public int CurrentHealth { get; private set; }
    public int CurrentMana { get; private set; }
    public int MaxHealth => maxHealth;
    public int MaxMana => maxMana;
    public bool IsDead { get; private set; }

    public event Action<int, int> HealthChanged;
    public event Action<int, int> ManaChanged;
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentMana = maxMana;
    }

    private void Start()
    {
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        ManaChanged?.Invoke(CurrentMana, maxMana);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
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

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        CurrentHealth = 0;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Died?.Invoke();
    }
}
