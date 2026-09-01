#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EagleKnightBossPlaytest
{
    private static int stage;
    private static double stageAt;
    private static Room room;
    private static BossHealth boss;
    private static EagleKnightBossCombat combat;
    private static PlayerStats player;
    private static int playerHealth;
    private static int goldBefore;

    [MenuItem("Tools/2D Dungeon/Playtest Eagle Knight Boss")]
    public static void Run()
    {
        stage = -1; stageAt = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        double delay = stage < 0 ? 2.5 : stage == 0 ? 2.8 : 1.8;
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - stageAt < delay) return;
        try
        {
            if (stage < 0) Setup();
            else if (stage == 0) ValidateCombatAndKill();
            else ValidateDeathAndStop();
        }
        catch (System.Exception exception) { Debug.LogException(exception); Stop(); }
    }

    private static void Setup()
    {
        DungeonGenerator dungeon = Object.FindAnyObjectByType<DungeonGenerator>();
        Require(dungeon != null, "DungeonGenerator missing.");
        dungeon.BeginDungeon(DungeonDifficulty.Normal);
        room = dungeon.Rooms.Values.First(candidate => candidate.Type == RoomType.Boss);
        room.gameObject.SetActive(true); room.Enter();
        boss = Object.FindObjectsByType<BossHealth>().First(candidate => candidate.transform.IsChildOf(room.transform));
        combat = boss.GetComponent<EagleKnightBossCombat>(); player = Object.FindAnyObjectByType<PlayerStats>();
        Require(boss.name == "EagleKnightBoss" && boss.BossName == "독수리 기사", "Normal boss identity failed.");
        Require(room.GridPosition != Vector2Int.zero && Vector2.Distance(boss.transform.position, room.transform.position) < 0.01f,
            "Eagle Knight was not placed in the generated boss room.");
        Require(combat != null && boss.GetComponent<EagleKnightAnimator>() != null, "Eagle Knight AI/animation missing.");
        Require(boss.GetComponent<Rigidbody2D>() != null && boss.GetComponent<CapsuleCollider2D>() != null, "Boss body physics missing.");
        Require(boss.transform.Find("SlashHitbox") != null && boss.transform.Find("ChargeHitbox") != null &&
            boss.transform.Find("SkillHitbox") != null, "Separated attack hitboxes missing.");
        Require(Object.FindAnyObjectByType<BossUI>() != null, "Boss HP UI missing.");
        Require(boss.MaxHealth == 18000, "Normal boss base HP 10000 must receive difficulty scaling.");
        int before = boss.CurrentHealth; boss.Damageable.TakeDamage(100);
        Require(boss.CurrentHealth == before - 78, "Boss defense calculation failed.");
        MovePlayer((Vector2)boss.transform.position + Vector2.right * 1.5f);
        playerHealth = player.CurrentHealth; Next(0);
    }

    private static void ValidateCombatAndKill()
    {
        Require(player.CurrentHealth < playerHealth, "Telegraphed Eagle Descent did not damage the nearby player.");
        Require(!boss.IsDead, "Boss died unexpectedly during combat test.");
        goldBefore = player.Gold;
        boss.Damageable.Kill(); Require(boss.IsDead && !combat.enabled, "Boss attacks did not stop on death.");
        Next(1);
    }

    private static void ValidateDeathAndStop()
    {
        Require(room.State == RoomState.Cleared, "Boss room did not clear after death.");
        Require(player.Gold > goldBefore || Object.FindObjectsByType<GoldPickup>().Length > 0,
            "Existing loot system did not grant or create boss gold rewards.");
        Debug.Log("EAGLE_KNIGHT_PLAYTEST_PASS: NORMAL spawn, Korean boss UI, HP/defense, AI detection, telegraph/skill damage, hitbox separation, death stop, loot and room clear.");
        Stop();
    }

    private static void MovePlayer(Vector2 position)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>(); if (body != null) body.position = position;
        player.transform.position = position; Physics2D.SyncTransforms();
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
    private static void Next(int next) { stage = next; stageAt = EditorApplication.timeSinceStartup; }
    private static void Stop()
    { EditorApplication.update -= Tick; if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); }
}
#endif
