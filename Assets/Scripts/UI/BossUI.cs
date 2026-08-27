using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BossUI : MonoBehaviour
{
    private static BossUI instance;
    private CanvasGroup group;
    private Image fill;
    private Text nameText;
    private Text healthText;
    private BossHealth boundBoss;
    private Coroutine fadeRoutine;

    public bool IsVisible => group != null && group.alpha > 0.01f;
    public float FillAmount => fill != null ? fill.fillAmount : 0f;

    public static void Show(BossHealth boss)
    {
        if (boss == null) return;
        EnsureInstance();
        instance.Bind(boss);
    }

    public static void Hide(BossHealth boss)
    {
        if (instance == null || instance.boundBoss != boss) return;
        instance.BeginFade(0f, true);
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("Boss UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        }
        GameObject root = new("BossUI", typeof(RectTransform), typeof(CanvasGroup), typeof(BossUI));
        root.transform.SetParent(canvas.transform, false);
        instance = root.GetComponent<BossUI>();
        instance.Build();
    }

    private void Build()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        RectTransform root = (RectTransform)transform;
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -35f);
        root.sizeDelta = new Vector2(720f, 92f);

        BossVisualDatabase visuals = Resources.Load<BossVisualDatabase>("BossVisualDatabase");
        nameText = CreateText("BossName", new Vector2(0f, -2f), new Vector2(650f, 32f), 25);
        GameObject bar = new("BossHPBackground", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(transform, false);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -36f);
        barRect.sizeDelta = new Vector2(650f, 42f);
        Image frame = bar.GetComponent<Image>();
        frame.sprite = visuals != null ? visuals.bossBarFrame : null;
        frame.type = Image.Type.Sliced;
        frame.color = Color.white;

        GameObject fillObject = new("BossHPFill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(bar.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(12f, 11f);
        fillRect.offsetMax = new Vector2(-12f, -11f);
        fill = fillObject.GetComponent<Image>();
        fill.sprite = visuals != null ? visuals.bossBarFill : null;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;

        healthText = CreateText("BossHPText", Vector2.zero, barRect.sizeDelta, 18, bar.transform);
    }

    private Text CreateText(string objectName, Vector2 position, Vector2 size, int fontSize, Transform parent = null)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent != null ? parent : transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = parent == null ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void Bind(BossHealth boss)
    {
        Unbind();
        boundBoss = boss;
        boundBoss.Damageable.HealthChanged += Refresh;
        nameText.text = boss.BossName;
        Refresh(boss.CurrentHealth, boss.MaxHealth);
        BeginFade(1f, false);
    }

    private void Refresh(int current, int maximum)
    {
        float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
        fill.fillAmount = ratio;
        healthText.text = $"{current} / {maximum}";
    }

    private void BeginFade(float target, bool unbindAfter)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(target, unbindAfter));
    }

    private IEnumerator FadeRoutine(float target, bool unbindAfter)
    {
        float start = group.alpha;
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, elapsed / 0.35f);
            yield return null;
        }
        group.alpha = target;
        fadeRoutine = null;
        if (unbindAfter) Unbind();
    }

    private void Unbind()
    {
        if (boundBoss != null) boundBoss.Damageable.HealthChanged -= Refresh;
        boundBoss = null;
    }

    private void OnDestroy()
    {
        Unbind();
        if (instance == this) instance = null;
    }
}
