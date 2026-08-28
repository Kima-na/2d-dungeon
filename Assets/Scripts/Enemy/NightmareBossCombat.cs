using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BossMovement), typeof(BossHealth))]
public sealed class NightmareBossCombat : MonoBehaviour
{
    private BossMovement movement;
    private BossHealth health;
    private bool automaticAttacks = true;
    private float nextAttackTime;
    private int phase = 1;
    private int lastSkill = -1;

    public bool IsAttacking { get; private set; }
    public int Phase => phase;
    public string CurrentSkillName { get; private set; }

    private void Awake()
    {
        movement = GetComponent<BossMovement>();
        health = GetComponent<BossHealth>();
    }

    private void Update()
    {
        if (!automaticAttacks || IsAttacking || health.IsDead || movement.Target == null ||
            Time.time < nextAttackTime || movement.DistanceToTarget > 10f) return;
        int skillCount = phase == 1 ? 3 : phase == 2 ? 4 : 5;
        int selected;
        do selected = Random.Range(0, skillCount); while (skillCount > 1 && selected == lastSkill);
        lastSkill = selected;
        StartCoroutine(ExecuteSkill(selected));
    }

    public void SetPhase(int value)
    {
        phase = Mathf.Clamp(value, 1, 3);
        nextAttackTime = Mathf.Min(nextAttackTime, Time.time + 0.4f);
    }

    public void SetAutomaticAttacks(bool value)
    {
        automaticAttacks = value;
        if (value || !IsAttacking) return;
        StopAllCoroutines();
        IsAttacking = false;
        CurrentSkillName = null;
    }

    private IEnumerator ExecuteSkill(int skill)
    {
        IsAttacking = true;
        CurrentSkillName = skill switch
        {
            0 => "혼돈 구체", 1 => "영혼 소환", 2 => "그림자 이동",
            3 => "혼돈의 폭풍", _ => "심판의 낙하"
        };
        yield return skill switch
        {
            0 => ChaosOrbs(), 1 => SoulSummon(), 2 => ShadowStep(),
            3 => ChaosStorm(), _ => JudgmentFall()
        };
        nextAttackTime = Time.time + (phase switch { 1 => 1.35f, 2 => 0.95f, _ => 0.62f });
        CurrentSkillName = null;
        IsAttacking = false;
    }

    private IEnumerator ChaosOrbs()
    {
        Transform target = movement.Target;
        Vector2 aim = ((Vector2)target.position - (Vector2)transform.position).normalized;
        Color color = phase == 1 ? new Color(0.75f, 0.05f, 0.18f) :
            phase == 2 ? new Color(0.45f, 0.1f, 1f) : new Color(1f, 0.02f, 0.08f);
        BossAttackEffect.Spawn(null, transform.position, Vector2.one * 1.8f, 0.45f, 0f, color);
        yield return new WaitForSeconds(0.45f);
        int count = phase == 1 ? 3 : phase == 2 ? 5 : 7;
        for (int i = 0; i < count; i++)
        {
            float angle = (i - (count - 1) * 0.5f) * 9f;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * aim;
            NightmareBossProjectile.Spawn(transform, target, direction, 30 + phase * 13,
                5.2f + phase, phase == 3 ? 2.8f : 1.4f, color);
        }
    }

    private IEnumerator SoulSummon()
    {
        Color color = phase == 1 ? new Color(0.45f, 0f, 0.12f, 0.8f) :
            new Color(0.25f, 0.05f, 0.65f, 0.8f);
        BossAttackEffect.Spawn(null, transform.position, Vector2.one * 3f, 0.65f, 0f, color);
        yield return new WaitForSeconds(0.65f);
        int count = phase == 1 ? 2 : phase == 2 ? 3 : 4;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * 2f * i / count;
            Vector2 position = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.7f;
            EnemyAI soul = MonsterRoster.Spawn(EnemyAI.MonsterType.Skeleton, transform.parent, position,
                MonsterRoster.PlaceholderSprite);
            soul.name = "Nightmare Soul";
            soul.SetDropsEnabled(false);
            SpriteRenderer renderer = soul.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = color;
            soul.gameObject.AddComponent<BossSummonLifetime>().SetLifetime(6f + phase * 2f);
        }
    }

    private IEnumerator ShadowStep()
    {
        Transform target = movement.Target;
        Vector2 start = transform.position;
        BossAttackEffect.Spawn(null, start, Vector2.one * 2f, 0.38f, 0f,
            new Color(0.3f, 0f, 0.55f, 0.85f));
        yield return new WaitForSeconds(0.28f);
        Vector2 offset = Random.insideUnitCircle.normalized * 2.2f;
        transform.position = (Vector2)target.position + offset;
        BossAttackEffect.Spawn(null, transform.position, Vector2.one * 3f, 0.4f, 0f,
            new Color(0.8f, 0.05f, 0.2f, 0.8f));
        DamagePlayerInRadius(transform.position, 1.55f, 38 + phase * 14);
    }

    private IEnumerator ChaosStorm()
    {
        Color color = phase == 2 ? new Color(0.35f, 0.08f, 1f) : new Color(1f, 0.02f, 0.1f);
        BossAttackEffect.Spawn(null, transform.position, Vector2.one * 4.5f, 0.7f, 0f, color);
        yield return new WaitForSeconds(0.55f);
        int count = phase == 2 ? 10 : 16;
        for (int i = 0; i < count; i++)
        {
            float angle = 360f * i / count;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            NightmareBossProjectile.Spawn(transform, movement.Target, direction, 35 + phase * 12,
                5.5f, phase == 3 ? 0.8f : 0f, color, 4.5f);
        }
    }

    private IEnumerator JudgmentFall()
    {
        Transform target = movement.Target;
        int strikes = 3;
        for (int i = 0; i < strikes; i++)
        {
            Vector2 position = target.position;
            BossAttackEffect warning = BossAttackEffect.Spawn(null, position, Vector2.one * 2.8f,
                0.55f, 0f, new Color(1f, 0f, 0.08f, 0.7f));
            yield return new WaitForSeconds(0.5f);
            if (warning != null) Destroy(warning.gameObject);
            BossAttackEffect.Spawn(null, position, Vector2.one * 3.2f, 0.35f, 0f,
                new Color(1f, 0.12f, 0.02f, 0.9f));
            DamagePlayerInRadius(position, 1.45f, 95);
            yield return new WaitForSeconds(0.14f);
        }
    }

    private void DamagePlayerInRadius(Vector2 center, float radius, int damage)
    {
        Transform target = movement.Target;
        if (target == null || Vector2.Distance(center, target.position) > radius) return;
        PlayerStats player = target.GetComponent<PlayerStats>();
        if (player != null && !player.IsDead) player.TakeDamage(damage);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsAttacking = false;
        CurrentSkillName = null;
    }
}
