#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LootVisualSetup
{
    [MenuItem("Tools/2D Dungeon/Setup Loot Visuals")]
    public static void Setup()
    {
        const string assetPath = "Assets/Resources/LootVisualDatabase.asset";
        LootVisualDatabase database = AssetDatabase.LoadAssetAtPath<LootVisualDatabase>(assetPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<LootVisualDatabase>();
            AssetDatabase.CreateAsset(database, assetPath);
        }
        database.coin = LoadFirstSprite("Assets/Sprites/Coin.png");
        database.chestYellow = LoadFirstSprite("Assets/Sprites/chest/ChestYellow.png");
        database.chestBlue = LoadFirstSprite("Assets/Sprites/chest/ChestBlue.png");
        database.chestGreen = LoadFirstSprite("Assets/Sprites/chest/ChestGreen.png");
        database.chestRed = LoadFirstSprite("Assets/Sprites/chest/ChestRed.png");
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log("Loot visuals connected: coin and four treasure chests.");
    }

    private static Sprite LoadFirstSprite(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite sprite) return sprite;
        return null;
    }
}
#endif
