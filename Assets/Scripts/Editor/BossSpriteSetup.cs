#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class BossSpriteSetup
{
    private const string SourcePath = "Assets/Sprites/boss.png";
    private const string AttackSourcePath = "Assets/Sprites/boss attack.png";
    private const string DeathSourcePath = "Assets/Sprites/boss died.png";
    private const string ProcessedPath = "Assets/Sprites/Enemies/Boss/boss_transparent.png";
    private const string AnimationFolder = "Assets/Animations/Enemies/Boss";
    private const string ControllerPath = AnimationFolder + "/EasyBoss.controller";
    private const string PrefabPath = "Assets/Prefabs/Enemies/EasyBoss.prefab";
    private const string DatabasePath = "Assets/Resources/BossVisualDatabase.asset";
    private const string ProcessedAttackPath = "Assets/Sprites/Enemies/Boss/boss_attack_transparent.png";
    private const string ProcessedDeathPath = "Assets/Sprites/Enemies/Boss/boss_death_transparent.png";

    private readonly struct AttackFrame
    {
        public readonly string Name;
        public readonly RectInt Rect;

        public AttackFrame(string name, int x, int y, int width, int height)
        {
            Name = name;
            Rect = new RectInt(x, y, width, height);
        }
    }

    private static readonly AttackFrame[] AttackFrames =
    {
        new("boss attack_2", 44, 727, 167, 251),
        new("boss attack_3", 238, 743, 214, 236),
        new("boss attack_7", 0, 535, 180, 181),
        new("boss attack_8", 180, 527, 193, 195),
        new("boss attack_4", 392, 531, 187, 271),
        new("boss attack_5", 618, 537, 173, 256),
        new("boss attack_11", 10, 303, 172, 177),
        new("boss attack_12", 187, 309, 217, 181),
        new("boss attack_15", 14, 0, 227, 256)
    };

    [InitializeOnLoadMethod]
    private static void ScheduleSetup() => EditorApplication.delayCall += Setup;

    [MenuItem("Tools/2D Dungeon/Setup Easy Boss")]
    public static void Setup()
    {
        if (!File.Exists(SourcePath)) return;
        EnsureFolder("Assets/Sprites/Enemies", "Boss");
        EnsureFolder("Assets/Animations/Enemies", "Boss");
        EnsureFolder("Assets/Prefabs", "Enemies");
        EnsureFolder("Assets", "Resources");
        CreateTransparentCopy();
        ConfigureAndSlice();
        CreateAttackCopy();
        ConfigureAttackEffects();
        CreateDeathCopy();
        ConfigureDeathSheet();

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(ProcessedPath).OfType<Sprite>()
            .OrderBy(sprite => int.Parse(sprite.name.Split('_').Last())).ToArray();
        if (sprites.Length != 24) return;

        string[] directions = { "Down", "Up", "Left", "Right" };
        AnimationClip[] idle = new AnimationClip[4];
        AnimationClip[] walk = new AnimationClip[4];
        for (int row = 0; row < 4; row++)
        {
            Sprite[] sourceFrames = sprites.Skip(row * 6).Take(6).ToArray();
            // The sheet is laid out from one extreme pose to the other rather
            // than as a seamless loop. Ping-pong the middle poses so the last
            // frame never snaps directly back to the first.
            int[] walkOrder = { 0, 1, 2, 3, 4, 5, 4, 3, 2, 1 };
            Sprite[] frames = walkOrder.Select(index => sourceFrames[index]).ToArray();
            idle[row] = CreateClip($"{AnimationFolder}/Boss_Idle_{directions[row]}.anim",
                $"Boss_Idle_{directions[row]}", new[] { frames[0] }, true);
            walk[row] = CreateClip($"{AnimationFolder}/Boss_Walk_{directions[row]}.anim",
                $"Boss_Walk_{directions[row]}", frames, true);
        }

        Sprite[] attackSprites = AssetDatabase.LoadAllAssetsAtPath(ProcessedAttackPath).OfType<Sprite>().ToArray();
        AnimationClip[] attacks =
        {
            CreateClip($"{AnimationFolder}/Boss_Attack1.anim", "Boss_Attack1", Select(attackSprites, "boss attack_2", "boss attack_3"), false),
            CreateClip($"{AnimationFolder}/Boss_Attack2.anim", "Boss_Attack2", Select(attackSprites, "boss attack_7", "boss attack_8", "boss attack_4", "boss attack_5"), false),
            CreateClip($"{AnimationFolder}/Boss_Attack3.anim", "Boss_Attack3", Select(attackSprites, "boss attack_11", "boss attack_12"), false),
            CreateClip($"{AnimationFolder}/Boss_Attack4.anim", "Boss_Attack4", Select(attackSprites, "boss attack_15"), false)
        };
        AnimationClip hit = CreateHitClip(sprites[0]);
        Sprite[] deathSprites = AssetDatabase.LoadAllAssetsAtPath(ProcessedDeathPath).OfType<Sprite>()
            .OrderBy(sprite => int.Parse(sprite.name.Split('_').Last())).ToArray();
        if (deathSprites.Length != 16) return;
        AnimationClip[] deaths = CreateDeathClips(deathSprites, directions);
        AnimatorController controller = CreateController(idle, walk, attacks, hit, deaths, directions);
        GameObject prefab = CreatePrefab(sprites[0], controller);
        BossVisualDatabase database = AssetDatabase.LoadAssetAtPath<BossVisualDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<BossVisualDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }
        database.easyBossPrefab = prefab;
        database.darkShockwave = FindSprite(attackSprites, "Effect_DarkShockwave");
        database.groundWarning = FindSprite(attackSprites, "Effect_GroundWarning");
        database.groundImpact = FindSprite(attackSprites, "Effect_GroundImpact");
        database.spinSlash = FindSprite(attackSprites, "Effect_SpinSlash");
        database.summonCircle = FindSprite(attackSprites, "Effect_SummonCircle");
        database.shadowMinion = FindSprite(attackSprites, "Effect_ShadowMinion");
        database.bossBarFrame = AssetDatabase.LoadAllAssetsAtPath("Assets/UI/ui/bar_enemy.png").OfType<Sprite>().FirstOrDefault();
        database.bossBarFill = AssetDatabase.LoadAllAssetsAtPath("Assets/UI/ui/bar_hp.png").OfType<Sprite>().FirstOrDefault();
        database.playerManaFill = AssetDatabase.LoadAllAssetsAtPath("Assets/UI/ui/bar_mp.png").OfType<Sprite>().FirstOrDefault();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        ValidateDirectionalFrames(idle, walk);
        Debug.Log("Easy boss setup complete. Attack frames were cleaned and the death animation was consolidated and normalized.");
    }

    private static void ValidateDirectionalFrames(AnimationClip[] idle, AnimationClip[] walk)
    {
        string[] directions = { "Down", "Up", "Left", "Right" };
        for (int direction = 0; direction < 4; direction++)
        {
            int expectedFirstFrame = direction * 6;
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(walk[direction],
                AnimationUtility.GetObjectReferenceCurveBindings(walk[direction]).First());
            Sprite first = keys.Length > 0 ? keys[0].value as Sprite : null;
            Debug.Assert(first != null && first.name == $"Boss_{expectedFirstFrame}",
                $"Easy boss {directions[direction]} clip references the wrong sprite row.");
            Debug.Assert(idle[direction] != null && keys.Length == 10,
                $"Easy boss {directions[direction]} animation was not rebuilt completely.");
        }
    }

    private static void CreateTransparentCopy()
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(SourcePath) as TextureImporter;
        sourceImporter.isReadable = true;
        sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
        sourceImporter.maxTextureSize = 2048;
        sourceImporter.SaveAndReimport();
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
        Color32[] pixels = source.GetPixels32();
        int width = source.width;
        int height = source.height;
        bool[] visited = new bool[pixels.Length];
        Queue<int> queue = new();
        for (int x = 0; x < width; x++) { Enqueue(x, 0); Enqueue(x, height - 1); }
        for (int y = 0; y < height; y++) { Enqueue(0, y); Enqueue(width - 1, y); }
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            Color32 color = pixels[index];
            if (!IsCheckerBackground(color)) continue;
            color.a = 0;
            pixels[index] = color;
            int x = index % width;
            int y = index / width;
            if (x > 0) Enqueue(x - 1, y);
            if (x + 1 < width) Enqueue(x + 1, y);
            if (y > 0) Enqueue(x, y - 1);
            if (y + 1 < height) Enqueue(x, y + 1);
            // Anti-aliased checker pixels often touch only diagonally. Including
            // diagonal neighbours removes those isolated white specks as well.
            if (x > 0 && y > 0) Enqueue(x - 1, y - 1);
            if (x + 1 < width && y > 0) Enqueue(x + 1, y - 1);
            if (x > 0 && y + 1 < height) Enqueue(x - 1, y + 1);
            if (x + 1 < width && y + 1 < height) Enqueue(x + 1, y + 1);
        }

        RemoveSmallFrameComponents(pixels, width);

        Texture2D output = new(width, height, TextureFormat.RGBA32, false);
        output.SetPixels32(pixels);
        output.Apply();
        File.WriteAllBytes(ProcessedPath, output.EncodeToPNG());
        Object.DestroyImmediate(output);
        AssetDatabase.ImportAsset(ProcessedPath, ImportAssetOptions.ForceSynchronousImport);

        void Enqueue(int x, int y)
        {
            int index = y * width + x;
            if (visited[index]) return;
            visited[index] = true;
            if (IsCheckerBackground(pixels[index])) queue.Enqueue(index);
        }
    }

    private static void RemoveSmallAttackComponents(Color32[] pixels, int textureWidth, int textureHeight)
    {
        int[] neighbourX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] neighbourY = { -1, -1, -1, 0, 0, 1, 1, 1 };

        foreach (AttackFrame frame in AttackFrames)
        {
            RectInt rect = frame.Rect;
            HashSet<int> remaining = new();
            for (int y = 0; y < rect.height; y++)
            for (int x = 0; x < rect.width; x++)
            {
                int sourceX = rect.x + x;
                int sourceY = rect.y + y;
                if (sourceX < 0 || sourceX >= textureWidth ||
                    sourceY < 0 || sourceY >= textureHeight) continue;
                int index = sourceY * textureWidth + sourceX;
                if (pixels[index].a >= 8) remaining.Add(index);
            }

            var components = new List<List<int>>();
            while (remaining.Count > 0)
            {
                int start = remaining.First();
                remaining.Remove(start);
                var component = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    component.Add(current);
                    int x = current % textureWidth;
                    int y = current / textureWidth;
                    for (int direction = 0; direction < neighbourX.Length; direction++)
                    {
                        int nextX = x + neighbourX[direction];
                        int nextY = y + neighbourY[direction];
                        if (nextX < rect.x || nextX >= rect.xMax ||
                            nextY < rect.y || nextY >= rect.yMax) continue;
                        int next = nextY * textureWidth + nextX;
                        if (remaining.Remove(next)) queue.Enqueue(next);
                    }
                }
                components.Add(component);
            }

            List<int> body = components.OrderByDescending(component => component.Count).FirstOrDefault();
            if (body == null) continue;
            foreach (List<int> component in components)
            {
                if (component == body) continue;
                foreach (int index in component)
                {
                    Color32 color = pixels[index];
                    color.a = 0;
                    pixels[index] = color;
                }
            }
        }
    }

    private static void CreateAttackCopy()
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(AttackSourcePath) as TextureImporter;
        if (sourceImporter == null) return;

        bool wasReadable = sourceImporter.isReadable;
        TextureImporterCompression previousCompression = sourceImporter.textureCompression;
        sourceImporter.isReadable = true;
        sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
        sourceImporter.SaveAndReimport();

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(AttackSourcePath);
        if (source == null) return;
        Color32[] pixels = source.GetPixels32();
        RemoveSmallAttackComponents(pixels, source.width, source.height);

        Texture2D output = new(source.width, source.height, TextureFormat.RGBA32, false);
        output.SetPixels32(pixels);
        output.Apply();
        File.WriteAllBytes(ProcessedAttackPath, output.EncodeToPNG());
        Object.DestroyImmediate(output);
        AssetDatabase.ImportAsset(ProcessedAttackPath, ImportAssetOptions.ForceSynchronousImport);

        sourceImporter = AssetImporter.GetAtPath(AttackSourcePath) as TextureImporter;
        if (sourceImporter != null)
        {
            sourceImporter.isReadable = wasReadable;
            sourceImporter.textureCompression = previousCompression;
            sourceImporter.SaveAndReimport();
        }
    }

    private static void CreateDeathCopy()
    {
        if (!File.Exists(DeathSourcePath)) return;
        TextureImporter sourceImporter = AssetImporter.GetAtPath(DeathSourcePath) as TextureImporter;
        if (sourceImporter == null) return;

        bool wasReadable = sourceImporter.isReadable;
        TextureImporterCompression previousCompression = sourceImporter.textureCompression;
        sourceImporter.isReadable = true;
        sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
        sourceImporter.SaveAndReimport();

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(DeathSourcePath);
        if (source == null) return;
        Color32[] sourcePixels = source.GetPixels32();

        const int cellSize = 384;
        const int sheetSize = cellSize * 4;
        const int groundY = 20;
        int[] rowBottoms = { 654, 459, 239, 0 };
        int[] rowHeights = { 370, 195, 220, 239 };
        Color32[] outputPixels = new Color32[sheetSize * sheetSize];

        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
        {
            int sourceLeft = column * cellSize;
            int sourceBottom = rowBottoms[row];
            int sourceHeight = rowHeights[row];
            int minX = cellSize, maxX = -1, minY = sourceHeight, maxY = -1;
            for (int y = 0; y < sourceHeight; y++)
            for (int x = 0; x < cellSize; x++)
            {
                Color32 pixel = sourcePixels[(sourceBottom + y) * source.width + sourceLeft + x];
                if (pixel.a < 8) continue;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
            if (maxX < 0) continue;

            int desiredShiftX = cellSize / 2 - Mathf.RoundToInt((minX + maxX) * 0.5f);
            int shiftX = Mathf.Clamp(desiredShiftX, -minX, cellSize - 1 - maxX);
            int desiredShiftY = groundY - minY;
            int shiftY = Mathf.Clamp(desiredShiftY, -minY, cellSize - 1 - maxY);
            int outputLeft = column * cellSize;
            int outputBottom = (3 - row) * cellSize;

            for (int y = 0; y < sourceHeight; y++)
            for (int x = 0; x < cellSize; x++)
            {
                Color32 pixel = sourcePixels[(sourceBottom + y) * source.width + sourceLeft + x];
                if (pixel.a == 0) continue;
                int outputX = outputLeft + x + shiftX;
                int outputY = outputBottom + y + shiftY;
                if (outputX < outputLeft || outputX >= outputLeft + cellSize ||
                    outputY < outputBottom || outputY >= outputBottom + cellSize) continue;
                outputPixels[outputY * sheetSize + outputX] = pixel;
            }
        }

        Texture2D output = new(sheetSize, sheetSize, TextureFormat.RGBA32, false);
        output.SetPixels32(outputPixels);
        output.Apply();
        File.WriteAllBytes(ProcessedDeathPath, output.EncodeToPNG());
        Object.DestroyImmediate(output);
        AssetDatabase.ImportAsset(ProcessedDeathPath, ImportAssetOptions.ForceSynchronousImport);

        sourceImporter = AssetImporter.GetAtPath(DeathSourcePath) as TextureImporter;
        if (sourceImporter != null)
        {
            sourceImporter.isReadable = wasReadable;
            sourceImporter.textureCompression = previousCompression;
            sourceImporter.SaveAndReimport();
        }
    }

    private static bool IsCheckerBackground(Color32 color)
    {
        int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        // The supplied sheet's checkerboard ranges from near-white to light
        // gray and contains compression/anti-alias variations. It is safe to
        // broaden this only because removal is flood-filled from the image edge;
        // enclosed white armor highlights remain untouched.
        return min >= 205 && max - min <= 20;
    }

    private static void RemoveSmallFrameComponents(Color32[] pixels, int textureWidth)
    {
        const int frameStride = 242, frameWidth = 220, firstX = 61;
        int[] rowBottoms = { 635, 428, 238, 49 };
        int[] rowHeights = { 216, 207, 190, 189 };
        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 6; column++)
        {
            int left = firstX + column * frameStride + 11;
            int right = left + frameWidth;
            int bottom = rowBottoms[row], top = bottom + rowHeights[row];
            var remaining = new HashSet<int>();
            for (int y = bottom; y < top; y++)
            for (int x = left; x < right; x++)
            {
                int index = y * textureWidth + x;
                if (pixels[index].a > 20) remaining.Add(index);
            }
            var components = new List<List<int>>();
            while (remaining.Count > 0)
            {
                int start = remaining.First();
                remaining.Remove(start);
                var component = new List<int>();
                var componentQueue = new Queue<int>();
                componentQueue.Enqueue(start);
                while (componentQueue.Count > 0)
                {
                    int index = componentQueue.Dequeue();
                    component.Add(index);
                    int x = index % textureWidth, y = index / textureWidth;
                    TryAdd(index - 1, x > left);
                    TryAdd(index + 1, x + 1 < right);
                    TryAdd(index - textureWidth, y > bottom);
                    TryAdd(index + textureWidth, y + 1 < top);
                }
                components.Add(component);

                void TryAdd(int index, bool inside)
                { if (inside && remaining.Remove(index)) componentQueue.Enqueue(index); }
            }
            // Every movement pose is one connected character silhouette. Any
            // additional component belongs to a neighbouring montage cell or
            // leftover checker/anti-alias pixels, regardless of its exact size.
            List<int> body = components.OrderByDescending(value => value.Count).FirstOrDefault();
            foreach (List<int> component in components)
            {
                if (component == body) continue;
                foreach (int index in component)
                { Color32 color = pixels[index]; color.a = 0; pixels[index] = color; }
            }
        }
    }

    private static void ConfigureAndSlice()
    {
        TextureImporter importer = AssetImporter.GetAtPath(ProcessedPath) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 108f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        var frames = new SpriteMetaData[24];
        const float frameStride = 242f;
        const float frameWidth = 220f;
        const float firstX = 61f;
        // The supplied montage has slightly irregular vertical spacing. These
        // measured row bottoms keep the helmet/feet of every direction intact.
        float[] rowBottoms = { 635f, 428f, 238f, 49f };
        // Each row ends exactly where the next begins. The previous uniform
        // 216px height overlapped adjacent direction rows by 9-27px.
        float[] rowHeights = { 216f, 207f, 190f, 189f };
        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 6; column++)
        {
            int index = row * 6 + column;
            frames[index] = new SpriteMetaData
            {
                name = $"Boss_{index}",
                // Center-trim each cell to prevent the neighbouring pose's shoulder
                // from entering the lower-right corner of front-facing frames.
                rect = new Rect(firstX + column * frameStride + 11f, rowBottoms[row],
                    frameWidth, rowHeights[row]),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        importer.spritesheet = frames;
        importer.SaveAndReimport();
    }

    private static void ConfigureAttackEffects()
    {
        AssetDatabase.ImportAsset(ProcessedAttackPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(ProcessedAttackPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        List<SpriteMetaData> metadata = new();
        foreach (AttackFrame frame in AttackFrames)
        {
            metadata.Add(new SpriteMetaData
            {
                name = frame.Name,
                rect = new Rect(frame.Rect.x, frame.Rect.y, frame.Rect.width, frame.Rect.height),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }
        AddEffect(metadata, "Effect_DarkShockwave", new Rect(690f, 805f, 520f, 170f));
        AddEffect(metadata, "Effect_GroundWarning", new Rect(1200f, 506f, 309f, 146f));
        AddEffect(metadata, "Effect_GroundImpact", new Rect(798f, 503f, 397f, 265f));
        AddEffect(metadata, "Effect_SpinSlash", new Rect(580f, 304f, 270f, 220f));
        AddEffect(metadata, "Effect_SummonCircle", new Rect(235f, 45f, 245f, 120f));
        AddEffect(metadata, "Effect_ShadowMinion", new Rect(680f, 35f, 130f, 190f));
        importer.spritesheet = metadata.ToArray();
        importer.SaveAndReimport();
    }

    private static void ConfigureDeathSheet()
    {
        if (!File.Exists(ProcessedDeathPath)) return;
        AssetDatabase.ImportAsset(ProcessedDeathPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(ProcessedDeathPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 128f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        const float cellSize = 384f;
        const float pivotY = 20f / cellSize;
        var frames = new SpriteMetaData[16];
        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
        {
            int index = row * 4 + column;
            frames[index] = new SpriteMetaData
            {
                name = $"Boss_Death_{index:00}",
                rect = new Rect(column * cellSize, (3 - row) * cellSize, cellSize, cellSize),
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, pivotY)
            };
        }
        importer.spritesheet = frames;
        importer.SaveAndReimport();
    }

    private static void AddEffect(List<SpriteMetaData> metadata, string name, Rect rect)
    {
        metadata.Add(new SpriteMetaData
        {
            name = name,
            rect = rect,
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        });
    }

    private static Sprite[] Select(Sprite[] sprites, params string[] names) =>
        names.Select(name => FindSprite(sprites, name)).Where(sprite => sprite != null).ToArray();

    private static Sprite FindSprite(Sprite[] sprites, string name) =>
        sprites.FirstOrDefault(sprite => sprite.name == name);

    private static AnimationClip CreateClip(string path, string name, Sprite[] sprites, bool loop,
        float frameRate = 8f)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = frameRate;
        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        ObjectReferenceKeyframe[] keys = sprites.Select((sprite, index) =>
            new ObjectReferenceKeyframe { time = index / frameRate, value = sprite }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip CreateHitClip(Sprite sprite)
    {
        AnimationClip clip = CreateClip($"{AnimationFolder}/Boss_Hit.anim", "Boss_Hit", new[] { sprite }, false);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Color.r"),
            AnimationCurve.Linear(0f, 1f, 0.12f, 1f));
        return clip;
    }

    private static AnimationClip[] CreateDeathClips(Sprite[] sprites, string[] directions)
    {
        // The supplied death sheet is a single sequence. Keep one shared clip
        // so death always plays in the same place and never branches by facing.
        if (sprites == null || sprites.Length == 0) return System.Array.Empty<AnimationClip>();
        AnimationClip clip = CreateClip(
            $"{AnimationFolder}/Boss_Death.anim", "Boss_Death", sprites, false, 12f);
        return new[] { clip };
    }

    private static AnimatorController CreateController(AnimationClip[] idle, AnimationClip[] walk,
        AnimationClip[] attacks, AnimationClip hit, AnimationClip[] deaths, string[] names)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Direction", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsAttacking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "AttackType", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        RemoveLegacyDeathState(machine);
        for (int i = 0; i < 4; i++)
        {
            AnimatorState idleState = FindOrCreateState(machine, "Idle_" + names[i], idle[i]);
            AnimatorState walkState = FindOrCreateState(machine, "Walk_" + names[i], walk[i]);
            EnsureTransition(machine, idleState, i, false);
            EnsureTransition(machine, walkState, i, true);
            if (i == 0) machine.defaultState = idleState;
        }
        for (int i = 0; i < attacks.Length; i++)
        {
            AnimatorState attack = FindOrCreateState(machine, "Attack" + (i + 1), attacks[i]);
            EnsureAttackTransition(machine, attack, i + 1);
        }
        AnimatorState hitState = FindOrCreateState(machine, "Hit", hit);
        EnsureTriggerTransition(machine, hitState, "Hit");
        if (deaths != null && deaths.Length > 0)
        {
            AnimatorState deathState = FindOrCreateState(machine, "Death", deaths[0]);
            EnsureDeathTransition(machine, deathState);
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void RemoveLegacyDeathState(AnimatorStateMachine machine)
    {
        AnimatorState[] legacyStates = machine.states.Select(item => item.state)
            .Where(state => state.name == "Death" || state.name.StartsWith("Death_")).ToArray();
        foreach (AnimatorState legacy in legacyStates)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions
                         .Where(item => item.destinationState == legacy).ToArray())
                machine.RemoveAnyStateTransition(transition);
            machine.RemoveState(legacy);
        }
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(parameter => parameter.name == name)) controller.AddParameter(name, type);
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine machine, string name, Motion motion)
    {
        AnimatorState state = machine.states.Select(item => item.state).FirstOrDefault(item => item.name == name);
        if (state == null) state = machine.AddState(name);
        state.motion = motion;
        return state;
    }

    private static void EnsureTransition(AnimatorStateMachine machine, AnimatorState target, int direction, bool moving)
    {
        AnimatorStateTransition transition = machine.anyStateTransitions.FirstOrDefault(item => item.destinationState == target);
        if (transition == null) transition = machine.AddAnyStateTransition(target);
        transition.conditions = System.Array.Empty<AnimatorCondition>();
        transition.hasExitTime = false;
        transition.duration = 0.04f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.Equals, direction, "Direction");
        transition.AddCondition(moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "IsMoving");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
    }

    private static void EnsureAttackTransition(AnimatorStateMachine machine, AnimatorState target, int attackType)
    {
        AnimatorStateTransition transition = machine.anyStateTransitions.FirstOrDefault(item => item.destinationState == target);
        if (transition == null) transition = machine.AddAnyStateTransition(target);
        transition.conditions = System.Array.Empty<AnimatorCondition>();
        transition.hasExitTime = false;
        transition.duration = 0.03f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "IsAttacking");
        transition.AddCondition(AnimatorConditionMode.Equals, attackType, "AttackType");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
    }

    private static void EnsureTriggerTransition(AnimatorStateMachine machine, AnimatorState target, string trigger)
    {
        AnimatorStateTransition transition = machine.anyStateTransitions.FirstOrDefault(item => item.destinationState == target);
        if (transition == null) transition = machine.AddAnyStateTransition(target);
        transition.conditions = System.Array.Empty<AnimatorCondition>();
        transition.hasExitTime = false;
        transition.duration = 0.02f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void EnsureDeathTransition(AnimatorStateMachine machine, AnimatorState target)
    {
        AnimatorStateTransition transition = machine.anyStateTransitions.FirstOrDefault(item => item.destinationState == target);
        if (transition == null) transition = machine.AddAnyStateTransition(target);
        transition.conditions = System.Array.Empty<AnimatorCondition>();
        transition.hasExitTime = false;
        transition.duration = 0.02f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");
    }

    private static GameObject CreatePrefab(Sprite sprite, RuntimeAnimatorController controller)
    {
        GameObject root = new("Easy Boss", typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CapsuleCollider2D), typeof(Damageable), typeof(Animator), typeof(BossHealth),
            typeof(BossAnimator), typeof(BossMovement), typeof(BossCombat));
        root.transform.localScale = Vector3.one * 3f;
        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;
        Animator animator = root.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        CapsuleCollider2D collider = root.GetComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.82f, 0.96f);
        collider.offset = new Vector2(0f, -0.2f);
        Rigidbody2D body = root.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        SerializedObject health = new(root.GetComponent<BossHealth>());
        SerializedProperty deathDelay = health.FindProperty("deathDelay");
        if (deathDelay != null) deathDelay.floatValue = 1.25f;
        health.ApplyModifiedPropertiesWithoutUndo();

        CreateDisabledHitbox(root.transform, "Skill1Hitbox", 0.4f);
        CreateDisabledHitbox(root.transform, "Skill2Hitbox", 2.1f);
        CreateDisabledHitbox(root.transform, "Skill3Hitbox", 2.2f);
        CreateDisabledHitbox(root.transform, "Skill4Hitbox", 1f);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void CreateDisabledHitbox(Transform parent, string name, float radius)
    {
        GameObject hitbox = new(name, typeof(CircleCollider2D));
        hitbox.transform.SetParent(parent, false);
        CircleCollider2D collider = hitbox.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = radius;
        collider.enabled = false;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
