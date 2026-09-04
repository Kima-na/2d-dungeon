using UnityEngine;

public static class BossRoster
{
    public static BossHealth SpawnNormal(Transform parent, Vector2 position, DifficultyModifiers modifiers)
    {
        GameObject prefab = Resources.Load<GameObject>("EagleKnight/EagleKnightBoss");
        GameObject boss = prefab != null
            ? Object.Instantiate(prefab, position, Quaternion.identity, parent)
            : CreateEagleKnightFallback(parent, position);
        boss.name = "EagleKnightBoss";

        Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f; body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BossHealth health = boss.GetComponent<BossHealth>();
        health.Configure(Mathf.RoundToInt(10000f * modifiers.EnemyHealth),
            Mathf.RoundToInt(500f * modifiers.Reward), "독수리 기사", 22, 0.8f);
        boss.GetComponent<BossMovement>()?.ConfigureSpeed(2.5f * modifiers.EnemySpeed);
        boss.GetComponent<EagleKnightBossCombat>()?.Configure(20f, 2f, 1.5f,
            Mathf.RoundToInt(42f * modifiers.EnemyDamage), Mathf.RoundToInt(50f * modifiers.EnemyDamage),
            Mathf.RoundToInt(35f * modifiers.EnemyDamage), Mathf.RoundToInt(65f * modifiers.EnemyDamage));
        return health;
    }

    private static GameObject CreateEagleKnightFallback(Transform parent, Vector2 position)
    {
        GameObject boss = new("EagleKnightBoss", typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CapsuleCollider2D), typeof(Damageable), typeof(BossHealth), typeof(BossMovement),
            typeof(EagleKnightAnimator), typeof(EagleKnightBossCombat));
        boss.transform.SetParent(parent, true); boss.transform.position = position;
        SpriteRenderer renderer = boss.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("EagleKnight/Idle_01") ?? MonsterRoster.PlaceholderSprite;
        renderer.sortingOrder = 8; renderer.color = Color.white;
        CreateHitbox(boss.transform, "SlashHitbox", new Vector2(1.8f, 1.1f));
        CreateHitbox(boss.transform, "ChargeHitbox", new Vector2(1.2f, 0.9f));
        CreateHitbox(boss.transform, "SkillHitbox", new Vector2(3.2f, 3.2f));
        return boss;
    }

    private static void CreateHitbox(Transform parent, string name, Vector2 size)
    {
        GameObject child = new(name, typeof(BoxCollider2D)); child.transform.SetParent(parent, false);
        BoxCollider2D hitbox = child.GetComponent<BoxCollider2D>(); hitbox.size = size;
        hitbox.isTrigger = true; hitbox.enabled = false;
    }

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
        health.Configure(5000,
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
            DungeonDifficulty.Easy => 5000,
            DungeonDifficulty.Normal => 10000,
            DungeonDifficulty.Hard => 12500,
            DungeonDifficulty.Nightmare => 15000,
            _ => 5000
        };
        BossHealth health = boss.GetComponent<BossHealth>();
        health.Configure(baseHealth,
            Mathf.RoundToInt(180f * modifiers.Reward), $"{difficulty} Dummy Boss", 0, 0.2f);
        return health;
    }

    public static BossHealth SpawnHard(Transform parent, Vector2 position, DifficultyModifiers modifiers)
    {
        GameObject prefab = Resources.Load<GameObject>("AncientGolem/AncientGolemBoss");
        if (prefab == null) return SpawnDummy(parent, position, DungeonDifficulty.Hard, modifiers);
        GameObject boss = Object.Instantiate(prefab, position, Quaternion.identity, parent);
        boss.name = "AncientGolemBoss";
        BossHealth health = boss.GetComponent<BossHealth>();
        health.Configure(12500,
            Mathf.RoundToInt(550f * modifiers.Reward), "고대 골렘", 30, 1.1f);
        boss.GetComponent<BossMovement>()?.ConfigureSpeed(1.65f * modifiers.EnemySpeed);
        boss.GetComponent<AncientGolemCombat>()?.Configure(modifiers.EnemyDamage);
        return health;
    }

    public static BossHealth SpawnNightmare(Transform parent, Vector2 position,
        DifficultyModifiers modifiers)
    {
        GameObject boss = new("Nightmare Boss");
        boss.SetActive(false);
        boss.AddComponent<SpriteRenderer>();
        boss.AddComponent<Rigidbody2D>();
        boss.AddComponent<CapsuleCollider2D>();
        boss.AddComponent<Damageable>();
        boss.AddComponent<BossHealth>();
        boss.AddComponent<BossMovement>();
        boss.AddComponent<NightmareBossCombat>();
        boss.AddComponent<NightmareBossPhaseController>();
        boss.transform.SetParent(parent, true);
        boss.transform.position = position;

        SpriteRenderer renderer = boss.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("NightmareBoss/Phase1");
        renderer.color = Color.white;
        renderer.sortingOrder = 8;
        boss.transform.localScale = Vector3.one;

        Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        boss.SetActive(true);
        BossHealth health = boss.GetComponent<BossHealth>();
        health.Configure(15000,
            Mathf.RoundToInt(750f * modifiers.Reward), "악몽의 대마도사", 18, 1.2f);
        return health;
    }
}
