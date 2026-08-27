using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Vector2Int direction;
    [SerializeField] private Collider2D transitionTrigger;
    [SerializeField] private Collider2D blocker;
    [SerializeField] private SpriteRenderer doorRenderer;

    private Room owner;
    private bool connected;
    private bool locked;

    public Vector2Int Direction => direction;
    public bool IsOpen => connected && !locked;

    public void Bind(Room room) => owner = room;

    public void Initialize(Room room, Vector2Int doorDirection, Collider2D trigger,
        Collider2D solidBlocker, SpriteRenderer renderer)
    {
        owner = room;
        direction = doorDirection;
        transitionTrigger = trigger;
        blocker = solidBlocker;
        doorRenderer = renderer;
        Refresh();
    }

    public void SetConnected(bool value)
    {
        connected = value;
        Refresh();
    }

    public void SetLocked(bool value)
    {
        locked = value;
        Refresh();
    }

    private void Refresh()
    {
        bool open = connected && !locked;
        if (transitionTrigger != null) transitionTrigger.enabled = open;
        if (blocker != null) blocker.enabled = !open;
        if (doorRenderer != null)
            doorRenderer.color = !connected
                ? new Color(0.12f, 0.13f, 0.16f)
                : locked ? new Color(0.75f, 0.12f, 0.08f) : new Color(0.25f, 0.85f, 0.4f, 0.45f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOpen || owner == null || other.GetComponentInParent<PlayerController>() == null) return;
        owner.RequestTransition(direction);
    }
}
