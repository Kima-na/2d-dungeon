using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class EagleKnightAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] idle, walk, hit, slash, charge, spearThrow, skill, death;
    [SerializeField, Min(0.03f)] private float frameDuration = 0.12f;
    private SpriteRenderer spriteRenderer;
    private Sprite[] current;
    private float nextFrame;
    private int frame;
    private bool loop = true;
    private bool dead;
    public float DeathAnimationLength => death == null ? 0f : death.Length * frameDuration;

    private void Awake() { spriteRenderer = GetComponent<SpriteRenderer>(); Play(idle, true); }
    private void Update()
    {
        if (current == null || current.Length == 0 || Time.time < nextFrame) return;
        nextFrame = Time.time + frameDuration;
        if (frame < current.Length - 1) frame++;
        else if (loop) frame = 0;
        else if (current == hit) { Play(idle, true); return; }
        spriteRenderer.sprite = current[frame];
    }
    public void SetMovement(Vector2 direction, bool moving)
    { Face(direction); if (!dead && (current == idle || current == walk)) Play(moving ? walk : idle, true); }
    public void PlaySlash(Vector2 direction) { Face(direction); Play(slash, false); }
    public void PlayCharge(Vector2 direction) { Face(direction); Play(charge, true); }
    public void PlayThrow(Vector2 direction) { Face(direction); Play(spearThrow, false); }
    public void PlaySkill() => Play(skill, false);
    public void PlayHit() { if (!dead) Play(hit, false); }
    public void EndAction() { if (!dead) Play(idle, true); }
    public void SetDead() { dead = true; Play(death, false); }
    private void Face(Vector2 direction)
    { if (Mathf.Abs(direction.x) > 0.05f) spriteRenderer.flipX = direction.x < 0f; }
    private void Play(Sprite[] frames, bool shouldLoop)
    {
        if (frames == null || frames.Length == 0 || current == frames && loop == shouldLoop) return;
        current = frames; loop = shouldLoop; frame = 0; nextFrame = Time.time + frameDuration;
        spriteRenderer.sprite = frames[0];
    }
}
