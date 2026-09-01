#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SkeletonVisualPlaytest
{
    private static int stage; private static double stageAt; private static EnemyAI skeleton; private static Vector3 deathPosition;
    [MenuItem("Tools/2D Dungeon/Playtest Skeleton Visual")]
    public static void Run()
    { stage = -1; stageAt = EditorApplication.timeSinceStartup; EditorApplication.update -= Tick;
      EditorApplication.update += Tick; EditorApplication.EnterPlaymode(); }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - stageAt < (stage < 0 ? 2.5 : 1.5)) return;
        try
        {
            if (stage < 0)
            {
                PlayerStats player = Object.FindAnyObjectByType<PlayerStats>();
                skeleton = MonsterRoster.Spawn(EnemyAI.MonsterType.Skeleton, null,
                    (Vector2)player.transform.position + Vector2.right * 20f, MonsterRoster.PlaceholderSprite);
                Require(skeleton.GetComponent<SkeletonVisualAnimator>() != null, "Skeleton visual prefab was not used.");
                Require(skeleton.GetComponent<SpriteRenderer>().sprite.name.StartsWith("Idle_"), "Skeleton idle sprite missing.");
                SkeletonVisualAnimator visual = skeleton.GetComponent<SkeletonVisualAnimator>();
                visual.SetMovement(Vector2.left, false); Require(skeleton.GetComponent<SpriteRenderer>().flipX, "Skeleton left facing failed.");
                visual.SetMovement(Vector2.right, false); Require(!skeleton.GetComponent<SpriteRenderer>().flipX, "Skeleton right facing failed.");
                deathPosition = skeleton.transform.position; skeleton.Health.Kill(); stage = 0; stageAt = EditorApplication.timeSinceStartup;
            }
            else if (stage == 0)
            {
                Require(!skeleton.Health.IsDead, "Skeleton did not revive after one second.");
                Require(Vector2.Distance(skeleton.transform.position, deathPosition) < 0.01f, "Skeleton revived at a different position.");
                Require(skeleton.GetComponent<SkeletonVisualAnimator>().IsReviving, "Skeleton revival animation was not locking movement.");
                stage = 1; stageAt = EditorApplication.timeSinceStartup;
            }
            else
            {
                Require(!skeleton.GetComponent<SkeletonVisualAnimator>().IsReviving, "Skeleton stayed locked after revival animation.");
                Debug.Log("SKELETON_VISUAL_PLAYTEST_PASS: bidirectional facing and movement locked through revival animation."); Stop();
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
