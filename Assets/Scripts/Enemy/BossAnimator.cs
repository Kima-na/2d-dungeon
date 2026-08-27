using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class BossAnimator : MonoBehaviour
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int IsDead = Animator.StringToHash("IsDead");
    private static readonly int Direction = Animator.StringToHash("Direction");
    private static readonly int AttackType = Animator.StringToHash("AttackType");
    private static readonly int Hit = Animator.StringToHash("Hit");

    private Animator animator;
    private Vector2 facing = Vector2.down;

    public Vector2 Facing => facing;
    public int FacingDirection => GetDirection(facing);

    private void Awake() => animator = GetComponent<Animator>();

    public void SetMovement(Vector2 direction, bool moving)
    {
        if (direction.sqrMagnitude > 0.01f) facing = direction.normalized;
        ApplyDirection(facing);
        animator.SetBool(IsMoving, moving);
    }

    public void PlayAttack(int attackType, Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f) facing = direction.normalized;
        ApplyDirection(facing);
        animator.SetInteger(AttackType, Mathf.Clamp(attackType, 1, 4));
        animator.SetBool(IsAttacking, true);
    }

    public void EndAttack() => animator.SetBool(IsAttacking, false);

    public void PlayHit()
    {
        if (!animator.GetBool(IsDead)) animator.SetTrigger(Hit);
    }

    public void SetDead()
    {
        animator.ResetTrigger(Hit);
        ApplyDirection(facing);
        animator.SetBool(IsMoving, false);
        animator.SetBool(IsAttacking, false);
        animator.SetBool(IsDead, true);
    }

    public float DeathAnimationLength
    {
        get
        {
            if (animator == null || animator.runtimeAnimatorController == null) return 0f;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                if (clip != null && clip.name.StartsWith("Boss_Death")) return clip.length;
            return 0f;
        }
    }

    private void ApplyDirection(Vector2 direction)
    {
        animator.SetFloat(MoveX, direction.x);
        animator.SetFloat(MoveY, direction.y);
        animator.SetInteger(Direction, GetDirection(direction));
    }

    private static int GetDirection(Vector2 direction) => Mathf.Abs(direction.y) >= Mathf.Abs(direction.x)
        ? (direction.y < 0f ? 0 : 1)
        : (direction.x < 0f ? 2 : 3);
}
