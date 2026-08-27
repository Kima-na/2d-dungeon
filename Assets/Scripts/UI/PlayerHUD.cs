using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Text healthText;
    [SerializeField] private Text manaText;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private Text experienceText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text weaponText;
    [SerializeField] private Text skillText;
    [SerializeField] private Text combatStatsText;
    [SerializeField] private Text goldText;
    [SerializeField] private AttackController attackController;
    [SerializeField] private ArcherController archerController;
    [SerializeField] private MageController mageController;
    [SerializeField] private SkillController skillController;
    [SerializeField] private GameObject deathPanel;

    private void Awake()
    {
        if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
        if (attackController == null && playerStats != null)
            attackController = playerStats.GetComponent<AttackController>();
        if (archerController == null && playerStats != null)
            archerController = playerStats.GetComponent<ArcherController>();
        if (mageController == null && playerStats != null)
            mageController = playerStats.GetComponent<MageController>();
        if (skillController == null && playerStats != null)
            skillController = playerStats.GetComponent<SkillController>();
        ApplyPlayerBarStyle();
        CreateMissingGrowthLabels();
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void ApplyPlayerBarStyle()
    {
        BossVisualDatabase visuals = Resources.Load<BossVisualDatabase>("BossVisualDatabase");
        if (visuals == null) return;
        ApplyBarStyle(healthSlider, visuals.bossBarFrame, visuals.bossBarFill);
        ApplyBarStyle(manaSlider, visuals.bossBarFrame,
            visuals.playerManaFill != null ? visuals.playerManaFill : visuals.bossBarFill);
    }

    private static void ApplyBarStyle(Slider slider, Sprite frameSprite, Sprite fillSprite)
    {
        if (slider == null) return;
        Image background = slider.transform.Find("Background")?.GetComponent<Image>();
        Image fillImage = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (background != null && frameSprite != null)
        {
            background.sprite = frameSprite;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
        }
        if (fillImage != null && fillSprite != null)
        {
            fillImage.sprite = fillSprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Color.white;
        }
    }

    private void CreateMissingGrowthLabels()
    {
        if (experienceSlider == null)
            experienceSlider = CreateRuntimeExperienceBar();
        if (levelText == null)
            levelText = CreateRuntimeLabel("Level Text", new Vector2(40f, -190f));
        if (experienceText == null)
            experienceText = CreateExperienceLabel(experienceSlider.transform);
        if (weaponText == null)
            weaponText = CreateRuntimeLabel("Weapon Text", new Vector2(40f, -230f));
        if (skillText == null)
            skillText = CreateRuntimeLabel("Skill Text", new Vector2(40f, -270f));
        if (combatStatsText == null)
            combatStatsText = CreateRuntimeLabel("Combat Stats Text", new Vector2(40f, -310f));
        if (goldText == null)
        {
            goldText = CreateRuntimeLabel("Gold Text", new Vector2(40f, -350f));
            goldText.color = new Color(1f, 0.78f, 0.12f);
        }
    }

    private Slider CreateRuntimeExperienceBar()
    {
        var root = new GameObject("Experience Bar", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(transform, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(40f, -154f);
        rect.sizeDelta = new Vector2(320f, 22f);

        CreateBarImage(root.transform, "Background", new Color(0.05f, 0.05f, 0.05f, 0.9f));
        Image fill = CreateBarImage(root.transform, "Fill", new Color(1f, 0.78f, 0.08f, 1f));
        fill.rectTransform.offsetMin = new Vector2(3f, 3f);
        fill.rectTransform.offsetMax = new Vector2(-3f, -3f);

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = fill;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 0f;
        return slider;
    }

    private static Image CreateBarImage(Transform parent, string objectName, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateExperienceLabel(Transform parent)
    {
        var go = new GameObject("EXP Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 14;
        text.color = Color.white;
        return text;
    }

    private void Update()
    {
        if (skillController == null && playerStats != null)
            skillController = playerStats.GetComponent<SkillController>();
        if (skillText != null && skillController != null)
        {
            float remaining = skillController.CooldownRemaining;
            skillText.text = remaining > 0f
                ? $"[Q] {skillController.CurrentSkillName}  CD {remaining:0.0}s"
                : $"[Q] {skillController.CurrentSkillName}  MP {skillController.CurrentManaCost}  READY";
        }
        RefreshEquippedWeaponText();
    }

    private void RefreshEquippedWeaponText()
    {
        if (weaponText == null || playerStats == null) return;
        if (playerStats.CurrentClass == PlayerStats.PlayerClass.Archer)
        {
            if (archerController == null) archerController = playerStats.GetComponent<ArcherController>();
            if (archerController == null) return;
            string weapon = archerController.EquippedWeapon == ArcherController.RangedWeapon.Crossbow ? "CROSSBOW" : "BOW";
            weaponText.text = $"[1/2] {weapon}  ATK {archerController.AttackDamage}  AIM: MOUSE";
        }
        else if (playerStats.CurrentClass == PlayerStats.PlayerClass.Mage)
        {
            if (mageController == null) mageController = playerStats.GetComponent<MageController>();
            if (mageController == null) return;
            string weapon = mageController.EquippedWeapon == MageController.MagicWeapon.Spellbook ? "SPELLBOOK" : "STAFF";
            weaponText.text = $"[1/2] {weapon}  ATK {mageController.AttackDamage}  MP {mageController.ManaCost}  AIM: MOUSE";
        }
    }

    private Text CreateRuntimeLabel(string objectName, Vector2 position)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(620f, 36f);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = 22;
        text.color = Color.white;
        return text;
    }

    private void OnEnable()
    {
        if (playerStats == null) return;
        playerStats.HealthChanged += UpdateHealth;
        playerStats.ManaChanged += UpdateMana;
        playerStats.ExperienceChanged += UpdateExperience;
        playerStats.LeveledUp += UpdateLevel;
        playerStats.ClassChanged += UpdateClass;
        playerStats.GoldChanged += UpdateGold;
        playerStats.Died += ShowDeath;
        if (attackController != null) attackController.WeaponChanged += UpdateWeapon;
        UpdateHealth(playerStats.CurrentHealth, playerStats.MaxHealth);
        UpdateMana(playerStats.CurrentMana, playerStats.MaxMana);
        UpdateExperience(playerStats.CurrentExperience, playerStats.ExperienceToNextLevel);
        UpdateClass(playerStats.CurrentClass);
        UpdateGold(playerStats.Gold);
    }

    private void OnDisable()
    {
        if (playerStats == null) return;
        playerStats.HealthChanged -= UpdateHealth;
        playerStats.ManaChanged -= UpdateMana;
        playerStats.ExperienceChanged -= UpdateExperience;
        playerStats.LeveledUp -= UpdateLevel;
        playerStats.ClassChanged -= UpdateClass;
        playerStats.GoldChanged -= UpdateGold;
        playerStats.Died -= ShowDeath;
        if (attackController != null) attackController.WeaponChanged -= UpdateWeapon;
    }

    private void UpdateHealth(int current, int maximum)
    {
        if (healthSlider != null) { healthSlider.maxValue = maximum; healthSlider.value = current; }
        if (healthText != null) healthText.text = $"HP  {current} / {maximum}";
    }

    private void UpdateMana(int current, int maximum)
    {
        if (manaSlider != null) { manaSlider.maxValue = maximum; manaSlider.value = current; }
        if (manaText != null) manaText.text = $"MP  {current} / {maximum}";
    }

    private void ShowDeath()
    {
        if (deathPanel != null) deathPanel.SetActive(true);
    }

    private void UpdateGold(int amount)
    {
        if (goldText != null) goldText.text = $"GOLD  {amount}";
    }

    private void UpdateExperience(int current, int required)
    {
        if (experienceSlider != null) { experienceSlider.maxValue = required; experienceSlider.value = current; }
        if (experienceText != null) experienceText.text = $"EXP  {current} / {required}";
    }

    private void UpdateLevel(int currentLevel)
    {
        if (levelText == null) return;
        levelText.text = playerStats.CurrentClass switch
        {
            PlayerStats.PlayerClass.Archer => $"[F1/F2/F3] ARCHER  LV.{currentLevel}  DEX {playerStats.Dexterity}  DEF {playerStats.Defense}",
            PlayerStats.PlayerClass.Mage => $"[F1/F2/F3] MAGE  LV.{currentLevel}  INT {playerStats.Intelligence}  DEF {playerStats.Defense}",
            _ => $"[F1/F2/F3] WARRIOR  LV.{currentLevel}  STR {playerStats.Strength}  DEF {playerStats.Defense}"
        };
        if (combatStatsText != null)
            combatStatsText.text = $"CRIT {playerStats.CriticalChance * 100f:0.#}%  " +
                                   $"CRIT DMG {playerStats.CriticalDamageMultiplier * 100f:0}%  " +
                                   $"ATK SPD {playerStats.AttackSpeedMultiplier:0.00}x  MOVE {playerStats.MoveSpeedMultiplier:0.00}x";
    }

    private void UpdateClass(PlayerStats.PlayerClass playerClass)
    {
        UpdateLevel(playerStats.Level);
        if (weaponText == null) return;
        if (playerClass == PlayerStats.PlayerClass.Archer)
        {
            if (archerController == null) archerController = playerStats.GetComponent<ArcherController>();
            weaponText.text = $"CROSSBOW  ATK {(archerController != null ? archerController.AttackDamage : playerStats.Dexterity)}  AIM: MOUSE";
        }
        else if (playerClass == PlayerStats.PlayerClass.Mage)
        {
            if (mageController == null) mageController = playerStats.GetComponent<MageController>();
            int attack = mageController != null ? mageController.AttackDamage : playerStats.Intelligence;
            int mana = mageController != null ? mageController.ManaCost : 0;
            weaponText.text = $"SPELLBOOK / MAGIC BOLT  ATK {attack}  MP {mana}  AIM: MOUSE";
        }
        else if (attackController != null)
            UpdateWeapon(attackController.EquippedWeapon);
    }

    private void UpdateWeapon(AttackController.WeaponType weapon)
    {
        if (weaponText == null || attackController == null) return;
        string displayName = weapon switch
        {
            AttackController.WeaponType.Greatsword => "GREATSWORD",
            AttackController.WeaponType.Spear => "SPEAR",
            _ => "ONE-HANDED SWORD"
        };
        weaponText.text = $"[1/2/3] {displayName}  ATK {attackController.AttackDamage}";
    }
}
