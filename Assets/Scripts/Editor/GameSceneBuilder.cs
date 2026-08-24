#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string SpritePath = "Assets/Sprites/TestSquare.png";
    private const string WeaponFolder = "Assets/Sprites/Test Weapons";

    [InitializeOnLoadMethod]
    private static void BuildMissingSceneOnEditorLoad()
    {
        if (Application.isBatchMode || File.Exists(ScenePath)) return;
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                !SceneManager.GetActiveScene().isDirty &&
                !File.Exists(ScenePath))
                BuildGameScene();
        };
    }

    [MenuItem("Tools/2D Dungeon/Build Game Scene")]
    public static void BuildGameScene()
    {
        EnsureFolders();
        Sprite square = CreateTestSprite();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        new GameObject("GameManager").AddComponent<GameManager>();
        CreateGround(square);
        PlayerStats stats = CreatePlayer(square, out AttackController attackController);
        CreateDummy(square);
        CreateCanvas(stats, attackController);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"Created playable demo scene: {ScenePath}");
    }

    public static void BuildFromCommandLine()
    {
        BuildGameScene();
    }

    private static void EnsureFolders()
    {
        string[] folders = { "Animations", "Audio", "Prefabs", "Scenes", "Scripts", "Sprites", "UI" };
        foreach (string folder in folders)
        {
            string path = $"Assets/{folder}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets", folder);
        }
    }

    private static Sprite CreateTestSprite()
    {
        if (!File.Exists(SpritePath))
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static void CreateCamera()
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.07f);
    }

    private static void CreateGround(Sprite sprite)
    {
        var ground = CreateSpriteObject("Test Ground", sprite, Vector2.zero, new Vector2(18f, 10f), new Color(0.11f, 0.14f, 0.18f));
        ground.transform.position = new Vector3(0f, 0f, 1f);
        BoxCollider2D floorCollider = ground.AddComponent<BoxCollider2D>();
        floorCollider.isTrigger = true;

        CreateWall("Wall Top", sprite, new Vector2(0f, 5.25f), new Vector2(18.5f, 0.5f));
        CreateWall("Wall Bottom", sprite, new Vector2(0f, -5.25f), new Vector2(18.5f, 0.5f));
        CreateWall("Wall Left", sprite, new Vector2(-9.25f, 0f), new Vector2(0.5f, 10f));
        CreateWall("Wall Right", sprite, new Vector2(9.25f, 0f), new Vector2(0.5f, 10f));
    }

    private static void CreateWall(string name, Sprite sprite, Vector2 position, Vector2 scale)
    {
        GameObject wall = CreateSpriteObject(name, sprite, position, scale, new Color(0.22f, 0.27f, 0.34f));
        wall.AddComponent<BoxCollider2D>();
    }

    private static PlayerStats CreatePlayer(Sprite sprite, out AttackController attackController)
    {
        GameObject player = CreateSpriteObject("Player", sprite, new Vector2(-3f, 0f), Vector2.one, new Color(0.2f, 0.75f, 1f));
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        player.AddComponent<BoxCollider2D>();
        PlayerStats stats = player.AddComponent<PlayerStats>();
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerDash>();
        attackController = player.AddComponent<AttackController>();
        SetReference(attackController, "oneHandedSwordSprite", AssetDatabase.LoadAssetAtPath<Sprite>($"{WeaponFolder}/LongSword.png"));
        SetReference(attackController, "greatswordSprite", AssetDatabase.LoadAssetAtPath<Sprite>($"{WeaponFolder}/GreatSword.png"));
        SetReference(attackController, "spearSprite", AssetDatabase.LoadAssetAtPath<Sprite>($"{WeaponFolder}/Spear.png"));
        player.AddComponent<ArcherController>();

        var tester = player.AddComponent<DebugDamageTester>();
        SetReference(tester, "target", stats);
        return stats;
    }

    private static void CreateDummy(Sprite sprite)
    {
        GameObject dummy = CreateSpriteObject("Dummy", sprite, new Vector2(1f, 0f), new Vector2(1.2f, 1.2f), new Color(1f, 0.38f, 0.28f));
        dummy.AddComponent<BoxCollider2D>();
        dummy.AddComponent<Damageable>();
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector2 position, Vector2 scale, Color color)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.position = position;
        go.transform.localScale = scale;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        return go;
    }

    private static void CreateCanvas(PlayerStats stats, AttackController attackController)
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Slider hp = CreateSlider(canvasGo.transform, "Health Bar", new Vector2(40f, -40f), new Color(0.8f, 0.12f, 0.12f));
        Slider mp = CreateSlider(canvasGo.transform, "Mana Bar", new Vector2(40f, -100f), new Color(0.1f, 0.4f, 0.9f));
        Slider xp = CreateSlider(canvasGo.transform, "Experience Bar", new Vector2(40f, -160f), new Color(0.85f, 0.65f, 0.08f));
        Text hpText = CreateText(hp.transform, "HP Text", "HP  100 / 100", TextAnchor.MiddleCenter, 22);
        Text mpText = CreateText(mp.transform, "MP Text", "MP  50 / 50", TextAnchor.MiddleCenter, 22);
        Text xpText = CreateText(xp.transform, "EXP Text", "EXP  0 / 100", TextAnchor.MiddleCenter, 22);
        Text levelText = CreateLabel(canvasGo.transform, "Level Text", "WARRIOR  LV.1  STR 10  DEF 5", new Vector2(40f, -215f));
        Text weaponText = CreateLabel(canvasGo.transform, "Weapon Text", "[1/2/3] ONE-HANDED SWORD  ATK 18", new Vector2(40f, -255f));

        GameObject deathPanel = new GameObject("Death Panel", typeof(RectTransform), typeof(Image));
        deathPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform deathRect = deathPanel.GetComponent<RectTransform>();
        deathRect.anchorMin = Vector2.zero;
        deathRect.anchorMax = Vector2.one;
        deathRect.offsetMin = deathRect.offsetMax = Vector2.zero;
        deathPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        Text deathText = CreateText(deathPanel.transform, "Death Text", "YOU DIED", TextAnchor.MiddleCenter, 72);
        deathText.color = new Color(0.8f, 0.05f, 0.05f);

        PlayerHUD hud = canvasGo.AddComponent<PlayerHUD>();
        SetReference(hud, "playerStats", stats);
        SetReference(hud, "healthSlider", hp);
        SetReference(hud, "manaSlider", mp);
        SetReference(hud, "healthText", hpText);
        SetReference(hud, "manaText", mpText);
        SetReference(hud, "experienceSlider", xp);
        SetReference(hud, "experienceText", xpText);
        SetReference(hud, "levelText", levelText);
        SetReference(hud, "weaponText", weaponText);
        SetReference(hud, "attackController", attackController);
        SetReference(hud, "deathPanel", deathPanel);
        deathPanel.SetActive(false);
    }

    private static Text CreateLabel(Transform parent, string name, string value, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(620f, 36f);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = 22;
        text.color = Color.white;
        return text;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 position, Color fillColor)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(420f, 42f);

        Image background = CreateImage(root.transform, "Background", new Color(0.05f, 0.05f, 0.05f, 0.9f));
        Image fill = CreateImage(root.transform, "Fill", fillColor);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.targetGraphic = fill;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        return slider;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(Transform parent, string name, string value, TextAnchor alignment, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.color = Color.white;
        return text;
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
