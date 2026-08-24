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
    [SerializeField] private AttackController attackController;
    [SerializeField] private ArcherController archerController;
    [SerializeField] private GameObject deathPanel;

    private void Awake()
    {
        if (attackController == null && playerStats != null)
            attackController = playerStats.GetComponent<AttackController>();
        if (archerController == null && playerStats != null)
            archerController = playerStats.GetComponent<ArcherController>();
        CreateMissingGrowthLabels();
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void CreateMissingGrowthLabels()
    {
        if (levelText == null)
            levelText = CreateRuntimeLabel("Level Text", new Vector2(40f, -160f));
        if (experienceText == null)
            experienceText = CreateRuntimeLabel("Experience Text", new Vector2(40f, -200f));
        if (weaponText == null)
            weaponText = CreateRuntimeLabel("Weapon Text", new Vector2(40f, -240f));
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
        playerStats.Died += ShowDeath;
        if (attackController != null) attackController.WeaponChanged += UpdateWeapon;
        UpdateHealth(playerStats.CurrentHealth, playerStats.MaxHealth);
        UpdateMana(playerStats.CurrentMana, playerStats.MaxMana);
        UpdateExperience(playerStats.CurrentExperience, playerStats.ExperienceToNextLevel);
        UpdateClass(playerStats.CurrentClass);
    }

    private void OnDisable()
    {
        if (playerStats == null) return;
        playerStats.HealthChanged -= UpdateHealth;
        playerStats.ManaChanged -= UpdateMana;
        playerStats.ExperienceChanged -= UpdateExperience;
        playerStats.LeveledUp -= UpdateLevel;
        playerStats.ClassChanged -= UpdateClass;
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

    private void UpdateExperience(int current, int required)
    {
        if (experienceSlider != null) { experienceSlider.maxValue = required; experienceSlider.value = current; }
        if (experienceText != null) experienceText.text = $"EXP  {current} / {required}";
    }

    private void UpdateLevel(int currentLevel)
    {
        if (levelText != null)
            levelText.text = playerStats.CurrentClass == PlayerStats.PlayerClass.Archer
                ? $"[F1/F2] ARCHER  LV.{currentLevel}  DEX {playerStats.Dexterity}  DEF {playerStats.Defense}"
                : $"[F1/F2] WARRIOR  LV.{currentLevel}  STR {playerStats.Strength}  DEF {playerStats.Defense}";
    }

    private void UpdateClass(PlayerStats.PlayerClass playerClass)
    {
        UpdateLevel(playerStats.Level);
        if (weaponText == null) return;
        if (playerClass == PlayerStats.PlayerClass.Archer)
        {
            if (archerController == null) archerController = playerStats.GetComponent<ArcherController>();
            weaponText.text = $"BOW  ATK {(archerController != null ? archerController.AttackDamage : playerStats.Dexterity)}  AIM: MOUSE";
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
