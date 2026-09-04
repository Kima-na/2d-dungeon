#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SkeletonSpriteSetup
{
    private const string Source = "Assets/Sprites/skeleton.png";
    private const string Output = "Assets/Resources/Skeleton";
    private readonly struct Frame
    {
        public readonly string Name; public readonly RectInt Rect;
        public Frame(string name, int x, int top, int width, int height)
        { Name = name; Rect = new RectInt(x, top, width, height); }
    }
    private static readonly Frame[] Frames =
    {
        new("Idle_01", 35, 45, 140, 150), new("Idle_02", 175, 45, 145, 150),
        new("Walk_01", 395, 45, 135, 150), new("Walk_02", 520, 45, 135, 150),
        new("Walk_03", 645, 45, 135, 150), new("Walk_04", 770, 45, 135, 150), new("Walk_05", 895, 45, 140, 150),
        new("Hit_01", 1080, 45, 145, 150), new("Hit_02", 1215, 45, 145, 150),
        new("Hit_03", 1350, 45, 150, 150), new("Hit_04", 1490, 45, 160, 150),
        new("Attack_01", 35, 245, 145, 140), new("Attack_02", 180, 245, 145, 140),
        new("Attack_03", 325, 215, 170, 170), new("Attack_04", 520, 215, 135, 170),
        new("Death_01", 685, 565, 150, 168), new("Death_02", 825, 565, 155, 168),
        new("Death_03", 970, 565, 165, 168), new("Death_04", 1125, 565, 170, 168),
        new("Death_05", 1285, 565, 170, 168), new("Death_06", 1440, 565, 180, 168),
        new("Revive_01", 30, 770, 170, 270), new("Revive_02", 245, 770, 170, 270),
        new("Revive_03", 450, 770, 175, 270), new("Revive_04", 650, 770, 175, 270),
        new("Revive_05", 840, 770, 175, 270), new("Revive_06", 1040, 770, 180, 270),
        new("Revive_07", 1240, 770, 180, 270)
    };

    [MenuItem("Tools/2D Dungeon/Setup Skeleton Sprite")]
    public static void Setup()
    {
        TextureImporter importer = AssetImporter.GetAtPath(Source) as TextureImporter;
        if (importer == null) { Debug.LogError("Skeleton source missing: " + Source); return; }
        bool readable = importer.isReadable; importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        EnsureFolder("Assets", "Resources"); EnsureFolder("Assets/Resources", "Skeleton");
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        foreach (Frame frame in Frames) WriteFrame(source, frame);
        AssetDatabase.Refresh(); foreach (Frame frame in Frames) ConfigureSprite($"{Output}/{frame.Name}.png");
        CreatePrefab(); importer = AssetImporter.GetAtPath(Source) as TextureImporter;
        importer.isReadable = readable; importer.SaveAndReimport(); AssetDatabase.SaveAssets();
        Debug.Log($"SKELETON_SPRITE_SETUP_PASS: {Frames.Length} frames and Skeleton prefab created.");
    }

    private static void WriteFrame(Texture2D source, Frame frame)
    {
        const int canvas = 256;
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

        var pixels = new List<(int x, int y, Color32 c)>();
        for (int index = 0; index < crop.Length; index++)
        {
            if (background[index]) continue;
            pixels.Add((index % width, index / width, crop[index]));
        }

        if (frame.Name.StartsWith("Revive_")) pixels = KeepLargestComponent(pixels, width);
        if (pixels.Count == 0) return;

        int minX = int.MaxValue;
        int maxX = 0;
        int minY = int.MaxValue;
        int maxY = 0;
        foreach (var pixel in pixels)
        {
            minX = Mathf.Min(minX, pixel.x);
            maxX = Mathf.Max(maxX, pixel.x);
            minY = Mathf.Min(minY, pixel.y);
            maxY = Mathf.Max(maxY, pixel.y);
        }

        int contentWidth = maxX - minX + 1;
        int contentHeight = maxY - minY + 1;
        float scale = Mathf.Min(1f, Mathf.Min(236f / contentWidth, 236f / contentHeight));
        int offsetX = (canvas - Mathf.RoundToInt(contentWidth * scale)) / 2;
        int offsetY = 10;
        Color32[] output = new Color32[canvas * canvas];

        foreach (var pixel in pixels)
        {
            int x = offsetX + Mathf.RoundToInt((pixel.x - minX) * scale);
            int y = offsetY + Mathf.RoundToInt((pixel.y - minY) * scale);
            if (x >= 0 && x < canvas && y >= 0 && y < canvas)
                output[y * canvas + x] = pixel.c;
        }

        Texture2D texture = new(canvas, canvas, TextureFormat.RGBA32, false);
        texture.SetPixels32(output); texture.Apply(); File.WriteAllBytes($"{Output}/{frame.Name}.png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static bool IsBackground(Color32 color, Color32 backgroundColor)
    {
        if (color.a < 8) return true;
        return Mathf.Abs((int)color.r - backgroundColor.r) <= 4 &&
            Mathf.Abs((int)color.g - backgroundColor.g) <= 4 &&
            Mathf.Abs((int)color.b - backgroundColor.b) <= 4;
    }

    private static void QueueBackground(Color32[] pixels, bool[] background, Queue<int> pending, Color32 backgroundColor, int index)
    {
        if (index < 0 || index >= pixels.Length || background[index] ||
            !IsBackground(pixels[index], backgroundColor)) return;
        background[index] = true;
        pending.Enqueue(index);
    }

    private static List<(int x, int y, Color32 c)> KeepLargestComponent(
        List<(int x, int y, Color32 c)> pixels, int width)
    {
        var byIndex = new Dictionary<int, (int x, int y, Color32 c)>();
        foreach (var pixel in pixels) byIndex[pixel.y * width + pixel.x] = pixel;
        var visited = new HashSet<int>(); var largest = new List<int>();
        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 }, dy = { 0, 0, -1, 1, -1, 1, -1, 1 };
        foreach (int start in byIndex.Keys)
        {
            if (!visited.Add(start)) continue;
            var component = new List<int>(); var queue = new Queue<int>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int index = queue.Dequeue(); component.Add(index); int x = index % width, y = index / width;
                for (int i = 0; i < dx.Length; i++)
                {
                    int next = (y + dy[i]) * width + x + dx[i];
                    if (x + dx[i] < 0 || x + dx[i] >= width || !byIndex.ContainsKey(next) || !visited.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
            if (component.Count > largest.Count) largest = component;
        }
        int minX = int.MaxValue, maxX = 0, minY = int.MaxValue, maxY = 0;
        foreach (int index in largest)
        { int x = index % width, y = index / width; minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
          minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        var result = new List<(int x, int y, Color32 c)>();
        foreach (var pixel in pixels)
            if (pixel.x >= minX - 12 && pixel.x <= maxX + 12 && pixel.y >= minY - 12 && pixel.y <= maxY + 12)
                result.Add(pixel);
        return result;
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
        GameObject root = new("Skeleton", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D),
            typeof(Damageable), typeof(EnemyAI), typeof(SkeletonVisualAnimator));
        try
        {
            root.GetComponent<SpriteRenderer>().sprite = Load("Idle_01");
            SerializedObject animator = new(root.GetComponent<SkeletonVisualAnimator>());
            Set(animator, "idle", "Idle", 2); Set(animator, "walk", "Walk", 5); Set(animator, "attack", "Attack", 4);
            Set(animator, "hit", "Hit", 4); Set(animator, "death", "Death", 6); Set(animator, "revive", "Revive", 7);
            animator.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, $"{Output}/Skeleton.prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }
    private static void Set(SerializedObject target, string property, string prefix, int count)
    { SerializedProperty array = target.FindProperty(property); array.arraySize = count;
      for (int i = 0; i < count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = Load($"{prefix}_{i + 1:00}"); }
    private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Output}/{name}.png");
    private static void EnsureFolder(string parent, string child)
    { if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child); }
}
#endif
