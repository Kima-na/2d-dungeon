using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    private Rigidbody2D body;
    private PlayerStats stats;
    private Vector2 moveInput;
    private bool movementLocked;

    public Vector2 MoveInput => moveInput;
    public Vector2 LastMoveDirection { get; private set; } = Vector2.right;
    public bool IsInputLocked => movementLocked;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
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
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        }

        moveInput = input.normalized;
        if (moveInput.sqrMagnitude > 0f) LastMoveDirection = moveInput;
    }

    private void FixedUpdate()
    {
        if (!movementLocked && !stats.IsDead)
            body.MovePosition(body.position + moveInput * moveSpeed * stats.MoveSpeedMultiplier * Time.fixedDeltaTime);
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked) body.linearVelocity = Vector2.zero;
    }
}
