using UnityEngine;

public static class CombatCalculator
{
    public static int RollDamage(PlayerStats attacker, int baseDamage, out bool critical)
    {
        critical = attacker != null && Random.value < attacker.CriticalChance;
        float multiplier = critical ? attacker.CriticalDamageMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    public static int ApplyTargetModifiers(GameObject target, int damage)
    {
        StatusEffectController effects = target.GetComponentInParent<StatusEffectController>();
        return effects != null ? effects.ModifyIncomingDamage(damage) : damage;
    }
}
