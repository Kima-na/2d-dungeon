using System.Collections;
using UnityEngine;

public enum StatusEffectType { Burn, Poison, Freeze, Shock }

public class StatusEffectController : MonoBehaviour
{
    private IDamageable damageable;
    private PlayerStats effectOwner;
    private SpriteRenderer targetRenderer;
    private Color originalColor;
    private Rigidbody2D targetBody;
    private RigidbodyConstraints2D originalConstraints;
    private float burnUntil;
    private float poisonUntil;
    private float freezeUntil;
    private float shockUntil;
    private int poisonStacks;
    private bool burnRunning;
    private bool poisonRunning;
    private bool shockRunning;

    public bool IsBurning => Time.time < burnUntil;
    public bool IsPoisoned => Time.time < poisonUntil;
    public bool IsFrozen => Time.time < freezeUntil;
    public bool IsShocked => Time.time < shockUntil;

    public static bool TryApply(GameObject hitObject, StatusEffectType effect, PlayerStats owner,
        float chance = 1f)
    {
        if (Random.value > chance) return false;
        MonoBehaviour target = null;
        foreach (MonoBehaviour behaviour in hitObject.GetComponentsInParent<MonoBehaviour>())
        {
            if (behaviour is IDamageable candidate && !candidate.IsDead)
            {
                target = behaviour;
                break;
            }
        }
        if (target == null) return false;
        StatusEffectController controller = target.GetComponent<StatusEffectController>();
        if (controller == null) controller = target.gameObject.AddComponent<StatusEffectController>();
        controller.Apply(effect, owner);
        return true;
    }

    private void Awake()
    {
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            if (behaviour is IDamageable found) { damageable = found; break; }
        targetRenderer = GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer != null) originalColor = targetRenderer.color;
        targetBody = GetComponent<Rigidbody2D>();
        if (targetBody != null) originalConstraints = targetBody.constraints;
    }

    public void Apply(StatusEffectType effect, PlayerStats owner)
    {
        effectOwner = owner;
        switch (effect)
        {
            case StatusEffectType.Burn:
                burnUntil = Mathf.Max(burnUntil, Time.time + 4f);
                if (!burnRunning) StartCoroutine(BurnRoutine());
                break;
            case StatusEffectType.Poison:
                poisonUntil = Mathf.Max(poisonUntil, Time.time + 6f);
                poisonStacks = Mathf.Min(3, poisonStacks + 1);
                if (!poisonRunning) StartCoroutine(PoisonRoutine());
                break;
            case StatusEffectType.Freeze:
                freezeUntil = Mathf.Max(freezeUntil, Time.time + 2f);
                StartCoroutine(FreezeRoutine());
                break;
            case StatusEffectType.Shock:
                shockUntil = Mathf.Max(shockUntil, Time.time + 3f);
                if (!shockRunning) StartCoroutine(ShockRoutine());
                break;
        }
        RefreshColor();
    }

    public int ModifyIncomingDamage(int damage) => IsShocked
        ? Mathf.Max(1, Mathf.RoundToInt(damage * 1.25f))
        : damage;

    public void ClearAllEffects()
    {
        StopAllCoroutines();
        burnUntil = poisonUntil = freezeUntil = shockUntil = 0f;
        poisonStacks = 0;
        burnRunning = poisonRunning = shockRunning = false;
        if (targetBody != null) targetBody.constraints = originalConstraints;
        RefreshColor();
    }

    private IEnumerator BurnRoutine()
    {
        burnRunning = true;
        while (IsBurning && !IsTargetDead())
        {
            yield return new WaitForSeconds(1f);
            DealPeriodicDamage(5);
        }
        burnRunning = false;
        RefreshColor();
    }

    private IEnumerator PoisonRoutine()
    {
        poisonRunning = true;
        while (IsPoisoned && !IsTargetDead())
        {
            yield return new WaitForSeconds(1f);
            DealPeriodicDamage(2 * poisonStacks);
        }
        poisonStacks = 0;
        poisonRunning = false;
        RefreshColor();
    }

    private IEnumerator ShockRoutine()
    {
        shockRunning = true;
        while (IsShocked && !IsTargetDead())
        {
            yield return new WaitForSeconds(1f);
            DealPeriodicDamage(4);
        }
        shockRunning = false;
        RefreshColor();
    }

    private IEnumerator FreezeRoutine()
    {
        if (targetBody != null) targetBody.constraints = RigidbodyConstraints2D.FreezeAll;
        while (IsFrozen && !IsTargetDead())
        {
            if (targetBody != null) targetBody.linearVelocity = Vector2.zero;
            yield return null;
        }
        if (targetBody != null) targetBody.constraints = originalConstraints;
        RefreshColor();
    }

    private void DealPeriodicDamage(int amount)
    {
        if (IsTargetDead()) return;
        damageable.TakeDamage(amount);
        if (damageable.IsDead && effectOwner != null && damageable is IExperienceSource source)
            effectOwner.AddExperience(source.ExperienceReward);
    }

    private bool IsTargetDead() => damageable == null || damageable.IsDead;

    private void RefreshColor()
    {
        if (targetRenderer == null) return;
        if (IsFrozen) targetRenderer.color = new Color(0.35f, 0.8f, 1f);
        else if (IsShocked) targetRenderer.color = new Color(1f, 0.9f, 0.2f);
        else if (IsBurning) targetRenderer.color = new Color(1f, 0.35f, 0.1f);
        else if (IsPoisoned) targetRenderer.color = new Color(0.35f, 1f, 0.25f);
        else targetRenderer.color = originalColor;
    }
}
