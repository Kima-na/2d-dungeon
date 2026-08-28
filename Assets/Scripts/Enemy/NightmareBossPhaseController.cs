using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BossHealth), typeof(NightmareBossCombat), typeof(BossMovement))]
public sealed class NightmareBossPhaseController : MonoBehaviour
{
    private readonly Sprite[] phaseSprites = new Sprite[3];
    private BossHealth health;
    private NightmareBossCombat combat;
    private BossMovement movement;
    private SpriteRenderer spriteRenderer;
    private int currentPhase;
    private Coroutine transitionRoutine;

    public int CurrentPhase => currentPhase;

    private void Awake()
    {
        health = GetComponent<BossHealth>();
        combat = GetComponent<NightmareBossCombat>();
        movement = GetComponent<BossMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        for (int i = 0; i < phaseSprites.Length; i++)
            phaseSprites[i] = Resources.Load<Sprite>($"NightmareBoss/Phase{i + 1}");
    }

    private void Start()
    {
        ApplyPhase(1, false);
        health.Damageable.HealthChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        if (health != null && health.Damageable != null)
            health.Damageable.HealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (current <= 0 || maximum <= 0) return;
        float ratio = current / (float)maximum;
        int targetPhase = ratio <= 0.33f ? 3 : ratio <= 0.66f ? 2 : 1;
        if (targetPhase > currentPhase) ApplyPhase(targetPhase, true);
    }

    private void ApplyPhase(int phase, bool showTransition)
    {
        currentPhase = Mathf.Clamp(phase, 1, 3);
        if (spriteRenderer != null && phaseSprites[currentPhase - 1] != null)
            spriteRenderer.sprite = phaseSprites[currentPhase - 1];

        combat.SetPhase(currentPhase);
        movement.ConfigureSpeed(currentPhase switch { 1 => 2.1f, 2 => 2.55f, _ => 3.05f });
        transform.localScale = Vector3.one * (currentPhase switch { 1 => 1f, 2 => 1.05f, _ => 1.1f });

        if (!showTransition) return;
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(PhaseTransition());
    }

    private IEnumerator PhaseTransition()
    {
        combat.SetAutomaticAttacks(false);
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
        Color aura = currentPhase == 2
            ? new Color(0.45f, 0.2f, 1f, 0.75f)
            : new Color(1f, 0.08f, 0.12f, 0.85f);
        BossAttackEffect.Spawn(null, transform.position, Vector2.one * (currentPhase == 2 ? 4.2f : 5.4f),
            0.9f, 0f, aura);

        float elapsed = 0f;
        while (elapsed < 0.8f && !health.IsDead)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(Color.white, aura, Mathf.PingPong(elapsed * 5f, 1f));
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        if (!health.IsDead) combat.SetAutomaticAttacks(true);
        transitionRoutine = null;
    }
}
