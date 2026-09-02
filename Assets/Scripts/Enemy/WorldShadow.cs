using UnityEngine;

public sealed class WorldShadow : MonoBehaviour
{
    private static Sprite shadowSprite;

    public static void Ensure(Transform owner, int sortingOrder, float size = 1f, float verticalOffset = -0.42f, float alpha = 0.42f)
    {
        Transform existing = owner.Find("Ground Shadow");
        GameObject shadow = existing != null ? existing.gameObject :
            new GameObject("Ground Shadow", typeof(SpriteRenderer));
        if (existing == null) shadow.transform.SetParent(owner, false);
        shadow.transform.localPosition = new Vector3(0f, verticalOffset, 0f);
        shadow.transform.localScale = new Vector3(0.82f * size, 0.3f * size, 1f);
        SpriteRenderer renderer = shadow.GetComponent<SpriteRenderer>();
        renderer.sprite = GetSprite();
        renderer.color = new Color(0f, 0f, 0f, alpha);
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite GetSprite()
    {
        if (shadowSprite != null) return shadowSprite;
        const int width = 32, height = 16;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        { name = "Runtime Ellipse Shadow", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float dx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
            float dy = (y + 0.5f - height * 0.5f) / (height * 0.5f);
            float alpha = Mathf.Clamp01((1f - dx * dx - dy * dy) * 2.5f);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        texture.Apply();
        shadowSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 32f);
        shadowSprite.name = "Ground Shadow";
        return shadowSprite;
    }
}
