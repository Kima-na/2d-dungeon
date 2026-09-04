#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EagleKnightBossSetup
{
    private const string Source = "Assets/Sprites/normal boss.png";
    private const string Output = "Assets/Resources/EagleKnight";
    private const string PrefabPath = Output + "/EagleKnightBoss.prefab";
    private readonly struct Frame
    {
        public readonly string Name; public readonly RectInt Rect;
        public Frame(string name, int x, int top, int width, int height)
        { Name = name; Rect = new RectInt(x, top, width, height); }
    }

    private static readonly Frame[] Frames =
    {
        // Row 1: idle, movement and hit. Bounds intentionally exclude all Korean captions.
        new("Idle_01", 18, 45, 180, 235), new("Idle_02", 205, 45, 175, 235),
        new("Walk_01", 445, 65, 185, 215), new("Walk_02", 625, 70, 185, 210),
        new("Walk_03", 815, 75, 185, 205), new("Walk_04", 1000, 75, 185, 205),
        new("Hit_01", 1260, 75, 190, 205), new("Hit_02", 1455, 75, 215, 205),

        // Row 2: slash and charge.
        new("Slash_01", 20, 340, 180, 170), new("Slash_02", 205, 340, 245, 170),
        new("Slash_03", 455, 340, 180, 170), new("Slash_04", 640, 340, 205, 170),
        new("Charge_01", 855, 350, 190, 150), new("Charge_02", 1045, 350, 195, 150),
        new("Charge_03", 1235, 350, 195, 150), new("Charge_04", 1430, 350, 240, 150),

        // Row 3: spear throw and eagle descent. Captions end above y=580.
        new("Throw_01", 18, 580, 180, 165), new("Throw_02", 200, 580, 195, 165),
        new("Throw_03", 405, 580, 195, 165), new("Throw_04", 595, 580, 245, 165),
        new("Skill_01", 850, 580, 220, 165), new("Skill_02", 1075, 580, 260, 165),
        new("Skill_03", 1335, 575, 340, 175),

        // Row 4: death. The death caption ends above y=790.
        new("Death_01", 20, 790, 170, 125), new("Death_02", 195, 790, 165, 125),
        new("Death_03", 360, 790, 170, 125), new("Death_04", 520, 805, 175, 110)
    };

    [MenuItem("Tools/2D Dungeon/Setup Eagle Knight Boss")]
    public static void Setup()
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(Source) as TextureImporter;
        if (sourceImporter == null) { Debug.LogError("Eagle Knight source missing: " + Source); return; }
        bool wasReadable = sourceImporter.isReadable;
        sourceImporter.isReadable = true; sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
        sourceImporter.SaveAndReimport();
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        EnsureFolder("Assets", "Resources"); EnsureFolder("Assets/Resources", "EagleKnight");
        foreach (Frame frame in Frames) WriteFrame(source, frame);
        AssetDatabase.Refresh();
        foreach (Frame frame in Frames) ConfigureSprite($"{Output}/{frame.Name}.png");
        CreatePrefab();
        sourceImporter = AssetImporter.GetAtPath(Source) as TextureImporter;
        sourceImporter.isReadable = wasReadable; sourceImporter.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log($"EAGLE_KNIGHT_SETUP_PASS: {Frames.Length} animation frames, spear, hitboxes and prefab created.");
    }

    private static void WriteFrame(Texture2D source, Frame frame)
    {
        const int canvasSize = 256;
        int width = frame.Rect.width;
        int height = frame.Rect.height;
        Color32[] crop = new Color32[width * height];
        bool[] background = new bool[crop.Length];

        for (int localY = 0; localY < height; localY++)
        for (int localX = 0; localX < width; localX++)
        {
            int sourceX = frame.Rect.x + localX;
            int sourceY = source.height - frame.Rect.y - height + localY;
            int index = localY * width + localX;
            if (sourceX < 0 || sourceX >= source.width || sourceY < 0 || sourceY >= source.height)
            {
                background[index] = true;
                continue;
            }
            crop[index] = source.GetPixel(sourceX, sourceY);
        }

        // The Eagle Knight uses near-black pixels in its armor and cape. Use the
        // frame edge color as the key so those dark body pixels are preserved.
        Color32 backgroundColor = crop[0];
        var pending = new Queue<int>();
        for (int x = 0; x < width; x++)
        {
            QueueBackground(crop, background, pending, backgroundColor, x);
            QueueBackground(crop, background, pending, backgroundColor, (height - 1) * width + x);
        }
        for (int y = 1; y < height - 1; y++)
        {
            QueueBackground(crop, background, pending, backgroundColor, y * width);
            QueueBackground(crop, background, pending, backgroundColor, y * width + width - 1);
        }

        int[] floodX = { -1, 1, 0, 0 };
        int[] floodY = { 0, 0, -1, 1 };
        while (pending.Count > 0)
        {
            int index = pending.Dequeue();
            int x = index % width;
            int y = index / width;
            for (int direction = 0; direction < floodX.Length; direction++)
            {
                int nextX = x + floodX[direction];
                int nextY = y + floodY[direction];
                if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height) continue;
                QueueBackground(crop, background, pending, backgroundColor, nextY * width + nextX);
            }
        }

        var visible = new List<(int x, int y, Color32 color)>();
        int minX = width, maxX = -1, minY = height, maxY = -1;
        for (int index = 0; index < background.Length; index++)
        {
            if (background[index]) continue;
            int x = index % width;
            int y = index / width;
            visible.Add((x, y, crop[index]));
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }
        if (visible.Count == 0) return;
        int contentWidth = maxX - minX + 1;
        int contentHeight = maxY - minY + 1;
        float scale = Mathf.Min(1f, Mathf.Min(236f / contentWidth, 236f / contentHeight));
        int drawWidth = Mathf.RoundToInt(contentWidth * scale);
        int offsetX = (canvasSize - drawWidth) / 2;
        int offsetY = 10;
        Color32[] output = new Color32[canvasSize * canvasSize];
        foreach (var pixel in visible)
        {
            int x = offsetX + Mathf.RoundToInt((pixel.x - minX) * scale);
            int y = offsetY + Mathf.RoundToInt((pixel.y - minY) * scale);
            if (x >= 0 && x < canvasSize && y >= 0 && y < canvasSize) output[y * canvasSize + x] = pixel.color;
        }
        var texture = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
        texture.SetPixels32(output); texture.Apply(); File.WriteAllBytes($"{Output}/{frame.Name}.png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static bool IsBackground(Color32 color, Color32 backgroundColor)
    {
        if (color.a < 8) return true;
        const int tolerance = 4;
        return Mathf.Abs((int)color.r - backgroundColor.r) <= tolerance &&
            Mathf.Abs((int)color.g - backgroundColor.g) <= tolerance &&
            Mathf.Abs((int)color.b - backgroundColor.b) <= tolerance;
    }

    private static void QueueBackground(Color32[] pixels, bool[] background, Queue<int> pending,
        Color32 backgroundColor, int index)
    {
        if (index < 0 || index >= pixels.Length || background[index] ||
            !IsBackground(pixels[index], backgroundColor)) return;
        background[index] = true;
        pending.Enqueue(index);
    }

    private static void ConfigureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
        importer.spritePivot = new Vector2(0.5f, 0.05f); importer.SaveAndReimport();
    }

    private static void CreatePrefab()
    {
        GameObject root = new("EagleKnightBoss", typeof(SpriteRenderer), typeof(Animator), typeof(Rigidbody2D),
            typeof(CapsuleCollider2D), typeof(Damageable), typeof(BossHealth), typeof(BossMovement),
            typeof(EagleKnightAnimator), typeof(EagleKnightBossCombat));
        try
        {
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>(); renderer.sortingOrder = 8;
            renderer.sprite = Load("Idle_01");
            CapsuleCollider2D bodyCollider = root.GetComponent<CapsuleCollider2D>();
            bodyCollider.size = new Vector2(1.15f, 1.65f); bodyCollider.offset = new Vector2(0f, 0.8f);
            Rigidbody2D body = root.GetComponent<Rigidbody2D>(); body.gravityScale = 0f; body.freezeRotation = true;
            CreateHitbox(root.transform, "SlashHitbox", new Vector2(2.6f, 1.5f));
            CreateHitbox(root.transform, "ChargeHitbox", new Vector2(1.4f, 1.4f));
            CreateHitbox(root.transform, "SkillHitbox", new Vector2(6.4f, 6.4f));
            SerializedObject animator = new(root.GetComponent<EagleKnightAnimator>());
            SetSprites(animator, "idle", "Idle", 2); SetSprites(animator, "walk", "Walk", 4);
            SetSprites(animator, "hit", "Hit", 2); SetSprites(animator, "slash", "Slash", 4);
            SetSprites(animator, "charge", "Charge", 4); SetSprites(animator, "spearThrow", "Throw", 4);
            SetSprites(animator, "skill", "Skill", 3); SetSprites(animator, "death", "Death", 4);
            animator.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject combat = new(root.GetComponent<EagleKnightBossCombat>());
            combat.FindProperty("spearSprite").objectReferenceValue = Load("Throw_04");
            combat.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { if (root != null) Object.DestroyImmediate(root); }
    }

    private static void SetSprites(SerializedObject serialized, string propertyName, string prefix, int count)
    {
        SerializedProperty array = serialized.FindProperty(propertyName); array.arraySize = count;
        for (int i = 0; i < count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = Load($"{prefix}_{i + 1:00}");
    }
    private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Output}/{name}.png");
    private static void CreateHitbox(Transform parent, string name, Vector2 size)
    {
        GameObject child = new(name, typeof(BoxCollider2D)); child.transform.SetParent(parent, false);
        BoxCollider2D collider = child.GetComponent<BoxCollider2D>(); collider.size = size;
        collider.isTrigger = true; collider.enabled = false;
    }
    private static void EnsureFolder(string parent, string child)
    { string path = parent + "/" + child; if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child); }
}
#endif
