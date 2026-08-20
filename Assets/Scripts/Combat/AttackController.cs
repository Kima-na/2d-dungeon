using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
public class AttackController : MonoBehaviour
{
    [SerializeField, Min(0)] private int attackDamage = 10;
    [SerializeField, Min(0.1f)] private float attackRange = 1.5f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask targetLayers = ~0;

    private PlayerController controller;
    private PlayerStats stats;
    private float nextAttackTime;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (!stats.IsDead && Time.time >= nextAttackTime && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Attack();
        }
    }

    public void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;
        Vector2 center = (Vector2)transform.position + controller.LastMoveDirection * (attackRange * 0.5f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRange * 0.5f, targetLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageable damageable && !damageable.IsDead)
                {
                    damageable.TakeDamage(attackDamage);
                    break;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 direction = Application.isPlaying && controller != null ? controller.LastMoveDirection : Vector2.right;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + direction * (attackRange * 0.5f), attackRange * 0.5f);
    }
}
