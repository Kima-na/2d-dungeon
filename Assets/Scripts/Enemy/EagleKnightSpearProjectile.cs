using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public sealed class EagleKnightSpearProjectile : MonoBehaviour
{
    private Transform owner; private int damage; private bool hit;
    public static void Spawn(Transform owner, Vector2 direction, int damage, float speed, float lifetime, Sprite sprite)
    {
        GameObject go = new("Eagle Knight Spear", typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CapsuleCollider2D), typeof(EagleKnightSpearProjectile));
        go.transform.SetPositionAndRotation(owner.position + (Vector3)(direction * 0.9f),
            Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : RuntimeCombatSprites.Projectile; renderer.sortingOrder = 11;
        Vector2 size = renderer.sprite.bounds.size;
        go.transform.localScale = new Vector3(1.7f / Mathf.Max(0.01f, size.x), 0.42f / Mathf.Max(0.01f, size.y), 1f);
        CapsuleCollider2D collider = go.GetComponent<CapsuleCollider2D>(); collider.isTrigger = true;
        collider.direction = CapsuleDirection2D.Horizontal;
        Rigidbody2D body = go.GetComponent<Rigidbody2D>(); body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.linearVelocity = direction * speed;
        EagleKnightSpearProjectile projectile = go.GetComponent<EagleKnightSpearProjectile>();
        projectile.owner = owner; projectile.damage = damage; Destroy(go, lifetime);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hit || other.transform.root == owner || other.GetComponentInParent<BossHealth>() != null) return;
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player != null && !player.IsDead) { hit = true; player.TakeDamage(damage); Destroy(gameObject); }
        else if (!other.isTrigger) Destroy(gameObject);
    }
}
