#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WeaponVisibilityPlaytest
{
    private static int stage; private static double stageAt; private static PlayerStats stats;
    private static ArcherController archer; private static MageController mage; private static Transform bow, staff, spellbook;
    [MenuItem("Tools/2D Dungeon/Playtest Weapon Visibility")]
    public static void Run()
    { stage = -1; stageAt = EditorApplication.timeSinceStartup; EditorApplication.update -= Tick;
      EditorApplication.update += Tick; EditorApplication.EnterPlaymode(); }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - stageAt < (stage < 0 ? 2.5 : 0.32)) return;
        try
        {
            if (stage < 0)
            {
                stats = Object.FindAnyObjectByType<PlayerStats>(); stats.GetComponent<PlayerController>().SetMovementLocked(false);
                archer = stats.GetComponent<ArcherController>(); mage = stats.GetComponent<MageController>();
                bow = stats.transform.Find("Equipped Bow"); staff = stats.transform.Find("Equipped Staff");
                spellbook = stats.transform.Find("Equipped Spellbook");
                Require(bow != null && staff != null && spellbook != null && !bow.gameObject.activeSelf &&
                    !staff.gameObject.activeSelf && !spellbook.gameObject.activeSelf,
                    "Class weapons must be hidden while idle.");
                stats.SelectClass(PlayerStats.PlayerClass.Archer); archer.Shoot();
                AttackController attack = stats.GetComponent<AttackController>();
                attack.ShowClassWeaponForAttack(Vector2.right, 0.22f);
                typeof(AttackController).GetMethod("UpdateClassWeaponPose",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(attack, null);
                Require(bow.gameObject.activeSelf, "Bow did not appear during attack.");
                float bowAngle = Mathf.Repeat(bow.eulerAngles.z, 360f);
                Require(bowAngle > 170f && bowAngle < 190f, "Bow direction was not reversed toward its string side.");
                stage = 0; stageAt = EditorApplication.timeSinceStartup;
            }
            else if (stage == 0)
            {
                Require(!bow.gameObject.activeSelf, "Bow stayed visible after attack.");
                stats.SelectClass(PlayerStats.PlayerClass.Mage); mage.Cast();
                Require(staff.gameObject.activeSelf || spellbook.gameObject.activeSelf, "Mage weapon did not appear during attack.");
                stage = 1; stageAt = EditorApplication.timeSinceStartup;
            }
            else
            {
                Require(!staff.gameObject.activeSelf && !spellbook.gameObject.activeSelf, "Mage weapon stayed visible after attack.");
                Debug.Log("WEAPON_VISIBILITY_PLAYTEST_PASS: idle hidden, attack-only visuals and reversed bow orientation."); Stop();
            }
        }
        catch (System.Exception exception) { Debug.LogException(exception); Stop(); }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
    private static void Stop()
    { EditorApplication.update -= Tick; if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); }
}
#endif
