using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class AncientGolemAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] phase1Idle, phase1Slam, phase1Throw, phase1Hit, phase1Death;
    [SerializeField] private Sprite[] phase2Idle, phase2Slam, phase2Crack, phase2Volley, phase2Hit, phase2Death;
    [SerializeField, Min(0.04f)] private float frameDuration = 0.13f;
    private SpriteRenderer rendererComponent; private Sprite[] current; private int frame; private float nextFrame;
    private bool loop, dead, phaseTwo;
    public float DeathAnimationLength => (phaseTwo ? phase2Death : phase1Death)?.Length * frameDuration ?? 0f;
    private void Awake() { rendererComponent = GetComponent<SpriteRenderer>(); Play(phase1Idle, true); }
    private void Update() { if (current == null || current.Length == 0 || Time.time < nextFrame) return; nextFrame = Time.time + frameDuration; if (++frame >= current.Length) frame = loop ? 0 : current.Length - 1; rendererComponent.sprite = current[frame]; }
    public void SetMovement(Vector2 direction, bool moving) { if (Mathf.Abs(direction.x) > .05f) rendererComponent.flipX = direction.x < 0; if (!dead && !IsAction()) Play(phaseTwo ? phase2Idle : phase1Idle, true); }
    public void SetPhaseTwo() { if (phaseTwo) return; phaseTwo = true; Play(phase2Idle, true); }
    public void PlaySlam() => Play(phaseTwo ? phase2Slam : phase1Slam, false);
    public void PlayRanged() => Play(phaseTwo ? phase2Volley : phase1Throw, false);
    public void PlayCrack() => Play(phase2Crack, false);
    public void PlayHit() { if (!dead) Play(phaseTwo ? phase2Hit : phase1Hit, false); }
    public void EndAction() { if (!dead) Play(phaseTwo ? phase2Idle : phase1Idle, true); }
    public void SetDead() { dead = true; Play(phaseTwo ? phase2Death : phase1Death, false); }
    private bool IsAction() => current == phase1Slam || current == phase1Throw || current == phase2Slam || current == phase2Crack || current == phase2Volley;
    private void Play(Sprite[] sprites, bool repeat) { if (sprites == null || sprites.Length == 0 || current == sprites && loop == repeat) return; current = sprites; loop = repeat; frame = 0; nextFrame = Time.time + frameDuration; rendererComponent.sprite = sprites[0]; }
}
