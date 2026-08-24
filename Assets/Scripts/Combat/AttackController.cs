using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
public class AttackController : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers = ~0;

    public enum WeaponType { OneHandedSword, Greatsword, Spear }

    [SerializeField] private WeaponType equippedWeapon = WeaponType.OneHandedSword;

    [Header("Weapon Visuals")]
    [SerializeField] private Sprite oneHandedSwordSprite;
    [SerializeField] private Sprite greatswordSprite;
    [SerializeField] private Sprite spearSprite;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField, Min(0f)] private float weaponGripOffset = 0.55f;

    private PlayerController controller;
    private PlayerStats stats;
    private float nextAttackTime;

    public WeaponType EquippedWeapon => equippedWeapon;
    public int AttackDamage => PlayerStats.Strength + GetWeaponDamage(equippedWeapon);
    public float AttackRange => GetWeaponRange(equippedWeapon);
    public float AttackCooldown => GetWeaponCooldown(equippedWeapon);
    public event System.Action<WeaponType> WeaponChanged;
    private PlayerStats PlayerStats => stats != null ? stats : stats = GetComponent<PlayerStats>();

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
        if (GetComponent<ArcherController>() == null) gameObject.AddComponent<ArcherController>();
        CreateWeaponRendererIfMissing();
        stats.ClassChanged += OnClassChanged;
        RefreshWeaponVisual();
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame) stats.SelectClass(PlayerStats.PlayerClass.Warrior);
            else if (Keyboard.current.f2Key.wasPressedThisFrame) stats.SelectClass(PlayerStats.PlayerClass.Archer);
        }
        if (stats.CurrentClass != PlayerStats.PlayerClass.Warrior) return;
        HandleWeaponInput();
        UpdateWeaponPose();
        if (!stats.IsDead && Time.time >= nextAttackTime && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Attack();
        }
    }

    public void Attack()
    {
        if (stats.IsDead || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + AttackCooldown;
        Vector2 center = (Vector2)transform.position + controller.LastMoveDirection * (AttackRange * 0.5f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, AttackRange * 0.5f, targetLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageable damageable && !damageable.IsDead)
                {
                    damageable.TakeDamage(AttackDamage);
                    if (damageable.IsDead && behaviour is IExperienceSource source)
                        stats.AddExperience(source.ExperienceReward);
                    break;
                }
            }
        }
    }

    public void EquipWeapon(WeaponType weapon)
    {
        if (equippedWeapon == weapon) return;
        equippedWeapon = weapon;
        RefreshWeaponVisual();
        WeaponChanged?.Invoke(equippedWeapon);
    }

    private void CreateWeaponRendererIfMissing()
    {
        if (weaponRenderer != null) return;
        var visual = new GameObject("Equipped Weapon", typeof(SpriteRenderer));
        visual.transform.SetParent(transform, false);
        weaponRenderer = visual.GetComponent<SpriteRenderer>();
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        weaponRenderer.sortingLayerID = playerRenderer != null ? playerRenderer.sortingLayerID : 0;
        weaponRenderer.sortingOrder = playerRenderer != null ? playerRenderer.sortingOrder + 1 : 1;
    }

    private void RefreshWeaponVisual()
    {
        if (weaponRenderer == null) return;
        weaponRenderer.sprite = equippedWeapon switch
        {
            WeaponType.Greatsword => greatswordSprite,
            WeaponType.Spear => spearSprite,
            _ => oneHandedSwordSprite
        };
        weaponRenderer.transform.localScale = Vector3.one * (equippedWeapon == WeaponType.Spear ? 0.9f : 0.8f);
        weaponRenderer.enabled = stats != null && stats.CurrentClass == PlayerStats.PlayerClass.Warrior &&
                                 weaponRenderer.sprite != null;
        UpdateWeaponPose();
    }

    private void UpdateWeaponPose()
    {
        if (weaponRenderer == null || controller == null) return;
        Vector2 direction = controller.LastMoveDirection.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 45f;
        Transform visual = weaponRenderer.transform;
        visual.localPosition = direction * weaponGripOffset;
        visual.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnClassChanged(PlayerStats.PlayerClass playerClass)
    {
        if (weaponRenderer != null)
            weaponRenderer.enabled = playerClass == PlayerStats.PlayerClass.Warrior && weaponRenderer.sprite != null;
    }

    private void OnDestroy()
    {
        if (stats != null) stats.ClassChanged -= OnClassChanged;
    }

    private void HandleWeaponInput()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame) EquipWeapon(WeaponType.OneHandedSword);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) EquipWeapon(WeaponType.Greatsword);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) EquipWeapon(WeaponType.Spear);
    }

    private static int GetWeaponDamage(WeaponType weapon) => weapon switch
    {
        WeaponType.Greatsword => 18,
        WeaponType.Spear => 11,
        _ => 8
    };

    private static float GetWeaponRange(WeaponType weapon) => weapon switch
    {
        WeaponType.Greatsword => 1.8f,
        WeaponType.Spear => 2.5f,
        _ => 1.5f
    };

    private static float GetWeaponCooldown(WeaponType weapon) => weapon switch
    {
        WeaponType.Greatsword => 0.9f,
        WeaponType.Spear => 0.65f,
        _ => 0.4f
    };

    private void OnDrawGizmosSelected()
    {
        Vector2 direction = Application.isPlaying && controller != null ? controller.LastMoveDirection : Vector2.right;
        Gizmos.color = Color.yellow;
        float range = Application.isPlaying && stats != null ? AttackRange : 1.5f;
        Gizmos.DrawWireSphere((Vector2)transform.position + direction * (range * 0.5f), range * 0.5f);
    }
}
