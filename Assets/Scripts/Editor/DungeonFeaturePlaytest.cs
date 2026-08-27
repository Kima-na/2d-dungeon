#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DungeonFeaturePlaytest
{
    private static int stage;
    private static double stageStarted;
    private static Room bossRoom;
    private static BossHealth boss;
    private static BossCombat combat;
    private static PlayerStats player;
    private static int healthBefore;

    public static void Run()
    {
        stage = -1;
        stageStarted = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (stage < 0 && EditorApplication.timeSinceStartup - stageStarted < 2.5f) return;
        if (stage >= 0 && stage < 4 && (combat == null || combat.IsAttacking)) return;
        if (stage >= 4 && EditorApplication.timeSinceStartup - stageStarted < 1.8f) return;
        try
        {
            if (stage < 0) Setup();
            else if (stage == 0) ValidateSkill1AndStartSkill2();
            else if (stage == 1) ValidateSkill2AndStartSkill3();
            else if (stage == 2) ValidateSkill3AndStartSkill4();
            else if (stage == 3) ValidateSkill4AndKillBoss();
            else ValidateDeath();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Finish(1);
        }
    }

    private static void Setup()
    {
        DungeonGenerator dungeon = Object.FindAnyObjectByType<DungeonGenerator>();
        Require(dungeon != null && dungeon.Rooms.Count > 0, "Dungeon did not generate.");
        Room combatRoom = dungeon.Rooms.Values.First(room => room.Type == RoomType.Combat);
        combatRoom.gameObject.SetActive(true);
        combatRoom.Enter();
        EnemyAI enemy = Object.FindObjectsByType<EnemyAI>()
            .First(item => item.transform.IsChildOf(combatRoom.transform));
        Require(enemy.GetComponent<WorldHealthBar>() != null, "Monster HP bar missing.");
        int enemyHealth = enemy.Health.CurrentHealth;
        enemy.Health.TakeDamage(1);
        Require(enemy.Health.CurrentHealth == enemyHealth - 1, "Monster damage/HP update failed.");

        bossRoom = dungeon.Rooms.Values.First(room => room.Type == RoomType.Boss);
        bossRoom.gameObject.SetActive(true);
        bossRoom.Enter();
        boss = Object.FindObjectsByType<BossHealth>().First(item => item.transform.IsChildOf(bossRoom.transform));
        combat = boss.GetComponent<BossCombat>();
        player = Object.FindAnyObjectByType<PlayerStats>();
        combat.SetAutomaticAttacks(false);
        Require(boss.GetComponent<WorldHealthBar>() != null, "Boss world HP bar missing.");
        Require(Object.FindAnyObjectByType<PlayerHUD>() != null, "Player HUD missing.");
        Require(boss.transform.Find("Skill1Hitbox") != null && boss.transform.Find("Skill2Hitbox") != null &&
            boss.transform.Find("Skill3Hitbox") != null && boss.transform.Find("Skill4Hitbox") != null,
            "Boss skill hitboxes missing.");
        int bossHealth = boss.CurrentHealth;
        boss.Damageable.TakeDamage(20);
        Require(boss.CurrentHealth == bossHealth - 10, "Boss defense/damage calculation failed.");
        MovePlayer(boss.transform.position + Vector3.right * 3f);
        healthBefore = player.CurrentHealth;
        Require(combat.TryUseSkill(BossCombat.SkillType.DarkShockwave), "Skill 1 did not start.");
        NextStage(0);
    }

    private static void ValidateSkill1AndStartSkill2()
    {
        Require(player.CurrentHealth < healthBefore, "Skill 1 projectile did not damage player.");
        player.Heal(999);
        MovePlayer(boss.transform.position + Vector3.right);
        healthBefore = player.CurrentHealth;
        Require(combat.TryUseSkill(BossCombat.SkillType.Hellfall), "Skill 2 did not start.");
        NextStage(1);
    }

    private static void ValidateSkill2AndStartSkill3()
    {
        Require(player.CurrentHealth < healthBefore, "Skill 2 area impact did not damage player.");
        player.Heal(999);
        MovePlayer(boss.transform.position + Vector3.right);
        healthBefore = player.CurrentHealth;
        Require(combat.TryUseSkill(BossCombat.SkillType.SpinningSlash), "Skill 3 did not start.");
        NextStage(2);
    }

    private static void ValidateSkill3AndStartSkill4()
    {
        Require(player.CurrentHealth < healthBefore, "Skill 3 multi-hit did not damage player.");
        player.Heal(999);
        MovePlayer(boss.transform.position + Vector3.right * 3f);
        Require(combat.TryUseSkill(BossCombat.SkillType.DarkSummon), "Skill 4 did not start.");
        NextStage(3);
    }

    private static void ValidateSkill4AndKillBoss()
    {
        Require(Object.FindObjectsByType<EnemyAI>().Any(enemy => enemy.name == "Summoned Shadow"),
            "Skill 4 did not create EnemyAI-based summons.");
        boss.Damageable.Kill();
        Require(boss.IsDead && !combat.enabled, "Boss did not stop combat on death.");
        NextStage(4);
    }

    private static void ValidateDeath()
    {
        Require(bossRoom.State == RoomState.Cleared, "Boss defeat did not clear the room after death animation.");
        Debug.Log("BOSS_FEATURE_PLAYTEST_PASS: player UI, boss world HP, defense, hitboxes, skills 1-4, player damage, boss hit/death and room clear.");
        Finish(0);
    }

    private static void MovePlayer(Vector2 position)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.position = position;
        player.transform.position = position;
        Physics2D.SyncTransforms();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }

    private static void NextStage(int next)
    {
        stage = next;
        stageStarted = EditorApplication.timeSinceStartup;
    }

    private static void Finish(int exitCode)
    {
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
        EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
    }
}
#endif
