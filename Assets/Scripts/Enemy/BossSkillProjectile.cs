using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public sealed class BossSkillProjectile : MonoBehaviour
{
    private Transform owner;
    private int damage;
    private PlayerStats target;
    private Vector2 origin;
    private Vector2 travelDirection;

    public static void Spawn(Transform owner, Vector2 direction, int damage, float speed,
        float lifetime, Sprite sprite)
    {
        GameObject projectile = new("Dark Shockwave", typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CapsuleCollider2D), typeof(BossSkillProjectile));
        projectile.transform.position = owner.position + (Vector3)(direction * 0.9f);
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : MonsterRoster.PlaceholderSprite;
        renderer.color = sprite != null ? Color.white : new Color(0.55f, 0.05f, 0.9f);
        renderer.sortingOrder = 9;
        Vector2 size = renderer.sprite.bounds.size;
        projectile.transform.localScale = new Vector3(1.8f / Mathf.Max(0.01f, size.x),
            0.65f / Mathf.Max(0.01f, size.y), 1f);
        CapsuleCollider2D hitbox = projectile.GetComponent<CapsuleCollider2D>();
        hitbox.isTrigger = true;
        hitbox.direction = CapsuleDirection2D.Horizontal;
        hitbox.size = size;
        Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearVelocity = direction * speed;
        BossSkillProjectile script = projectile.GetComponent<BossSkillProjectile>();
        script.owner = owner;
        script.damage = damage;
        script.target = FindAnyObjectByType<PlayerStats>();
        script.origin = projectile.transform.position;
        script.travelDirection = direction.normalized;
        Destroy(projectile, lifetime);
    }

    private void FixedUpdate()
    {
        // Keep the trigger collider as the primary hitbox, with a swept-distance
        // fallback for fast projectiles that cross a small collider in one step.
        if (target == null || target.IsDead) return;
        Vector2 toTarget = (Vector2)target.transform.position - origin;
        float forward = Vector2.Dot(toTarget, travelDirection);
        float travelled = Vector2.Dot((Vector2)transform.position - origin, travelDirection);
        float lateral = Mathf.Abs(travelDirection.x * toTarget.y - travelDirection.y * toTarget.x);
        if (forward >= -0.2f && forward <= travelled + 0.75f && lateral <= 0.7f)
            DamageAndDestroy(target);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root == owner || other.GetComponentInParent<BossHealth>() != null ||
            other.GetComponentInParent<EnemyAI>() != null) return;
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player != null)
        {
            DamageAndDestroy(player);
        }
        else if (!other.isTrigger) Destroy(gameObject);
    }

    private void DamageAndDestroy(PlayerStats player)
    {
        if (player == null || player.IsDead) return;
        player.TakeDamage(damage);
        Destroy(gameObject);
    }
}
