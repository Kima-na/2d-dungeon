using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
public class MageController : MonoBehaviour
{
    public enum MagicWeapon { Staff, Spellbook }

    [SerializeField] private MagicWeapon equippedWeapon = MagicWeapon.Staff;
    [SerializeField, Min(0)] private int staffDamage = 8;
    [SerializeField, Min(0)] private int spellbookDamage = 12;
    [SerializeField, Min(0)] private int staffManaCost = 2;
    [SerializeField, Min(0)] private int spellbookManaCost = 4;
    [SerializeField, Min(0.1f)] private float staffCooldown = 0.5f;
    [SerializeField, Min(0.1f)] private float spellbookCooldown = 0.8f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 9f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 2.5f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private PlayerController controller;
    private PlayerStats stats;
    private float nextAttackTime;

    public MagicWeapon EquippedWeapon => equippedWeapon;
    public int AttackDamage => Stats.Intelligence + Stats.AttackPowerBonus +
                               (equippedWeapon == MagicWeapon.Spellbook ? spellbookDamage : staffDamage);
    public int ManaCost => equippedWeapon == MagicWeapon.Spellbook ? spellbookManaCost : staffManaCost;
    public float AttackCooldown => (equippedWeapon == MagicWeapon.Spellbook ? spellbookCooldown : staffCooldown) /
                                   Stats.AttackSpeedMultiplier;
    private PlayerStats Stats => stats != null ? stats : stats = GetComponent<PlayerStats>();

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (controller.IsInputLocked || stats.IsDead || stats.CurrentClass != PlayerStats.PlayerClass.Mage) return;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) equippedWeapon = MagicWeapon.Staff;
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) equippedWeapon = MagicWeapon.Spellbook;
        }
        if (
            Time.time < nextAttackTime || Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame) return;
        Cast();
    }

    public void Cast()
    {
        if (controller.IsInputLocked || stats.IsDead || stats.CurrentClass != PlayerStats.PlayerClass.Mage ||
            Time.time < nextAttackTime || !stats.UseMana(ManaCost)) return;
        nextAttackTime = Time.time + AttackCooldown;
        FireMagicProjectile(GetAimDirection(), CombatCalculator.RollDamage(stats, AttackDamage, out _));
    }

    public void FireMagicProjectile(Vector2 direction, int damage)
    {
        GameObject projectile = new GameObject("Magic Bolt", typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CircleCollider2D), typeof(MagicProjectile));
        projectile.transform.position = (Vector2)transform.position + direction * 0.65f;
        projectile.transform.localScale = Vector3.one * 0.32f;
        SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
        renderer.sprite = GetComponent<SpriteRenderer>()?.sprite;
        renderer.color = new Color(0.65f, 0.25f, 1f, 0.95f);
        Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        projectile.GetComponent<CircleCollider2D>().isTrigger = true;
        projectile.GetComponent<MagicProjectile>().Initialize(transform.root, stats,
            direction * projectileSpeed, damage, projectileLifetime, targetLayers);
    }

    public Vector2 GetAimDirection()
    {
        Camera camera = Camera.main;
        if (camera != null && Mouse.current != null)
        {
            Vector2 aim = (Vector2)camera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) -
                          (Vector2)transform.position;
            if (aim.sqrMagnitude > 0.01f) return aim.normalized;
        }
        return controller.LastMoveDirection;
    }
}
