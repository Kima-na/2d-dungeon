using UnityEngine;

// Extension point for future boss patterns. Easy boss intentionally has no skill yet.
public abstract class BossSkill : MonoBehaviour
{
    public abstract bool CanUse { get; }
    public abstract void Use(Transform target);
}
