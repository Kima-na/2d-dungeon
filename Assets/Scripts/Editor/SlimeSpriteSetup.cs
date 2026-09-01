#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SlimeSpriteSetup
{
    private const string TexturePath = "Assets/Sprites/Enemies/Slime/slime_monster_spritesheet.png";
    private const string LeftTexturePath = "Assets/Sprites/Enemies/Slime/left.png";
    private const string AnimationFolder = "Assets/Animations/Enemies/Slime";
    private const string FrontClipPath = AnimationFolder + "/Slime_Front.anim";
    private const string BackClipPath = AnimationFolder + "/Slime_Back.anim";
    private const string SideClipPath = AnimationFolder + "/Slime_Side.anim";
    private const string LeftClipPath = AnimationFolder + "/Slime_Left.anim";
    private const string ControllerPath = AnimationFolder + "/Slime.controller";
    private const string PrefabFolder = "Assets/Prefabs/Enemies";
    private const string PrefabPath = PrefabFolder + "/Slime.prefab";
    private const string ResourcesFolder = "Assets/Resources";
    private const string DatabasePath = ResourcesFolder + "/SlimeVisualDatabase.asset";

    [InitializeOnLoadMethod]
    private static void ScheduleSetup()
    {
        EditorApplication.delayCall += Setup;
    }

    [MenuItem("Tools/2D Dungeon/Setup Slime Sprite")]
    public static void Setup()
    {
        if (!File.Exists(TexturePath)) return;
        EnsureFolder("Assets/Animations", "Enemies");
        EnsureFolder("Assets/Animations/Enemies", "Slime");
        EnsureFolder("Assets/Prefabs", "Enemies");
        EnsureFolder("Assets", "Resources");

        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) return;

        bool requiresSlice = importer.spriteImportMode != SpriteImportMode.Multiple ||
                             importer.spritesheet == null || importer.spritesheet.Length != 9;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        if (requiresSlice)
        {
            var frames = new SpriteMetaData[9];
            for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
            {
                int index = row * 3 + column;
                frames[index] = new SpriteMetaData
                {
                    name = $"Slime_{index}",
                    rect = new Rect(column * 24f, (2 - row) * 24f, 24f, 24f),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }
            importer.spritesheet = frames;
            importer.SaveAndReimport();
        }
        else importer.SaveAndReimport();

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(TexturePath)
            .OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
        if (sprites.Length != 9) return;
        AnimationClip backClip = CreateOrUpdateClip(BackClipPath, "Slime_Back", sprites.Take(3).ToArray());
        Sprite[] sideSprites = sprites.Skip(3).Take(3).ToArray();
        AnimationClip sideClip = CreateOrUpdateClip(SideClipPath, "Slime_Side", sideSprites);
        AnimationClip frontClip = CreateOrUpdateClip(FrontClipPath, "Slime_Front", sprites.Skip(6).Take(3).ToArray());
        // Keep both side directions on the same 24px source art. EnemyAI flips
        // the left-facing state, avoiding a visibly different high-resolution frame set.
        AnimationClip leftClip = CreateOrUpdateClip(LeftClipPath, "Slime_Left", sideSprites);
        AnimatorController controller = CreateOrUpdateController(frontClip, backClip, sideClip, leftClip);
        GameObject prefab = CreateOrUpdatePrefab(sprites[0], controller);
        CreateOrUpdateDatabase(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("Slime sprite setup complete: Front, Back, Right, and Left loops linked to EnemyAI direction.");
    }

    private static void ConfigureLeftTexture()
    {
        if (!File.Exists(LeftTexturePath)) return;
        AssetDatabase.ImportAsset(LeftTexturePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(LeftTexturePath) as TextureImporter;
        if (importer == null) return;
        // The left sheet can be wider than Unity's 2048 default. Import it at
        // full resolution before calculating the three square frame rects.
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(LeftTexturePath);
        if (texture == null) return;
        int frameWidth = texture.width / 3;
        int frameHeight = texture.height;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        // Match the original 24px / 32 PPU sprite's 0.75 world-unit height.
        importer.spritePixelsPerUnit = frameHeight / 0.75f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        var frames = new SpriteMetaData[3];
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = new SpriteMetaData
            {
                name = $"Left_{i}",
                rect = new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        importer.spritesheet = frames;
        importer.SaveAndReimport();
    }

    private static AnimationClip CreateOrUpdateClip(string path, string clipName, Sprite[] sprites)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = clipName, frameRate = 6f };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = 6f;
        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / 6f, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateOrUpdateController(AnimationClip frontClip,
        AnimationClip backClip, AnimationClip sideClip, AnimationClip leftClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (!controller.parameters.Any(parameter => parameter.name == "Direction"))
            controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState front = FindOrCreateState(stateMachine, "Front", frontClip);
        AnimatorState back = FindOrCreateState(stateMachine, "Back", backClip);
        AnimatorState side = FindOrCreateState(stateMachine, "Side", sideClip);
        AnimatorState left = FindOrCreateState(stateMachine, "Left", leftClip);
        stateMachine.defaultState = front;
        EnsureDirectionTransition(stateMachine, front, 0);
        EnsureDirectionTransition(stateMachine, back, 1);
        EnsureDirectionTransition(stateMachine, side, 2);
        EnsureDirectionTransition(stateMachine, left, 3);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine stateMachine,
        string stateName, Motion motion)
    {
        AnimatorState state = stateMachine.states.Select(child => child.state)
            .FirstOrDefault(candidate => candidate.name == stateName);
        if (state == null) state = stateMachine.AddState(stateName);
        state.motion = motion;
        return state;
    }

    private static void EnsureDirectionTransition(AnimatorStateMachine stateMachine,
        AnimatorState destination, int direction)
    {
        if (stateMachine.anyStateTransitions.Any(transition => transition.destinationState == destination)) return;
        AnimatorStateTransition created = stateMachine.AddAnyStateTransition(destination);
        created.hasExitTime = false;
        created.duration = 0.05f;
        created.canTransitionToSelf = false;
        created.AddCondition(AnimatorConditionMode.Equals, direction, "Direction");
    }

    private static GameObject CreateOrUpdatePrefab(Sprite firstFrame, RuntimeAnimatorController controller)
    {
        GameObject root = new("Slime", typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(BoxCollider2D), typeof(Damageable), typeof(EnemyAI), typeof(Animator));
        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = firstFrame;
        renderer.color = Color.white;
        renderer.sortingOrder = 1;
        Rigidbody2D body = root.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        root.GetComponent<BoxCollider2D>().size = new Vector2(0.75f, 0.6f);
        Animator animator = root.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void CreateOrUpdateDatabase(GameObject prefab)
    {
        SlimeVisualDatabase database = AssetDatabase.LoadAssetAtPath<SlimeVisualDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<SlimeVisualDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }
        database.slimePrefab = prefab;
        EditorUtility.SetDirty(database);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
