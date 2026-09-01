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
    [SerializeField] private Text potionText;
    [SerializeField] private AttackController attackController;
    [SerializeField] private ArcherController archerController;
    [SerializeField] private MageController mageController;
    [SerializeField] private SkillController skillController;
    [SerializeField] private PlayerPotionController potionController;
    [SerializeField] private GameObject deathPanel;
    private DungeonFlowController flowController;
    private bool hudVisible = true;

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
        if (potionController == null && playerStats != null)
            potionController = playerStats.GetComponent<PlayerPotionController>();
        ApplyPlayerBarStyle();
        CreateMissingGrowthLabels();
        ApplyReadableLayout();
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void ApplyReadableLayout()
    {
        RectTransform panel = EnsurePanel("Player HUD Panel", new Vector2(20f, -20f), new Vector2(500f, 154f));
        EnsurePanel("Player HUD Accent", new Vector2(20f, -20f), new Vector2(5f, 154f),
            new Color(0.15f, 0.72f, 1f, 0.95f));

        StyleBar(healthSlider, new Vector2(40f, -58f), new Vector2(330f, 34f),
            new Color(0.82f, 0.09f, 0.12f, 1f));
        StyleBar(manaSlider, new Vector2(40f, -98f), new Vector2(330f, 28f),
            new Color(0.08f, 0.38f, 0.95f, 1f));
        StyleBar(experienceSlider, new Vector2(40f, -132f), new Vector2(330f, 16f),
            new Color(1f, 0.68f, 0.06f, 1f));

        StyleText(healthText, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        StyleText(manaText, 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        StyleText(experienceText, 11, FontStyle.Bold, new Color(1f, 0.93f, 0.65f), TextAnchor.MiddleCenter);
        PlaceLabel(levelText, new Vector2(40f, -27f), 330f, 24f, 16, new Color(0.55f, 0.88f, 1f), true);
        PlaceLabel(goldText, new Vector2(382f, -54f), 110f, 24f, 15, new Color(1f, 0.78f, 0.12f), true);
        PlaceLabel(potionText, new Vector2(382f, -84f), 110f, 55f, 12, new Color(0.52f, 1f, 0.66f), true);
        RectTransform skillPanel = EnsurePanel("Skill HUD Panel", Vector2.zero, new Vector2(840f, 58f),
            new Color(0.018f, 0.025f, 0.045f, 0.88f));
        skillPanel.anchorMin = skillPanel.anchorMax = new Vector2(0.5f, 0f);
        skillPanel.pivot = new Vector2(0.5f, 0f); skillPanel.anchoredPosition = new Vector2(0f, 18f);
        skillPanel.SetAsFirstSibling();
        if (skillText != null)
        {
            RectTransform skillRect = skillText.rectTransform;
            skillRect.anchorMin = skillRect.anchorMax = new Vector2(0.5f, 0f);
            skillRect.pivot = new Vector2(0.5f, 0f); skillRect.anchoredPosition = new Vector2(0f, 22f);
            skillRect.sizeDelta = new Vector2(810f, 42f);
            StyleText(skillText, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            skillText.supportRichText = true;
            skillText.transform.SetAsLastSibling();
        }
        if (weaponText != null) weaponText.gameObject.SetActive(false);
        if (combatStatsText != null) combatStatsText.gameObject.SetActive(false);
        if (skillText != null) skillText.horizontalOverflow = HorizontalWrapMode.Wrap;
        if (potionText != null) potionText.horizontalOverflow = HorizontalWrapMode.Wrap;

        panel.SetAsFirstSibling();
        Transform accent = transform.Find("Player HUD Accent");
        if (accent != null) accent.SetSiblingIndex(1);
    }

    private RectTransform EnsurePanel(string objectName, Vector2 position, Vector2 size, Color? tint = null)
    {
        Transform existing = transform.Find(objectName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(Image));
        if (existing == null) go.transform.SetParent(transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        Image image = go.GetComponent<Image>(); image.color = tint ?? new Color(0.018f, 0.025f, 0.045f, 0.9f); image.raycastTarget = false;
        return rect;
    }

    private static void StyleBar(Slider slider, Vector2 position, Vector2 size, Color fillColor)
    {
        if (slider == null) return;
        RectTransform rect = slider.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
        Image background = slider.transform.Find("Background")?.GetComponent<Image>();
        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (background != null) { background.color = new Color(0.025f, 0.03f, 0.045f, 0.98f); background.raycastTarget = false; }
        if (fill != null) { fill.color = fillColor; fill.raycastTarget = false; }
    }

    private static void PlaceLabel(Text text, Vector2 position, float width, float height, int size, Color color, bool bold)
    {
        if (text == null) return;
        RectTransform rect = text.rectTransform; rect.anchoredPosition = position; rect.sizeDelta = new Vector2(width, height);
        StyleText(text, size, bold ? FontStyle.Bold : FontStyle.Normal, color, TextAnchor.MiddleLeft);
    }

    private static void StyleText(Text text, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        if (text == null) return;
        text.fontSize = size; text.fontStyle = style; text.color = color; text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
        Outline outline = text.GetComponent<Outline>();
        if (outline == null) outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f); outline.effectDistance = new Vector2(1.5f, -1.5f);
        text.raycastTarget = false;
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
        if (potionText == null)
            potionText = CreateRuntimeLabel("Potion Text", new Vector2(40f, -390f));
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
        SyncHudVisibility();
        if (skillController == null && playerStats != null)
            skillController = playerStats.GetComponent<SkillController>();
        if (skillText != null && skillController != null)
        {
            skillText.text = FormatSkillLine(SkillController.SkillSlot.Q) + "    " +
                             FormatSkillLine(SkillController.SkillSlot.E) + "    " +
                             FormatSkillLine(SkillController.SkillSlot.R);
        }
        if (potionController == null && playerStats != null)
            potionController = playerStats.GetComponent<PlayerPotionController>();
        if (potionText != null && potionController != null)
        {
            string health = potionController.HealthCooldownRemaining > 0f
                ? $"CD {potionController.HealthCooldownRemaining:0.0}s" : "READY";
            string mana = potionController.ManaCooldownRemaining > 0f
                ? $"CD {potionController.ManaCooldownRemaining:0.0}s" : "READY";
            potionText.text = $"F1 HP {health}\nF2 MP {mana}";
        }
        RefreshEquippedWeaponText();
    }

    private void SyncHudVisibility()
    {
        if (flowController == null) flowController = FindAnyObjectByType<DungeonFlowController>();
        bool shouldShow = flowController == null || !flowController.IsMenuVisible;
        if (hudVisible == shouldShow) return;
        hudVisible = shouldShow;
        GameObject[] elements =
        {
            healthSlider != null ? healthSlider.gameObject : null,
            manaSlider != null ? manaSlider.gameObject : null,
            experienceSlider != null ? experienceSlider.gameObject : null,
            levelText != null ? levelText.gameObject : null,
            skillText != null ? skillText.gameObject : null,
            goldText != null ? goldText.gameObject : null,
            potionText != null ? potionText.gameObject : null,
            transform.Find("Player HUD Panel")?.gameObject,
            transform.Find("Player HUD Accent")?.gameObject,
            transform.Find("Skill HUD Panel")?.gameObject
        };
        foreach (GameObject element in elements)
            if (element != null) element.SetActive(shouldShow);
    }

    private string FormatSkillLine(SkillController.SkillSlot slot)
    {
        float remaining = skillController.GetCooldownRemaining(slot);
        string key = slot.ToString();
        return remaining > 0f
            ? $"<color=#FFD166>[{key}]</color> {skillController.GetSkillName(slot)}  <color=#FF9F43>{remaining:0.0}s</color>"
            : $"<color=#65DFFF>[{key}]</color> {skillController.GetSkillName(slot)}  " +
              $"<color=#7CFF9B>{skillController.GetManaCost(slot)} MP READY</color>";
    }

    private void RefreshEquippedWeaponText()
    {
        if (weaponText == null || playerStats == null) return;
        if (playerStats.CurrentClass == PlayerStats.PlayerClass.Archer)
        {
            if (archerController == null) archerController = playerStats.GetComponent<ArcherController>();
            if (archerController == null) return;
            string weapon = archerController.EquippedWeapon == ArcherController.RangedWeapon.Crossbow ? "CROSSBOW" : "BOW";
            weaponText.text = $"{weapon}  ATK {archerController.AttackDamage}  AIM: MOUSE";
        }
        else if (playerStats.CurrentClass == PlayerStats.PlayerClass.Mage)
        {
            if (mageController == null) mageController = playerStats.GetComponent<MageController>();
            if (mageController == null) return;
            string weapon = mageController.EquippedWeapon == MageController.MagicWeapon.Spellbook ? "SPELLBOOK" : "STAFF";
            weaponText.text = $"{weapon}  ATK {mageController.AttackDamage}  MP {mageController.ManaCost}  AIM: MOUSE";
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
            PlayerStats.PlayerClass.Archer => $"ARCHER  LV.{currentLevel}  DEX {playerStats.Dexterity}  DEF {playerStats.Defense}",
            PlayerStats.PlayerClass.Mage => $"MAGE  LV.{currentLevel}  INT {playerStats.Intelligence}  DEF {playerStats.Defense}",
            _ => $"WARRIOR  LV.{currentLevel}  STR {playerStats.Strength}  DEF {playerStats.Defense}"
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
        weaponText.text = $"{displayName}  ATK {attackController.AttackDamage}";
    }
}
