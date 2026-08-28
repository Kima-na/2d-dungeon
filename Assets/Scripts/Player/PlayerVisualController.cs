using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(PlayerController), typeof(PlayerStats))]
public sealed class PlayerVisualController : MonoBehaviour
{
    [SerializeField, Range(0, 3)] private int designIndex;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;
    private PlayerVisualDatabase database;
    private PlayerController controller;
    private PlayerStats stats;
    private SpriteRenderer spriteRenderer;
    private float frameTimer;
    private int frame;
    public int DesignIndex => designIndex;

    private void Awake()
    {
        transform.localScale *= 1.18f;
        database = Resources.Load<PlayerVisualDatabase>("PlayerVisualDatabase");
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = Color.white;
        SetDesign(designIndex);
    }

    public void SetDesign(int index)
    {
        if (database == null) database = Resources.Load<PlayerVisualDatabase>("PlayerVisualDatabase");
        int count = database != null && database.designs != null ? database.designs.Length : 0;
        designIndex = count > 0 ? Mathf.Clamp(index, 0, count - 1) : 0;
        frame = 0;
        frameTimer = 0f;
        RefreshSprite();
    }

    private void Update()
    {
        if (database == null || database.designs == null || database.designs.Length == 0) return;
        frameTimer += Time.deltaTime * framesPerSecond;
        if (frameTimer >= 1f) { frameTimer -= 1f; frame++; }
        RefreshSprite();
    }

    private void RefreshSprite()
    {
        if (database == null || designIndex >= database.designs.Length || spriteRenderer == null) return;
        PlayerDesign design = database.designs[designIndex];
        if (stats != null && stats.IsDead)
        {
            spriteRenderer.sprite = Safe(design.dead, Mathf.Min(frame / 3, 1));
            return;
        }
        Vector2 move = controller != null ? controller.MoveInput : Vector2.zero;
        Vector2 facing = controller != null ? controller.LastMoveDirection : Vector2.down;
        int direction = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? 1 : facing.y > 0 ? 2 : 0;
        spriteRenderer.flipX = direction == 1 && facing.x < 0;
        spriteRenderer.sprite = move.sqrMagnitude > 0.01f
            ? Safe(design.run, direction + (frame % 2) * 3)
            : Safe(design.stand, direction);
    }

    private static Sprite Safe(Sprite[] sprites, int index) =>
        sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
}
