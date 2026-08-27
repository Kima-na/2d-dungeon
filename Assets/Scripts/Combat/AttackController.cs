using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
public class AttackController : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField, Range(1f, 2f)] private float meleeHitboxScale = 1.22f;

    public enum WeaponType { OneHandedSword, Greatsword, Spear }
    public enum WeaponGrade { Common, Uncommon, Rare, Epic, Legendary, Mythic, Ancient, Celestial, Demonic, Divine }

    [SerializeField] private WeaponType equippedWeapon = WeaponType.OneHandedSword;
    [SerializeField] private WeaponGrade weaponGrade = WeaponGrade.Common;

    [Header("Warrior Weapons - Idle / Action by grade")]
    [SerializeField] private Sprite[] shortSwordIdleSprites;
    [SerializeField] private Sprite[] shortSwordActionSprites;
    [SerializeField] private Sprite[] greatswordIdleSprites;
    [SerializeField] private Sprite[] greatswordActionSprites;
    [SerializeField] private Sprite[] spearIdleSprites;
    [SerializeField] private Sprite[] spearActionSprites;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private SpriteRenderer weaponActionRenderer;
    [SerializeField, Min(0f)] private float weaponGripOffset = 0.42f;
    [Header("Weapon world size (independent of sprite PPU)")]
    [SerializeField, Min(0.1f)] private float oneHandedSwordLength = 1.45f;
    [SerializeField, Min(0.1f)] private float greatswordLength = 2.05f;
    [SerializeField, Min(0.1f)] private float spearLength = 2.4f;

    private PlayerController controller;
    private PlayerStats stats;
    private EquipmentInventory equipmentInventory;
    private float nextAttackTime;
    private Coroutine weaponActionRoutine;
    private Transform crossbowVisual;
    private Transform bowVisual;
    private Transform spellbookVisual;
    private Transform staffVisual;
    private Transform temporaryWarriorWeapon;
    private Transform temporarySword;
    private Transform temporaryGreatsword;
    private Transform temporarySpear;

    public WeaponType EquippedWeapon => equippedWeapon;
    public WeaponGrade Grade => weaponGrade;
    public int AttackDamage => PlayerStats.Strength + PlayerStats.AttackPowerBonus + GetWeaponDamage(equippedWeapon);
    public float AttackRange => GetWeaponRange(equippedWeapon);
    public float AttackCooldown => GetWeaponCooldown(equippedWeapon) / PlayerStats.AttackSpeedMultiplier;
    public event System.Action<WeaponType> WeaponChanged;
    private PlayerStats PlayerStats => stats != null ? stats : stats = GetComponent<PlayerStats>();

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
        equipmentInventory = GetComponent<EquipmentInventory>();
        if (GetComponent<ArcherController>() == null) gameObject.AddComponent<ArcherController>();
        if (GetComponent<MageController>() == null) gameObject.AddComponent<MageController>();
        if (GetComponent<SkillController>() == null) gameObject.AddComponent<SkillController>();
        CreateWeaponRendererIfMissing();
        CreateTemporaryWarriorWeapons();
        CreateClassWeaponVisuals();
        stats.ClassChanged += OnClassChanged;
        if (equipmentInventory != null) equipmentInventory.EquipmentChanged += OnEquipmentChanged;
        RefreshWeaponVisual();
        OnClassChanged(stats.CurrentClass);
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame) stats.SelectClass(PlayerStats.PlayerClass.Warrior);
            else if (Keyboard.current.f2Key.wasPressedThisFrame) stats.SelectClass(PlayerStats.PlayerClass.Archer);
            else if (Keyboard.current.f3Key.wasPressedThisFrame) stats.SelectClass(PlayerStats.PlayerClass.Mage);
        }
        UpdateClassWeaponPose();
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
        Vector2 attackDirection = GetAttackDirection();
        PlayWeaponAction(attackDirection);
        Vector2 center = (Vector2)transform.position + attackDirection * (AttackRange * 0.5f);
        float hitboxRadius = AttackRange * 0.5f * meleeHitboxScale;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitboxRadius, targetLayers);

        var damagedTargets = new System.Collections.Generic.HashSet<Damageable>();
        foreach (Collider2D hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            Damageable damageable = hit.GetComponentInParent<Damageable>();
            if (damageable != null && !damageable.IsDead && damagedTargets.Add(damageable))
                DamageTarget(damageable, hit.gameObject);
        }

        // Runtime-generated rooms can be moved before the next physics sync.
        // This registry fallback makes melee hits independent of collider timing/layers.
        foreach (EnemyAI enemy in EnemyAI.ActiveEnemies)
        {
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead || damagedTargets.Contains(enemy.Health)) continue;
            Vector2 toEnemy = (Vector2)enemy.transform.position - (Vector2)transform.position;
            if (toEnemy.sqrMagnitude > Mathf.Pow(AttackRange + 0.65f, 2f) ||
                Vector2.Dot(attackDirection, toEnemy.normalized) < -0.1f) continue;
            damagedTargets.Add(enemy.Health);
            DamageTarget(enemy.Health, enemy.gameObject);
        }
    }

    private void DamageTarget(Damageable damageable, GameObject hitObject)
    {
        int damage = CombatCalculator.RollDamage(stats, AttackDamage, out _);
        damage = CombatCalculator.ApplyTargetModifiers(hitObject, damage);
        damageable.TakeDamage(damage);
        if (!damageable.IsDead)
        {
            if (equippedWeapon == WeaponType.Greatsword)
                StatusEffectController.TryApply(hitObject, StatusEffectType.Burn, stats, 0.4f);
            else if (equippedWeapon == WeaponType.Spear)
                StatusEffectController.TryApply(hitObject, StatusEffectType.Shock, stats, 0.3f);
        }
        if (damageable.IsDead) stats.AddExperience(damageable.ExperienceReward);
    }

    private Vector2 GetAttackDirection()
    {
        Camera camera = Camera.main;
        if (camera != null && Mouse.current != null)
        {
            Vector2 mouseWorld = camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 aim = mouseWorld - (Vector2)transform.position;
            if (aim.sqrMagnitude > 0.01f) return aim.normalized;
        }
        return controller.LastMoveDirection;
    }

    public void EquipWeapon(WeaponType weapon)
    {
        if (equippedWeapon == weapon) return;
        equippedWeapon = weapon;
        RefreshWeaponVisual();
        WeaponChanged?.Invoke(equippedWeapon);
    }

    public void SetWeaponGrade(WeaponGrade grade)
    {
        weaponGrade = grade;
        RefreshWeaponVisual();
    }

    private void CreateWeaponRendererIfMissing()
    {
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        RemoveDuplicateWeaponVisuals();
        if (weaponRenderer == null)
        {
            var visual = new GameObject("Equipped Weapon", typeof(SpriteRenderer));
            visual.transform.SetParent(transform, false);
            weaponRenderer = visual.GetComponent<SpriteRenderer>();
            weaponRenderer.sortingLayerID = playerRenderer != null ? playerRenderer.sortingLayerID : 0;
            weaponRenderer.sortingOrder = playerRenderer != null ? playerRenderer.sortingOrder + 1 : 1;
        }
        if (weaponActionRenderer == null)
        {
            var actionVisual = new GameObject("Weapon Action", typeof(SpriteRenderer));
            actionVisual.transform.SetParent(transform, false);
            weaponActionRenderer = actionVisual.GetComponent<SpriteRenderer>();
        }
        weaponActionRenderer.sortingLayerID = weaponRenderer.sortingLayerID;
        weaponActionRenderer.sortingOrder = weaponRenderer.sortingOrder + 1;
        weaponActionRenderer.enabled = false;
    }

    private void RemoveDuplicateWeaponVisuals()
    {
        SpriteRenderer keptWeapon = null;
        SpriteRenderer keptAction = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            bool isWeapon = child.name == "Equipped Weapon";
            bool isAction = child.name == "Weapon Action";
            if (!isWeapon && !isAction) continue;
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (isWeapon && keptWeapon == null) { keptWeapon = renderer; continue; }
            if (isAction && keptAction == null) { keptAction = renderer; continue; }
            Destroy(child.gameObject);
        }
        if (weaponRenderer == null) weaponRenderer = keptWeapon;
        if (weaponActionRenderer == null) weaponActionRenderer = keptAction;
    }

    private void CreateClassWeaponVisuals()
    {
        Sprite sprite = GetComponent<SpriteRenderer>()?.sprite;
        if (sprite == null) return;

        bowVisual = new GameObject("Equipped Bow").transform;
        bowVisual.SetParent(transform, false);
        CreateVisualPart(bowVisual, "Upper Limb", sprite, new Vector2(0.12f, 0.33f),
            new Vector2(0.12f, 0.72f), new Color(0.72f, 0.46f, 0.16f));
        bowVisual.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, -22f);
        CreateVisualPart(bowVisual, "Lower Limb", sprite, new Vector2(0.12f, -0.33f),
            new Vector2(0.12f, 0.72f), new Color(0.72f, 0.46f, 0.16f));
        bowVisual.GetChild(1).localRotation = Quaternion.Euler(0f, 0f, 22f);
        CreateVisualPart(bowVisual, "Grip", sprite, Vector2.zero,
            new Vector2(0.16f, 0.26f), new Color(0.3f, 0.15f, 0.06f));

        crossbowVisual = new GameObject("Equipped Crossbow").transform;
        crossbowVisual.SetParent(transform, false);
        CreateVisualPart(crossbowVisual, "Stock", sprite, Vector2.zero,
            new Vector2(1.25f, 0.16f), new Color(0.42f, 0.2f, 0.08f));
        CreateVisualPart(crossbowVisual, "Bow", sprite, new Vector2(0.3f, 0f),
            new Vector2(0.14f, 1.05f), new Color(0.72f, 0.5f, 0.2f));
        CreateVisualPart(crossbowVisual, "Bolt", sprite, new Vector2(0.25f, 0f),
            new Vector2(1.1f, 0.055f), new Color(0.85f, 0.9f, 1f));

        spellbookVisual = new GameObject("Equipped Spellbook").transform;
        spellbookVisual.SetParent(transform, false);
        CreateVisualPart(spellbookVisual, "Left Page", sprite, new Vector2(-0.22f, 0f),
            new Vector2(0.42f, 0.62f), new Color(0.92f, 0.82f, 0.58f));
        CreateVisualPart(spellbookVisual, "Right Page", sprite, new Vector2(0.22f, 0f),
            new Vector2(0.42f, 0.62f), new Color(0.98f, 0.9f, 0.68f));
        CreateVisualPart(spellbookVisual, "Spine", sprite, Vector2.zero,
            new Vector2(0.07f, 0.68f), new Color(0.35f, 0.12f, 0.55f));

        staffVisual = new GameObject("Equipped Staff").transform;
        staffVisual.SetParent(transform, false);
        CreateVisualPart(staffVisual, "Shaft", sprite, Vector2.zero,
            new Vector2(1.45f, 0.13f), new Color(0.38f, 0.18f, 0.08f));
        CreateVisualPart(staffVisual, "Magic Gem", sprite, new Vector2(0.78f, 0f),
            new Vector2(0.34f, 0.34f), new Color(0.7f, 0.25f, 1f));
    }

    private void CreateVisualPart(Transform parent, string partName, Sprite sprite,
        Vector2 localPosition, Vector2 localScale, Color color)
    {
        var part = new GameObject(partName, typeof(SpriteRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        renderer.sortingOrder = playerRenderer != null ? playerRenderer.sortingOrder + 1 : 1;
    }

    private void RefreshWeaponVisual()
    {
        if (weaponRenderer == null) return;
        EquipmentItem item = equipmentInventory?.GetEquipped(EquipmentSlot.Weapon);
        Sprite equippedSprite = item?.data != null && item.data.WarriorWeaponType == equippedWeapon
            ? item.data.EquippedSprite
            : null;
        weaponRenderer.sprite = equippedSprite != null ? equippedSprite : GetGradeSprite(equippedWeapon switch
        {
            WeaponType.Greatsword => greatswordIdleSprites,
            WeaponType.Spear => spearIdleSprites,
            _ => shortSwordIdleSprites
        });
        weaponRenderer.enabled = stats != null && stats.CurrentClass == PlayerStats.PlayerClass.Warrior &&
                                 weaponRenderer.sprite != null;
        RefreshTemporaryWarriorWeapon();
        UpdateWeaponPose();
    }

    private void UpdateWeaponPose()
    {
        if (controller == null) return;
        Vector2 direction = controller.LastMoveDirection.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (temporaryWarriorWeapon != null && weaponActionRoutine == null)
        {
            temporaryWarriorWeapon.localPosition = direction * weaponGripOffset;
            temporaryWarriorWeapon.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        if (weaponRenderer == null) return;
        Transform visual = weaponRenderer.transform;
        visual.localPosition = direction * (weaponGripOffset + GetGripReach());
        visual.localRotation = Quaternion.Euler(0f, 0f, angle - GetSpriteForwardAngle());
        weaponRenderer.flipX = false;
        weaponRenderer.flipY = false;
        ApplyWeaponWorldSize(weaponRenderer);
    }

    private void PlayWeaponAction(Vector2 direction)
    {
        if (temporaryWarriorWeapon != null && temporaryWarriorWeapon.gameObject.activeSelf)
        {
            if (weaponActionRoutine != null) StopCoroutine(weaponActionRoutine);
            weaponActionRoutine = StartCoroutine(TemporaryWeaponActionRoutine(direction));
            return;
        }
        if (weaponActionRenderer == null) return;
        if (weaponActionRoutine != null) StopCoroutine(weaponActionRoutine);
        weaponActionRoutine = StartCoroutine(WeaponActionRoutine(direction));
    }

    private IEnumerator TemporaryWeaponActionRoutine(Vector2 direction)
    {
        float aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float duration = Mathf.Min(equippedWeapon == WeaponType.Greatsword ? 0.3f : 0.2f,
            AttackCooldown * 0.45f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (equippedWeapon == WeaponType.Spear)
            {
                float thrust = Mathf.Sin(t * Mathf.PI) * 0.65f;
                temporaryWarriorWeapon.localPosition = direction * (weaponGripOffset + thrust);
                temporaryWarriorWeapon.localRotation = Quaternion.Euler(0f, 0f, aimAngle);
            }
            else
            {
                float arc = equippedWeapon == WeaponType.Greatsword ? 120f : 95f;
                temporaryWarriorWeapon.localPosition = direction * weaponGripOffset;
                temporaryWarriorWeapon.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Lerp(aimAngle - arc * 0.55f, aimAngle + arc * 0.45f,
                        Mathf.SmoothStep(0f, 1f, t)));
            }
            yield return null;
        }
        weaponActionRoutine = null;
        UpdateWeaponPose();
    }

    private void CreateTemporaryWarriorWeapons()
    {
        if (temporaryWarriorWeapon != null) return;
        Sprite pixel = MonsterRoster.PlaceholderSprite;
        if (pixel == null) return;
        temporaryWarriorWeapon = new GameObject("Temporary Warrior Weapon").transform;
        temporaryWarriorWeapon.SetParent(transform, false);

        temporarySword = CreateWeaponRoot("Temporary One-Handed Sword");
        CreateVisualPart(temporarySword, "Grip", pixel, new Vector2(0.12f, 0f), new Vector2(0.34f, 0.16f), new Color(0.32f, 0.16f, 0.06f));
        CreateVisualPart(temporarySword, "Guard", pixel, new Vector2(0.32f, 0f), new Vector2(0.12f, 0.46f), new Color(0.9f, 0.7f, 0.18f));
        CreateVisualPart(temporarySword, "Blade", pixel, new Vector2(0.82f, 0f), new Vector2(0.92f, 0.2f), new Color(0.78f, 0.88f, 1f));
        CreateTip(temporarySword, pixel, 1.3f, 0.23f, new Color(0.92f, 0.97f, 1f));

        temporaryGreatsword = CreateWeaponRoot("Temporary Greatsword");
        CreateVisualPart(temporaryGreatsword, "Grip", pixel, new Vector2(0.18f, 0f), new Vector2(0.5f, 0.2f), new Color(0.28f, 0.12f, 0.05f));
        CreateVisualPart(temporaryGreatsword, "Guard", pixel, new Vector2(0.48f, 0f), new Vector2(0.16f, 0.72f), new Color(0.95f, 0.58f, 0.12f));
        CreateVisualPart(temporaryGreatsword, "Blade", pixel, new Vector2(1.18f, 0f), new Vector2(1.32f, 0.34f), new Color(0.68f, 0.78f, 0.92f));
        CreateTip(temporaryGreatsword, pixel, 1.9f, 0.38f, new Color(0.88f, 0.94f, 1f));

        temporarySpear = CreateWeaponRoot("Temporary Spear");
        CreateVisualPart(temporarySpear, "Shaft", pixel, new Vector2(0.82f, 0f), new Vector2(1.75f, 0.12f), new Color(0.48f, 0.25f, 0.08f));
        CreateVisualPart(temporarySpear, "Collar", pixel, new Vector2(1.72f, 0f), new Vector2(0.18f, 0.22f), new Color(0.92f, 0.62f, 0.12f));
        CreateTip(temporarySpear, pixel, 2.0f, 0.32f, new Color(0.8f, 0.9f, 1f));
        RefreshTemporaryWarriorWeapon();
    }

    private Transform CreateWeaponRoot(string objectName)
    {
        Transform root = new GameObject(objectName).transform;
        root.SetParent(temporaryWarriorWeapon, false);
        return root;
    }

    private void CreateTip(Transform parent, Sprite pixel, float x, float size, Color color)
    {
        CreateVisualPart(parent, "Tip", pixel, new Vector2(x, 0f), new Vector2(size, size), color);
        parent.GetChild(parent.childCount - 1).localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private void RefreshTemporaryWarriorWeapon()
    {
        bool warrior = stats != null && stats.CurrentClass == PlayerStats.PlayerClass.Warrior &&
                       (weaponRenderer == null || weaponRenderer.sprite == null);
        if (temporaryWarriorWeapon != null) temporaryWarriorWeapon.gameObject.SetActive(warrior);
        if (temporarySword != null) temporarySword.gameObject.SetActive(warrior && equippedWeapon == WeaponType.OneHandedSword);
        if (temporaryGreatsword != null) temporaryGreatsword.gameObject.SetActive(warrior && equippedWeapon == WeaponType.Greatsword);
        if (temporarySpear != null) temporarySpear.gameObject.SetActive(warrior && equippedWeapon == WeaponType.Spear);
    }

    private IEnumerator WeaponActionRoutine(Vector2 direction)
    {
        EquipmentItem item = equipmentInventory?.GetEquipped(EquipmentSlot.Weapon);
        Sprite equippedAction = item?.data != null && item.data.WarriorWeaponType == equippedWeapon
            ? item.data.AttackSprite
            : null;
        weaponActionRenderer.sprite = equippedAction != null ? equippedAction : GetGradeSprite(equippedWeapon switch
        {
            WeaponType.Greatsword => greatswordActionSprites,
            WeaponType.Spear => spearActionSprites,
            _ => shortSwordActionSprites
        });
        float aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        ApplyWeaponWorldSize(weaponActionRenderer);
        weaponActionRenderer.flipX = false;
        weaponActionRenderer.flipY = false;
        weaponActionRenderer.enabled = weaponActionRenderer.sprite != null;
        weaponRenderer.enabled = false;
        float duration = Mathf.Min(equippedWeapon == WeaponType.Greatsword ? 0.28f : 0.2f,
            AttackCooldown * 0.45f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (equippedWeapon == WeaponType.Spear)
            {
                // A spear keeps its heading and travels forward instead of swinging.
                float thrust = Mathf.Sin(t * Mathf.PI) * 0.48f;
                weaponActionRenderer.transform.localPosition = direction *
                    (weaponGripOffset + GetGripReach() + thrust);
                weaponActionRenderer.transform.localRotation = Quaternion.Euler(0f, 0f,
                    aimAngle - GetSpriteForwardAngle());
            }
            else
            {
                float arc = equippedWeapon == WeaponType.Greatsword ? 115f : 95f;
                float start = aimAngle - arc * 0.55f;
                weaponActionRenderer.transform.localPosition = direction *
                    (weaponGripOffset + GetGripReach() + 0.12f);
                weaponActionRenderer.transform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Lerp(start, start + arc, Mathf.SmoothStep(0f, 1f, t)) - GetSpriteForwardAngle());
            }
            yield return null;
        }
        weaponActionRenderer.enabled = false;
        weaponRenderer.enabled = stats.CurrentClass == PlayerStats.PlayerClass.Warrior && weaponRenderer.sprite != null;
        weaponActionRoutine = null;
    }

    private Sprite GetGradeSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return null;
        return sprites[Mathf.Clamp((int)weaponGrade, 0, sprites.Length - 1)];
    }

    private float GetDesiredWeaponLength() => equippedWeapon switch
    {
        WeaponType.Greatsword => greatswordLength,
        WeaponType.Spear => spearLength,
        _ => oneHandedSwordLength
    };

    private float GetGripReach() => equippedWeapon switch
    {
        WeaponType.Greatsword => 0.38f,
        WeaponType.Spear => 0.55f,
        _ => 0.28f
    };

    // Complete warrior sprites point from the lower-left grip toward the
    // upper-right tip. Keep that source-space axis separate from character
    // facing so neither parent scale nor SpriteRenderer flipping is required.
    private float GetSpriteForwardAngle() => 90f;

    private void ApplyWeaponWorldSize(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null) return;
        Vector2 bounds = renderer.sprite.bounds.size;
        float spriteLength = Mathf.Max(bounds.x, bounds.y);
        float parentScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        float scale = GetDesiredWeaponLength() / Mathf.Max(0.001f, spriteLength * parentScale);
        renderer.transform.localScale = Vector3.one * scale;
    }

    private void UpdateClassWeaponPose()
    {
        if (controller == null) return;
        RefreshClassWeaponVisuals(stats.CurrentClass);
        Vector2 direction = controller.LastMoveDirection.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (crossbowVisual != null)
        {
            crossbowVisual.localPosition = direction * 0.62f;
            crossbowVisual.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        if (bowVisual != null)
        {
            bowVisual.localPosition = direction * 0.62f;
            bowVisual.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        if (spellbookVisual != null)
        {
            Vector2 side = new Vector2(-direction.y, direction.x);
            spellbookVisual.localPosition = direction * 0.3f + side * 0.72f;
            spellbookVisual.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2f) * 5f);
        }
        if (staffVisual != null)
        {
            staffVisual.localPosition = direction * 0.58f;
            staffVisual.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnClassChanged(PlayerStats.PlayerClass playerClass)
    {
        RefreshWeaponVisual();
        RefreshClassWeaponVisuals(playerClass);
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            playerRenderer.color = playerClass switch
            {
                PlayerStats.PlayerClass.Archer => new Color(0.25f, 0.85f, 0.4f),
                PlayerStats.PlayerClass.Mage => new Color(0.65f, 0.3f, 1f),
                _ => new Color(0.2f, 0.75f, 1f)
            };
        }
    }

    private void OnEquipmentChanged()
    {
        EquipmentItem weapon = equipmentInventory?.GetEquipped(EquipmentSlot.Weapon);
        if (weapon?.data != null) equippedWeapon = weapon.data.WarriorWeaponType;
        RefreshWeaponVisual();
        WeaponChanged?.Invoke(equippedWeapon);
    }

    private void RefreshClassWeaponVisuals(PlayerStats.PlayerClass playerClass)
    {
        ArcherController archer = GetComponent<ArcherController>();
        MageController mage = GetComponent<MageController>();
        bool isArcher = playerClass == PlayerStats.PlayerClass.Archer;
        bool isMage = playerClass == PlayerStats.PlayerClass.Mage;
        if (bowVisual != null) bowVisual.gameObject.SetActive(isArcher && archer != null &&
            archer.EquippedWeapon == ArcherController.RangedWeapon.Bow);
        if (crossbowVisual != null) crossbowVisual.gameObject.SetActive(isArcher && archer != null &&
            archer.EquippedWeapon == ArcherController.RangedWeapon.Crossbow);
        if (staffVisual != null) staffVisual.gameObject.SetActive(isMage && mage != null &&
            mage.EquippedWeapon == MageController.MagicWeapon.Staff);
        if (spellbookVisual != null) spellbookVisual.gameObject.SetActive(isMage && mage != null &&
            mage.EquippedWeapon == MageController.MagicWeapon.Spellbook);
    }

    private void OnDestroy()
    {
        if (stats != null) stats.ClassChanged -= OnClassChanged;
        if (equipmentInventory != null) equipmentInventory.EquipmentChanged -= OnEquipmentChanged;
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
        WeaponType.Greatsword => 2f,
        WeaponType.Spear => 2.55f,
        _ => 1.6f
    };

    private static float GetWeaponCooldown(WeaponType weapon) => weapon switch
    {
        WeaponType.Greatsword => 0.9f,
        WeaponType.Spear => 0.65f,
        _ => 0.4f
    };

}
