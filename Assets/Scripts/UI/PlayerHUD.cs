using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Text healthText;
    [SerializeField] private Text manaText;
    [SerializeField] private GameObject deathPanel;

    private void Awake()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerStats == null) return;
        playerStats.HealthChanged += UpdateHealth;
        playerStats.ManaChanged += UpdateMana;
        playerStats.Died += ShowDeath;
        UpdateHealth(playerStats.CurrentHealth, playerStats.MaxHealth);
        UpdateMana(playerStats.CurrentMana, playerStats.MaxMana);
    }

    private void OnDisable()
    {
        if (playerStats == null) return;
        playerStats.HealthChanged -= UpdateHealth;
        playerStats.ManaChanged -= UpdateMana;
        playerStats.Died -= ShowDeath;
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
}
