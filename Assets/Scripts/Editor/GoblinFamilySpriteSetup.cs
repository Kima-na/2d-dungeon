#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GoblinFamilySpriteSetup
{
    private const float PixelsPerUnit = 200f;
    private const float FrameRate = 8f;
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
            clips[0, direction] = CreateClip(folder, $"Idle_{direction}", new[] { row[0] }, true);
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
        AssetDatabase.ImportAsset(definition.TexturePath, ImportAssetOptions.ForceSynchronousImport);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.TexturePath);
        TextureImporter importer = AssetImporter.GetAtPath(definition.TexturePath) as TextureImporter;
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
                metadata[index++] = new SpriteMetaData
                {
                    name = $"{definition.Name}_{topRow}_{column}",
                    rect = new Rect(xLeft, yBottom, xRight - xLeft, yTop - yBottom),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = projectileCell ? new Vector2(0.5f, 0.5f) :
                        new Vector2(0.5f, definition.FootPivot), border = Vector4.zero
                };
            }
        }
        importer.spritesheet = metadata;
        importer.SaveAndReimport();
        Sprite[] result = AssetDatabase.LoadAllAssetsAtPath(definition.TexturePath).OfType<Sprite>()
            .OrderBy(sprite => SpriteOrder(sprite, definition.Columns)).ToArray();
        if (result.Length == metadata.Length) return result;
        Debug.LogError($"{definition.Name}: expected {metadata.Length} exact cells, imported {result.Length}.");
        return null;
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
