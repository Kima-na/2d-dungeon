using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class DungeonMinimap : MonoBehaviour
{
    private readonly Dictionary<Room, Image> roomCells = new();
    private readonly List<ConnectionVisual> connections = new();
    private DungeonGenerator generator;
    private RectTransform mapContent;
    private Text zoomHint;
    private float zoom = 1f;

    private sealed class ConnectionVisual
    {
        public Room First;
        public Room Second;
        public Image Image;
    }

    public static DungeonMinimap Create(DungeonGenerator dungeon)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        Transform previous = canvas.transform.Find("Dungeon Minimap");
        if (previous != null) Destroy(previous.gameObject);

        GameObject panelObject = new("Dungeon Minimap", typeof(RectTransform), typeof(Image),
            typeof(RectMask2D), typeof(DungeonMinimap));
        panelObject.transform.SetParent(canvas.transform, false);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-32f, -32f);
        panel.sizeDelta = new Vector2(250f, 190f);
        panelObject.GetComponent<Image>().color = new Color(0.025f, 0.03f, 0.045f, 0.88f);

        DungeonMinimap minimap = panelObject.GetComponent<DungeonMinimap>();
        minimap.generator = dungeon;
        minimap.Build();
        minimap.CreateZoomButtons();
        return minimap;
    }

    private void Update()
    {
        float zoomInput = 0f;
        if (Mouse.current != null) zoomInput += Mathf.Sign(Mouse.current.scroll.ReadValue().y);
        if (Keyboard.current != null)
        {
            if (Keyboard.current.equalsKey.wasPressedThisFrame ||
                Keyboard.current.numpadPlusKey.wasPressedThisFrame) zoomInput += 1f;
            if (Keyboard.current.minusKey.wasPressedThisFrame ||
                Keyboard.current.numpadMinusKey.wasPressedThisFrame) zoomInput -= 1f;
        }
        if (Mathf.Abs(zoomInput) < 0.01f) return;
        AdjustZoom(zoomInput * 0.3f);
    }

    public void AdjustZoom(float amount)
    {
        zoom = Mathf.Clamp(zoom + amount, 0.6f, 3.5f);
        if (mapContent != null) mapContent.localScale = Vector3.one * zoom;
        RefreshZoomHint();
    }

    public void Refresh()
    {
        foreach (KeyValuePair<Room, Image> pair in roomCells)
        {
            Room room = pair.Key;
            bool discovered = room.State != RoomState.Unvisited;
            pair.Value.enabled = discovered;
            if (!discovered) continue;
            if (room == generator.CurrentRoom)
            {
                pair.Value.color = new Color(1f, 0.82f, 0.12f);
                continue;
            }
            pair.Value.color = room.Type switch
            {
                RoomType.Start => new Color(0.25f, 0.7f, 1f),
                RoomType.Treasure => new Color(1f, 0.7f, 0.12f),
                RoomType.Shop => new Color(0.2f, 0.9f, 0.5f),
                RoomType.Boss => new Color(0.9f, 0.12f, 0.16f),
                _ => room.State == RoomState.Cleared
                    ? new Color(0.72f, 0.76f, 0.82f)
                    : new Color(0.42f, 0.46f, 0.54f)
            };
        }
        foreach (ConnectionVisual connection in connections)
            connection.Image.enabled = connection.First.State != RoomState.Unvisited &&
                                       connection.Second.State != RoomState.Unvisited;
    }

    private void Build()
    {
        if (generator.Rooms.Count == 0) return;
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (Vector2Int coordinate in generator.Rooms.Keys)
        {
            minX = Mathf.Min(minX, coordinate.x); maxX = Mathf.Max(maxX, coordinate.x);
            minY = Mathf.Min(minY, coordinate.y); maxY = Mathf.Max(maxY, coordinate.y);
        }

        float spacing = Mathf.Min(23f,
            205f / Mathf.Max(1, maxX - minX),
            145f / Mathf.Max(1, maxY - minY));
        Vector2 gridCenter = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        mapContent = new GameObject("Map", typeof(RectTransform)).GetComponent<RectTransform>();
        mapContent.SetParent(transform, false);
        mapContent.anchorMin = mapContent.anchorMax = new Vector2(0.5f, 0.5f);
        mapContent.anchoredPosition = new Vector2(0f, 8f);
        mapContent.sizeDelta = new Vector2((maxX - minX + 1) * spacing, (maxY - minY + 1) * spacing);

        foreach (KeyValuePair<Vector2Int, Room> pair in generator.Rooms)
        {
            Vector2 position = ((Vector2)pair.Key - gridCenter) * spacing;
            if (generator.Rooms.ContainsKey(pair.Key + Vector2Int.right))
                AddConnection(pair.Value, generator.Rooms[pair.Key + Vector2Int.right],
                    position + Vector2.right * spacing * 0.5f, new Vector2(spacing, 4f));
            if (generator.Rooms.ContainsKey(pair.Key + Vector2Int.up))
                AddConnection(pair.Value, generator.Rooms[pair.Key + Vector2Int.up],
                    position + Vector2.up * spacing * 0.5f, new Vector2(4f, spacing));
        }

        foreach (KeyValuePair<Vector2Int, Room> pair in generator.Rooms)
        {
            Vector2 position = ((Vector2)pair.Key - gridCenter) * spacing;
            roomCells[pair.Value] = CreateImage(mapContent, pair.Value.name, position,
                new Vector2(16f, 13f), Color.gray);
        }
        CreateZoomHint();
        Refresh();
    }

    private void AddConnection(Room first, Room second, Vector2 position, Vector2 size)
    {
        connections.Add(new ConnectionVisual
        {
            First = first,
            Second = second,
            Image = CreateImage(mapContent, "Connection", position, size, new Color(0.32f, 0.35f, 0.4f))
        });
    }

    private void CreateZoomHint()
    {
        GameObject go = new("Zoom Hint", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(82f, 5f);
        rect.sizeDelta = new Vector2(155f, 22f);
        zoomHint = go.GetComponent<Text>();
        zoomHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        zoomHint.alignment = TextAnchor.MiddleCenter;
        zoomHint.fontSize = 11;
        zoomHint.color = new Color(0.75f, 0.78f, 0.84f, 0.8f);
        RefreshZoomHint();
    }

    private void RefreshZoomHint()
    {
        if (zoomHint != null)
            zoomHint.text = $"ZOOM  {zoom * 100f:0}%";
    }

    private void CreateZoomButtons()
    {
        EnsureEventSystem();
        CreateButton("Zoom Out", "-", new Vector2(-64f, 7f), () => AdjustZoom(-0.3f));
        CreateButton("Zoom In", "+", new Vector2(-27f, 7f), () => AdjustZoom(0.3f));
    }

    private void CreateButton(string objectName, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(32f, 26f);
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.21f, 0.27f, 0.95f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textObject = new("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(go.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 20;
        text.color = Color.white;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Image CreateImage(Transform parent, string objectName, Vector2 position,
        Vector2 size, Color color)
    {
        GameObject go = new(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }
}
