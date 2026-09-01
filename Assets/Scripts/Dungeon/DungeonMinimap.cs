using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class DungeonMinimap : MonoBehaviour
{
    private sealed class RoomVisual
    {
        public Room Room;
        public RectTransform Root;
        public Image Fill, Border, Player;
        public Text Icon;
    }

    private sealed class LinkVisual
    {
        public Room A, B;
        public Image Image;
    }

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private readonly Dictionary<Room, RoomVisual> rooms = new();
    private readonly List<LinkVisual> links = new();
    private DungeonGenerator generator;
    private RectTransform panel, viewport, content;
    private Text title, roomInfo, controls, legend;
    private Vector2 gridCenter;
    private float spacing = 42f;
    private float zoom = 1f;
    private bool expanded;
    private bool hidden;

    public static DungeonMinimap Create(DungeonGenerator dungeon)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform old = canvas.transform.Find("Dungeon Minimap");
        if (old != null) Destroy(old.gameObject);
        GameObject root = new("Dungeon Minimap", typeof(RectTransform), typeof(Image), typeof(DungeonMinimap));
        root.transform.SetParent(canvas.transform, false);
        DungeonMinimap map = root.GetComponent<DungeonMinimap>();
        map.generator = dungeon;
        map.panel = root.GetComponent<RectTransform>();
        map.BuildChrome();
        map.BuildMap();
        map.ApplyLayout(false);
        map.Refresh();
        return map;
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.mKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)
            {
                expanded = !expanded;
                hidden = false;
                ApplyLayout(expanded);
            }
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                hidden = !hidden;
                panel.gameObject.SetActive(!hidden);
            }
            if (!hidden && Keyboard.current.rKey.wasPressedThisFrame) ResetView();
            if (!hidden && (Keyboard.current.equalsKey.wasPressedThisFrame ||
                            Keyboard.current.numpadPlusKey.wasPressedThisFrame)) AdjustZoom(0.2f);
            if (!hidden && (Keyboard.current.minusKey.wasPressedThisFrame ||
                            Keyboard.current.numpadMinusKey.wasPressedThisFrame)) AdjustZoom(-0.2f);
        }
        if (hidden || Mouse.current == null || !PointerInside()) return;
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.1f) AdjustZoom(Mathf.Sign(scroll) * 0.15f);
        if (expanded && Mouse.current.leftButton.isPressed)
            content.anchoredPosition += Mouse.current.delta.ReadValue();
    }

    public void Refresh()
    {
        if (generator == null) return;
        foreach (RoomVisual visual in rooms.Values)
        {
            bool visited = visual.Room.State != RoomState.Unvisited;
            bool adjacent = !visited && IsAdjacentToVisited(visual.Room.GridPosition);
            visual.Root.gameObject.SetActive(visited || adjacent);
            if (!visited)
            {
                visual.Fill.color = new Color(0.09f, 0.105f, 0.14f, 0.92f);
                visual.Border.color = new Color(0.27f, 0.31f, 0.39f, 0.9f);
                visual.Icon.text = "?";
                visual.Icon.color = new Color(0.48f, 0.53f, 0.62f);
                visual.Player.enabled = false;
                continue;
            }

            bool current = visual.Room == generator.CurrentRoom;
            Color typeColor = RoomColor(visual.Room.Type);
            visual.Fill.color = visual.Room.State == RoomState.Cleared
                ? Color.Lerp(typeColor, new Color(0.12f, 0.15f, 0.19f), 0.25f)
                : Color.Lerp(typeColor, Color.black, 0.36f);
            visual.Border.color = current ? new Color(1f, 0.82f, 0.24f) : typeColor;
            visual.Icon.text = RoomIcon(visual.Room.Type);
            visual.Icon.color = Color.white;
            visual.Player.enabled = current;
        }

        foreach (LinkVisual link in links)
        {
            bool a = link.A.State != RoomState.Unvisited;
            bool b = link.B.State != RoomState.Unvisited;
            link.Image.enabled = (a && b) || (a && IsAdjacentToVisited(link.B.GridPosition)) ||
                                 (b && IsAdjacentToVisited(link.A.GridPosition));
            link.Image.color = a && b
                ? new Color(0.42f, 0.5f, 0.62f, 0.95f)
                : new Color(0.2f, 0.23f, 0.3f, 0.65f);
        }

        Room currentRoom = generator.CurrentRoom;
        roomInfo.text = currentRoom == null ? "NO SIGNAL" :
            $"{currentRoom.Type.ToString().ToUpperInvariant()}  [{currentRoom.GridPosition.x},{currentRoom.GridPosition.y}]  " +
            (currentRoom.State == RoomState.Cleared ? "CLEARED" : "ACTIVE");
        if (!expanded) CenterOnCurrentRoom();
    }

    public void AdjustZoom(float amount)
    {
        zoom = Mathf.Clamp(zoom + amount, expanded ? 0.65f : 0.85f, expanded ? 2.4f : 1.75f);
        content.localScale = Vector3.one * zoom;
        UpdateControls();
        if (!expanded) CenterOnCurrentRoom();
    }

    private void BuildChrome()
    {
        Image background = GetComponent<Image>();
        background.color = new Color(0.025f, 0.035f, 0.055f, 0.96f);
        CreateImage("Top Accent", panel, new Color(0.16f, 0.72f, 0.92f, 0.95f),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f), new Vector2(0f, 5f));
        title = CreateText("Title", panel, "DUNGEON SCAN", 20, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Color(0.86f, 0.94f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -32f), new Vector2(-16f, 30f));
        roomInfo = CreateText("Room Info", panel, "", 12, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Color(0.46f, 0.86f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -57f), new Vector2(-16f, 20f));
        viewport = new GameObject("Map Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D))
            .GetComponent<RectTransform>();
        viewport.SetParent(panel, false);
        viewport.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.025f, 0.82f);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 42f);
        viewport.offsetMax = new Vector2(-12f, -70f);
        controls = CreateText("Controls", panel, "", 11, FontStyle.Normal, TextAnchor.MiddleLeft,
            new Color(0.56f, 0.62f, 0.72f), Vector2.zero, new Vector2(1f, 0f),
            new Vector2(14f, 20f), new Vector2(-14f, 20f));
        legend = CreateText("Legend", panel, "S START   C COMBAT   T TREASURE   $ SHOP   ! BOSS   P YOU", 11,
            FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.66f, 0.72f, 0.8f),
            Vector2.zero, new Vector2(1f, 0f), new Vector2(14f, 3f), new Vector2(-14f, 16f));
    }

    private void BuildMap()
    {
        content = new GameObject("Map Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (Vector2Int p in generator.Rooms.Keys)
        {
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
        gridCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        content.sizeDelta = new Vector2((maxX - minX + 1) * spacing + 80f,
            (maxY - minY + 1) * spacing + 80f);

        foreach (KeyValuePair<Vector2Int, Room> pair in generator.Rooms)
        {
            Vector2 position = GridPosition(pair.Key);
            if (generator.Rooms.TryGetValue(pair.Key + Vector2Int.right, out Room right))
                AddLink(pair.Value, right, position + Vector2.right * spacing * 0.5f, new Vector2(spacing, 7f));
            if (generator.Rooms.TryGetValue(pair.Key + Vector2Int.up, out Room up))
                AddLink(pair.Value, up, position + Vector2.up * spacing * 0.5f, new Vector2(7f, spacing));
        }
        foreach (KeyValuePair<Vector2Int, Room> pair in generator.Rooms) AddRoom(pair.Value, GridPosition(pair.Key));
    }

    private void AddRoom(Room room, Vector2 position)
    {
        RectTransform root = new GameObject($"Room {room.GridPosition}", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(content, false);
        root.anchoredPosition = position;
        root.sizeDelta = new Vector2(32f, 26f);
        Image border = CreateImage("Border", root, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform fillRect = CreateImage("Fill", root, Color.black, Vector2.zero, Vector2.one,
            new Vector2(3f, 3f), new Vector2(-3f, -3f)).rectTransform;
        Image fill = fillRect.GetComponent<Image>();
        Text icon = CreateText("Icon", root, "?", 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image player = CreateImage("Player", root, new Color(1f, 0.84f, 0.22f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-4f, -7f), new Vector2(8f, 4f));
        rooms[room] = new RoomVisual { Room = room, Root = root, Fill = fill, Border = border, Icon = icon, Player = player };
    }

    private void AddLink(Room a, Room b, Vector2 position, Vector2 size)
    {
        Image image = CreateImage("Connection", content, Color.gray, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, size);
        image.transform.SetAsFirstSibling();
        links.Add(new LinkVisual { A = a, B = b, Image = image });
    }

    private void ApplyLayout(bool full)
    {
        panel.anchorMin = panel.anchorMax = full ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
        panel.pivot = full ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
        panel.anchoredPosition = full ? Vector2.zero : new Vector2(-24f, -24f);
        panel.sizeDelta = full ? new Vector2(900f, 650f) : new Vector2(330f, 250f);
        title.text = full ? "DUNGEON OVERVIEW" : "DUNGEON SCAN";
        legend.gameObject.SetActive(full);
        zoom = full ? 1.25f : 1f;
        content.localScale = Vector3.one * zoom;
        ResetView();
        UpdateControls();
        Refresh();
    }

    private void ResetView()
    {
        content.anchoredPosition = Vector2.zero;
        if (!expanded) CenterOnCurrentRoom();
    }

    private void CenterOnCurrentRoom()
    {
        if (generator.CurrentRoom == null) return;
        content.anchoredPosition = -GridPosition(generator.CurrentRoom.GridPosition) * zoom;
    }

    private bool PointerInside() => RectTransformUtility.RectangleContainsScreenPoint(panel,
        Mouse.current.position.ReadValue(), null);

    private bool IsAdjacentToVisited(Vector2Int coordinate)
    {
        foreach (Vector2Int direction in Directions)
            if (generator.Rooms.TryGetValue(coordinate + direction, out Room room) &&
                room.State != RoomState.Unvisited) return true;
        return false;
    }

    private Vector2 GridPosition(Vector2Int coordinate) => ((Vector2)coordinate - gridCenter) * spacing;

    private void UpdateControls() => controls.text = expanded
        ? $"M/TAB CLOSE   DRAG PAN   WHEEL +/- ZOOM   R RESET   H HIDE   {zoom:0.00}x"
        : $"M/TAB EXPAND   H HIDE   {zoom:0.00}x";

    private static string RoomIcon(RoomType type) => type switch
    {
        RoomType.Start => "S", RoomType.Treasure => "T", RoomType.Shop => "$",
        RoomType.Boss => "!", _ => "C"
    };

    private static Color RoomColor(RoomType type) => type switch
    {
        RoomType.Start => new Color(0.22f, 0.7f, 1f),
        RoomType.Treasure => new Color(1f, 0.72f, 0.16f),
        RoomType.Shop => new Color(0.26f, 0.92f, 0.58f),
        RoomType.Boss => new Color(1f, 0.24f, 0.3f),
        _ => new Color(0.56f, 0.64f, 0.76f)
    };

    private static Image CreateImage(string name, Transform parent, Color color, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        if (anchorMin == anchorMax) { rect.anchoredPosition = position; rect.sizeDelta = size; }
        else { rect.offsetMin = position; rect.offsetMax = size; }
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style,
        TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        Text text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value; text.fontSize = size; text.fontStyle = style;
        text.alignment = alignment; text.color = color; text.raycastTarget = false;
        return text;
    }
}
