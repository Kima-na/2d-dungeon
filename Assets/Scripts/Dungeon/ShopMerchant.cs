using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ShopMerchant : MonoBehaviour
{
    private PlayerStats nearbyPlayer;
    private EquipmentInventory nearbyInventory;
    private TextMesh prompt;
    private bool shopOpen;
    private int selectedOffer;
    private readonly bool[] sold = new bool[3];
    private static readonly int[] Prices = { 15, 25, 40 };
    private static readonly EquipmentRarity[] Rarities =
        { EquipmentRarity.Common, EquipmentRarity.Uncommon, EquipmentRarity.Rare };

    public bool IsOpenFor(PlayerStats player) => shopOpen && player != null && player == nearbyPlayer;

    private void Awake()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.8f, 2.3f);
        prompt = CreatePrompt();
        RefreshPrompt();
    }

    private void Update()
    {
        if (nearbyPlayer == null || Keyboard.current == null) return;
        if (shopOpen && Keyboard.current.leftArrowKey.wasPressedThisFrame)
        { selectedOffer = (selectedOffer + 2) % 3; RefreshPrompt(); }
        if (shopOpen && Keyboard.current.rightArrowKey.wasPressedThisFrame)
        { selectedOffer = (selectedOffer + 1) % 3; RefreshPrompt(); }
        if (shopOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        { shopOpen = false; RefreshPrompt(); return; }
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;
        if (!shopOpen) { shopOpen = true; RefreshPrompt(); return; }
        PurchaseSelected();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player == null) return;
        nearbyPlayer = player;
        nearbyInventory = other.GetComponentInParent<EquipmentInventory>();
        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyPlayer == null || other.GetComponentInParent<PlayerStats>() != nearbyPlayer) return;
        nearbyPlayer = null;
        nearbyInventory = null;
        shopOpen = false;
        RefreshPrompt();
    }

    private void PurchaseSelected()
    {
        if (nearbyPlayer == null || nearbyInventory == null || sold[selectedOffer]) return;
        int price = Prices[selectedOffer];
        if (!nearbyPlayer.TrySpendGold(price))
        {
            prompt.text = $"골드가 {price - nearbyPlayer.Gold} 부족합니다\n[←/→] 상품 선택  [F] 구매  [ESC] 닫기";
            prompt.color = new Color(1f, 0.36f, 0.32f);
            return;
        }
        EquipmentItem item = nearbyInventory.RollShopLoot(Rarities[selectedOffer], nearbyPlayer.CurrentClass);
        if (item == null)
        {
            nearbyPlayer.AddGold(price);
            prompt.text = "구매 가능한 직업 장비가 없습니다";
            prompt.color = new Color(1f, 0.36f, 0.32f);
            return;
        }
        sold[selectedOffer] = true;
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (prompt == null) return;
        if (nearbyPlayer == null) prompt.text = "상인";
        else if (!shopOpen) prompt.text = "[F] 상인과 대화";
        else
        {
            string rarity = selectedOffer switch { 0 => "일반", 1 => "고급", _ => "희귀" };
            string state = sold[selectedOffer] ? "판매 완료" : $"{Prices[selectedOffer]} GOLD";
            prompt.text = $"<{rarity} 직업 장비>  {state}\n[←/→] 상품 선택  [F] 구매  [ESC] 닫기";
        }
        prompt.color = shopOpen ? new Color(0.42f, 1f, 0.68f) : Color.white;
    }

    private TextMesh CreatePrompt()
    {
        GameObject label = new("Merchant Prompt", typeof(TextMesh));
        label.transform.SetParent(transform, false);
        label.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        TextMesh text = label.GetComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.045f;
        text.fontSize = 34;
        label.GetComponent<MeshRenderer>().sortingOrder = 9;
        return text;
    }
}
