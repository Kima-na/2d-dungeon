using System.Collections.Generic;
using UnityEngine;

public static class DungeonPlaytestValidator
{
    public static bool Validate(DungeonGenerator dungeon)
    {
        if (dungeon == null || dungeon.Rooms.Count == 0) return Fail("No dungeon rooms were generated.");
        DifficultyModifiers modifiers = dungeon.Modifiers;
        if (dungeon.Rooms.Count < modifiers.MinimumRooms || dungeon.Rooms.Count > modifiers.MaximumRooms)
            return Fail($"Room count {dungeon.Rooms.Count} is outside the {dungeon.Difficulty} range.");
        if (!dungeon.Rooms.TryGetValue(Vector2Int.zero, out Room start) || start.Type != RoomType.Start)
            return Fail("The start room is missing.");

        int bossCount = 0;
        var visited = new HashSet<Vector2Int> { Vector2Int.zero };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(Vector2Int.zero);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Room room in dungeon.Rooms.Values) if (room.Type == RoomType.Boss) bossCount++;
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;
                if (dungeon.Rooms.ContainsKey(next) && visited.Add(next)) queue.Enqueue(next);
            }
        }
        if (visited.Count != dungeon.Rooms.Count) return Fail("The room graph is not fully connected.");
        if (bossCount != 1) return Fail($"Expected exactly one boss room, found {bossCount}.");
        Debug.Log($"Dungeon playtest passed: {dungeon.Difficulty}, {dungeon.Rooms.Count} connected rooms, seed {dungeon.ActiveSeed}.");
        return true;
    }

    private static bool Fail(string message)
    {
        Debug.LogError($"Dungeon playtest failed: {message}");
        return false;
    }
}
