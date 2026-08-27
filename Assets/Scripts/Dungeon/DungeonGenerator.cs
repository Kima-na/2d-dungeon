using System;
using System.Collections.Generic;
using UnityEngine;

// Generates one connected room graph and owns room-to-room transitions.
public class DungeonGenerator : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField, Min(2)] private int minimumRooms = 10;
    [SerializeField, Min(2)] private int maximumRooms = 15;
    [SerializeField] private Room roomPrefab;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 12345;
    [SerializeField] private DungeonDifficulty difficulty = DungeonDifficulty.Normal;

    [Header("Layout")]
    [SerializeField] private Vector2 roomWorldSize = new(20f, 12f);
    [SerializeField] private Vector2 entryOffset = new(7.4f, 3.7f);

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private readonly Dictionary<Vector2Int, Room> rooms = new();
    private readonly List<Vector2Int> coordinates = new();
    private System.Random random;
    private PlayerController player;
    private Camera mainCamera;
    private Room currentRoom;
    private DungeonMinimap minimap;
    private float nextTransitionTime;

    public IReadOnlyDictionary<Vector2Int, Room> Rooms => rooms;
    public Room CurrentRoom => currentRoom;
    public int ActiveSeed { get; private set; }
    public DungeonDifficulty Difficulty => difficulty;
    public DifficultyModifiers Modifiers => DungeonDifficultyTable.Get(difficulty);

    public void SetDifficulty(DungeonDifficulty value, bool regenerate = true)
    {
        difficulty = value;
        if (regenerate && Application.isPlaying) GenerateDungeon();
    }

    private void Start()
    {
        DungeonFlowController.Create(this);
    }

    public void GenerateDungeon()
    {
        ClearGeneratedRooms();
        DifficultyModifiers modifiers = Modifiers;
        minimumRooms = modifiers.MinimumRooms;
        maximumRooms = modifiers.MaximumRooms;
        ActiveSeed = useRandomSeed ? Environment.TickCount : seed;
        random = new System.Random(ActiveSeed);

        int targetCount = random.Next(minimumRooms, maximumRooms + 1);
        GenerateCoordinates(targetCount);
        Dictionary<Vector2Int, RoomType> types = AssignRoomTypes();
        Sprite sprite = FindPlaceholderSprite();

        foreach (Vector2Int coordinate in coordinates)
        {
            Room room = roomPrefab != null
                ? Instantiate(roomPrefab, GridToWorld(coordinate), Quaternion.identity, transform)
                : new GameObject().AddComponent<Room>();
            if (roomPrefab == null) room.transform.SetParent(transform, false);
            room.transform.position = GridToWorld(coordinate);
            room.Initialize(this, coordinate, types[coordinate], sprite, random);
            rooms.Add(coordinate, room);
        }

        ConnectRooms();
        foreach (Room room in rooms.Values) room.gameObject.SetActive(false);
        PrepareExistingScene();
        PlacePlayerInStartRoom();
        minimap = DungeonMinimap.Create(this);
#if UNITY_EDITOR
        MonsterRoster.ValidateDeathPipeline(transform);
#endif
        Debug.Log($"Dungeon generated: {rooms.Count} rooms, seed {ActiveSeed}");
        DungeonPlaytestValidator.Validate(this);
    }

    public void BeginDungeon(DungeonDifficulty selectedDifficulty)
    {
        difficulty = selectedDifficulty;
        GenerateDungeon();
    }

    public void ExitDungeon()
    {
        if (minimap != null) Destroy(minimap.gameObject);
        minimap = null;
        currentRoom = null;
        ClearGeneratedRooms();
    }

    public bool TryMoveToRoom(Room fromRoom, Vector2Int direction)
    {
        if (Time.time < nextTransitionTime || fromRoom != currentRoom || fromRoom.IsLocked) return false;
        if (!rooms.TryGetValue(fromRoom.GridPosition + direction, out Room destination)) return false;

        nextTransitionTime = Time.time + 0.35f;
        destination.gameObject.SetActive(true);
        currentRoom = destination;
        Vector2 roomCenter = destination.transform.position;
        Vector2 arrival = direction.x != 0
            ? roomCenter - new Vector2(direction.x * entryOffset.x, 0f)
            : roomCenter - new Vector2(0f, direction.y * entryOffset.y);

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.position = arrival;
        body.linearVelocity = Vector2.zero;
        SnapCamera(roomCenter);
        destination.Enter();
        minimap?.Refresh();
        fromRoom.gameObject.SetActive(false);
        return true;
    }

    public void NotifyRoomStateChanged() => minimap?.Refresh();

    private void GenerateCoordinates(int targetCount)
    {
        var occupied = new HashSet<Vector2Int> { Vector2Int.zero };
        coordinates.Add(Vector2Int.zero);

        while (coordinates.Count < targetCount)
        {
            Vector2Int origin = coordinates[random.Next(coordinates.Count)];
            int directionOffset = random.Next(Directions.Length);
            bool added = false;
            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int candidate = origin + Directions[(directionOffset + i) % Directions.Length];
                if (!occupied.Add(candidate)) continue;
                coordinates.Add(candidate);
                added = true;
                break;
            }
            if (!added) continue;
        }
    }

    private Dictionary<Vector2Int, RoomType> AssignRoomTypes()
    {
        var result = new Dictionary<Vector2Int, RoomType>();
        foreach (Vector2Int coordinate in coordinates) result[coordinate] = RoomType.Combat;
        result[Vector2Int.zero] = RoomType.Start;

        Vector2Int boss = FindFarthestCoordinate();
        result[boss] = RoomType.Boss;
        var specialCandidates = new List<Vector2Int>();
        foreach (Vector2Int coordinate in coordinates)
            if (coordinate != Vector2Int.zero && coordinate != boss) specialCandidates.Add(coordinate);
        Shuffle(specialCandidates);
        if (specialCandidates.Count > 0) result[specialCandidates[0]] = RoomType.Treasure;
        if (specialCandidates.Count > 1) result[specialCandidates[1]] = RoomType.Shop;
        return result;
    }

    private Vector2Int FindFarthestCoordinate()
    {
        var distance = new Dictionary<Vector2Int, int> { [Vector2Int.zero] = 0 };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(Vector2Int.zero);
        Vector2Int farthest = Vector2Int.zero;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (distance[current] > distance[farthest]) farthest = current;
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = current + direction;
                if (!coordinates.Contains(next) || distance.ContainsKey(next)) continue;
                distance[next] = distance[current] + 1;
                queue.Enqueue(next);
            }
        }
        return farthest;
    }

    private void ConnectRooms()
    {
        foreach (KeyValuePair<Vector2Int, Room> pair in rooms)
            foreach (Vector2Int direction in Directions)
                pair.Value.SetConnection(direction, rooms.ContainsKey(pair.Key + direction));
    }

    private void PlacePlayerInStartRoom()
    {
        player = FindAnyObjectByType<PlayerController>();
        mainCamera = Camera.main;
        if (player == null) return;
        currentRoom = rooms[Vector2Int.zero];
        currentRoom.gameObject.SetActive(true);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.position = currentRoom.transform.position;
        body.linearVelocity = Vector2.zero;
        SnapCamera(currentRoom.transform.position);
        currentRoom.Enter();
    }

    private void SnapCamera(Vector2 position)
    {
        if (mainCamera == null) return;
        mainCamera.transform.position = new Vector3(position.x, position.y, mainCamera.transform.position.z);
    }

    private void PrepareExistingScene()
    {
        string[] legacyObjects = { "Test Ground", "Wall Top", "Wall Bottom", "Wall Left", "Wall Right", "Dummy" };
        foreach (string objectName in legacyObjects)
        {
            GameObject oldObject = GameObject.Find(objectName);
            if (oldObject != null && oldObject.transform.parent != transform) oldObject.SetActive(false);
        }
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>())
            if (!enemy.transform.IsChildOf(transform)) Destroy(enemy.gameObject);
    }

    private Sprite FindPlaceholderSprite()
    {
        SpriteRenderer renderer = FindAnyObjectByType<PlayerController>()?.GetComponent<SpriteRenderer>();
        return renderer != null && renderer.sprite != null ? renderer.sprite : MonsterRoster.PlaceholderSprite;
    }

    private Vector3 GridToWorld(Vector2Int coordinate) =>
        new(coordinate.x * roomWorldSize.x, coordinate.y * roomWorldSize.y, 0f);

    private void ClearGeneratedRooms()
    {
        rooms.Clear();
        coordinates.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.25f, 0.8f, 1f, 0.75f);
        foreach (Vector2Int coordinate in coordinates)
        {
            Vector3 center = GridToWorld(coordinate);
            Gizmos.DrawWireCube(center, new Vector3(roomWorldSize.x - 1f, roomWorldSize.y - 1f, 0f));
            foreach (Vector2Int direction in Directions)
                if (coordinates.Contains(coordinate + direction))
                    Gizmos.DrawLine(center, GridToWorld(coordinate + direction));
#if UNITY_EDITOR
            UnityEditor.Handles.Label(center, $"[{coordinate.x},{coordinate.y}]");
#endif
        }
    }
}
