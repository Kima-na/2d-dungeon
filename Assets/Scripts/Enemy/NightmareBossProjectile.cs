using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public sealed class NightmareBossProjectile : MonoBehaviour
{
    private static Sprite[] phaseOneOrbs;
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
        projectile.transform.localScale = Vector3.one * 0.72f;
        SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
        NightmareBossCombat combat = owner.GetComponent<NightmareBossCombat>();
        bool usingPhaseOneArt = combat != null && combat.Phase == 1;
        if (usingPhaseOneArt)
        {
            if (phaseOneOrbs == null)
            {
                phaseOneOrbs = new Sprite[4];
                for (int i = 0; i < phaseOneOrbs.Length; i++)
                    phaseOneOrbs[i] = Resources.Load<Sprite>($"NightmareBoss/Phase1Effects/Orb_{i + 2:00}");
            }
            renderer.sprite = phaseOneOrbs[Random.Range(0, phaseOneOrbs.Length)];
        }
        else renderer.sprite = RuntimeCombatSprites.Circle;
        renderer.color = usingPhaseOneArt ? Color.white : color;
        renderer.sortingOrder = 10;
        PlayerAttackVfx.AttachTrail(projectile, new Color(color.r, color.g, color.b, 0.75f), 0.2f);
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
