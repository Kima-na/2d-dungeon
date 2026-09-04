using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    private Rigidbody2D body;
    private PlayerStats stats;
    private DungeonGenerator dungeon;
    private Vector2 moveInput;
    private bool movementLocked;

    public Vector2 MoveInput => moveInput;
    public Vector2 LastMoveDirection { get; private set; } = Vector2.right;
    public bool IsInputLocked => movementLocked;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        dungeon = FindAnyObjectByType<DungeonGenerator>();
        if (GetComponent<PlayerVisualController>() == null)
            gameObject.AddComponent<PlayerVisualController>();
    }

    private void Update()
    {
        if (stats.IsDead)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
        }

        moveInput = input.normalized;
        if (moveInput.sqrMagnitude > 0f) LastMoveDirection = moveInput;
    }

    private void FixedUpdate()
    {
        if (movementLocked || stats.IsDead) return;

        Vector2 nextPosition = body.position +
            moveInput * moveSpeed * stats.MoveSpeedMultiplier * Time.fixedDeltaTime;
        if (dungeon == null) dungeon = FindAnyObjectByType<DungeonGenerator>();
        if (dungeon != null && dungeon.CurrentRoom != null)
            nextPosition = dungeon.CurrentRoom.ClampToInterior(nextPosition, 0.1f);

        body.MovePosition(nextPosition);
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked) body.linearVelocity = Vector2.zero;
    }
}
