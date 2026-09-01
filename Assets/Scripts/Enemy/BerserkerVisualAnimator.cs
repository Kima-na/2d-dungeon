using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class BerserkerVisualAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] idle, walk, attack, hit, death;
    [SerializeField, Min(0.03f)] private float frameDuration = 0.1f;
    private SpriteRenderer spriteRenderer;
    private Sprite[] current;
    private int frame;
    private float nextFrame;
    private bool loop;
    private bool action;

    private void Awake() { spriteRenderer = GetComponent<SpriteRenderer>(); Play(idle, true, false); }
    private void Update()
    {
        if (current == null || current.Length == 0 || Time.time < nextFrame) return;
        nextFrame = Time.time + frameDuration;
        if (frame < current.Length - 1) frame++;
        else if (loop) frame = 0;
        else { action = false; Play(idle, true, false); return; }
        spriteRenderer.sprite = current[frame];
    }

    public void SetMovement(Vector2 direction, bool moving)
    {
        if (Mathf.Abs(direction.x) > 0.03f) spriteRenderer.flipX = direction.x < 0f;
        if (!action) Play(moving ? walk : idle, true, false);
    }
    public void PlayAttack(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > 0.03f) spriteRenderer.flipX = direction.x < 0f;
        Play(attack, false, true);
    }
    public void PlayHit() => Play(hit, false, true);
    public void PlayDeath() => Play(death, false, true);

    private void Play(Sprite[] frames, bool shouldLoop, bool isAction)
    {
        if (frames == null || frames.Length == 0 || current == frames && loop == shouldLoop) return;
        current = frames; loop = shouldLoop; action = isAction; frame = 0;
        nextFrame = Time.time + frameDuration; spriteRenderer.sprite = frames[0];
    }
}
