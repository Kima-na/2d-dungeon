using System.Collections;
using UnityEngine;

public sealed class BossAttackEffect : MonoBehaviour
{
    public static BossAttackEffect Spawn(Sprite sprite, Vector2 position, Vector2 worldSize,
        float lifetime, float angle = 0f, Color? tint = null, Transform parent = null,
        bool expand = false, float spinSpeed = 0f)
    {
        if (sprite == null) return null;
        GameObject effectObject = new("Boss Attack Effect", typeof(SpriteRenderer), typeof(BossAttackEffect));
        if (parent != null) effectObject.transform.SetParent(parent, true);
        effectObject.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
        SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint ?? Color.white;
        renderer.sortingOrder = 10;
        Vector2 spriteSize = sprite.bounds.size;
        Vector3 targetScale = new(worldSize.x / Mathf.Max(0.01f, spriteSize.x),
            worldSize.y / Mathf.Max(0.01f, spriteSize.y), 1f);
        effectObject.transform.localScale = expand ? targetScale * 0.18f : targetScale;
        BossAttackEffect effect = effectObject.GetComponent<BossAttackEffect>();
        effect.StartCoroutine(effect.FadeAndDestroy(renderer, lifetime, targetScale, expand, spinSpeed));
        return effect;
    }

    private IEnumerator FadeAndDestroy(SpriteRenderer renderer, float lifetime, Vector3 targetScale,
        bool expand, float spinSpeed)
    {
        float elapsed = 0f;
        Color color = renderer.color;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            if (expand)
            {
                float expansion = Mathf.SmoothStep(0.18f, 1f, Mathf.InverseLerp(0f, lifetime * 0.42f, elapsed));
                transform.localScale = targetScale * expansion;
            }
            if (Mathf.Abs(spinSpeed) > 0.01f) transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            if (elapsed > lifetime * 0.7f) renderer.color = new Color(color.r, color.g, color.b,
                color.a * (1f - Mathf.InverseLerp(lifetime * 0.7f, lifetime, elapsed)));
            yield return null;
        }
        Destroy(gameObject);
    }
}
