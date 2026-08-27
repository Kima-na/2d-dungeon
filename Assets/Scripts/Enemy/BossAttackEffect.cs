using System.Collections;
using UnityEngine;

public sealed class BossAttackEffect : MonoBehaviour
{
    public static BossAttackEffect Spawn(Sprite sprite, Vector2 position, Vector2 worldSize,
        float lifetime, float angle = 0f, Color? tint = null, Transform parent = null)
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
        effectObject.transform.localScale = new Vector3(worldSize.x / Mathf.Max(0.01f, spriteSize.x),
            worldSize.y / Mathf.Max(0.01f, spriteSize.y), 1f);
        BossAttackEffect effect = effectObject.GetComponent<BossAttackEffect>();
        effect.StartCoroutine(effect.FadeAndDestroy(renderer, lifetime));
        return effect;
    }

    private IEnumerator FadeAndDestroy(SpriteRenderer renderer, float lifetime)
    {
        float elapsed = 0f;
        Color color = renderer.color;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            if (elapsed > lifetime * 0.7f) renderer.color = new Color(color.r, color.g, color.b,
                color.a * (1f - Mathf.InverseLerp(lifetime * 0.7f, lifetime, elapsed)));
            yield return null;
        }
        Destroy(gameObject);
    }
}
