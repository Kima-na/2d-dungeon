using UnityEngine;

public sealed class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.85f;
    private TextMesh textMesh;
    private Color baseColor;
    private float elapsed;
    private Vector3 drift;

    public static void Spawn(Transform target, int damage)
    {
        if (target == null || damage <= 0) return;

        Bounds bounds = new(target.position, Vector3.one);
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
        }

        GameObject popupObject = new("Damage Popup", typeof(TextMesh), typeof(DamagePopup));
        popupObject.transform.position = new Vector3(
            bounds.center.x + Random.Range(-0.12f, 0.12f),
            bounds.max.y + 0.28f,
            target.position.z - 0.5f);

        TextMesh text = popupObject.GetComponent<TextMesh>();
        text.text = damage.ToString();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 40;
        text.characterSize = 0.075f;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.28f, 0.18f, 1f);

        MeshRenderer renderer = popupObject.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 500;
        popupObject.GetComponent<DamagePopup>().Initialize(text);
    }

    private void Initialize(TextMesh text)
    {
        textMesh = text;
        baseColor = text.color;
        drift = new Vector3(Random.Range(-0.12f, 0.12f), 0.9f, 0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += drift * Time.deltaTime;
        drift.y = Mathf.Lerp(drift.y, 0.25f, Time.deltaTime * 4f);

        if (textMesh != null)
        {
            Color color = baseColor;
            color.a = 1f - Mathf.Clamp01(elapsed / Lifetime);
            textMesh.color = color;
        }

        if (elapsed >= Lifetime) Destroy(gameObject);
    }
}
