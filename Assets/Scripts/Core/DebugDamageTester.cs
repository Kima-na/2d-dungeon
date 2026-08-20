using UnityEngine;
using UnityEngine.InputSystem;

public class DebugDamageTester : MonoBehaviour
{
    [SerializeField] private PlayerStats target;
    [SerializeField, Min(1)] private int testDamage = 25;

    private void Update()
    {
        if (target != null && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            target.TakeDamage(testDamage);
    }
}
