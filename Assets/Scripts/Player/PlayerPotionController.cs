using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerPotionController : MonoBehaviour
{
    [SerializeField, Min(1f)] private float cooldown = 15f;
    [SerializeField, Range(0.05f, 1f)] private float healthRestoreRatio = 0.5f;
    [SerializeField, Range(0.05f, 1f)] private float manaRestoreRatio = 0.5f;

    private PlayerStats stats;
    private PlayerController controller;
    private float nextHealthUseTime;
    private float nextManaUseTime;

    public float HealthCooldownRemaining => Mathf.Max(0f, nextHealthUseTime - Time.time);
    public float ManaCooldownRemaining => Mathf.Max(0f, nextManaUseTime - Time.time);
    public float Cooldown => cooldown;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Keyboard.current == null || stats.IsDead ||
            (controller != null && controller.IsInputLocked)) return;
        if (Keyboard.current.f1Key.wasPressedThisFrame) TryUseHealthPotion();
        if (Keyboard.current.f2Key.wasPressedThisFrame) TryUseManaPotion();
    }

    public bool TryUseHealthPotion()
    {
        if (stats.IsDead || Time.time < nextHealthUseTime || stats.CurrentHealth >= stats.MaxHealth)
            return false;
        stats.Heal(Mathf.Max(1, Mathf.RoundToInt(stats.MaxHealth * healthRestoreRatio)));
        nextHealthUseTime = Time.time + cooldown;
        return true;
    }

    public bool TryUseManaPotion()
    {
        if (stats.IsDead || Time.time < nextManaUseTime || stats.CurrentMana >= stats.MaxMana)
            return false;
        stats.RestoreMana(Mathf.Max(1, Mathf.RoundToInt(stats.MaxMana * manaRestoreRatio)));
        nextManaUseTime = Time.time + cooldown;
        return true;
    }
}
