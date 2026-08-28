using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public sealed class NightmareBossProjectile : MonoBehaviour
{
    private Transform owner;
    private Transform target;
    private Vector2 direction;
    private float speed;
    private float turnSpeed;
    private int damage;

    public static void Spawn(Transform owner, Transform target, Vector2 direction, int damage,
        float speed, float turnSpeed, Color color, float lifetime = 4f)
    {
        GameObject projectile = new("Nightmare Chaos Orb", typeof(SpriteRenderer),
            typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(NightmareBossProjectile));
        projectile.transform.position = owner.position + (Vector3)(direction.normalized * 0.9f);
        projectile.transform.localScale = Vector3.one * 0.32f;
        SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
        renderer.sprite = MonsterRoster.PlaceholderSprite;
        renderer.color = color;
        renderer.sortingOrder = 10;
        CircleCollider2D hitbox = projectile.GetComponent<CircleCollider2D>();
        hitbox.isTrigger = true;
        hitbox.radius = 0.55f;
        Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        NightmareBossProjectile script = projectile.GetComponent<NightmareBossProjectile>();
        script.owner = owner;
        script.target = target;
        script.direction = direction.normalized;
        script.damage = damage;
        script.speed = speed;
        script.turnSpeed = turnSpeed;
        Destroy(projectile, lifetime);
    }

    private void FixedUpdate()
    {
        if (target != null && turnSpeed > 0f)
        {
            Vector2 desired = ((Vector2)target.position - (Vector2)transform.position).normalized;
            direction = Vector2.Lerp(direction, desired, turnSpeed * Time.fixedDeltaTime).normalized;
        }
        transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root == owner || other.GetComponentInParent<BossHealth>() != null ||
            other.GetComponentInParent<EnemyAI>() != null) return;
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player == null || player.IsDead) return;
        player.TakeDamage(damage);
        Destroy(gameObject);
    }
}
