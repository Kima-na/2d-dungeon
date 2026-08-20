using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerController), typeof(PlayerStats))]
public class PlayerDash : MonoBehaviour
{
    [SerializeField, Min(0f)] private float dashDistance = 3f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.15f;
    [SerializeField, Min(0f)] private float dashCooldown = 1f;

    private Rigidbody2D body;
    private PlayerController controller;
    private PlayerStats stats;
    private float nextDashTime;
    private bool isDashing;

    public bool IsDashing => isDashing;
    public float CooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (!stats.IsDead && !isDashing && Time.time >= nextDashTime &&
            Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;
        controller.SetMovementLocked(true);

        Vector2 direction = controller.MoveInput.sqrMagnitude > 0f
            ? controller.MoveInput.normalized
            : controller.LastMoveDirection;
        float speed = dashDistance / dashDuration;
        float elapsed = 0f;

        while (elapsed < dashDuration && !stats.IsDead)
        {
            float step = Mathf.Min(Time.fixedDeltaTime, dashDuration - elapsed);
            body.MovePosition(body.position + direction * speed * step);
            elapsed += step;
            yield return new WaitForFixedUpdate();
        }

        controller.SetMovementLocked(false);
        isDashing = false;
    }

    private void OnDisable()
    {
        if (controller != null) controller.SetMovementLocked(false);
        isDashing = false;
    }
}
