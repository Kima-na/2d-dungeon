using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
public class ArcherController : MonoBehaviour
{
    [SerializeField, Min(0)] private int bowDamage = 8;
    [SerializeField, Min(0.1f)] private float attackCooldown = 0.55f;
    [SerializeField, Min(0.1f)] private float arrowSpeed = 12f;
    [SerializeField, Min(0.1f)] private float arrowLifetime = 2f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private PlayerController controller;
    private PlayerStats stats;
    private float nextAttackTime;

    public int AttackDamage => PlayerStats.Dexterity + bowDamage;
    public float AttackCooldown => attackCooldown;
    private PlayerStats PlayerStats => stats != null ? stats : stats = GetComponent<PlayerStats>();

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (stats.IsDead || stats.CurrentClass != PlayerStats.PlayerClass.Archer ||
            Time.time < nextAttackTime || Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame) return;

        Shoot();
    }

    public void Shoot()
    {
        if (stats.IsDead || stats.CurrentClass != PlayerStats.PlayerClass.Archer || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        Vector2 direction = GetAimDirection();
        GameObject arrow = new GameObject("Arrow", typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CapsuleCollider2D), typeof(ArrowProjectile));
        arrow.transform.position = (Vector2)transform.position + direction * 0.7f;
        arrow.transform.right = direction;
        arrow.transform.localScale = new Vector3(0.5f, 0.12f, 1f);

        SpriteRenderer renderer = arrow.GetComponent<SpriteRenderer>();
        renderer.sprite = GetComponent<SpriteRenderer>()?.sprite;
        renderer.color = new Color(1f, 0.82f, 0.25f);

        Rigidbody2D body = arrow.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        arrow.GetComponent<CapsuleCollider2D>().isTrigger = true;
        arrow.GetComponent<ArrowProjectile>().Initialize(transform.root, stats, direction * arrowSpeed,
            AttackDamage, arrowLifetime, targetLayers);
    }

    private Vector2 GetAimDirection()
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
