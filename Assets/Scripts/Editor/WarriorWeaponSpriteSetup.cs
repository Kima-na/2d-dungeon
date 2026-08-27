#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class WarriorWeaponSpriteSetup
{
    private const string SourcePath = "Assets/Sprites/Weapons.png";
    private const string RootFolder = "Assets/Sprites/Warrior Weapons";
    private const int SourceX = 190;
    private const int SheetWidth = 1312;
    private const int ColumnWidth = 164;
    private const float PixelsPerUnit = 100f;

    [MenuItem("Tools/2D Dungeon/Apply Warrior Weapon Sprites")]
    public static void ApplyToLoadedScene()
    {
        if (!File.Exists(SourcePath))
        {
            Debug.LogWarning($"Warrior weapon source not found: {SourcePath}");
            return;
        }

        EnsureFolder("Assets/Sprites", "Warrior Weapons");
        CreateWeaponSheet("Greatsword", 669, 355);
        CreateWeaponSheet("Shortsword", 404, 265);
        CreateWeaponSheet("Spear", 0, 404);

        foreach (AttackController controller in Object.FindObjectsByType<AttackController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            AssignSprites(controller);
            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        EquipmentSystemSetup.Setup();
        AssetDatabase.SaveAssets();
        Debug.Log("Warrior weapons imported: 24 sprites (8 Greatsword, 8 Shortsword, 8 Spear), PPU 100.");
    }

    public static void AssignSprites(AttackController controller)
    {
        AssignArray(controller, "shortSwordIdleSprites", LoadSprites("Shortsword"));
        AssignArray(controller, "shortSwordActionSprites", LoadSprites("Shortsword"));
        AssignArray(controller, "greatswordIdleSprites", LoadSprites("Greatsword"));
        AssignArray(controller, "greatswordActionSprites", LoadSprites("Greatsword"));
        AssignArray(controller, "spearIdleSprites", LoadSprites("Spear"));
        AssignArray(controller, "spearActionSprites", LoadSprites("Spear"));
    }

    public static Sprite[] LoadSprites(string weaponName) =>
        AssetDatabase.LoadAllAssetsAtPath(GetSheetPath(weaponName))
            .OfType<Sprite>()
            .Where(sprite => sprite.name.StartsWith(weaponName + "_"))
            .OrderBy(sprite => sprite.name)
            .ToArray();

    private static void CreateWeaponSheet(string weaponName, int sourceY, int height)
    {
        EnsureFolder(RootFolder, weaponName);
        TextureImporter sourceImporter = (TextureImporter)AssetImporter.GetAtPath(SourcePath);
        sourceImporter.isReadable = true;
        sourceImporter.alphaIsTransparency = true;
        sourceImporter.SaveAndReimport();

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
        Texture2D sheet = new(SheetWidth, height, TextureFormat.RGBA32, false);
        sheet.SetPixels(source.GetPixels(SourceX, sourceY, SheetWidth, height));
        sheet.Apply();

        string path = GetSheetPath(weaponName);
        File.WriteAllBytes(path, sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.SaveAndReimport();
        importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider =
            factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        SpriteRect[] previous = provider.GetSpriteRects();
        SpriteRect[] spriteRects = BuildMetadata(
            AssetDatabase.LoadAssetAtPath<Texture2D>(path), weaponName);
        foreach (SpriteRect spriteRect in spriteRects)
        {
            SpriteRect existing = previous.FirstOrDefault(item => item.name == spriteRect.name);
            if (existing != null) spriteRect.spriteID = existing.spriteID;
        }
        provider.SetSpriteRects(spriteRects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static SpriteRect[] BuildMetadata(Texture2D sheet, string weaponName)
    {
        var metadata = new SpriteRect[8];
        for (int column = 0; column < 8; column++)
        {
            int cellX = column * ColumnWidth;
            int cellWidth = Mathf.Min(ColumnWidth, sheet.width - cellX);
            int minX = cellX + cellWidth;
            int minY = sheet.height;
            int maxX = cellX;
            int maxY = 0;

            for (int y = 0; y < sheet.height; y++)
            for (int x = cellX; x < cellX + cellWidth; x++)
            {
                if (sheet.GetPixel(x, y).a <= 0.03f) continue;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }

            const int padding = 3;
            minX = Mathf.Max(cellX, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(cellX + cellWidth - 1, maxX + padding);
            maxY = Mathf.Min(sheet.height - 1, maxY + padding);
            metadata[column] = new SpriteRect
            {
                name = $"{weaponName}_{column + 1:00}",
                rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.04f),
                spriteID = GUID.Generate()
            };
        }
        return metadata;
    }

    private static void AssignArray(AttackController controller, string propertyName, Sprite[] sprites)
    {
        SerializedObject serialized = new(controller);
        SerializedProperty property = serialized.FindProperty(propertyName);
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string GetSheetPath(string weaponName) =>
        $"{RootFolder}/{weaponName}/{weaponName}.png";

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
