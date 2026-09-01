#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EquipmentDropPlaytest
{
    private static int stage; private static double stageAt;
    private static DungeonGenerator dungeon; private static PlayerStats player;
    private static EquipmentInventory inventory; private static int ownedBefore;
    private static Room bossRoom; private static BossHealth boss;

    [MenuItem("Tools/2D Dungeon/Playtest Equipment Drops")]
    public static void Run()
    { stage = -1; stageAt = EditorApplication.timeSinceStartup; EditorApplication.update -= Tick;
      EditorApplication.update += Tick; EditorApplication.EnterPlaymode(); }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - stageAt < (stage < 0 ? 2.5 : 1.4)) return;
        try { if (stage < 0) TestMonsterDrop(); else if (stage == 0) CollectMonsterDrop();
              else if (stage == 1) KillBoss(); else ValidateBossDrop(); }
        catch (System.Exception exception) { Debug.LogException(exception); Stop(); }
    }

    private static void TestMonsterDrop()
    {
        dungeon = Object.FindAnyObjectByType<DungeonGenerator>(); dungeon.BeginDungeon(DungeonDifficulty.Normal);
        player = Object.FindAnyObjectByType<PlayerStats>(); inventory = player.GetComponent<EquipmentInventory>();
        Room room = dungeon.Rooms.Values.First(value => value.Type == RoomType.Combat);
        room.gameObject.SetActive(true); room.Enter();
        EnemyAI enemy = Object.FindObjectsByType<EnemyAI>().First(value => value.transform.IsChildOf(room.transform));
        SerializedObject serialized = new(enemy); serialized.FindProperty("equipmentDropChance").floatValue = 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        MovePlayer((Vector2)enemy.transform.position + Vector2.right * 6f); enemy.Health.Kill();
        stage = 0; stageAt = EditorApplication.timeSinceStartup;
    }

    private static void CollectMonsterDrop()
    {
        EquipmentPickup pickup = Object.FindObjectsByType<EquipmentPickup>()
            .OrderBy(value => Vector2.Distance(value.transform.position, player.transform.position)).FirstOrDefault();
        Require(pickup != null, "Forced normal-monster equipment drop was not created.");
        ownedBefore = inventory.OwnedItems.Count; MovePlayer(pickup.transform.position);
        stage = 1; stageAt = EditorApplication.timeSinceStartup;
    }

    private static void KillBoss()
    {
        Require(inventory.OwnedItems.Count == ownedBefore + 1, "Monster equipment pickup was not added to inventory.");
        bossRoom = dungeon.Rooms.Values.First(value => value.Type == RoomType.Boss);
        bossRoom.gameObject.SetActive(true); bossRoom.Enter();
        boss = Object.FindObjectsByType<BossHealth>().First(value => value.transform.IsChildOf(bossRoom.transform));
        MovePlayer((Vector2)boss.transform.position + Vector2.right * 8f); boss.Damageable.Kill();
        stage = 2; stageAt = EditorApplication.timeSinceStartup;
    }

    private static void ValidateBossDrop()
    {
        Require(Object.FindAnyObjectByType<EquipmentPickup>() != null, "Guaranteed boss equipment drop was not created.");
        var appearances = new System.Collections.Generic.HashSet<string>();
        foreach (EquipmentRarity rarity in System.Enum.GetValues(typeof(EquipmentRarity)))
        {
            EquipmentPickup sample = EquipmentPickup.Spawn(new Vector2(1000f + (int)rarity, 1000f), rarity);
            SpriteRenderer renderer = sample.GetComponent<SpriteRenderer>();
            appearances.Add($"{renderer.sprite?.name}:{renderer.color}:{sample.transform.localScale.x:0.00}");
        }
        Require(appearances.Count == 5, "Equipment rarity chests were not visually distinct.");
        Debug.Log("EQUIPMENT_DROP_PLAYTEST_PASS: low-chance monster path, pickup collection and guaranteed boss drop.");
        Stop();
    }
    private static void MovePlayer(Vector2 position)
    { Rigidbody2D body = player.GetComponent<Rigidbody2D>(); if (body != null) body.position = position;
      player.transform.position = position; Physics2D.SyncTransforms(); }
    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
    private static void Stop()
    { EditorApplication.update -= Tick; if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); }
}
#endif
