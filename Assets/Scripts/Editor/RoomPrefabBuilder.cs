#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RoomPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Room.prefab";

    [InitializeOnLoadMethod]
    private static void EnsureRoomPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
        EditorApplication.delayCall += CreateRoomPrefab;
    }

    [MenuItem("Tools/2D Dungeon/Create Room Prefab")]
    public static void CreateRoomPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/TestSquare.png");
        var root = new GameObject("Room", typeof(Room));
        Room room = root.GetComponent<Room>();
        room.Initialize(null, Vector2Int.zero, RoomType.Combat, sprite, new System.Random(0));
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created fallback room prefab: {PrefabPath}");
    }
}
#endif
