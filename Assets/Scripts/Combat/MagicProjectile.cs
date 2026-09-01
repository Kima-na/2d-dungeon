using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MagicProjectile : MonoBehaviour
{
    private Transform owner;
    private PlayerStats ownerStats;
    private int damage;
    private LayerMask targetLayers;

    public void Initialize(Transform projectileOwner, PlayerStats stats, Vector2 velocity,
        int attackDamage, float lifetime, LayerMask layers)
    {
        owner = projectileOwner;
        ownerStats = stats;
        damage = attackDamage;
        targetLayers = layers;
        GetComponent<Rigidbody2D>().linearVelocity = velocity;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root == owner || (targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
        Damageable damageable = other.GetComponentInParent<Damageable>();
        if (TryDamage(damageable, other.gameObject)) return;
        if (!other.isTrigger) { PlayerAttackVfx.SpawnImpact(transform.position, new Color(0.55f, 0.2f, 1f), 0.7f); Destroy(gameObject); }
    }

    private void FixedUpdate()
    {
        foreach (EnemyAI enemy in EnemyAI.ActiveEnemies)
        {
            if (enemy == null || Vector2.Distance(transform.position, enemy.transform.position) > 0.72f) continue;
            if (TryDamage(enemy.Health, enemy.gameObject)) return;
        }
    }

    private bool TryDamage(Damageable damageable, GameObject hitObject)
    {
        if (damageable == null || damageable.IsDead) return false;
        damage = CombatCalculator.ApplyTargetModifiers(hitObject, damage);
        damageable.TakeDamage(damage);
        PlayerAttackVfx.SpawnImpact(transform.position, new Color(0.7f, 0.3f, 1f), 0.9f);
        if (!damageable.IsDead)
            StatusEffectController.TryApply(hitObject, StatusEffectType.Shock, ownerStats, 0.35f);
        if (damageable.IsDead) ownerStats.AddExperience(damageable.ExperienceReward);
        Destroy(gameObject);
        return true;
    }
}
