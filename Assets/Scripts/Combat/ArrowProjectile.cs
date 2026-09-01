using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ArrowProjectile : MonoBehaviour
{
    private const int MaxStuckArrows = 15;
    private static readonly Queue<ArrowProjectile> StuckArrows = new();

    private Transform owner;
    private PlayerStats ownerStats;
    private int damage;
    private LayerMask targetLayers;
    private bool isStuck;
    private StatusEffectType statusEffect;
    private float statusChance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStuckArrowRegistry()
    {
        StuckArrows.Clear();
    }

    public void Initialize(Transform projectileOwner, PlayerStats stats, Vector2 velocity,
        int attackDamage, float lifetime, LayerMask layers,
        StatusEffectType effect = StatusEffectType.Poison, float effectChance = 0f)
    {
        owner = projectileOwner;
        ownerStats = stats;
        damage = attackDamage;
        targetLayers = layers;
        statusEffect = effect;
        statusChance = effectChance;
        GetComponent<Rigidbody2D>().linearVelocity = velocity;
        StartCoroutine(DestroyAfterLifetime(lifetime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isStuck || other.transform.root == owner ||
            (targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Damageable damageable = other.GetComponentInParent<Damageable>();
        if (TryDamage(damageable, other.gameObject)) return;

        // The ground is a large trigger in the prototype scene. Only solid
        // non-damageable colliders (the walls) should catch an arrow.
        if (!other.isTrigger) StickInto(other.transform);
    }

    private void FixedUpdate()
    {
        if (isStuck) return;
        foreach (EnemyAI enemy in EnemyAI.ActiveEnemies)
        {
            if (enemy == null || Vector2.Distance(transform.position, enemy.transform.position) > 0.7f) continue;
            if (TryDamage(enemy.Health, enemy.gameObject)) return;
        }
    }

    private bool TryDamage(Damageable damageable, GameObject hitObject)
    {
        if (damageable == null || damageable.IsDead) return false;
        damage = CombatCalculator.ApplyTargetModifiers(hitObject, damage);
        damageable.TakeDamage(damage);
        PlayerAttackVfx.SpawnImpact(transform.position, GetComponent<SpriteRenderer>().color, 0.65f);
        if (!damageable.IsDead && statusChance > 0f)
            StatusEffectController.TryApply(hitObject, statusEffect, ownerStats, statusChance);
        if (damageable.IsDead) ownerStats.AddExperience(damageable.ExperienceReward);
        Destroy(gameObject);
        return true;
    }

    private void StickInto(Transform surface)
    {
        isStuck = true;
        PlayerAttackVfx.SpawnImpact(transform.position, GetComponent<SpriteRenderer>().color, 0.42f);
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;

        // Push the arrowhead slightly into the wall and follow moving walls.
        transform.position += transform.right * 0.12f;
        transform.SetParent(surface, true);

        while (StuckArrows.Count >= MaxStuckArrows)
        {
            ArrowProjectile oldest = StuckArrows.Dequeue();
            if (oldest != null) Destroy(oldest.gameObject);
        }
        StuckArrows.Enqueue(this);
    }

    private IEnumerator DestroyAfterLifetime(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        if (!isStuck) Destroy(gameObject);
    }
}
