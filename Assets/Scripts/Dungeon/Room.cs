using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private RoomType roomType;
    [SerializeField] private RoomState state = RoomState.Unvisited;
    [SerializeField] private Door doorTop;
    [SerializeField] private Door doorBottom;
    [SerializeField] private Door doorLeft;
    [SerializeField] private Door doorRight;

    private readonly List<EnemyAI> enemies = new();
    private DungeonGenerator generator;
    private Sprite placeholderSprite;
    private System.Random random;
    private bool combatStarted;
    private bool bossDefeated;
    private BossHealth boss;

    public Vector2Int GridPosition => gridPosition;
    public RoomType Type => roomType;
    public RoomState State => state;
    public bool IsLocked => combatStarted && state != RoomState.Cleared;
    public bool BossAlive => boss != null && !boss.IsDead;
    public bool BossDefeated => bossDefeated;

    public void Initialize(DungeonGenerator owner, Vector2Int coordinate, RoomType type,
        Sprite sprite, System.Random dungeonRandom)
    {
        generator = owner;
        gridPosition = coordinate;
        roomType = type;
        placeholderSprite = sprite;
        random = dungeonRandom;
        transform.localScale = Vector3.one * 1.3f;
        name = $"Room {coordinate.x},{coordinate.y} [{type}]";
        if (doorTop == null || doorBottom == null || doorLeft == null || doorRight == null)
            BuildFallbackVisuals();
        NormalizeRoomGeometrySprites();
        doorTop.Bind(this);
        doorBottom.Bind(this);
        doorLeft.Bind(this);
        doorRight.Bind(this);
        Transform oldLabel = transform.Find("Room Label");
        if (oldLabel != null) oldLabel.gameObject.SetActive(false);
        EnsureRoomDesign();
        CreateSpecialRoomDummy();
        ApplyRoomColor();
    }

    private void NormalizeRoomGeometrySprites()
    {
        Sprite neutral = MonsterRoster.PlaceholderSprite;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            renderer.sprite = neutral;
    }

    public void SetConnection(Vector2Int direction, bool connected)
    {
        Door door = GetDoor(direction);
        if (door != null) door.SetConnected(connected);
    }

    public void Enter()
    {
        if (state == RoomState.Unvisited) state = RoomState.Visited;
        if (roomType == RoomType.Combat && !combatStarted)
        {
            combatStarted = true;
            LockDoors(true);
            SpawnEnemies();
        }
        else if (roomType == RoomType.Boss && !combatStarted)
        {
            combatStarted = true;
            LockDoors(true);
            Transform dummy = transform.Find("Boss Dummy");
            if (dummy != null) dummy.gameObject.SetActive(false);
            boss = generator.Difficulty switch
            {
                DungeonDifficulty.Easy => BossRoster.SpawnEasy(transform, transform.position, generator.Modifiers),
                DungeonDifficulty.Normal => BossRoster.SpawnNormal(transform, transform.position, generator.Modifiers),
                DungeonDifficulty.Hard => BossRoster.SpawnHard(transform, transform.position, generator.Modifiers),
                DungeonDifficulty.Nightmare => BossRoster.SpawnNightmare(transform, transform.position, generator.Modifiers),
                _ => BossRoster.SpawnDummy(transform, transform.position, generator.Difficulty, generator.Modifiers)
            };
            boss.Defeated += OnBossDefeated;
            BossUI.Show(boss);
        }
        else if (roomType != RoomType.Combat && roomType != RoomType.Boss)
        {
            state = RoomState.Cleared;
        }
        ApplyRoomColor();
        generator.NotifyRoomStateChanged();
    }

    public void RequestTransition(Vector2Int direction)
    {
        if (!IsLocked) generator.TryMoveToRoom(this, direction);
    }

    private void Update()
    {
        if (!IsLocked || roomType != RoomType.Combat) return;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyAI enemy = enemies[i];
            if (enemy != null && !enemy.IsPermanentlyDefeated) continue;
            if (enemy != null) enemy.Defeated -= OnEnemyDefeated;
            enemies.RemoveAt(i);
        }
        if (enemies.Count == 0) CompleteCombat();
    }

    public Door GetDoor(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return doorTop;
        if (direction == Vector2Int.down) return doorBottom;
        if (direction == Vector2Int.left) return doorLeft;
        if (direction == Vector2Int.right) return doorRight;
        return null;
    }

    public Vector2 ClampToInterior(Vector2 worldPosition, float padding = 0.65f)
    {
        Vector2 center = transform.position;
        return new Vector2(
            Mathf.Clamp(worldPosition.x, center.x - 10.7f + padding, center.x + 10.7f - padding),
            Mathf.Clamp(worldPosition.y, center.y - 5.5f + padding, center.y + 5.5f - padding));
    }

    private void SpawnEnemies()
    {
        DifficultyModifiers modifiers = generator.Modifiers;
        int count = random.Next(modifiers.MinimumEnemies, modifiers.MaximumEnemies + 1);
        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI * 2f * i / count) + (float)random.NextDouble();
            Vector2 offset = new(Mathf.Cos(angle) * 4.6f, Mathf.Sin(angle) * 3.1f);
            EnemyAI enemy = MonsterRoster.SpawnRandom(transform, (Vector2)transform.position + offset, random, modifiers);
            enemies.Add(enemy);
            enemy.Defeated += OnEnemyDefeated;
            enemy.OffspringSpawned += OnEnemyOffspringSpawned;
        }
    }

    private void OnEnemyOffspringSpawned(EnemyAI offspring)
    {
        if (offspring == null) return;
        enemies.Add(offspring);
        offspring.Defeated += OnEnemyDefeated;
        offspring.OffspringSpawned += OnEnemyOffspringSpawned;
    }

    private void OnEnemyDefeated(EnemyAI defeated)
    {
        defeated.Defeated -= OnEnemyDefeated;
        defeated.OffspringSpawned -= OnEnemyOffspringSpawned;
        enemies.Remove(defeated);
        if (enemies.Count == 0) CompleteCombat();
    }

    private void OnBossDefeated(BossHealth defeated)
    {
        defeated.Defeated -= OnBossDefeated;
        BossUI.Hide(defeated);
        bossDefeated = true;
        boss = null;
        CompleteCombat();
    }

    private void CompleteCombat()
    {
        if (state == RoomState.Cleared) return;
        state = RoomState.Cleared;
        LockDoors(false);
        ApplyRoomColor();
        generator.NotifyRoomStateChanged();
    }

    private void LockDoors(bool value)
    {
        doorTop.SetLocked(value);
        doorBottom.SetLocked(value);
        doorLeft.SetLocked(value);
        doorRight.SetLocked(value);
    }

    private void BuildFallbackVisuals()
    {
        Sprite sprite = placeholderSprite != null ? placeholderSprite : MonsterRoster.PlaceholderSprite;
        CreateSprite("Floor", Vector2.zero, new Vector2(18f, 10f), new Color(0.09f, 0.11f, 0.14f), sprite, -10);

        CreateWallPair("Wall Top", new Vector2(0f, 5f), true, sprite);
        CreateWallPair("Wall Bottom", new Vector2(0f, -5f), true, sprite);
        CreateWallPair("Wall Left", new Vector2(-9f, 0f), false, sprite);
        CreateWallPair("Wall Right", new Vector2(9f, 0f), false, sprite);

        doorTop = CreateDoor("Door Top", new Vector2(0f, 4.75f), new Vector2(2.2f, 0.5f), Vector2Int.up, sprite);
        doorBottom = CreateDoor("Door Bottom", new Vector2(0f, -4.75f), new Vector2(2.2f, 0.5f), Vector2Int.down, sprite);
        doorLeft = CreateDoor("Door Left", new Vector2(-8.75f, 0f), new Vector2(0.5f, 2.2f), Vector2Int.left, sprite);
        doorRight = CreateDoor("Door Right", new Vector2(8.75f, 0f), new Vector2(0.5f, 2.2f), Vector2Int.right, sprite);

    }

    private void CreateSpecialRoomDummy()
    {
        Sprite sprite = placeholderSprite != null ? placeholderSprite : MonsterRoster.PlaceholderSprite;
        if (roomType == RoomType.Treasure && transform.Find("Treasure Chest") == null)
            TreasureChest.Spawn(transform, Vector2.zero);
        if (roomType == RoomType.Shop && transform.Find("Shop Dummy") == null)
        {
            Sprite merchantSprite = null;
            Sprite[] merchantSprites = Resources.LoadAll<Sprite>("Sprites/merchant");
            foreach (Sprite candidate in merchantSprites)
            {
                if (candidate.name == "merchant_0")
                {
                    merchantSprite = candidate;
                    break;
                }
            }
            if (merchantSprite == null && merchantSprites.Length > 0) merchantSprite = merchantSprites[0];

            GameObject counter = CreateSprite("Shop Counter", new Vector2(0f, 1.1f),
                new Vector2(4.5f, 0.7f), new Color(0.35f, 0.2f, 0.08f), sprite, -1);
            BoxCollider2D counterCollider = counter.AddComponent<BoxCollider2D>();
            if (merchantSprite != null)
            {
                counter.GetComponent<SpriteRenderer>().enabled = false;
                counterCollider.enabled = false;
            }

            GameObject merchant = CreateSprite("Shop Dummy", new Vector2(0f, 2f),
                merchantSprite != null ? new Vector2(0.18f, 0.18f) : new Vector2(1.1f, 1.3f),
                merchantSprite != null ? Color.white : new Color(0.2f, 0.9f, 0.55f),
                merchantSprite != null ? merchantSprite : sprite, 0);
            merchant.AddComponent<ShopMerchant>();
        }
        // Boss rooms spawn their gameplay boss on first entry. Keeping a visual
        // dummy here would display two bosses and leave a stray collider behind.
    }

    private void EnsureRoomDesign()
    {
        if (transform.Find("Room Design") != null) return;
        Sprite sprite = placeholderSprite != null ? placeholderSprite : MonsterRoster.PlaceholderSprite;
        GameObject design = new("Room Design");
        design.transform.SetParent(transform, false);
        Color accent = roomType switch
        {
            RoomType.Start => new Color(0.18f, 0.48f, 0.58f, 0.32f),
            RoomType.Treasure => new Color(0.75f, 0.55f, 0.12f, 0.3f),
            RoomType.Shop => new Color(0.12f, 0.62f, 0.38f, 0.3f),
            RoomType.Boss => new Color(0.7f, 0.08f, 0.1f, 0.34f),
            _ => new Color(0.3f, 0.34f, 0.42f, 0.25f)
        };

        for (int x = -6; x <= 6; x += 2)
            CreateSprite("Floor Line V", new Vector2(x, 0f), new Vector2(0.035f, 8.3f),
                accent, sprite, -9, design.transform);
        for (int y = -3; y <= 3; y += 2)
            CreateSprite("Floor Line H", new Vector2(0f, y), new Vector2(16.3f, 0.035f),
                accent, sprite, -9, design.transform);

        CreateSprite("Inner Border Top", new Vector2(0f, 4.25f), new Vector2(16.5f, 0.12f), accent, sprite, -8, design.transform);
        CreateSprite("Inner Border Bottom", new Vector2(0f, -4.25f), new Vector2(16.5f, 0.12f), accent, sprite, -8, design.transform);
        CreateSprite("Inner Border Left", new Vector2(-8.25f, 0f), new Vector2(0.12f, 8.5f), accent, sprite, -8, design.transform);
        CreateSprite("Inner Border Right", new Vector2(8.25f, 0f), new Vector2(0.12f, 8.5f), accent, sprite, -8, design.transform);

        Vector2[] corners = { new(-7.7f, 3.7f), new(7.7f, 3.7f), new(-7.7f, -3.7f), new(7.7f, -3.7f) };
        foreach (Vector2 corner in corners)
            CreateSprite("Corner Stone", corner, new Vector2(0.65f, 0.65f),
                Color.Lerp(accent, Color.white, 0.18f), sprite, -7, design.transform);
    }

    private void CreateWallPair(string baseName, Vector2 center, bool horizontal, Sprite sprite)
    {
        Vector2 partScale = horizontal ? new Vector2(7.9f, 0.5f) : new Vector2(0.5f, 3.9f);
        Vector2 offset = horizontal ? new Vector2(5.05f, 0f) : new Vector2(0f, 3.05f);
        CreateWall(baseName + " A", center - offset, partScale, sprite);
        CreateWall(baseName + " B", center + offset, partScale, sprite);
    }

    private void CreateWall(string objectName, Vector2 localPosition, Vector2 scale, Sprite sprite)
    {
        GameObject wall = CreateSprite(objectName, localPosition, scale, new Color(0.22f, 0.27f, 0.34f), sprite, 0);
        wall.AddComponent<BoxCollider2D>();
    }

    private Door CreateDoor(string objectName, Vector2 localPosition, Vector2 scale,
        Vector2Int direction, Sprite sprite)
    {
        GameObject root = new(objectName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = localPosition;
        BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = direction.x == 0 ? new Vector2(2.2f, 1.3f) : new Vector2(1.3f, 2.2f);

        GameObject visual = CreateSprite("Visual", Vector2.zero, scale,
            new Color(0.12f, 0.13f, 0.16f), sprite, -1, root.transform);
        BoxCollider2D blocker = visual.AddComponent<BoxCollider2D>();
        Door door = root.AddComponent<Door>();
        door.Initialize(this, direction, trigger, blocker, visual.GetComponent<SpriteRenderer>());
        return door;
    }

    private GameObject CreateSprite(string objectName, Vector2 localPosition, Vector2 scale,
        Color color, Sprite sprite, int sortingOrder, Transform customParent = null)
    {
        var go = new GameObject(objectName, typeof(SpriteRenderer));
        go.transform.SetParent(customParent != null ? customParent : transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    private void ApplyRoomColor()
    {
        SpriteRenderer floor = transform.Find("Floor")?.GetComponent<SpriteRenderer>();
        if (floor == null) return;
        Color baseColor = roomType switch
        {
            RoomType.Start => new Color(0.1f, 0.22f, 0.28f),
            RoomType.Treasure => new Color(0.28f, 0.23f, 0.08f),
            RoomType.Shop => new Color(0.08f, 0.24f, 0.18f),
            RoomType.Boss => new Color(0.3f, 0.07f, 0.08f),
            _ => new Color(0.09f, 0.11f, 0.14f)
        };
        floor.color = state == RoomState.Cleared ? Color.Lerp(baseColor, Color.white, 0.12f) : baseColor;
    }
}
