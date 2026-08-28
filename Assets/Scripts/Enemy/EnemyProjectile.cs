using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class EnemyProjectile : MonoBehaviour
{
    private Transform owner;
    private int damage;
    private bool magic;

    public static void Spawn(Transform projectileOwner, Vector2 direction, int attackDamage,
        float speed, Color color, bool isMagic)
    {
        GoblinWarriorVisualDatabase database =
            Resources.Load<GoblinWarriorVisualDatabase>("GoblinWarriorVisualDatabase");
        GameObject prefab = database != null ? database.GetProjectilePrefab(isMagic) : null;
        GameObject projectile = prefab != null ? Instantiate(prefab) :
            new GameObject(isMagic ? "Goblin Magic" : "Goblin Arrow",
                typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyProjectile));
        projectile.name = isMagic ? "Goblin Magic" : "Goblin Arrow";
        projectile.transform.position = projectileOwner.position + (Vector3)(direction * 0.65f);
        projectile.transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
        if (renderer.sprite == null) renderer.sprite = MonsterRoster.PlaceholderSprite;
        if (prefab == null)
        {
            projectile.transform.localScale = isMagic ? Vector3.one * 0.28f : new Vector3(0.5f, 0.12f, 1f);
            renderer.color = color;
        }
        var collider = projectile.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        var body = projectile.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearVelocity = direction * speed;
        projectile.GetComponent<EnemyProjectile>().Initialize(projectileOwner, attackDamage, isMagic);
    }

    private void Initialize(Transform projectileOwner, int attackDamage, bool isMagic)
    {
        owner = projectileOwner;
        damage = attackDamage;
        magic = isMagic;
        Destroy(gameObject, 4f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root == owner || other.GetComponentInParent<EnemyAI>() != null) return;
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player != null)
        {
            player.TakeDamage(damage);
            if (magic) StatusEffectController.TryApply(player.gameObject, StatusEffectType.Shock, null, 1f);
            Destroy(gameObject);
        }
        else if (!other.isTrigger) Destroy(gameObject);
    }
}
