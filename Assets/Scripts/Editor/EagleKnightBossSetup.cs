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
        new("Idle_01", 20, 45, 185, 235), new("Idle_02", 210, 45, 190, 235),
        new("Walk_01", 465, 45, 185, 240), new("Walk_02", 645, 45, 185, 240),
        new("Walk_03", 825, 45, 185, 240), new("Walk_04", 1000, 45, 185, 240),
        new("Hit_01", 1240, 55, 205, 230), new("Hit_02", 1450, 55, 235, 230),
        new("Slash_01", 20, 330, 195, 200), new("Slash_02", 215, 330, 210, 200),
        new("Slash_03", 425, 330, 205, 200), new("Slash_04", 625, 330, 205, 200),
        new("Charge_01", 835, 330, 205, 200), new("Charge_02", 1035, 330, 205, 200),
        new("Charge_03", 1235, 330, 210, 200), new("Charge_04", 1445, 330, 240, 200),
        new("Throw_01", 20, 550, 195, 205), new("Throw_02", 215, 550, 205, 205),
        new("Throw_03", 420, 550, 205, 205), new("Throw_04", 625, 550, 210, 205),
        // Start below the montage caption so no Korean label is baked into the skill frame.
        new("Skill_01", 835, 580, 250, 190), new("Skill_02", 1085, 535, 255, 235),
        new("Skill_03", 1340, 535, 345, 235),
        new("Death_01", 20, 775, 175, 150), new("Death_02", 195, 775, 180, 150),
        new("Death_03", 375, 775, 195, 150), new("Death_04", 570, 775, 205, 150)
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
        Color32[] output = new Color32[canvasSize * canvasSize];
        var visible = new List<(int x, int y, Color32 color)>();
        for (int localY = 0; localY < frame.Rect.height; localY++)
        for (int localX = 0; localX < frame.Rect.width; localX++)
        {
            int sourceX = frame.Rect.x + localX;
            int sourceY = source.height - frame.Rect.y - frame.Rect.height + localY;
            if (sourceX < 0 || sourceX >= source.width || sourceY < 0 || sourceY >= source.height) continue;
            Color32 color = source.GetPixel(sourceX, sourceY);
            if (color.r <= 18 && color.g <= 18 && color.b <= 18) continue;
            visible.Add((localX, localY, new Color32(color.r, color.g, color.b, color.a)));
        }
        if (visible.Count == 0) return;
        int minX = int.MaxValue, maxX = 0, minY = int.MaxValue, maxY = 0;
        foreach (var pixel in visible)
        { minX = Mathf.Min(minX, pixel.x); maxX = Mathf.Max(maxX, pixel.x); minY = Mathf.Min(minY, pixel.y); maxY = Mathf.Max(maxY, pixel.y); }
        int contentWidth = maxX - minX + 1, contentHeight = maxY - minY + 1;
        float scale = Mathf.Min(1f, Mathf.Min(236f / contentWidth, 236f / contentHeight));
        int drawWidth = Mathf.RoundToInt(contentWidth * scale);
        int offsetX = (canvasSize - drawWidth) / 2;
        int offsetY = 10;
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
