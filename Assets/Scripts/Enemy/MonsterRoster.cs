using UnityEngine;

public static class MonsterRoster
{
    private static Sprite placeholderSprite;
    public static Sprite PlaceholderSprite
    {
        get
        {
            if (placeholderSprite == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.name = "Runtime Monster Placeholder";
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                placeholderSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                placeholderSprite.name = "Runtime Monster Placeholder";
            }
            return placeholderSprite;
        }
    }

    public static void CreateRoster(Sprite sprite)
    {
        Spawn(EnemyAI.MonsterType.Slime, null, new Vector2(0f, 2.8f), sprite);
        Spawn(EnemyAI.MonsterType.GoblinWarrior, null, new Vector2(3f, 2.5f), sprite);
        Spawn(EnemyAI.MonsterType.GoblinArcher, null, new Vector2(6f, 2.2f), sprite);
        Spawn(EnemyAI.MonsterType.GoblinMage, null, new Vector2(6f, -2.2f), sprite);
        Spawn(EnemyAI.MonsterType.Skeleton, null, new Vector2(3f, -2.5f), sprite);
        Spawn(EnemyAI.MonsterType.Berserker, null, new Vector2(0f, -2.8f), sprite);
    }

    public static EnemyAI SpawnRandom(Transform parent, Vector2 position, System.Random random)
    {
        EnemyAI.MonsterType type = (EnemyAI.MonsterType)random.Next(0, 6);
        return Spawn(type, parent, position, PlaceholderSprite);
    }

    public static EnemyAI SpawnRandom(Transform parent, Vector2 position, System.Random random,
        DifficultyModifiers modifiers)
    {
        EnemyAI.MonsterType type = (EnemyAI.MonsterType)random.Next(0, 6);
        return Spawn(type, parent, position, PlaceholderSprite, modifiers);
    }

    public static EnemyAI Spawn(EnemyAI.MonsterType type, Transform parent, Vector2 position, Sprite sprite = null)
        => Spawn(type, parent, position, sprite, DungeonDifficultyTable.Get(DungeonDifficulty.Normal));

    public static EnemyAI Spawn(EnemyAI.MonsterType type, Transform parent, Vector2 position, Sprite sprite,
        DifficultyModifiers modifiers)
        => SpawnInternal(type, parent, position, sprite, modifiers, 0);

    public static EnemyAI SpawnSlimeDivision(Transform parent, Vector2 position, int generation,
        DifficultyModifiers modifiers)
        => SpawnInternal(EnemyAI.MonsterType.Slime, parent, position, PlaceholderSprite,
            modifiers, Mathf.Clamp(generation, 1, 3));

    private static EnemyAI SpawnInternal(EnemyAI.MonsterType type, Transform parent, Vector2 position,
        Sprite sprite, DifficultyModifiers modifiers, int slimeGeneration)
    {
        string objectName;
        Vector2 scale;
        Color color;
        int health;
        int reward;
        float speed;
        float sight;
        float range;
        int damage;
        float cooldown;
        float keepDistance = 0f;

        switch (type)
        {
            case EnemyAI.MonsterType.Slime:
                objectName = "Slime"; scale = Vector2.one * 1.35f; color = Color.white;
                health = 28; reward = 25; speed = 1.8f; sight = 5f; range = 1f; damage = 7; cooldown = 1.25f; break;
            case EnemyAI.MonsterType.GoblinWarrior:
                objectName = "Goblin Warrior"; scale = Vector2.one; color = new Color(0.25f, 0.65f, 0.2f);
                health = 45; reward = 40; speed = 2.2f; sight = 6f; range = 1.15f; damage = 11; cooldown = 1.1f; break;
            case EnemyAI.MonsterType.GoblinArcher:
                objectName = "Goblin Archer"; scale = new Vector2(0.9f, 1f); color = new Color(0.65f, 0.75f, 0.18f);
                health = 34; reward = 50; speed = 2f; sight = 7.5f; range = 5.5f; damage = 9; cooldown = 1.5f; keepDistance = 3.5f; break;
            case EnemyAI.MonsterType.GoblinMage:
                objectName = "Goblin Mage"; scale = new Vector2(0.9f, 1.05f); color = new Color(0.6f, 0.25f, 0.9f);
                health = 32; reward = 60; speed = 1.8f; sight = 8f; range = 5.8f; damage = 13; cooldown = 1.9f; keepDistance = 4f; break;
            case EnemyAI.MonsterType.Skeleton:
                objectName = "Skeleton"; scale = new Vector2(0.85f, 1.1f); color = new Color(0.82f, 0.82f, 0.72f);
                health = 38; reward = 55; speed = 2.15f; sight = 6f; range = 1.1f; damage = 10; cooldown = 1.15f; break;
            default:
                objectName = "Berserker"; scale = new Vector2(1.25f, 1.25f); color = new Color(0.75f, 0.12f, 0.1f);
                health = 85; reward = 90; speed = 1.75f; sight = 6.5f; range = 1.3f; damage = 16; cooldown = 1.35f; break;
        }

        if (type == EnemyAI.MonsterType.Slime && slimeGeneration > 0)
        {
            float generationScale = Mathf.Pow(0.72f, slimeGeneration);
            scale *= generationScale;
            health = Mathf.Max(6, Mathf.RoundToInt(health * Mathf.Pow(0.58f, slimeGeneration)));
            reward = Mathf.Max(4, Mathf.RoundToInt(reward * Mathf.Pow(0.5f, slimeGeneration)));
            damage = Mathf.Max(2, Mathf.RoundToInt(damage * Mathf.Pow(0.75f, slimeGeneration)));
            speed *= 1f + slimeGeneration * 0.12f;
        }

        GameObject enemy = null;
        if (type == EnemyAI.MonsterType.Slime)
        {
            SlimeVisualDatabase database = Resources.Load<SlimeVisualDatabase>("SlimeVisualDatabase");
            if (database != null && database.slimePrefab != null)
                enemy = Object.Instantiate(database.slimePrefab);
        }
        else if ((sprite == null || sprite == PlaceholderSprite) &&
                 type is EnemyAI.MonsterType.GoblinWarrior or
                 EnemyAI.MonsterType.GoblinArcher or EnemyAI.MonsterType.GoblinMage)
        {
            GoblinWarriorVisualDatabase database =
                Resources.Load<GoblinWarriorVisualDatabase>("GoblinWarriorVisualDatabase");
            GameObject prefab = database != null ? database.GetEnemyPrefab(type) : null;
            if (prefab != null) enemy = Object.Instantiate(prefab);
        }
        if (enemy == null)
            enemy = new GameObject(objectName, typeof(SpriteRenderer), typeof(Rigidbody2D),
                typeof(BoxCollider2D), typeof(Damageable), typeof(EnemyAI));
        enemy.name = objectName;
        if (parent != null) enemy.transform.SetParent(parent, true);
        enemy.transform.position = position;
        enemy.transform.localScale = scale;
        ConfigureHitbox(enemy.GetComponent<Collider2D>(), type);
        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        if (renderer.sprite == null) renderer.sprite = sprite != null ? sprite : PlaceholderSprite;
        bool usesGoblinSheet = type is EnemyAI.MonsterType.GoblinWarrior or
            EnemyAI.MonsterType.GoblinArcher or EnemyAI.MonsterType.GoblinMage;
        renderer.color = usesGoblinSheet && enemy.GetComponent<Animator>() != null
            ? Color.white : color;
        if (usesGoblinSheet)
            enemy.GetComponent<Damageable>().SetDamageReduction(type == EnemyAI.MonsterType.GoblinWarrior ? 2 : 1);
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        ai.Configure(type, Mathf.Max(1, Mathf.RoundToInt(health * modifiers.EnemyHealth)),
            Mathf.Max(1, Mathf.RoundToInt(reward * modifiers.Reward)), speed * modifiers.EnemySpeed,
            sight, range, Mathf.Max(1, Mathf.RoundToInt(damage * modifiers.EnemyDamage)), cooldown, keepDistance);
        if (type == EnemyAI.MonsterType.Slime)
            ai.ConfigureSlimeDivision(slimeGeneration, modifiers);
        return ai;
    }

    private static void ConfigureHitbox(Collider2D collider, EnemyAI.MonsterType type)
    {
        if (collider == null) return;
        collider.isTrigger = false;
        if (collider is BoxCollider2D box)
        {
            bool slime = type == EnemyAI.MonsterType.Slime;
            bool goblin = type is EnemyAI.MonsterType.GoblinWarrior or
                EnemyAI.MonsterType.GoblinArcher or EnemyAI.MonsterType.GoblinMage;
            bool berserker = type == EnemyAI.MonsterType.Berserker;
            box.size = slime ? new Vector2(0.64f, 0.46f) : goblin ? new Vector2(0.46f, 0.52f) :
                berserker ? new Vector2(0.62f, 0.7f) : new Vector2(0.54f, 0.68f);
            box.offset = slime ? new Vector2(0f, -0.11f) :
                goblin ? new Vector2(0f, 0.26f) : new Vector2(0f, -0.08f);
        }
        else if (collider is CapsuleCollider2D capsule)
        {
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = type == EnemyAI.MonsterType.Slime
                ? new Vector2(0.64f, 0.46f) : new Vector2(0.54f, 0.72f);
            capsule.offset = type == EnemyAI.MonsterType.Slime
                ? new Vector2(0f, -0.11f) : new Vector2(0f, -0.08f);
        }
        else if (collider is CircleCollider2D circle)
        {
            circle.radius = type == EnemyAI.MonsterType.Slime ? 0.3f : 0.34f;
            circle.offset = new Vector2(0f, -0.08f);
        }
    }

#if UNITY_EDITOR
    public static void ValidateDeathPipeline(Transform temporaryParent)
    {
        EnemyAI testEnemy = Spawn(EnemyAI.MonsterType.Slime, temporaryParent,
            new Vector2(10000f, 10000f), PlaceholderSprite);
        bool defeatedEventRaised = false;
        testEnemy.SetDropsEnabled(false);
        testEnemy.Defeated += _ => defeatedEventRaised = true;
        Damageable damageable = testEnemy.GetComponent<Damageable>();
        damageable.TakeDamage(int.MaxValue);
        bool passed = damageable.IsDead && defeatedEventRaised && !testEnemy.gameObject.activeSelf;
        Debug.Assert(passed, "Monster death pipeline validation failed.");
        if (passed) Debug.Log("Monster death pipeline validated: damage, death event, and deactivation succeeded.");
        Object.Destroy(testEnemy.gameObject);
    }
#endif
}
