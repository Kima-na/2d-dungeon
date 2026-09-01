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
    [SerializeField, Range(0f, 1f)] private float equipmentDropChance = 0.05f;

    private Rigidbody2D body;
    private Damageable health;
    private Transform target;
    private Vector2 spawnPosition;
    private State state;
    private float nextAttackTime;
    private float slimeStep;
    private bool skeletonRevived;
    private Vector3 skeletonDeathPosition;
    private bool permanentlyDefeated;
    private int finalExperienceReward;
    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private Coroutine hitFlashRoutine;
    private Room ownerRoom;
    private bool dropsEnabled = true;
    private Animator animator;
    private SkeletonVisualAnimator skeletonVisual;
    private BerserkerVisualAnimator berserkerVisual;
    private Vector2 lastFacingDirection = Vector2.down;
    private int slimeDivisionGeneration;
    private DifficultyModifiers slimeDifficulty;
    private float visualActionUntil;
    private int visualAction;
    private Coroutine attackRoutine;
    private Coroutine deathRoutine;
    private int lastKnownHealth;

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
    public event Action<EnemyAI> OffspringSpawned;
    public void SetDropsEnabled(bool value) => dropsEnabled = value;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Damageable>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        skeletonVisual = GetComponent<SkeletonVisualAnimator>();
        berserkerVisual = GetComponent<BerserkerVisualAnimator>();
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
        if (skeletonVisual != null && skeletonVisual.IsReviving)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        FindTarget();
        UpdateState();

        switch (state)
        {
            case State.Chase: MoveForCombat(); break;
            case State.Attack: StopAndAttack(); break;
            case State.Return: MoveTowards(spawnPosition, CurrentSpeed); break;
            default: body.linearVelocity = Vector2.zero; break;
        }
        UpdateVisualAnimation();
    }

    private void UpdateVisualAnimation()
    {
        if (monsterType == MonsterType.Skeleton && skeletonVisual != null)
        {
            Vector2 skeletonFacing = target != null && state != State.Return
                ? (Vector2)target.position - (Vector2)transform.position : body.linearVelocity;
            skeletonVisual.SetMovement(skeletonFacing, body.linearVelocity.sqrMagnitude > 0.01f);
            return;
        }
        if (monsterType == MonsterType.Berserker && berserkerVisual != null)
        {
            Vector2 berserkerFacing = target != null && state != State.Return
                ? (Vector2)target.position - (Vector2)transform.position : body.linearVelocity;
            berserkerVisual.SetMovement(berserkerFacing, body.linearVelocity.sqrMagnitude > 0.01f);
            return;
        }
        if (!HasAnimator || (monsterType != MonsterType.Slime && !IsGoblinFamily)) return;
        Vector2 velocity = body.linearVelocity;
        // Combatants keep their eyes on the player, including ranged enemies
        // that are backing away. Returning enemies face their actual movement.
        Vector2 facing = target != null && state != State.Return
            ? (Vector2)target.position - (Vector2)transform.position : velocity;
        if (facing.sqrMagnitude > 0.01f) lastFacingDirection = facing.normalized;

        int direction;
        if (Mathf.Abs(lastFacingDirection.y) > Mathf.Abs(lastFacingDirection.x))
            direction = lastFacingDirection.y > 0f ? 1 : 0; // Back : Front
        else
            direction = lastFacingDirection.x < 0f ? 3 : 2; // Left : Right
        animator.SetInteger("Direction", direction);
        if (IsGoblinFamily)
        {
            if (Time.time >= visualActionUntil) visualAction = velocity.sqrMagnitude > 0.01f ? 1 : 0;
            animator.SetInteger("Action", visualAction);
        }
        // Goblins have authored four-way frames. Slime shares one side animation
        // for left/right so its left state is mirrored without changing art style.
        if (spriteRenderer != null)
            spriteRenderer.flipX = monsterType == MonsterType.Slime && direction == 3;
    }

    private bool IsGoblinFamily => monsterType is MonsterType.GoblinWarrior or
        MonsterType.GoblinArcher or MonsterType.GoblinMage;

    private bool HasAnimator
    {
        get
        {
            if (animator == null) animator = GetComponent<Animator>();
            return animator != null;
        }
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
        float shadowSize = type == MonsterType.Slime ? 0.86f : IsGoblinFamily ? 0.88f :
            type == MonsterType.Berserker ? 1.12f : 0.94f;
        // Goblin sheets use a grounded custom pivot. Keep all three classes on
        // the same tight offset so the feet overlap the ellipse instead of
        // appearing to float above it.
        float shadowOffset = type == MonsterType.Slime ? -0.27f :
            IsGoblinFamily ? -0.025f : -0.38f;
        WorldShadow.Ensure(transform,
            spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : -1,
            IsGoblinFamily ? 0.82f : shadowSize, shadowOffset);
    }

    public void ConfigureSlimeDivision(int generation, DifficultyModifiers modifiers)
    {
        slimeDivisionGeneration = Mathf.Max(0, generation);
        slimeDifficulty = modifiers;
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
        if (Mathf.Abs(direction.x) > 0.01f && !HasAnimator && skeletonVisual == null && berserkerVisual == null)
            transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y, transform.localScale.z);
    }

    private void StopAndAttack()
    {
        body.linearVelocity = Vector2.zero;
        if (target == null || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        if (attackRoutine == null) attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        Vector2 direction = target != null ? ((Vector2)target.position - (Vector2)transform.position).normalized : lastFacingDirection;
        if (direction.sqrMagnitude > 0.01f) lastFacingDirection = direction;
        if (monsterType == MonsterType.Skeleton) skeletonVisual?.PlayAttack(direction);
        if (monsterType == MonsterType.Berserker) berserkerVisual?.PlayAttack(direction);
        SetVisualAction(2, 0.36f);
        yield return new WaitForSeconds(monsterType is MonsterType.GoblinArcher or MonsterType.GoblinMage ? 0.22f : 0.12f);
        if (!health.IsDead && target != null)
        {
            if (monsterType is MonsterType.GoblinArcher or MonsterType.GoblinMage) FireProjectile();
            else
            {
                PlayerStats player = target.GetComponent<PlayerStats>();
                if (player != null) player.TakeDamage(CurrentDamage);
            }
        }
        yield return new WaitForSeconds(0.14f);
        attackRoutine = null;
    }

    private void SetVisualAction(int action, float duration)
    {
        if (!IsGoblinFamily || !HasAnimator) return;
        // Exactly one visual state owns the SpriteRenderer. Higher actions
        // have priority: Death > Hit > Attack > Walk > Idle.
        if (visualAction == 4) return;
        if (Time.time < visualActionUntil && visualAction > action) return;
        visualAction = action;
        visualActionUntil = Mathf.Max(visualActionUntil, Time.time + duration);
        animator.SetInteger("Action", visualAction);
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
            skeletonVisual?.PlayDeath();
            skeletonRevived = true;
            skeletonDeathPosition = transform.position;
            Invoke(nameof(ReviveSkeleton), 1f);
            return;
        }
        if (monsterType == MonsterType.Slime && slimeDivisionGeneration < 3)
            SpawnSlimeOffspring();
        permanentlyDefeated = true;
        if (dropsEnabled)
        {
            GoldPickup.Spawn(transform.position, UnityEngine.Random.Range(1, 4) + (int)monsterType);
            if (UnityEngine.Random.value < equipmentDropChance)
                EquipmentPickup.Spawn((Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 0.45f);
        }
        if (IsGoblinFamily)
        {
            health.SetDeactivateOnDeath(false);
            SetVisualAction(4, 10f);
            foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) hitbox.enabled = false;
            deathRoutine = StartCoroutine(GoblinDeathRoutine());
        }
        else health.SetDeactivateOnDeath(true);
        Defeated?.Invoke(this);
    }

    private IEnumerator GoblinDeathRoutine()
    {
        yield return new WaitForSeconds(0.65f);
        deathRoutine = null;
        gameObject.SetActive(false);
    }

    private void SpawnSlimeOffspring()
    {
        Vector2 center = transform.position;
        Vector2 side = UnityEngine.Random.value < 0.5f ? Vector2.right : Vector2.up;
        for (int i = 0; i < 2; i++)
        {
            float sign = i == 0 ? -1f : 1f;
            Vector2 position = center + side * sign * 0.38f;
            EnemyAI child = MonsterRoster.SpawnSlimeDivision(transform.parent, position,
                slimeDivisionGeneration + 1, slimeDifficulty);
            child.SetDropsEnabled(dropsEnabled);
            OffspringSpawned?.Invoke(child);
        }
    }

    private void OnHealthChanged(int current, int maximum)
    {
        bool tookDamage = current > 0 && current < maximum &&
            (lastKnownHealth <= 0 || current < lastKnownHealth);
        lastKnownHealth = current;
        if (!tookDamage || spriteRenderer == null) return;
        if (monsterType == MonsterType.Skeleton) skeletonVisual?.PlayHit();
        if (monsterType == MonsterType.Berserker) berserkerVisual?.PlayHit();
        SetVisualAction(3, 0.14f);
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
        transform.position = skeletonDeathPosition;
        body.position = skeletonDeathPosition;
        body.linearVelocity = Vector2.zero;
        skeletonVisual?.PlayRevive();
    }
}
