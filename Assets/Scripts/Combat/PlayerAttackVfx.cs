using UnityEngine;

public static class PlayerAttackVfx
{
    private static Material trailMaterial;
    private static Material TrailMaterial => trailMaterial != null ? trailMaterial :
        trailMaterial = new Material(Shader.Find("Sprites/Default"));

    public static void SpawnMuzzle(Vector2 position, Vector2 direction, Color color)
    {
        SpawnPulse(position + direction.normalized * 0.45f, 0.48f, color, 0.16f, false);
    }

    public static void SpawnMeleeArc(Vector2 center, Vector2 direction, float range, Color color)
    {
        GameObject effect = CreateSprite("Player Melee Arc", center, RuntimeCombatSprites.Ring, color, 0.2f);
        effect.transform.right = direction;
        effect.transform.localScale = new Vector3(range * 1.35f, range * 0.7f, 1f);
    }

    public static void SpawnImpact(Vector2 position, Color color, float size = 0.75f)
    {
        SpawnPulse(position, size, Color.Lerp(color, Color.white, 0.35f), 0.2f, false);
        SpawnPulse(position, size * 1.45f, new Color(color.r, color.g, color.b, 0.65f), 0.28f, true);
    }

    public static void SpawnSkillBurst(Vector2 position, float radius, Color color)
    {
        SpawnPulse(position, radius * 2f, new Color(color.r, color.g, color.b, 0.78f), 0.34f, true);
        SpawnPulse(position, radius * 1.5f, new Color(1f, 1f, 1f, 0.45f), 0.22f, true);
        SpawnPulse(position, Mathf.Max(0.7f, radius * 0.45f), color, 0.25f, false);
    }

    public static void AttachTrail(GameObject projectile, Color color, float width)
    {
        TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
        if (trail == null) trail = projectile.AddComponent<TrailRenderer>();
        trail.material = TrailMaterial; trail.time = 0.16f; trail.minVertexDistance = 0.04f;
        trail.startWidth = width; trail.endWidth = 0f; trail.sortingOrder = 18;
        trail.startColor = new Color(color.r, color.g, color.b, 0.9f);
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    private static void SpawnPulse(Vector2 position, float size, Color color, float lifetime, bool ring)
    {
        GameObject effect = CreateSprite("Player Attack Impact", position,
            ring ? RuntimeCombatSprites.Ring : RuntimeCombatSprites.Circle, color, lifetime);
        effect.transform.localScale = Vector3.one * size;
    }

    private static GameObject CreateSprite(string name, Vector2 position, Sprite sprite, Color color, float lifetime)
    {
        GameObject effect = new(name, typeof(SpriteRenderer), typeof(PlayerAttackVfxPulse));
        effect.transform.position = position;
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>(); renderer.sprite = sprite;
        renderer.color = color; renderer.sortingOrder = 20;
        effect.GetComponent<PlayerAttackVfxPulse>().Initialize(lifetime);
        return effect;
    }
}

public sealed class PlayerAttackVfxPulse : MonoBehaviour
{
    private SpriteRenderer rendererComponent; private float duration, elapsed; private Vector3 startScale; private Color startColor;
    public void Initialize(float lifetime) { duration = Mathf.Max(0.05f, lifetime); rendererComponent = GetComponent<SpriteRenderer>(); startScale = transform.localScale; startColor = rendererComponent.color; }
    private void Start() { startScale = transform.localScale; startColor = rendererComponent.color; }
    private void Update()
    {
        elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / duration);
        transform.localScale = startScale * Mathf.Lerp(0.72f, 1.18f, t);
        if (rendererComponent != null) { Color c = startColor; c.a *= 1f - t; rendererComponent.color = c; }
        if (t >= 1f) Destroy(gameObject);
    }
}
