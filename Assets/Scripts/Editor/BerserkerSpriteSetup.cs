#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BerserkerSpriteSetup
{
    private const string Source = "Assets/Sprites/Berserker.png";
    private const string Output = "Assets/Resources/Berserker";
    private readonly struct Frame
    {
        public readonly string Name; public readonly RectInt Rect;
        public Frame(string name, int x, int top, int width, int height)
        { Name = name; Rect = new RectInt(x, top, width, height); }
    }
    private static readonly Frame[] Frames =
    {
        new("Idle_01",270,55,145,135), new("Idle_02",420,55,145,135), new("Idle_03",570,55,150,135),
        new("Walk_01",800,55,145,135), new("Walk_02",945,55,145,135), new("Walk_03",1095,55,180,135),
        new("Walk_04",1245,55,180,135), new("Walk_05",1420,55,155,135),
        new("Attack_01",270,235,150,140), new("Attack_02",420,235,150,140),
        new("Attack_03",570,235,130,140), new("Attack_04",720,235,170,140),
        new("Hit_01",930,400,150,145), new("Hit_02",1080,400,150,145),
        new("Hit_03",1230,400,150,145), new("Hit_04",1380,400,170,145),
        new("Death_01",270,570,150,135), new("Death_02",420,570,150,135),
        new("Death_03",570,570,150,135), new("Death_04",720,570,150,135),
        new("Death_05",870,570,150,135), new("Death_06",1020,570,150,135), new("Death_07",1170,570,170,135)
    };

    [MenuItem("Tools/2D Dungeon/Setup Berserker Sprite")]
    public static void Setup()
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(Source) as TextureImporter;
        if (sourceImporter == null) { Debug.LogError("Berserker source missing: " + Source); return; }
        bool readable = sourceImporter.isReadable;
        sourceImporter.isReadable = true; sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
        sourceImporter.SaveAndReimport();
        EnsureFolder("Assets", "Resources"); EnsureFolder("Assets/Resources", "Berserker");
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        foreach (Frame frame in Frames) WriteFrame(source, frame);
        AssetDatabase.Refresh();
        foreach (Frame frame in Frames) ConfigureSprite($"{Output}/{frame.Name}.png");
        CreatePrefab();
        sourceImporter = AssetImporter.GetAtPath(Source) as TextureImporter;
        sourceImporter.isReadable = readable; sourceImporter.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log($"BERSERKER_SPRITE_SETUP_PASS: {Frames.Length} frames and prefab created.");
    }

    private static void WriteFrame(Texture2D source, Frame frame)
    {
        const int canvas = 256;
        int width = frame.Rect.width;
        int height = frame.Rect.height;
        Color32[] crop = new Color32[width * height];
        bool[] background = new bool[crop.Length];
        var pending = new System.Collections.Generic.Queue<Vector2Int>();

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int sourceX = frame.Rect.x + x;
            int sourceY = source.height - frame.Rect.y - height + y;
            int index = y * width + x;
            if (sourceX < 0 || sourceX >= source.width || sourceY < 0 || sourceY >= source.height)
            {
                background[index] = true;
                continue;
            }
            crop[index] = source.GetPixel(sourceX, sourceY);
        }

        Color32 backgroundColor = crop[0];
        bool IsBackground(Color32 color)
        {
            if (color.a < 8) return true;
            return Mathf.Abs((int)color.r - backgroundColor.r) <= 2 &&
                Mathf.Abs((int)color.g - backgroundColor.g) <= 2 &&
                Mathf.Abs((int)color.b - backgroundColor.b) <= 2;
        }

        void Seed(int x, int y)
        {
            int index = y * width + x;
            if (background[index] || !IsBackground(crop[index])) return;
            background[index] = true;
            pending.Enqueue(new Vector2Int(x, y));
        }

        for (int x = 0; x < width; x++)
        {
            Seed(x, 0);
            Seed(x, height - 1);
        }
        for (int y = 1; y < height - 1; y++)
        {
            Seed(0, y);
            Seed(width - 1, y);
        }

        int[] floodX = { -1, 1, 0, 0 };
        int[] floodY = { 0, 0, -1, 1 };
        while (pending.Count > 0)
        {
            Vector2Int point = pending.Dequeue();
            for (int direction = 0; direction < floodX.Length; direction++)
            {
                int nextX = point.x + floodX[direction];
                int nextY = point.y + floodY[direction];
                if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height) continue;
                Seed(nextX, nextY);
            }
        }

        var kept = new System.Collections.Generic.List<(int x, int y, Color32 c)>();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;
            if (!background[index]) kept.Add((x, y, crop[index]));
        }

        if (kept.Count == 0) return;
        if (frame.Name is "Walk_03" or "Walk_04")
            RemoveEdgeFragments(kept, width, height, false);
        if (kept.Count == 0) return;

        bool[] retained = new bool[crop.Length];
        int minX = width;
        int maxX = -1;
        int minY = height;
        int maxY = -1;
        foreach (var pixel in kept)
        {
            retained[pixel.y * width + pixel.x] = true;
            minX = Mathf.Min(minX, pixel.x);
            maxX = Mathf.Max(maxX, pixel.x);
            minY = Mathf.Min(minY, pixel.y);
            maxY = Mathf.Max(maxY, pixel.y);
        }

        int contentWidth = maxX - minX + 1;
        int contentHeight = maxY - minY + 1;
        float scale = Mathf.Min(1f, Mathf.Min(236f / contentWidth, 236f / contentHeight));
        int drawWidth = Mathf.Max(1, Mathf.RoundToInt(contentWidth * scale));
        int drawHeight = Mathf.Max(1, Mathf.RoundToInt(contentHeight * scale));
        int offsetX = (canvas - drawWidth) / 2;
        int offsetY = 10;
        Color32[] output = new Color32[canvas * canvas];

        for (int y = 0; y < drawHeight; y++)
        for (int x = 0; x < drawWidth; x++)
        {
            int sourceX = minX + Mathf.Clamp(Mathf.FloorToInt(x / scale), 0, contentWidth - 1);
            int sourceY = minY + Mathf.Clamp(Mathf.FloorToInt(y / scale), 0, contentHeight - 1);
            int sourceIndex = sourceY * width + sourceX;
            if (retained[sourceIndex])
                output[(offsetY + y) * canvas + offsetX + x] = crop[sourceIndex];
        }

        Texture2D texture = new(canvas, canvas, TextureFormat.RGBA32, false);
        texture.SetPixels32(output);
        texture.Apply();
        File.WriteAllBytes($"{Output}/{frame.Name}.png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }
    private static void RemoveEdgeFragments(System.Collections.Generic.List<(int x, int y, Color32 c)> pixels, int width, int height, bool preserveRight)
    {
        if (pixels.Count == 0) return;

        var byPosition = new System.Collections.Generic.Dictionary<int, int>();
        for (int i = 0; i < pixels.Count; i++)
            byPosition[pixels[i].y * width + pixels[i].x] = i;

        var visited = new bool[pixels.Count];
        var components = new System.Collections.Generic.List<System.Collections.Generic.List<int>>();
        int[] offsets = { -1, 0, 1 };

        for (int start = 0; start < pixels.Count; start++)
        {
            if (visited[start]) continue;

            var component = new System.Collections.Generic.List<int>();
            var queue = new System.Collections.Generic.Queue<int>();
            visited[start] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                component.Add(current);
                var pixel = pixels[current];

                foreach (int dy in offsets)
                foreach (int dx in offsets)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (!byPosition.TryGetValue((pixel.y + dy) * width + pixel.x + dx, out int next)) continue;
                    if (visited[next]) continue;
                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }

            components.Add(component);
        }

        var remove = new System.Collections.Generic.HashSet<int>();
        foreach (var component in components)
        {
            int minX = width;
            int maxX = -1;
            foreach (int index in component)
            {
                minX = Mathf.Min(minX, pixels[index].x);
                maxX = Mathf.Max(maxX, pixels[index].x);
            }

            bool leftEdgeFragment = minX <= 1 && maxX <= Mathf.Max(12, width / 4);
            bool rightEdgeFragment = !preserveRight && maxX >= width - 2 && minX >= width * 3 / 4;
            if (leftEdgeFragment || rightEdgeFragment)
                foreach (int index in component) remove.Add(index);
        }

        for (int i = pixels.Count - 1; i >= 0; i--)
            if (remove.Contains(i)) pixels.RemoveAt(i);
    }
    private static void ConfigureSprite(string path)
    {
        TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100f;
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
        importer.spritePivot=new Vector2(0.5f,0.05f);importer.SaveAndReimport();
    }
    private static void CreatePrefab()
    {
        GameObject root=new("Berserker",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(BoxCollider2D),typeof(Damageable),typeof(EnemyAI),typeof(BerserkerVisualAnimator));
        try
        {
            root.GetComponent<SpriteRenderer>().sprite=Load("Idle_01"); SerializedObject animator=new(root.GetComponent<BerserkerVisualAnimator>());
            Set(animator,"idle","Idle",3);Set(animator,"walk","Walk",5);Set(animator,"attack","Attack",4);Set(animator,"hit","Hit",4);Set(animator,"death","Death",7);
            animator.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,$"{Output}/Berserker.prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }
    private static void Set(SerializedObject target,string property,string prefix,int count)
    {SerializedProperty array=target.FindProperty(property);array.arraySize=count;for(int i=0;i<count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=Load($"{prefix}_{i+1:00}");}
    private static Sprite Load(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>($"{Output}/{name}.png");
    private static void EnsureFolder(string parent,string child){if(!AssetDatabase.IsValidFolder(parent+"/"+child))AssetDatabase.CreateFolder(parent,child);}
}
#endif
