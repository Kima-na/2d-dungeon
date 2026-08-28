#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class PlayerVisualSetup
{
    [MenuItem("Tools/2D Dungeon/Setup Player Designs")]
    public static void Setup()
    {
        const string databasePath = "Assets/Resources/PlayerVisualDatabase.asset";
        PlayerVisualDatabase database = AssetDatabase.LoadAssetAtPath<PlayerVisualDatabase>(databasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<PlayerVisualDatabase>();
            AssetDatabase.CreateAsset(database, databasePath);
        }
        database.designs = new PlayerDesign[4];
        for (int i = 0; i < database.designs.Length; i++)
        {
            string path = $"Assets/Undead Survivor/Sprites/Farmer {i}.png";
            ConfigureImporter(path);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            database.designs[i] = new PlayerDesign
            {
                displayName = $"Design {i + 1}",
                run = LoadNamed(assets, "Run", 6),
                stand = LoadNamed(assets, "Stand", 4),
                dead = LoadNamed(assets, "Dead", 2)
            };
        }
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log("Player designs connected: Farmer 0-3.");
    }

    private static Sprite[] LoadNamed(UnityEngine.Object[] assets, string prefix, int count)
    {
        var sprites = new Sprite[count];
        for (int i = 0; i < count; i++)
            sprites[i] = Array.Find(assets, asset => asset is Sprite && asset.name == $"{prefix} {i}") as Sprite;
        return sprites;
    }

    private static void ConfigureImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
#endif
