using UnityEngine;

// Keeps existing scenes compatible without requiring a destructive scene rebuild.
public static class DungeonBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureDungeonGenerator()
    {
        if (Object.FindAnyObjectByType<PlayerController>() == null ||
            Object.FindAnyObjectByType<DungeonGenerator>() != null) return;
        new GameObject("Dungeon Generator").AddComponent<DungeonGenerator>();
    }
}
