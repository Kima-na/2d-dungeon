using System;
using System.Collections;
using UnityEngine;

public class Damageable : MonoBehaviour, IDamageable, IExperienceSource
{
    [SerializeField, Min(1)] private int maxHealth = 30;
    [SerializeField, Min(0)] private int experienceReward = 100;
    [Header("Prototype Respawn")]
    [SerializeField] private bool respawnAfterDeath = true;
    [SerializeField, Min(0.1f)] private float respawnDelay = 1.5f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public int ExperienceReward => experienceReward;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Renderer[] cachedRenderers;
    private Collider2D[] cachedColliders;

    private void Awake()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider2D>(true);
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
        {
            IsDead = true;
            Died?.Invoke();
            if (respawnAfterDeath) StartCoroutine(RespawnRoutine());
            else gameObject.SetActive(false);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        SetVisibleAndCollidable(false);
        yield return new WaitForSeconds(respawnDelay);

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        CurrentHealth = maxHealth;
        IsDead = false;
        SetVisibleAndCollidable(true);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void SetVisibleAndCollidable(bool value)
    {
        foreach (Renderer cachedRenderer in cachedRenderers)
            if (cachedRenderer != null) cachedRenderer.enabled = value;
        foreach (Collider2D cachedCollider in cachedColliders)
            if (cachedCollider != null) cachedCollider.enabled = value;
    }
}
