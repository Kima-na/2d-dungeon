using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public sealed class BossHealth : MonoBehaviour
{
    [SerializeField] private string bossName = "망자의 기사";
    [SerializeField, Min(1)] private int maxHealth = 1000;
    [SerializeField, Min(0)] private int defense = 10;
    [SerializeField, Min(0)] private int experienceReward = 300;
    [SerializeField, Min(0f)] private float deathDelay = 1.5f;

    private Damageable damageable;
    private bool deathStarted;
    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private int lastHealth;

    public Damageable Damageable => damageable;
    public int CurrentHealth => damageable != null ? damageable.CurrentHealth : 0;
    public int MaxHealth => damageable != null ? damageable.MaxHealth : maxHealth;
    public bool IsDead => damageable != null && damageable.IsDead;
    public string BossName => bossName;
    public event Action<BossHealth> Defeated;
    public static event Action<BossHealth> AnyBossDefeated;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        damageable.Configure(maxHealth, experienceReward, false, true);
        damageable.SetDamageReduction(defense);
        lastHealth = damageable.CurrentHealth;
        WorldHealthBar bar = GetComponent<WorldHealthBar>();
        if (bar == null) bar = gameObject.AddComponent<WorldHealthBar>();
        bar.Bind(damageable, true);
        ConfigureBodyCollider();
        WorldShadow.Ensure(transform, spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : -1,
            1.25f, -0.34f);
    }

    private void ConfigureBodyCollider()
    {
        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (capsule == null) return;
        capsule.direction = CapsuleDirection2D.Vertical;
        bool isEagleKnight = GetComponent<EagleKnightBossCombat>() != null;
        capsule.size = isEagleKnight ? new Vector2(1.15f, 1.65f) : new Vector2(0.82f, 0.96f);
        capsule.offset = isEagleKnight ? new Vector2(0f, 0.8f) : new Vector2(0f, -0.2f);
        capsule.isTrigger = false;
    }

    private void OnEnable()
    {
        damageable.Died += OnDied;
        damageable.HealthChanged += OnHealthChanged;
    }
    private void OnDisable()
    {
        if (damageable != null) damageable.Died -= OnDied;
        if (damageable != null) damageable.HealthChanged -= OnHealthChanged;
    }

    public void Configure(int health, int reward)
    {
        Configure(health, reward, bossName, defense, deathDelay);
    }

    public void Configure(int health, int reward, string displayName, int damageDefense, float delay)
    {
        maxHealth = Mathf.Max(1, health);
        experienceReward = Mathf.Max(0, reward);
        bossName = displayName;
        defense = Mathf.Max(0, damageDefense);
        deathDelay = Mathf.Max(0f, delay);
        damageable.Configure(maxHealth, experienceReward, false, true);
        damageable.SetDamageReduction(defense);
        lastHealth = damageable.CurrentHealth;
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (current < lastHealth && current > 0)
        {
            GetComponent<BossAnimator>()?.PlayHit();
            GetComponent<EagleKnightAnimator>()?.PlayHit();
            GetComponent<AncientGolemAnimator>()?.PlayHit();
            if (spriteRenderer != null) StartCoroutine(HitFlash());
        }
        lastHealth = current;
    }

    private IEnumerator HitFlash()
    {
        spriteRenderer.color = new Color(1f, 0.45f, 0.55f, 1f);
        yield return new WaitForSeconds(0.09f);
        if (!IsDead) spriteRenderer.color = normalColor;
    }

    private void OnDied()
    {
        if (deathStarted) return;
        deathStarted = true;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        BossMovement movement = GetComponent<BossMovement>();
        BossCombat combat = GetComponent<BossCombat>();
        NightmareBossCombat nightmareCombat = GetComponent<NightmareBossCombat>();
        EagleKnightBossCombat eagleCombat = GetComponent<EagleKnightBossCombat>();
        EagleKnightAnimator eagleAnimator = GetComponent<EagleKnightAnimator>();
        AncientGolemAnimator golemAnimator = GetComponent<AncientGolemAnimator>();
        AncientGolemCombat golemCombat = GetComponent<AncientGolemCombat>();
        BossAnimator bossAnimator = GetComponent<BossAnimator>();
        if (movement != null) movement.enabled = false;
        if (combat != null) combat.enabled = false;
        if (nightmareCombat != null) nightmareCombat.enabled = false;
        if (eagleCombat != null) eagleCombat.enabled = false;
        if (golemCombat != null) golemCombat.enabled = false;
        if (bossAnimator != null) bossAnimator.SetDead();
        if (eagleAnimator != null) eagleAnimator.SetDead();
        if (golemAnimator != null) golemAnimator.SetDead();
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;

        // Damageable disables renderers after raising Died. Restore only the boss
        // visual for the short death presentation; colliders remain disabled.
        yield return null;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.enabled = true;
        float animationLength = bossAnimator != null ? bossAnimator.DeathAnimationLength :
            eagleAnimator != null ? eagleAnimator.DeathAnimationLength :
            golemAnimator != null ? golemAnimator.DeathAnimationLength : 0f;
        yield return new WaitForSeconds(Mathf.Max(deathDelay, animationLength));
        eagleCombat?.GrantRewards();
        EquipmentPickup.Spawn(transform.position);
        Defeated?.Invoke(this);
        AnyBossDefeated?.Invoke(this);
        BossUI.Hide(this);
        Destroy(gameObject);
    }
}
