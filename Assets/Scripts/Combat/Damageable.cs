using System;
using System.Collections;
using UnityEngine;

public class Damageable : MonoBehaviour, IDamageable, IExperienceSource
{
    [SerializeField, Min(1)] private int maxHealth = 30;
    [SerializeField, Min(0)] private int experienceReward = 100;
    [Header("Respawn")]
    [SerializeField] private bool respawnAfterDeath;
    [SerializeField, Min(0.1f)] private float respawnDelay = 1.5f;
    [SerializeField] private bool invincible;
    [SerializeField] private bool deactivateOnDeath = true;
    [SerializeField, Min(0)] private int damageReduction;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public int ExperienceReward => experienceReward;
    public bool RespawnAfterDeath => respawnAfterDeath;
    public float RespawnDelay => respawnDelay;
    public bool Invincible => invincible;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Renderer[] cachedRenderers;
    private Collider2D[] cachedColliders;
    private Coroutine respawnRoutine;

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider2D>(true);
        CurrentHealth = maxHealth;
    }

    private void Start() => HealthChanged?.Invoke(CurrentHealth, maxHealth);

    public void TakeDamage(int damage)
    {
        if (IsDead || invincible || damage <= 0) return;

        int finalDamage = Mathf.Max(1, damage - damageReduction);
        CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
        DamagePopup.Spawn(transform, finalDamage);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
            CompleteDeath();
    }

    public void Kill()
    {
        if (IsDead) return;
        CurrentHealth = 0;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        CompleteDeath();
    }

    private void CompleteDeath()
    {
        IsDead = true;
        try
        {
            Died?.Invoke();
        }
        finally
        {
            if (respawnAfterDeath) respawnRoutine = StartCoroutine(RespawnRoutine());
            else if (deactivateOnDeath) gameObject.SetActive(false);
            else SetVisibleAndCollidable(false);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        SetVisibleAndCollidable(false);
        yield return new WaitForSeconds(respawnDelay);
        respawnRoutine = null;
        if (respawnAfterDeath) ResetState();
    }

    public void SetMaximumHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        ResetState();
    }

    public void SetRespawnEnabled(bool value)
    {
        respawnAfterDeath = value;
        if (value && IsDead && respawnRoutine == null) ResetState();
    }

    public void SetRespawnDelay(float value) => respawnDelay = Mathf.Max(0.1f, value);

    public void SetInvincible(bool value) => invincible = value;

    public void SetDeactivateOnDeath(bool value) => deactivateOnDeath = value;

    public void SetExperienceReward(int value) => experienceReward = Mathf.Max(0, value);
    public void SetDamageReduction(int value) => damageReduction = Mathf.Max(0, value);

    public void Configure(int health, int reward, bool shouldRespawn = false, bool keepActiveOnDeath = false)
    {
        maxHealth = Mathf.Max(1, health);
        experienceReward = Mathf.Max(0, reward);
        respawnAfterDeath = shouldRespawn;
        deactivateOnDeath = !keepActiveOnDeath;
        invincible = false;
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        ResetState();
    }

    public void ResetState()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
        StatusEffectController effects = GetComponent<StatusEffectController>();
        if (effects != null) effects.ClearAllEffects();
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        CurrentHealth = maxHealth;
        IsDead = false;
        SetVisibleAndCollidable(true);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void SetVisibleAndCollidable(bool value)
    {
        // Another component can call Configure from its Awake before this
        // component's Awake has populated the caches. Build them lazily so
        // boss prefab initialization is independent of Awake ordering.
        if (cachedRenderers == null)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        if (cachedColliders == null)
            cachedColliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Renderer cachedRenderer in cachedRenderers)
            if (cachedRenderer != null) cachedRenderer.enabled = value;
        foreach (Collider2D cachedCollider in cachedColliders)
            if (cachedCollider != null) cachedCollider.enabled = value;
    }
}
