using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
public class ArcherController : MonoBehaviour
{
    public enum RangedWeapon { Bow, Crossbow }

    [SerializeField] private RangedWeapon equippedWeapon = RangedWeapon.Bow;
    [SerializeField, Min(0)] private int bowDamage = 8;
    [SerializeField, Min(0)] private int crossbowDamage = 12;
    [SerializeField, Min(0.1f)] private float bowCooldown = 0.5f;
    [SerializeField, Min(0.1f)] private float crossbowCooldown = 0.8f;
    [SerializeField, Min(0.1f)] private float arrowSpeed = 12f;
    [SerializeField, Min(0.1f)] private float arrowLifetime = 2f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private PlayerController controller;
    private PlayerStats stats;
    private float nextAttackTime;

    public RangedWeapon EquippedWeapon => equippedWeapon;
    public int AttackDamage => PlayerStats.Dexterity + PlayerStats.AttackPowerBonus +
                               (equippedWeapon == RangedWeapon.Crossbow ? crossbowDamage : bowDamage);
    public float AttackCooldown => (equippedWeapon == RangedWeapon.Crossbow ? crossbowCooldown : bowCooldown) /
                                   PlayerStats.AttackSpeedMultiplier;
    private PlayerStats PlayerStats => stats != null ? stats : stats = GetComponent<PlayerStats>();

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (controller.IsInputLocked || stats.IsDead || stats.CurrentClass != PlayerStats.PlayerClass.Archer) return;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) equippedWeapon = RangedWeapon.Bow;
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) equippedWeapon = RangedWeapon.Crossbow;
        }
        if (
            Time.time < nextAttackTime || Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame) return;

        Shoot();
    }

    public void Shoot()
    {
        if (controller.IsInputLocked || stats.IsDead || stats.CurrentClass != PlayerStats.PlayerClass.Archer || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + AttackCooldown;

        FireArrow(GetAimDirection(), CombatCalculator.RollDamage(stats, AttackDamage, out _));
    }

    public void FireArrow(Vector2 direction, int damage)
    {
        bool crossbow = equippedWeapon == RangedWeapon.Crossbow;
        GameObject arrow = new GameObject(crossbow ? "Crossbow Bolt" : "Arrow", typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CapsuleCollider2D), typeof(ArrowProjectile));
        arrow.transform.position = (Vector2)transform.position + direction * 0.7f;
        arrow.transform.right = direction;
        arrow.transform.localScale = crossbow ? new Vector3(0.36f, 0.09f, 1f) : new Vector3(0.5f, 0.12f, 1f);

        SpriteRenderer renderer = arrow.GetComponent<SpriteRenderer>();
        renderer.sprite = GetComponent<SpriteRenderer>()?.sprite;
        renderer.color = new Color(1f, 0.82f, 0.25f);

        Rigidbody2D body = arrow.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        arrow.GetComponent<CapsuleCollider2D>().isTrigger = true;
        arrow.GetComponent<ArrowProjectile>().Initialize(transform.root, stats, direction * arrowSpeed,
            damage, arrowLifetime, targetLayers, StatusEffectType.Poison, 0.4f);
    }

    public Vector2 GetAimDirection()
    {
        Camera camera = Camera.main;
        if (camera != null && Mouse.current != null)
        {
            Vector3 mouseWorld = camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 aim = (Vector2)mouseWorld - (Vector2)transform.position;
            if (aim.sqrMagnitude > 0.01f) return aim.normalized;
        }
        return controller.LastMoveDirection;
    }
}
