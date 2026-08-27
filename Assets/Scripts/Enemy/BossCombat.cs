using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BossMovement), typeof(BossHealth))]
public sealed class BossCombat : MonoBehaviour
{
    public enum SkillType { DarkShockwave = 1, Hellfall = 2, SpinningSlash = 3, DarkSummon = 4 }

    [Header("General")]
    [SerializeField] private bool automaticAttacks = true;
    [SerializeField, Min(0.1f)] private float globalAttackCooldown = 0.8f;
    [SerializeField, Min(0f)] private float attackRecovery = 0.35f;
    [Header("Skill 1 - Dark Shockwave")]
    [SerializeField, Min(1)] private int skill1Damage = 32;
    [SerializeField, Min(0.1f)] private float skill1Cooldown = 4f;
    [SerializeField, Min(0.1f)] private float skill1Range = 8f;
    [SerializeField, Min(0f)] private float skill1Warning = 0.55f;
    [SerializeField, Min(0.1f)] private float skill1ProjectileSpeed = 8f;
    [Header("Skill 2 - Hellfall")]
    [SerializeField, Min(1)] private int skill2Damage = 38;
    [SerializeField, Min(0.1f)] private float skill2Cooldown = 6f;
    [SerializeField, Min(0.1f)] private float skill2Area = 2.1f;
    [SerializeField, Min(0f)] private float skill2Warning = 0.85f;
    [Header("Skill 3 - Spinning Slash")]
    [SerializeField, Min(1)] private int skill3Damage = 12;
    [SerializeField, Min(0.1f)] private float skill3Cooldown = 5f;
    [SerializeField, Min(0.1f)] private float skill3Radius = 2.2f;
    [SerializeField, Min(0.1f)] private float skill3Duration = 1.1f;
    [SerializeField, Min(0.1f)] private float skill3HitCooldown = 0.25f;
    [Header("Skill 4 - Dark Summon")]
    [SerializeField, Min(1)] private int skill4Damage = 10;
    [SerializeField, Min(0.1f)] private float skill4Cooldown = 8f;
    [SerializeField, Range(1, 6)] private int skill4SummonCount = 3;
    [SerializeField, Min(1f)] private float skill4SummonLifetime = 8f;
    [SerializeField, Min(0f)] private float skill4Warning = 0.7f;

    private readonly float[] readyTimes = new float[4];
    private BossMovement movement;
    private BossHealth health;
    private BossAnimator bossAnimator;
    private BossVisualDatabase visuals;
    private float nextGlobalAttack;
    private int lastSkill = -1;
    private readonly CircleCollider2D[] skillHitboxes = new CircleCollider2D[4];
    public bool IsAttacking { get; private set; }
    public SkillType? CurrentSkill { get; private set; }

    private void Awake()
    {
        movement = GetComponent<BossMovement>();
        health = GetComponent<BossHealth>();
        bossAnimator = GetComponent<BossAnimator>();
        visuals = Resources.Load<BossVisualDatabase>("BossVisualDatabase");
        for (int i = 0; i < skillHitboxes.Length; i++)
            skillHitboxes[i] = transform.Find($"Skill{i + 1}Hitbox")?.GetComponent<CircleCollider2D>();
    }

    private void Update()
    {
        if (!automaticAttacks || health.IsDead || IsAttacking || movement.Target == null || Time.time < nextGlobalAttack) return;
        int selected = SelectSkill(movement.DistanceToTarget);
        if (selected >= 0) StartCoroutine(ExecuteSkill((SkillType)(selected + 1)));
    }

    private int SelectSkill(float distance)
    {
        List<int> available = new();
        for (int i = 0; i < readyTimes.Length; i++)
        {
            if (i == lastSkill || Time.time < readyTimes[i]) continue;
            bool usable = i switch
            {
                0 => distance <= skill1Range,
                1 => distance <= skill1Range,
                2 => distance <= skill3Radius + 0.6f,
                _ => distance <= skill1Range
            };
            if (usable) available.Add(i);
        }
        return available.Count == 0 ? -1 : available[Random.Range(0, available.Count)];
    }

    public bool TryUseSkill(SkillType skill)
    {
        int index = (int)skill - 1;
        if (health.IsDead || IsAttacking || movement.Target == null || Time.time < readyTimes[index]) return false;
        StartCoroutine(ExecuteSkill(skill));
        return true;
    }

    public void SetAutomaticAttacks(bool value)
    {
        automaticAttacks = value;
        if (value || !IsAttacking) return;
        StopAllCoroutines();
        IsAttacking = false;
        CurrentSkill = null;
        bossAnimator?.EndAttack();
    }

    private IEnumerator ExecuteSkill(SkillType skill)
    {
        IsAttacking = true;
        CurrentSkill = skill;
        lastSkill = (int)skill - 1;
        readyTimes[lastSkill] = Time.time + GetCooldown(skill);
        nextGlobalAttack = Time.time + globalAttackCooldown;
        Vector2 direction = ((Vector2)movement.Target.position - (Vector2)transform.position).normalized;
        bossAnimator?.PlayAttack((int)skill, direction);
        IEnumerator routine = skill switch
        {
            SkillType.DarkShockwave => DarkShockwave(direction),
            SkillType.Hellfall => Hellfall(),
            SkillType.SpinningSlash => SpinningSlash(),
            _ => DarkSummon()
        };
        yield return StartCoroutine(routine);
        if (!health.IsDead)
        {
            yield return new WaitForSeconds(attackRecovery);
            bossAnimator?.EndAttack();
        }
        CurrentSkill = null;
        IsAttacking = false;
    }

    private IEnumerator DarkShockwave(Vector2 direction)
    {
        BossAttackEffect.Spawn(visuals != null ? visuals.darkShockwave : null,
            transform.position + (Vector3)(direction * 0.9f), new Vector2(1.2f, 0.45f), skill1Warning,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, new Color(0.7f, 0.15f, 1f, 0.55f));
        yield return new WaitForSeconds(skill1Warning);
        if (!health.IsDead)
        {
            BossSkillProjectile.Spawn(transform, direction, skill1Damage,
                skill1ProjectileSpeed, skill1Range / skill1ProjectileSpeed,
                visuals != null ? visuals.darkShockwave : null);
        }
    }

    private IEnumerator Hellfall()
    {
        Vector2 targetPosition = movement.Target.position;
        BossAttackEffect warning = BossAttackEffect.Spawn(visuals != null ? visuals.groundWarning : null,
            targetPosition, Vector2.one * (skill2Area * 2f), skill2Warning, 0f,
            new Color(1f, 0.12f, 0.35f, 0.7f));
        Vector2 start = transform.position;
        float elapsed = 0f;
        while (elapsed < skill2Warning)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / skill2Warning);
            transform.position = Vector2.Lerp(start, targetPosition, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 1.5f;
            yield return null;
        }
        transform.position = targetPosition;
        if (warning != null) Destroy(warning.gameObject);
        BossAttackEffect.Spawn(visuals != null ? visuals.groundImpact : null,
            targetPosition, Vector2.one * (skill2Area * 2.2f), 0.5f);
        StartCoroutine(PulseHitbox(1));
        DamagePlayerInRadius(targetPosition, skill2Area, skill2Damage);
    }

    private IEnumerator SpinningSlash()
    {
        yield return new WaitForSeconds(0.4f);
        BossAttackEffect.Spawn(visuals != null ? visuals.spinSlash : null,
            transform.position, Vector2.one * (skill3Radius * 2.3f), skill3Duration, 0f, Color.white, transform);
        float elapsed = 0f;
        float nextHit = 0f;
        while (elapsed < skill3Duration && !health.IsDead)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= nextHit)
            {
                nextHit = elapsed + skill3HitCooldown;
                StartCoroutine(PulseHitbox(2));
                DamagePlayerInRadius(transform.position, skill3Radius, skill3Damage);
            }
            yield return null;
        }
    }

    private IEnumerator DarkSummon()
    {
        BossAttackEffect.Spawn(visuals != null ? visuals.summonCircle : null,
            transform.position, Vector2.one * 2.5f, skill4Warning + 0.4f);
        yield return new WaitForSeconds(skill4Warning);
        StartCoroutine(PulseHitbox(3));
        for (int i = 0; i < skill4SummonCount; i++)
        {
            float angle = Mathf.PI * 2f * i / skill4SummonCount;
            Vector2 position = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.5f;
            EnemyAI summon = MonsterRoster.Spawn(EnemyAI.MonsterType.GoblinWarrior,
                transform.parent, position, visuals != null ? visuals.shadowMinion : null);
            summon.name = "Summoned Shadow";
            summon.SetDropsEnabled(false);
            summon.Configure(EnemyAI.MonsterType.GoblinWarrior, 24, 0, 2.8f, 10f, 0.8f,
                skill4Damage, 0.9f);
            summon.gameObject.AddComponent<BossSummonLifetime>().SetLifetime(skill4SummonLifetime);
        }
    }

    private void DamagePlayerInRadius(Vector2 center, float radius, int damage)
    {
        if (movement.Target == null) return;
        PlayerStats player = movement.Target.GetComponent<PlayerStats>();
        if (player != null && !player.IsDead && Vector2.Distance(center, player.transform.position) <= radius)
            player.TakeDamage(damage);
    }

    private float GetCooldown(SkillType skill) => skill switch
    {
        SkillType.DarkShockwave => skill1Cooldown,
        SkillType.Hellfall => skill2Cooldown,
        SkillType.SpinningSlash => skill3Cooldown,
        _ => skill4Cooldown
    };

    private IEnumerator PulseHitbox(int index)
    {
        CircleCollider2D hitbox = skillHitboxes[index];
        if (hitbox == null) yield break;
        hitbox.enabled = true;
        yield return new WaitForFixedUpdate();
        if (hitbox != null) hitbox.enabled = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        foreach (CircleCollider2D hitbox in skillHitboxes)
            if (hitbox != null) hitbox.enabled = false;
        IsAttacking = false;
        CurrentSkill = null;
        bossAnimator?.EndAttack();
    }
}
