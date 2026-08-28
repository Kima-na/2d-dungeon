using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BossHealth))]
public sealed class BossMovement : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.8f;
    [SerializeField, Min(0.1f)] private float stoppingDistance = 1.45f;

    private Rigidbody2D body;
    private BossHealth health;
    private BossCombat combat;
    private BossAnimator bossAnimator;
    private Transform target;
    private Room ownerRoom;

    public Transform Target
    {
        get
        {
            FindTarget();
            return target;
        }
    }
    public float DistanceToTarget => target == null ? float.PositiveInfinity :
        Vector2.Distance(transform.position, target.position);

    public void ConfigureSpeed(float speed) => moveSpeed = Mathf.Max(0.1f, speed);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<BossHealth>();
        combat = GetComponent<BossCombat>();
        bossAnimator = GetComponent<BossAnimator>();
        ownerRoom = GetComponentInParent<Room>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        if (health.IsDead) { Stop(); return; }
        FindTarget();
        if (target == null || DistanceToTarget <= stoppingDistance ||
            (combat != null && combat.IsAttacking))
        {
            Stop();
            return;
        }

        Vector2 direction = ((Vector2)target.position - body.position).normalized;
        body.linearVelocity = direction * moveSpeed;
        bossAnimator?.SetMovement(direction, true);
    }

    private void LateUpdate()
    {
        if (ownerRoom == null || health.IsDead) return;
        Vector2 clamped = ownerRoom.ClampToInterior(transform.position, 1f);
        if ((clamped - (Vector2)transform.position).sqrMagnitude < 0.0001f) return;
        body.position = clamped;
        body.linearVelocity = Vector2.zero;
    }

    private void Stop()
    {
        body.linearVelocity = Vector2.zero;
        bossAnimator?.SetMovement(Vector2.zero, false);
    }

    private void FindTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy) return;
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        target = player != null && !player.IsDead ? player.transform : null;
    }
}
