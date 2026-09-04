using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BossHealth))]
public sealed class BossMovement : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.8f;
    [SerializeField, Min(0.1f)] private float stoppingDistance = 1.45f;

    private Rigidbody2D body;
    private BossHealth health;
    private BossCombat combat;
    private NightmareBossCombat nightmareCombat;
    private EagleKnightBossCombat eagleKnightCombat;
    private AncientGolemCombat ancientGolemCombat;
    private AncientGolemAnimator ancientGolemAnimator;
    private EagleKnightAnimator eagleKnightAnimator;
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
        nightmareCombat = GetComponent<NightmareBossCombat>();
        eagleKnightCombat = GetComponent<EagleKnightBossCombat>();
        ancientGolemCombat = GetComponent<AncientGolemCombat>();
        ancientGolemAnimator = GetComponent<AncientGolemAnimator>();
        eagleKnightAnimator = GetComponent<EagleKnightAnimator>();
        bossAnimator = GetComponent<BossAnimator>();
        ownerRoom = GetComponentInParent<Room>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.useFullKinematicContacts = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

private void FixedUpdate()
    {
        if (health.IsDead) { Stop(); return; }
        FindTarget();

        IgnorePlayerCollision();

        Vector2 direction = target == null ? Vector2.zero : ((Vector2)target.position - body.position).normalized;

        if (target == null || DistanceToTarget <= stoppingDistance ||
            (combat != null && combat.IsAttacking) ||
            (nightmareCombat != null && nightmareCombat.IsAttacking) ||
            (ancientGolemCombat != null && ancientGolemCombat.IsAttacking) ||
            (eagleKnightCombat != null && (!eagleKnightCombat.CanMove || eagleKnightCombat.IsAttacking)))
        {
            Stop();
            return;
        }

        body.linearVelocity = direction * moveSpeed;
        bossAnimator?.SetMovement(direction, true);
        eagleKnightAnimator?.SetMovement(direction, true);
        ancientGolemAnimator?.SetMovement(direction, true);
    }

    private void LateUpdate()
    {
        if (health == null || health.IsDead) return;
        if (ownerRoom != null)
        {
            Vector2 clamped = ownerRoom.ClampToInterior(transform.position, 1f);
            if ((clamped - (Vector2)transform.position).sqrMagnitude >= 0.0001f)
            {
                body.position = clamped;
                body.linearVelocity = Vector2.zero;
            }
        }
        IgnorePlayerCollision();
        ResolvePlayerOverlap();
    }

    private void IgnorePlayerCollision()
    {
        if (target == null) return;

        Collider2D playerCollider = target.GetComponent<Collider2D>();
        Collider2D bossCollider = GetComponent<Collider2D>();
        if (playerCollider == null || bossCollider == null ||
            !playerCollider.enabled || !bossCollider.enabled) return;

        Physics2D.IgnoreCollision(playerCollider, bossCollider, true);
    }

    private void ResolvePlayerOverlap()
    {
        if (target == null) FindTarget();
        if (target == null) return;

        PlayerStats player = target.GetComponent<PlayerStats>();
        Rigidbody2D playerBody = target.GetComponent<Rigidbody2D>();
        Collider2D playerCollider = target.GetComponent<Collider2D>();
        Collider2D bossCollider = GetComponent<Collider2D>();
        if (player == null || player.IsDead || playerBody == null ||
            playerCollider == null || bossCollider == null ||
            !playerCollider.enabled || !bossCollider.enabled) return;

        Bounds bossBounds = bossCollider.bounds;
        Bounds playerBounds = playerCollider.bounds;
        Vector2 delta = playerBounds.center - bossBounds.center;
        float overlapX = bossBounds.extents.x + playerBounds.extents.x - Mathf.Abs(delta.x);
        float overlapY = bossBounds.extents.y + playerBounds.extents.y - Mathf.Abs(delta.y);
        if (overlapX <= 0f || overlapY <= 0f) return;

        Vector2 separation;
        if (overlapX <= overlapY)
        {
            float sign = Mathf.Abs(delta.x) > 0.001f
                ? Mathf.Sign(delta.x)
                : Mathf.Sign(playerBody.position.x - body.position.x);
            if (Mathf.Abs(sign) < 0.5f) sign = 1f;
            separation = Vector2.right * sign * (overlapX + 0.04f);
        }
        else
        {
            float sign = Mathf.Abs(delta.y) > 0.001f
                ? Mathf.Sign(delta.y)
                : Mathf.Sign(playerBody.position.y - body.position.y);
            if (Mathf.Abs(sign) < 0.5f) sign = 1f;
            separation = Vector2.up * sign * (overlapY + 0.04f);
        }

        Vector2 originalPosition = playerBody.position;
        Vector2 nextPosition = originalPosition + separation;
        if (ownerRoom != null) nextPosition = ownerRoom.ClampToInterior(nextPosition, 0.1f);
        playerBody.position = nextPosition;
        Physics2D.SyncTransforms();

        if (playerCollider.bounds.Intersects(bossCollider.bounds))
        {
            Vector2 alternatePosition = originalPosition - separation;
            if (ownerRoom != null) alternatePosition = ownerRoom.ClampToInterior(alternatePosition, 0.1f);
            playerBody.position = alternatePosition;
            Physics2D.SyncTransforms();

            if (playerCollider.bounds.Intersects(bossCollider.bounds))
            {
                playerBody.position = originalPosition;
                Physics2D.SyncTransforms();
            }
        }

        playerBody.linearVelocity = Vector2.zero;
    }

    private void Stop()
    {
        body.linearVelocity = Vector2.zero;
        bossAnimator?.SetMovement(Vector2.zero, false);
        eagleKnightAnimator?.SetMovement(Vector2.zero, false);
        ancientGolemAnimator?.SetMovement(Vector2.zero, false);
    }

    private void FindTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy) return;
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        target = player != null && !player.IsDead ? player.transform : null;
    }
}
