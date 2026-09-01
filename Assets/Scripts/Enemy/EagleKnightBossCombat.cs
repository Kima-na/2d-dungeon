using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BossMovement), typeof(BossHealth), typeof(Rigidbody2D))]
public sealed class EagleKnightBossCombat : MonoBehaviour
{
    [Header("AI")]
    [SerializeField, Min(1f)] private float detectionRange = 8f;
    [SerializeField, Min(0.1f)] private float attackRange = 2f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.5f;
    [SerializeField, Min(0f)] private float introDelay = 1f;
    [Header("Spear Slash")]
    [SerializeField, Min(1)] private int slashDamage = 70;
    [SerializeField, Min(0f)] private float slashWarning = 0.45f;
    [Header("Charge")]
    [SerializeField, Min(1)] private int chargeDamage = 85;
    [SerializeField, Min(0.1f)] private float chargeSpeed = 8f;
    [SerializeField, Min(0f)] private float chargeWarning = 0.75f;
    [SerializeField, Min(0.1f)] private float chargeDuration = 0.55f;
    [Header("Spear Throw")]
    [SerializeField, Min(1)] private int throwDamage = 60;
    [SerializeField, Min(0.1f)] private float throwSpeed = 10f;
    [SerializeField, Min(0.1f)] private float throwCooldown = 4f;
    [SerializeField] private Sprite spearSprite;
    [Header("Eagle Descent")]
    [SerializeField, Min(1)] private int skillDamage = 110;
    [SerializeField, Min(0.1f)] private float skillRadius = 3.2f;
    [SerializeField, Min(0.1f)] private float skillCooldown = 9f;
    [SerializeField, Min(0f)] private float skillWarning = 1.1f;

    private BossMovement movement; private BossHealth health; private Rigidbody2D body;
    private EagleKnightAnimator visuals; private Collider2D slashHitbox, chargeHitbox, skillHitbox;
    private float nextAttack, nextThrow, nextSkill, activeAt;
    public bool IsAttacking { get; private set; }
    public bool CanMove => Time.time >= activeAt && movement != null && movement.DistanceToTarget <= detectionRange;

    private void Awake()
    {
        movement = GetComponent<BossMovement>(); health = GetComponent<BossHealth>(); body = GetComponent<Rigidbody2D>();
        visuals = GetComponent<EagleKnightAnimator>();
        slashHitbox = transform.Find("SlashHitbox")?.GetComponent<Collider2D>();
        chargeHitbox = transform.Find("ChargeHitbox")?.GetComponent<Collider2D>();
        skillHitbox = transform.Find("SkillHitbox")?.GetComponent<Collider2D>();
        activeAt = Time.time + introDelay;
    }
    public void Configure(float detection, float range, float cooldown, int slash, int charge, int spearThrow, int descent)
    { detectionRange = detection; attackRange = range; attackCooldown = cooldown; slashDamage = slash;
      chargeDamage = charge; throwDamage = spearThrow; skillDamage = descent; }

    private void Update()
    {
        if (health.IsDead || IsAttacking || !CanMove || movement.Target == null || Time.time < nextAttack) return;
        float distance = movement.DistanceToTarget;
        if (Time.time >= nextSkill && distance <= skillRadius + 1.5f) StartCoroutine(EagleDescent());
        else if (distance <= attackRange) StartCoroutine(SpearSlash());
        else if (distance <= 5f && Random.value < 0.45f) StartCoroutine(Charge());
        else if (Time.time >= nextThrow) StartCoroutine(ThrowSpear());
    }
    private IEnumerator SpearSlash()
    {
        Begin(); Vector2 direction = DirectionToPlayer(); visuals?.PlaySlash(direction);
        yield return new WaitForSeconds(slashWarning);
        if (!health.IsDead) { yield return Pulse(slashHitbox); DamageInFront(direction, attackRange + 0.45f, slashDamage); }
        yield return new WaitForSeconds(0.45f); End();
    }
    private IEnumerator Charge()
    {
        Begin(); Vector2 direction = DirectionToPlayer(); visuals?.PlayCharge(direction);
        BossAttackEffect.Spawn(RuntimeCombatSprites.Projectile, transform.position, new Vector2(3.2f, 0.35f),
            chargeWarning, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, new Color(1f, 0.65f, 0.12f, 0.55f));
        yield return new WaitForSeconds(chargeWarning);
        bool damaged = false; float elapsed = 0f; if (chargeHitbox != null) chargeHitbox.enabled = true;
        while (elapsed < chargeDuration && !health.IsDead)
        {
            elapsed += Time.fixedDeltaTime; body.MovePosition(body.position + direction * chargeSpeed * Time.fixedDeltaTime);
            if (!damaged && PlayerWithin(transform.position, 1.15f))
            { damaged = true; movement.Target.GetComponent<PlayerStats>()?.TakeDamage(chargeDamage); }
            yield return new WaitForFixedUpdate();
        }
        if (chargeHitbox != null) chargeHitbox.enabled = false;
        yield return new WaitForSeconds(0.5f); End();
    }
    private IEnumerator ThrowSpear()
    {
        Begin(); nextThrow = Time.time + throwCooldown; Vector2 direction = DirectionToPlayer(); visuals?.PlayThrow(direction);
        yield return new WaitForSeconds(0.55f);
        if (!health.IsDead) EagleKnightSpearProjectile.Spawn(transform, direction, throwDamage, throwSpeed, 3f,
            spearSprite != null ? spearSprite : Resources.Load<Sprite>("EagleKnight/Throw_04"));
        yield return new WaitForSeconds(0.45f); End();
    }
    private IEnumerator EagleDescent()
    {
        Begin(); nextSkill = Time.time + skillCooldown; visuals?.PlaySkill();
        BossAttackEffect.Spawn(RuntimeCombatSprites.Ring, transform.position, Vector2.one * skillRadius * 2f,
            skillWarning, 0f, new Color(1f, 0.72f, 0.15f, 0.7f));
        yield return new WaitForSeconds(skillWarning);
        if (!health.IsDead)
        {
            yield return Pulse(skillHitbox);
            if (PlayerWithin(transform.position, skillRadius)) movement.Target.GetComponent<PlayerStats>()?.TakeDamage(skillDamage);
            BossAttackEffect.Spawn(RuntimeCombatSprites.Ring, transform.position, Vector2.one * skillRadius * 2.2f, 0.45f);
        }
        yield return new WaitForSeconds(0.65f); End();
    }
    private void Begin() { IsAttacking = true; body.linearVelocity = Vector2.zero; }
    private void End() { IsAttacking = false; nextAttack = Time.time + attackCooldown; visuals?.EndAction(); }
    private Vector2 DirectionToPlayer() => movement.Target == null ? Vector2.right :
        ((Vector2)movement.Target.position - (Vector2)transform.position).normalized;
    private bool PlayerWithin(Vector2 center, float radius) => movement.Target != null &&
        Vector2.Distance(center, movement.Target.position) <= radius;
    private void DamageInFront(Vector2 direction, float range, int damage)
    {
        if (movement.Target == null) return; Vector2 offset = (Vector2)movement.Target.position - (Vector2)transform.position;
        if (offset.magnitude <= range && Vector2.Dot(direction, offset.normalized) >= 0.05f)
            movement.Target.GetComponent<PlayerStats>()?.TakeDamage(damage);
    }
    private static IEnumerator Pulse(Collider2D collider)
    { if (collider == null) yield break; collider.enabled = true; yield return new WaitForFixedUpdate(); if (collider != null) collider.enabled = false; }
    public void GrantRewards()
    {
        for (int i = 0; i < 6; i++) GoldPickup.Spawn((Vector2)transform.position + Random.insideUnitCircle, 12);
    }
    private void OnDisable()
    {
        StopAllCoroutines(); if (slashHitbox != null) slashHitbox.enabled = false;
        if (chargeHitbox != null) chargeHitbox.enabled = false; if (skillHitbox != null) skillHitbox.enabled = false;
        IsAttacking = false;
    }
}
