using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Vector2 worldOffset = new(0f, 0.95f);
    [SerializeField, Min(0.1f)] private float width = 1.1f;
    [SerializeField, Min(0.02f)] private float height = 0.12f;
    [SerializeField] private Color fillColor = new(0.85f, 0.12f, 0.12f, 1f);
    [SerializeField] private bool hideAtFullHealth;

    private Damageable health;
    private Transform root;
    private Transform fill;
    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer fillRenderer;

    public void Bind(Damageable target, bool boss = false)
    {
        Unbind();
        health = target;
        if (boss)
        {
            width = 2.2f;
            height = 0.16f;
            worldOffset = new Vector2(0f, 1.55f);
            fillColor = new Color(0.72f, 0.04f, 0.12f, 1f);
        }
        EnsureVisuals();
        health.HealthChanged += OnHealthChanged;
        health.Died += OnDied;
        Refresh(health.CurrentHealth, health.MaxHealth);
    }

    private void LateUpdate()
    {
        if (root == null) return;
        Vector3 parentScale = transform.lossyScale;
        root.localScale = new Vector3(SafeInverse(parentScale.x), SafeInverse(parentScale.y), 1f);
        root.localPosition = new Vector3(
            worldOffset.x * SafeInverse(parentScale.x), worldOffset.y * SafeInverse(parentScale.y), 0f);
        root.rotation = Quaternion.identity;
    }

    private static float SafeInverse(float value) => Mathf.Abs(value) < 0.0001f ? 1f : 1f / Mathf.Abs(value);

    private void EnsureVisuals()
    {
        if (root != null) return;
        root = new GameObject("World HP Bar").transform;
        root.SetParent(transform, false);
        backgroundRenderer = CreatePart("Background", root, new Color(0.08f, 0.08f, 0.1f, 0.92f), 50);
        backgroundRenderer.transform.localScale = new Vector3(width + 0.08f, height + 0.08f, 1f);
        fillRenderer = CreatePart("Fill", root, fillColor, 51);
        fill = fillRenderer.transform;
    }

    private static SpriteRenderer CreatePart(string name, Transform parent, Color color, int sortingOrder)
    {
        GameObject part = new(name, typeof(SpriteRenderer));
        part.transform.SetParent(parent, false);
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = MonsterRoster.PlaceholderSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void OnHealthChanged(int current, int maximum) => Refresh(current, maximum);

    private void Refresh(int current, int maximum)
    {
        if (root == null) return;
        float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
        fill.localScale = new Vector3(width * ratio, height, 1f);
        fill.localPosition = new Vector3((ratio - 1f) * width * 0.5f, 0f, 0f);
        root.gameObject.SetActive(current > 0 && (!hideAtFullHealth || current < maximum));
    }

    private void OnDied()
    {
        if (root != null) root.gameObject.SetActive(false);
    }

    private void OnDestroy() => Unbind();

    private void Unbind()
    {
        if (health == null) return;
        health.HealthChanged -= OnHealthChanged;
        health.Died -= OnDied;
        health = null;
    }
}
