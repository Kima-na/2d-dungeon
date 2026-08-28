using UnityEngine;

public static class BossRoster
{
    public static BossHealth SpawnEasy(Transform parent, Vector2 position, DifficultyModifiers modifiers)
    {
        BossVisualDatabase database = Resources.Load<BossVisualDatabase>("BossVisualDatabase");
        GameObject boss;
        if (database != null && database.easyBossPrefab != null)
            boss = Object.Instantiate(database.easyBossPrefab, position, Quaternion.identity, parent);
        else
        {
            boss = new GameObject("Easy Boss", typeof(SpriteRenderer), typeof(Rigidbody2D),
                typeof(BoxCollider2D), typeof(Damageable), typeof(Animator), typeof(BossHealth),
                typeof(BossAnimator), typeof(BossMovement), typeof(BossCombat));
            boss.transform.SetParent(parent, true);
            boss.transform.position = position;
            boss.GetComponent<SpriteRenderer>().sprite = MonsterRoster.PlaceholderSprite;
            boss.GetComponent<SpriteRenderer>().color = new Color(0.72f, 0.08f, 0.12f);
        }

        BossHealth health = boss.GetComponent<BossHealth>();
        health.Configure(Mathf.RoundToInt(10000f * modifiers.EnemyHealth),
            Mathf.RoundToInt(300f * modifiers.Reward));
        return health;
    }

    public static BossHealth SpawnDummy(Transform parent, Vector2 position,
        DungeonDifficulty difficulty, DifficultyModifiers modifiers)
    {
        GameObject boss = new($"Dummy Boss - {difficulty}", typeof(SpriteRenderer),
            typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Damageable), typeof(BossHealth));
        boss.transform.SetParent(parent, true);
        boss.transform.position = position;
        boss.transform.localScale = Vector3.one * 2.2f;

        SpriteRenderer renderer = boss.GetComponent<SpriteRenderer>();
        renderer.sprite = MonsterRoster.PlaceholderSprite;
        renderer.color = difficulty switch
        {
            DungeonDifficulty.Hard => new Color(0.75f, 0.18f, 0.15f),
            DungeonDifficulty.Nightmare => new Color(0.5f, 0.12f, 0.7f),
            _ => new Color(0.85f, 0.55f, 0.12f)
        };
        Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        boss.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);

        int baseHealth = difficulty switch
        {
            DungeonDifficulty.Hard => 15000,
            DungeonDifficulty.Nightmare => 30000,
            _ => 8000
        };
        BossHealth health = boss.GetComponent<BossHealth>();
        health.Configure(Mathf.RoundToInt(baseHealth * modifiers.EnemyHealth),
            Mathf.RoundToInt(180f * modifiers.Reward), $"{difficulty} Dummy Boss", 0, 0.2f);
        return health;
    }
}
