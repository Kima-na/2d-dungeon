using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats), typeof(PlayerController))]
public class SkillController : MonoBehaviour
{
    [SerializeField, Min(0)] private int warriorManaCost = 10;
    [SerializeField, Min(0.1f)] private float warriorCooldown = 4f;
    [SerializeField, Min(0)] private int archerManaCost = 12;
    [SerializeField, Min(0.1f)] private float archerCooldown = 3.5f;
    [SerializeField, Min(0)] private int mageManaCost = 20;
    [SerializeField, Min(0.1f)] private float mageCooldown = 5f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private PlayerStats stats;
    private PlayerController controller;
    private ArcherController archer;
    private float[] nextUseTimes = new float[3];
    private PlayerStats Stats => stats != null ? stats : stats = GetComponent<PlayerStats>();

    public string CurrentSkillName => Stats.CurrentClass switch
    {
        PlayerStats.PlayerClass.Archer => GetArcherSkillName(),
        PlayerStats.PlayerClass.Mage => "ARCANE NOVA",
        _ => "WHIRLWIND"
    };
    public int CurrentManaCost => Stats.CurrentClass switch
    {
        PlayerStats.PlayerClass.Archer => archerManaCost,
        PlayerStats.PlayerClass.Mage => mageManaCost,
        _ => warriorManaCost
    };
    public float CooldownRemaining => Mathf.Max(0f, nextUseTimes[(int)Stats.CurrentClass] - Time.time);
    public event Action SkillUsed;

    private string GetArcherSkillName()
    {
        if (archer == null) archer = GetComponent<ArcherController>();
        return archer != null && archer.EquippedWeapon == ArcherController.RangedWeapon.Crossbow
            ? "BOLT VOLLEY"
            : "MULTISHOT";
    }

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        archer = GetComponent<ArcherController>();
    }

    private void Update()
    {
        if (!controller.IsInputLocked && !stats.IsDead && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            TryUseSkill();
    }

    public bool TryUseSkill()
    {
        int classIndex = (int)stats.CurrentClass;
        if (controller.IsInputLocked || stats.IsDead || Time.time < nextUseTimes[classIndex] || !stats.UseMana(CurrentManaCost)) return false;

        nextUseTimes[classIndex] = Time.time + GetCurrentCooldown();
        switch (stats.CurrentClass)
        {
            case PlayerStats.PlayerClass.Archer: UseMultishot(); break;
            case PlayerStats.PlayerClass.Mage:
                DamageAround(stats.Intelligence + 18, 3f, StatusEffectType.Freeze, StatusEffectType.Shock);
                ShowRangeEffect(3f, new Color(0.65f, 0.2f, 1f, 0.28f));
                break;
            default:
                DamageAround(stats.Strength + 14, 2.2f, StatusEffectType.Burn);
                ShowRangeEffect(2.2f, new Color(1f, 0.35f, 0.15f, 0.28f));
                break;
        }
        SkillUsed?.Invoke();
        return true;
    }

    private void UseMultishot()
    {
        if (archer == null) archer = GetComponent<ArcherController>();
        Vector2 aim = archer.GetAimDirection();
        for (int angle = -12; angle <= 12; angle += 12)
            archer.FireArrow((Vector2)(Quaternion.Euler(0f, 0f, angle) * aim),
                CombatCalculator.RollDamage(stats, archer.AttackDamage, out _));
    }

    private void DamageAround(int baseDamage, float radius, StatusEffectType primaryEffect,
        StatusEffectType? secondaryEffect = null)
    {
        var damaged = new HashSet<IDamageable>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius, targetLayers))
        {
            if (hit.transform.root == transform.root) continue;
            foreach (MonoBehaviour behaviour in hit.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is not IDamageable damageable || damageable.IsDead || !damaged.Add(damageable)) continue;
                int damage = CombatCalculator.RollDamage(stats, baseDamage, out _);
                damage = CombatCalculator.ApplyTargetModifiers(hit.gameObject, damage);
                damageable.TakeDamage(damage);
                if (!damageable.IsDead)
                {
                    StatusEffectController.TryApply(hit.gameObject, primaryEffect, stats);
                    if (secondaryEffect.HasValue)
                        StatusEffectController.TryApply(hit.gameObject, secondaryEffect.Value, stats);
                }
                if (damageable.IsDead && behaviour is IExperienceSource source)
                    stats.AddExperience(source.ExperienceReward);
                break;
            }
        }
    }

    private float GetCurrentCooldown() => stats.CurrentClass switch
    {
        PlayerStats.PlayerClass.Archer => archerCooldown,
        PlayerStats.PlayerClass.Mage => mageCooldown,
        _ => warriorCooldown
    };

    private void ShowRangeEffect(float radius, Color color)
    {
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        if (source == null || source.sprite == null) return;
        var effect = new GameObject("Skill Range Effect", typeof(SpriteRenderer));
        effect.transform.position = transform.position;
        effect.transform.localScale = Vector3.one * (radius * 2f);
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.color = color;
        renderer.sortingOrder = source.sortingOrder - 1;
        Destroy(effect, 0.18f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || stats == null) return;
        Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.7f);
        float radius = stats.CurrentClass == PlayerStats.PlayerClass.Mage ? 3f : 2.2f;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
