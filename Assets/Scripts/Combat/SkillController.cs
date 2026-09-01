using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats), typeof(PlayerController))]
public class SkillController : MonoBehaviour
{
    public enum SkillSlot { Q, E, R }

    [SerializeField, Min(0)] private int warriorManaCost = 10;
    [SerializeField, Min(0.1f)] private float warriorCooldown = 4f;
    [SerializeField, Min(0)] private int archerManaCost = 12;
    [SerializeField, Min(0.1f)] private float archerCooldown = 3.5f;
    [SerializeField, Min(0)] private int mageManaCost = 20;
    [SerializeField, Min(0.1f)] private float mageCooldown = 5f;
    [Header("E Skills")]
    [SerializeField, Min(0)] private int warriorEManaCost = 16;
    [SerializeField, Min(0.1f)] private float warriorECooldown = 7f;
    [SerializeField, Min(0)] private int archerEManaCost = 18;
    [SerializeField, Min(0.1f)] private float archerECooldown = 6f;
    [SerializeField, Min(0)] private int mageEManaCost = 28;
    [SerializeField, Min(0.1f)] private float mageECooldown = 8f;
    [Header("R Skills")]
    [SerializeField, Min(0)] private int warriorRManaCost = 28;
    [SerializeField, Min(0.1f)] private float warriorRCooldown = 12f;
    [SerializeField, Min(0)] private int archerRManaCost = 30;
    [SerializeField, Min(0.1f)] private float archerRCooldown = 11f;
    [SerializeField, Min(0)] private int mageRManaCost = 45;
    [SerializeField, Min(0.1f)] private float mageRCooldown = 14f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private PlayerStats stats;
    private PlayerController controller;
    private ArcherController archer;
    private readonly float[,] nextUseTimes = new float[3, 3];
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
    public float CooldownRemaining => GetCooldownRemaining(SkillSlot.Q);
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
        if (controller.IsInputLocked || stats.IsDead || Keyboard.current == null) return;
        if (Keyboard.current.qKey.wasPressedThisFrame) TryUseSkill(SkillSlot.Q);
        else if (Keyboard.current.eKey.wasPressedThisFrame) TryUseSkill(SkillSlot.E);
        else if (Keyboard.current.rKey.wasPressedThisFrame) TryUseSkill(SkillSlot.R);
    }

    public bool TryUseSkill() => TryUseSkill(SkillSlot.Q);

    public bool TryUseSkill(SkillSlot slot)
    {
        int classIndex = (int)stats.CurrentClass;
        int slotIndex = (int)slot;
        int manaCost = GetManaCost(slot);
        if (controller.IsInputLocked || stats.IsDead || Time.time < nextUseTimes[classIndex, slotIndex] ||
            !stats.UseMana(manaCost)) return false;

        nextUseTimes[classIndex, slotIndex] = Time.time + GetCooldown(slot);
        switch (slot)
        {
            case SkillSlot.E: UseESkill(); break;
            case SkillSlot.R: UseRSkill(); break;
            default: UseQSkill(); break;
        }
        SkillUsed?.Invoke();
        return true;
    }

    private void UseQSkill()
    {
        switch (stats.CurrentClass)
        {
            case PlayerStats.PlayerClass.Archer: UseArrowFan(3, 12f); break;
            case PlayerStats.PlayerClass.Mage:
                DamageAt(transform.position, stats.Intelligence + 18, 3f, StatusEffectType.Freeze, StatusEffectType.Shock);
                ShowRangeEffect(transform.position, 3f, new Color(0.65f, 0.2f, 1f, 0.55f));
                break;
            default:
                DamageAt(transform.position, stats.Strength + 14, 2.2f, StatusEffectType.Burn);
                ShowRangeEffect(transform.position, 2.2f, new Color(1f, 0.35f, 0.15f, 0.55f));
                break;
        }
    }

    private void UseESkill()
    {
        switch (stats.CurrentClass)
        {
            case PlayerStats.PlayerClass.Archer:
                UseArrowFan(7, 8f);
                break;
            case PlayerStats.PlayerClass.Mage:
                DamageAt(transform.position, stats.Intelligence + 28, 3.6f, StatusEffectType.Freeze);
                ShowRangeEffect(transform.position, 3.6f, new Color(0.2f, 0.75f, 1f, 0.65f));
                break;
            default:
                DamageAt(transform.position, stats.Strength + 24, 2.8f, StatusEffectType.Shock);
                ShowRangeEffect(transform.position, 2.8f, new Color(1f, 0.78f, 0.2f, 0.65f));
                break;
        }
    }

    private void UseRSkill()
    {
        Vector2 target = GetTargetPoint(6f);
        switch (stats.CurrentClass)
        {
            case PlayerStats.PlayerClass.Archer:
                DamageAt(target, stats.Dexterity + 42, 2.8f, StatusEffectType.Poison);
                ShowRangeEffect(target, 2.8f, new Color(0.25f, 0.9f, 0.3f, 0.7f));
                break;
            case PlayerStats.PlayerClass.Mage:
                DamageAt(target, stats.Intelligence + 55, 3.2f, StatusEffectType.Burn, StatusEffectType.Shock);
                ShowRangeEffect(target, 3.2f, new Color(0.85f, 0.25f, 1f, 0.75f));
                break;
            default:
                DamageAt(transform.position, stats.Strength + 48, 3.5f, StatusEffectType.Burn);
                ShowRangeEffect(transform.position, 3.5f, new Color(1f, 0.15f, 0.05f, 0.75f));
                break;
        }
    }

    private void UseArrowFan(int arrowCount, float angleStep)
    {
        if (archer == null) archer = GetComponent<ArcherController>();
        Vector2 aim = archer.GetAimDirection();
        float startAngle = -(arrowCount - 1) * angleStep * 0.5f;
        for (int index = 0; index < arrowCount; index++)
            archer.FireArrow((Vector2)(Quaternion.Euler(0f, 0f, startAngle + index * angleStep) * aim),
                CombatCalculator.RollDamage(stats, archer.AttackDamage, out _));
    }

    private void DamageAt(Vector2 center, int baseDamage, float radius, StatusEffectType primaryEffect,
        StatusEffectType? secondaryEffect = null)
    {
        var damaged = new HashSet<IDamageable>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, radius, targetLayers))
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

    public string GetSkillName(SkillSlot slot) => (stats.CurrentClass, slot) switch
    {
        (PlayerStats.PlayerClass.Warrior, SkillSlot.E) => "SHIELD SHOCK",
        (PlayerStats.PlayerClass.Warrior, SkillSlot.R) => "INFERNO CYCLONE",
        (PlayerStats.PlayerClass.Archer, SkillSlot.E) => "SEVENFOLD VOLLEY",
        (PlayerStats.PlayerClass.Archer, SkillSlot.R) => "POISON RAIN",
        (PlayerStats.PlayerClass.Mage, SkillSlot.E) => "FROST NOVA",
        (PlayerStats.PlayerClass.Mage, SkillSlot.R) => "ARCANE METEOR",
        _ => CurrentSkillName
    };

    public int GetManaCost(SkillSlot slot) => (stats.CurrentClass, slot) switch
    {
        (PlayerStats.PlayerClass.Warrior, SkillSlot.E) => warriorEManaCost,
        (PlayerStats.PlayerClass.Warrior, SkillSlot.R) => warriorRManaCost,
        (PlayerStats.PlayerClass.Archer, SkillSlot.E) => archerEManaCost,
        (PlayerStats.PlayerClass.Archer, SkillSlot.R) => archerRManaCost,
        (PlayerStats.PlayerClass.Mage, SkillSlot.E) => mageEManaCost,
        (PlayerStats.PlayerClass.Mage, SkillSlot.R) => mageRManaCost,
        _ => CurrentManaCost
    };

    public float GetCooldownRemaining(SkillSlot slot) =>
        Mathf.Max(0f, nextUseTimes[(int)Stats.CurrentClass, (int)slot] - Time.time);

    private float GetCooldown(SkillSlot slot) => (stats.CurrentClass, slot) switch
    {
        (PlayerStats.PlayerClass.Warrior, SkillSlot.E) => warriorECooldown,
        (PlayerStats.PlayerClass.Warrior, SkillSlot.R) => warriorRCooldown,
        (PlayerStats.PlayerClass.Archer, SkillSlot.E) => archerECooldown,
        (PlayerStats.PlayerClass.Archer, SkillSlot.R) => archerRCooldown,
        (PlayerStats.PlayerClass.Mage, SkillSlot.E) => mageECooldown,
        (PlayerStats.PlayerClass.Mage, SkillSlot.R) => mageRCooldown,
        _ => GetQCooldown()
    };

    private float GetQCooldown() => stats.CurrentClass switch
    {
        PlayerStats.PlayerClass.Archer => archerCooldown,
        PlayerStats.PlayerClass.Mage => mageCooldown,
        _ => warriorCooldown
    };

    private Vector2 GetTargetPoint(float maxRange)
    {
        Vector2 origin = transform.position;
        if (Camera.main == null || Mouse.current == null) return origin;
        Vector2 offset = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - origin;
        return origin + Vector2.ClampMagnitude(offset, maxRange);
    }

    private void ShowRangeEffect(Vector2 center, float radius, Color color)
    {
        PlayerAttackVfx.SpawnSkillBurst(center, radius, color);
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        var effect = new GameObject("Skill Range Effect", typeof(SpriteRenderer));
        effect.transform.position = center;
        effect.transform.localScale = Vector3.one * (radius * 2f);
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeCombatSprites.Ring;
        renderer.color = color;
        renderer.sortingLayerID = source != null ? source.sortingLayerID : 0;
        renderer.sortingOrder = source != null ? source.sortingOrder - 1 : 0;
        Destroy(effect, 0.34f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || stats == null) return;
        Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.7f);
        float radius = stats.CurrentClass == PlayerStats.PlayerClass.Mage ? 3f : 2.2f;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

/// <summary>Simple runtime sprites used when a dedicated combat-effect asset is unavailable.</summary>
public static class RuntimeCombatSprites
{
    private static Sprite circle;
    private static Sprite ring;
    private static Sprite projectile;

    public static Sprite Circle => circle != null ? circle : circle = CreateCircle(32, 0f);
    public static Sprite Ring => ring != null ? ring : ring = CreateCircle(64, 0.72f);
    public static Sprite Projectile => projectile != null ? projectile : projectile = CreateArrowSprite();

    private static Sprite CreateCircle(int size, float innerRadius)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = innerRadius > 0f ? "Runtime Skill Ring" : "Runtime Magic Orb",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float normalizedDistance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / radius;
            float outerAlpha = Mathf.Clamp01((1f - normalizedDistance) * 6f);
            float innerAlpha = innerRadius <= 0f ? 1f : Mathf.Clamp01((normalizedDistance - innerRadius) * 8f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, outerAlpha * innerAlpha);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
    }

    private static Sprite CreateArrowSprite()
    {
        const int width = 32;
        const int height = 8;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Runtime Arrow",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            bool shaft = x < 24 && y >= 3 && y <= 4;
            bool head = x >= 22 && Mathf.Abs(y - 3.5f) <= (x - 21) * 0.5f;
            bool feathers = x <= 5 && (y <= 2 || y >= 5);
            pixels[y * width + x] = shaft || head || feathers ? Color.white : Color.clear;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
    }
}
