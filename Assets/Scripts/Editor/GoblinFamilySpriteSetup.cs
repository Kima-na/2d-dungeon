#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GoblinFamilySpriteSetup
{
    private const float PixelsPerUnit = 200f;
    private const float FrameRate = 8f;
    private const int CellHorizontalPadding = 7;
    private const string CleanWarriorPath = "Assets/Sprites/Enemies/GoblinWarrior_clean.png";
    private const string AnimationRoot = "Assets/Animations/Enemies";
    private const string PrefabRoot = "Assets/Prefabs/Enemies";
    private const string DatabasePath = "Assets/Resources/GoblinWarriorVisualDatabase.asset";

    private sealed class SheetDefinition
    {
        public readonly string Name, TexturePath, PrefabName;
        public readonly int Columns, WalkCount, AttackStart, AttackCount;
        public readonly int[] RowCounts;
        public readonly float FootPivot;
        public SheetDefinition(string name, string texturePath, string prefabName, int columns,
            int[] rowCounts, int walkCount, int attackStart, int attackCount, float footPivot)
        {
            Name = name; TexturePath = texturePath; PrefabName = prefabName; Columns = columns;
            RowCounts = rowCounts; WalkCount = walkCount; AttackStart = attackStart;
            AttackCount = attackCount; FootPivot = footPivot;
        }
    }

    private static readonly SheetDefinition Warrior = new("GoblinWarrior",
        "Assets/Sprites/Goblin_monster.png", "Goblin Warrior", 10,
        new[] { 10, 10, 10, 10, 5 }, 8, 8, 2, 0.08f);
    private static readonly SheetDefinition Archer = new("GoblinArcher",
        "Assets/Sprites/Goblin_Archer.png", "Goblin Archer", 8,
        new[] { 8, 8, 8, 8, 4 }, 4, 4, 3, 0.07f);
    private static readonly SheetDefinition Mage = new("GoblinMage",
        "Assets/Sprites/Goblin_Wizerd.png", "Goblin Mage", 8,
        new[] { 8, 8, 8, 8, 4 }, 4, 4, 3, 0.07f);

    [InitializeOnLoadMethod]
    private static void ScheduleSetup() => EditorApplication.delayCall += SetupIfNeeded;

    [MenuItem("Tools/2D Dungeon/Setup Goblin Family Sprites")]
    public static void Setup()
    {
        EnsureFolders();
        GameObject warrior = BuildEnemy(Warrior, out _);
        GameObject archer = BuildEnemy(Archer, out Sprite arrow);
        GameObject mage = BuildEnemy(Mage, out Sprite magic);
        if (warrior == null || archer == null || mage == null) return;
        GameObject arrowPrefab = BuildProjectile("GoblinArrowProjectile", arrow, false);
        GameObject magicPrefab = BuildProjectile("GoblinMagicProjectile", magic, true);
        AssignController(warrior, Warrior);
        AssignController(archer, Archer);
        AssignController(mage, Mage);
        UpdateDatabase(warrior, archer, mage, arrowPrefab, magicPrefab);
        AssetDatabase.SaveAssets();
        Debug.Log("Goblin family setup complete: grounded pivots, exact cell slicing, five animation states, and projectile prefabs created.");
    }

    private static void SetupIfNeeded()
    {
        if (!File.Exists(Path.GetFullPath(Archer.TexturePath)) || !File.Exists(Path.GetFullPath(Mage.TexturePath))) return;
        TextureImporter archer = AssetImporter.GetAtPath(Archer.TexturePath) as TextureImporter;
        TextureImporter mage = AssetImporter.GetAtPath(Mage.TexturePath) as TextureImporter;
        if (archer == null || mage == null || archer.spritesheet.Length != 36 || mage.spritesheet.Length != 36)
            Setup();
    }

    private static GameObject BuildEnemy(SheetDefinition definition, out Sprite projectileSprite)
    {
        projectileSprite = null;
        Sprite[] sprites = ImportAndSlice(definition);
        if (sprites == null) return null;
        string folder = $"{AnimationRoot}/{definition.Name}";
        AnimationClip[,] clips = new AnimationClip[5, 4];
        Sprite[] deathFrames = GetRow(sprites, definition, 4);
        // EnemyAI direction values are Front, Back, Right, Left while the
        // authored sheets are ordered Front, Right, Back, Left.
        int[] sheetRowsByDirection = { 0, 2, 1, 3 };
        for (int direction = 0; direction < 4; direction++)
        {
            Sprite[] row = GetRow(sprites, definition, sheetRowsByDirection[direction]);
            // The warrior's idle previously contained a single sprite, so it looked as if
            // the whole animator had frozen whenever the AI paused between movements.
            Sprite[] idleFrames = definition == Warrior
                ? row.Take(definition.WalkCount).ToArray()
                : new[] { row[0] };
            clips[0, direction] = CreateClip(folder, $"Idle_{direction}", idleFrames, true);
            clips[1, direction] = CreateClip(folder, $"Walk_{direction}", row.Take(definition.WalkCount).ToArray(), true);
            clips[2, direction] = CreateClip(folder, $"Attack_{direction}",
                row.Skip(definition.AttackStart).Take(definition.AttackCount).ToArray(), false);
            clips[3, direction] = CreateClip(folder, $"Hit_{direction}", new[] { deathFrames[0] }, false);
        }
        AnimationClip death = CreateClip(folder, "Death", deathFrames, false);
        for (int direction = 0; direction < 4; direction++) clips[4, direction] = death;
        AnimatorController controller = CreateController($"{folder}/{definition.Name}.controller", clips);
        GameObject prefab = CreateEnemyPrefab($"{PrefabRoot}/{definition.Name}.prefab",
            definition.PrefabName, sprites[0], controller);
        if (definition != Warrior) projectileSprite = GetRow(sprites, definition, 1)[7];
        return prefab;
    }

    private static Sprite[] ImportAndSlice(SheetDefinition definition)
    {
        if (!File.Exists(Path.GetFullPath(definition.TexturePath))) return null;
        string texturePath = definition == Warrior ? CreateCleanWarriorCopy(definition) : definition.TexturePath;
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (texture == null || importer == null) return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;

        var metadata = new SpriteMetaData[definition.RowCounts.Sum()];
        int index = 0, rows = definition.RowCounts.Length;
        for (int topRow = 0; topRow < rows; topRow++)
        {
            int yTop = Mathf.RoundToInt(texture.height * (rows - topRow) / (float)rows);
            int yBottom = Mathf.RoundToInt(texture.height * (rows - topRow - 1) / (float)rows);
            for (int column = 0; column < definition.RowCounts[topRow]; column++)
            {
                int xLeft = Mathf.RoundToInt(texture.width * column / (float)definition.Columns);
                int xRight = Mathf.RoundToInt(texture.width * (column + 1) / (float)definition.Columns);
                bool projectileCell = definition != Warrior && topRow == 1 && column == 7;
                int horizontalPadding = definition == Warrior || projectileCell ? 0 : CellHorizontalPadding;
                metadata[index++] = new SpriteMetaData
                {
                    name = $"{definition.Name}_{topRow}_{column}",
                    rect = new Rect(xLeft + horizontalPadding, yBottom,
                        xRight - xLeft - horizontalPadding * 2, yTop - yBottom),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = projectileCell ? new Vector2(0.5f, 0.5f) :
                        new Vector2(0.5f, definition.FootPivot), border = Vector4.zero
                };
            }
        }
        importer.spritesheet = metadata;
        importer.SaveAndReimport();
        Sprite[] result = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>()
            .OrderBy(sprite => SpriteOrder(sprite, definition.Columns)).ToArray();
        if (result.Length == metadata.Length) return result;
        Debug.LogError($"{definition.Name}: expected {metadata.Length} exact cells, imported {result.Length}.");
        return null;
    }

    private static string CreateCleanWarriorCopy(SheetDefinition definition)
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(definition.TexturePath) as TextureImporter;
        bool wasReadable = sourceImporter != null && sourceImporter.isReadable;
        if (sourceImporter == null) return definition.TexturePath;
        sourceImporter.isReadable = true;
        sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
        sourceImporter.SaveAndReimport();
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.TexturePath);
        Color32[] sourcePixels = source.GetPixels32();
        Color32[] output = new Color32[sourcePixels.Length];
        int rows = definition.RowCounts.Length;
        for (int topRow = 0; topRow < rows; topRow++)
        {
            int yTop = Mathf.RoundToInt(source.height * (rows - topRow) / (float)rows);
            int yBottom = Mathf.RoundToInt(source.height * (rows - topRow - 1) / (float)rows);
            RepackWarriorRow(sourcePixels, output, source.width, yBottom, yTop,
                definition.Columns, definition.RowCounts[topRow]);
        }
        Texture2D clean = new(source.width, source.height, TextureFormat.RGBA32, false);
        clean.SetPixels32(output); clean.Apply();
        File.WriteAllBytes(CleanWarriorPath, clean.EncodeToPNG());
        Object.DestroyImmediate(clean);
        AssetDatabase.ImportAsset(CleanWarriorPath, ImportAssetOptions.ForceSynchronousImport);
        sourceImporter = AssetImporter.GetAtPath(definition.TexturePath) as TextureImporter;
        sourceImporter.isReadable = wasReadable;
        sourceImporter.SaveAndReimport();
        return CleanWarriorPath;
    }

    private static void RepackWarriorRow(Color32[] source, Color32[] output, int width,
        int yBottom, int yTop, int columns, int frameCount)
    {
        int rowHeight = yTop - yBottom;
        var points = new List<int>();
        var lookup = new Dictionary<int, int>();
        for (int y = yBottom; y < yTop; y++)
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;
            if (source[index].a <= 12) continue;
            lookup[(y - yBottom) * width + x] = points.Count;
            points.Add(index);
        }
        var visited = new bool[points.Count];
        var components = new List<List<int>>();
        for (int start = 0; start < points.Count; start++)
        {
            if (visited[start]) continue;
            var component = new List<int>(); var queue = new Queue<int>();
            visited[start] = true; queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue(); component.Add(current);
                int index = points[current], x = index % width, y = index / width - yBottom;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= rowHeight ||
                        !lookup.TryGetValue(ny * width + nx, out int next) || visited[next]) continue;
                    visited[next] = true; queue.Enqueue(next);
                }
            }
            if (component.Count > 32) components.Add(component);
        }
        components = components.OrderByDescending(value => value.Count).Take(frameCount)
            .OrderBy(value => value.Average(point => points[point] % width)).ToList();
        float cellWidth = width / (float)columns;
        for (int frame = 0; frame < components.Count; frame++)
        {
            GetBounds(components[frame], points, width, out int minX, out int maxX, out _, out _);
            int targetCenter = Mathf.RoundToInt((frame + 0.5f) * cellWidth);
            int shiftX = targetCenter - (minX + maxX) / 2;
            foreach (int point in components[frame])
            {
                int sourceIndex = points[point], x = sourceIndex % width, y = sourceIndex / width;
                int targetX = x + shiftX;
                if (targetX < 0 || targetX >= width) continue;
                output[y * width + targetX] = source[sourceIndex];
            }
        }
    }

    private static void CopyMainFrameComponents(Color32[] source, Color32[] output, int textureWidth,
        int xLeft, int xRight, int yBottom, int yTop)
    {
        int cellWidth = xRight - xLeft;
        var points = new List<int>();
        var lookup = new Dictionary<int, int>();
        for (int y = yBottom; y < yTop; y++)
        for (int x = xLeft; x < xRight; x++)
        {
            int sourceIndex = y * textureWidth + x;
            if (source[sourceIndex].a <= 12) continue;
            lookup[(y - yBottom) * cellWidth + x - xLeft] = points.Count;
            points.Add(sourceIndex);
        }
        if (points.Count == 0) return;
        bool[] visited = new bool[points.Count];
        var components = new List<List<int>>();
        for (int start = 0; start < points.Count; start++)
        {
            if (visited[start]) continue;
            var component = new List<int>();
            var queue = new Queue<int>();
            visited[start] = true; queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue(); component.Add(current);
                int sourceIndex = points[current], x = sourceIndex % textureWidth - xLeft;
                int y = sourceIndex / textureWidth - yBottom;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= cellWidth || ny < 0 || ny >= yTop - yBottom ||
                        !lookup.TryGetValue(ny * cellWidth + nx, out int next) || visited[next]) continue;
                    visited[next] = true; queue.Enqueue(next);
                }
            }
            components.Add(component);
        }
        int main = 0;
        for (int i = 1; i < components.Count; i++) if (components[i].Count > components[main].Count) main = i;
        GetBounds(components[main], points, textureWidth, out int mainMinX, out int mainMaxX,
            out int mainMinY, out int mainMaxY);
        for (int i = 0; i < components.Count; i++)
        {
            GetBounds(components[i], points, textureWidth, out int minX, out int maxX, out int minY, out int maxY);
            int gapX = Mathf.Max(0, Mathf.Max(mainMinX - maxX, minX - mainMaxX));
            int gapY = Mathf.Max(0, Mathf.Max(mainMinY - maxY, minY - mainMaxY));
            if (i != main && gapX * gapX + gapY * gapY > 64) continue;
            foreach (int point in components[i]) { int index = points[point]; output[index] = source[index]; }
        }
    }

    private static void GetBounds(List<int> component, List<int> points, int width,
        out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = minY = int.MaxValue; maxX = maxY = int.MinValue;
        foreach (int point in component)
        {
            int index = points[point], x = index % width, y = index / width;
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
        }
    }

    private static int SpriteOrder(Sprite sprite, int columns)
    {
        string[] parts = sprite.name.Split('_');
        return int.Parse(parts[^2]) * columns + int.Parse(parts[^1]);
    }

    private static Sprite[] GetRow(Sprite[] sprites, SheetDefinition definition, int row)
    {
        int start = 0;
        for (int i = 0; i < row; i++) start += definition.RowCounts[i];
        return sprites.Skip(start).Take(definition.RowCounts[row]).ToArray();
    }

    private static AnimationClip CreateClip(string folder, string name, Sprite[] frames, bool loop)
    {
        string path = $"{folder}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.name = name; clip.frameRate = FrameRate;
        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames.Select((frame, i) =>
            new ObjectReferenceKeyframe { time = i / FrameRate, value = frame }).ToArray());
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(string path, AnimationClip[,] clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("Action", AnimatorControllerParameterType.Int);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        string[] names = { "Idle", "Walk", "Attack", "Hit", "Death" };
        for (int action = 0; action < 5; action++)
        for (int direction = 0; direction < 4; direction++)
        {
            AnimatorState state = machine.AddState($"{names[action]}_{direction}");
            state.motion = clips[action, direction];
            if (action == 0 && direction == 0) machine.defaultState = state;
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false; transition.duration = 0f; transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, action, "Action");
            transition.AddCondition(AnimatorConditionMode.Equals, direction, "Direction");
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateEnemyPrefab(string path, string name, Sprite sprite, RuntimeAnimatorController controller)
    {
        GameObject root = new(name, typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(BoxCollider2D), typeof(Damageable), typeof(EnemyAI), typeof(Animator));
        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.color = Color.white; renderer.sortingOrder = 1;
        Rigidbody2D body = root.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f; body.freezeRotation = true; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(0.46f, 0.52f); collider.offset = new Vector2(0f, 0.26f);
        Animator animator = root.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AssignController(GameObject prefab, SheetDefinition definition)
    {
        if (prefab == null) return;
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        Animator animator = contents.GetComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            $"{AnimationRoot}/{definition.Name}/{definition.Name}.controller");
        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static GameObject BuildProjectile(string name, Sprite sprite, bool magic)
    {
        if (sprite == null) return null;
        GameObject root = new(name, typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CircleCollider2D), typeof(EnemyProjectile));
        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.color = Color.white; renderer.sortingOrder = 2;
        Rigidbody2D body = root.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
        collider.isTrigger = true; collider.radius = magic ? 0.16f : 0.1f;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/{name}.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void UpdateDatabase(GameObject warrior, GameObject archer, GameObject mage,
        GameObject arrow, GameObject magic)
    {
        GoblinWarriorVisualDatabase database = AssetDatabase.LoadAssetAtPath<GoblinWarriorVisualDatabase>(DatabasePath);
        if (database == null) { database = ScriptableObject.CreateInstance<GoblinWarriorVisualDatabase>(); AssetDatabase.CreateAsset(database, DatabasePath); }
        database.warriorPrefab = warrior; database.archerPrefab = archer; database.magePrefab = mage;
        database.arrowProjectilePrefab = arrow; database.magicProjectilePrefab = magic;
        EditorUtility.SetDirty(database);
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Animations", "Enemies");
        foreach (SheetDefinition definition in new[] { Warrior, Archer, Mage }) EnsureFolder(AnimationRoot, definition.Name);
        EnsureFolder("Assets/Prefabs", "Enemies"); EnsureFolder("Assets", "Resources");
    }
    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
