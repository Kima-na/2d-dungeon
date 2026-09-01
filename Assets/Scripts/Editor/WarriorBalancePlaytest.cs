#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WarriorBalancePlaytest
{
    private static int stage;
    private static double stageAt;
    private static PlayerStats stats;
    private static AttackController attack;
    private static EquipmentInventoryUI inventoryUI;
    private static Damageable target;
    private static int healthBefore;

    [MenuItem("Tools/2D Dungeon/Playtest Warrior Balance")]
    public static void Run()
    {
        stage = -1; stageAt = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - stageAt < (stage < 0 ? 2.5 : 0.2)) return;
        try { if (stage < 0) Setup(); else Validate(); }
        catch (System.Exception exception) { Debug.LogException(exception); Stop(); }
    }

    private static void Setup()
    {
        stats = Object.FindAnyObjectByType<PlayerStats>(); attack = stats.GetComponent<AttackController>();
        inventoryUI = stats.GetComponent<EquipmentInventoryUI>();
        if (inventoryUI == null) inventoryUI = stats.gameObject.AddComponent<EquipmentInventoryUI>();
        stats.ResetForNewGame(PlayerStats.PlayerClass.Warrior);
        stats.GetComponent<PlayerController>().SetMovementLocked(false);
        Require(stats.MaxHealth >= stats.BaseMaxHealth + 50, "Warrior base-health bonus failed.");
        Require(stats.AttackSpeedMultiplier >= stats.BaseAttackSpeed + 0.099f,
            "Warrior attack-speed bonus failed.");
        GameObject dummy = new("Inventory Attack Test Target", typeof(CircleCollider2D), typeof(Damageable));
        dummy.transform.position = stats.transform.position;
        dummy.GetComponent<CircleCollider2D>().radius = 5f; target = dummy.GetComponent<Damageable>();
        target.Configure(100, 0, false, true); healthBefore = target.CurrentHealth;
        Physics2D.SyncTransforms();
        SetInventoryVisible(true); attack.Attack();
        Require(target.CurrentHealth == healthBefore, "Basic attack fired while inventory was open.");
        SetInventoryVisible(false); attack.Attack(); stage = 0; stageAt = EditorApplication.timeSinceStartup;
    }

    private static void Validate()
    {
        Require(target.CurrentHealth < healthBefore, "Basic attack did not resume after inventory closed.");
        Debug.Log("WARRIOR_BALANCE_PLAYTEST_PASS: warrior HP +50, attack speed +10%, inventory blocks basic attacks.");
        Stop();
    }

    private static void SetInventoryVisible(bool value)
    { inventoryUI.SetVisible(value); }
    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
    private static void Stop()
    { EditorApplication.update -= Tick; if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); }
}
#endif
