using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Damageable))]
public class EnemyAI : MonoBehaviour
{
    private static readonly HashSet<EnemyAI> ActiveRegistry = new();
    public enum MonsterType { Slime, GoblinWarrior, GoblinArcher, GoblinMage, Skeleton, Berserker }
    private enum State { Idle, Chase, Attack, Return }

    [SerializeField] private MonsterType monsterType;
    [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
    [SerializeField, Min(0.1f)] private float detectionRange = 12f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.1f;
    [SerializeField, Min(1)] private int attackDamage = 10;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.2f;
    [SerializeField, Min(0f)] private float preferredDistance;
    [SerializeField, Min(1f)] private float leashRange = 14f;

    private Rigidbody2D body;
    private Damageable health;
    private Transform target;
    private Vector2 spawnPosition;
    private State state;
    private float nextAttackTime;
    private float slimeStep;
    private bool skeletonRevived;
    private bool permanentlyDefeated;
    private int finalExperienceReward;
    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private Coroutine hitFlashRoutine;
    private Room ownerRoom;
    private bool dropsEnabled = true;
    private Animator animator;
    private Vector2 lastFacingDirection = Vector2.down;

    public MonsterType Type => monsterType;
    public bool IsPermanentlyDefeated => permanentlyDefeated;
    public Damageable Health => health;
    public static EnemyAI[] ActiveEnemies
    {
        get
        {
            var snapshot = new EnemyAI[ActiveRegistry.Count];
            ActiveRegistry.CopyTo(snapshot);
            return snapshot;
        }
    }
    public event Action<EnemyAI> Defeated;
    public void SetDropsEnabled(bool value) => dropsEnabled = value;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Damageable>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        WorldHealthBar healthBar = GetComponent<WorldHealthBar>();
        if (healthBar == null) healthBar = gameObject.AddComponent<WorldHealthBar>();
        healthBar.Bind(health);
    }

    private void OnEnable()
    {
        ActiveRegistry.Add(this);
        if (health != null)
        {
            health.Died += OnDied;
            health.HealthChanged += OnHealthChanged;
        }
    }

    private void OnDisable()
    {
        ActiveRegistry.Remove(this);
        if (health != null)
        {
            health.Died -= OnDied;
            health.HealthChanged -= OnHealthChanged;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => ActiveRegistry.Clear();

    private void FixedUpdate()
    {
        if (health.IsDead) return;
        FindTarget();
        UpdateState();

        switch (state)
        {
            case State.Chase: MoveForCombat(); break;
            case State.Attack: StopAndAttack(); break;
            case State.Return: MoveTowards(spawnPosition, CurrentSpeed); break;
            default: body.linearVelocity = Vector2.zero; break;
        }
        UpdateDirectionalAnimation();
    }

    private void UpdateDirectionalAnimation()
    {
        if (animator == null || monsterType != MonsterType.Slime) return;
        Vector2 velocity = body.linearVelocity;
        if (velocity.sqrMagnitude > 0.01f) lastFacingDirection = velocity.normalized;

        int direction;
        if (Mathf.Abs(lastFacingDirection.y) > Mathf.Abs(lastFacingDirection.x))
            direction = lastFacingDirection.y > 0f ? 1 : 0; // Back : Front
        else
            direction = lastFacingDirection.x < 0f ? 3 : 2; // Left : Right
        animator.SetInteger("Direction", direction);
        // The dedicated Left sheet is authored facing screen-right, so mirror it
        // only while the slime is travelling left. Other directions stay intact.
        if (spriteRenderer != null) spriteRenderer.flipX = direction == 3;
    }

    public void Configure(MonsterType type, int healthPoints, int reward, float speed,
        float sight, float range, int damage, float cooldown, float keepDistance = 0f)
    {
        monsterType = type;
        moveSpeed = speed;
        detectionRange = Mathf.Max(12f, sight);
        attackRange = range;
        attackDamage = damage;
        attackCooldown = cooldown;
        preferredDistance = keepDistance;
        leashRange = Mathf.Max(14f, detectionRange + 1f);
        finalExperienceReward = reward;
        spawnPosition = transform.position;
        if (spriteRenderer != null) normalColor = spriteRenderer.color;
        health.Configure(healthPoints, type == MonsterType.Skeleton ? 0 : reward,
            false, type == MonsterType.Skeleton);
        ownerRoom = GetComponentInParent<Room>();
    }

    private void LateUpdate()
    {
        if (ownerRoom == null || health == null || health.IsDead) return;
        Vector2 clamped = ownerRoom.ClampToInterior(transform.position);
        if ((clamped - (Vector2)transform.position).sqrMagnitude < 0.0001f) return;
        transform.position = clamped;
        body.linearVelocity = Vector2.zero;
    }

    private float CurrentSpeed => monsterType == MonsterType.Berserker &&
        health.CurrentHealth <= health.MaxHealth * 0.4f ? moveSpeed * 1.65f : moveSpeed;

    private int CurrentDamage => monsterType == MonsterType.Berserker &&
        health.CurrentHealth <= health.MaxHealth * 0.4f ? Mathf.RoundToInt(attackDamage * 1.6f) : attackDamage;

    private void FindTarget()
    {
        if (target != null) return;
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        if (player != null && !player.IsDead) target = player.transform;
    }

    private void UpdateState()
    {
        float homeDistance = Vector2.Distance(transform.position, spawnPosition);
        if (homeDistance > leashRange) { state = State.Return; return; }
        if (state == State.Return && homeDistance > 0.15f) return;
        if (state == State.Return) { transform.position = spawnPosition; state = State.Idle; }
        if (target == null) { state = State.Idle; return; }

        float distance = Vector2.Distance(transform.position, target.position);
        if (preferredDistance > 0f && distance < preferredDistance) state = State.Chase;
        else if (distance <= attackRange) state = State.Attack;
        else if (distance <= detectionRange) state = State.Chase;
        else state = State.Idle;
    }

    private void MoveForCombat()
    {
        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;
        if (preferredDistance > 0f && distance < preferredDistance)
            body.linearVelocity = -toTarget.normalized * CurrentSpeed;
        else
            MoveTowards(target.position, CurrentSpeed);

        if (monsterType == MonsterType.Slime)
        {
            slimeStep += Time.fixedDeltaTime * 8f;
            body.linearVelocity *= Mathf.Lerp(0.35f, 1.35f, (Mathf.Sin(slimeStep) + 1f) * 0.5f);
        }
    }

    private void MoveTowards(Vector2 destination, float speed)
    {
        Vector2 direction = (destination - (Vector2)transform.position).normalized;
        body.linearVelocity = direction * speed;
        if (Mathf.Abs(direction.x) > 0.01f)
            transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y, transform.localScale.z);
    }

    private void StopAndAttack()
    {
        body.linearVelocity = Vector2.zero;
        if (target == null || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        if (monsterType is MonsterType.GoblinArcher or MonsterType.GoblinMage)
            FireProjectile();
        else
        {
            PlayerStats player = target.GetComponent<PlayerStats>();
            if (player != null) player.TakeDamage(CurrentDamage);
        }
    }

    private void FireProjectile()
    {
        Vector2 direction = (target.position - transform.position).normalized;
        bool magic = monsterType == MonsterType.GoblinMage;
        EnemyProjectile.Spawn(transform, direction, CurrentDamage, magic ? 5.5f : 7.5f,
            magic ? new Color(0.75f, 0.25f, 1f) : new Color(0.9f, 0.72f, 0.25f), magic);
    }

    private void OnDied()
    {
        body.linearVelocity = Vector2.zero;
        if (monsterType == MonsterType.Skeleton && !skeletonRevived)
        {
            skeletonRevived = true;
            Invoke(nameof(ReviveSkeleton), 2.5f);
            return;
        }
        permanentlyDefeated = true;
        if (dropsEnabled)
            GoldPickup.Spawn(transform.position, UnityEngine.Random.Range(1, 4) + (int)monsterType);
        health.SetDeactivateOnDeath(true);
        Defeated?.Invoke(this);
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (current <= 0 || spriteRenderer == null) return;
        if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = normalColor;
        hitFlashRoutine = null;
    }

    private void ReviveSkeleton()
    {
        if (this == null) return;
        gameObject.SetActive(true);
        health.SetExperienceReward(finalExperienceReward);
        health.ResetState();
    }
}
