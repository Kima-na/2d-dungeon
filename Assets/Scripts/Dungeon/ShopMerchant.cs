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
        EquipmentItem item = nearbyInventory.RollShopLoot(GetOfferRarity(selectedOffer), nearbyPlayer.CurrentClass);
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

    private EquipmentRarity GetOfferRarity(int offer)
    {
        DungeonGenerator generator = FindAnyObjectByType<DungeonGenerator>();
        DungeonDifficulty difficulty = generator != null ? generator.Difficulty : DungeonDifficulty.Easy;
        return difficulty switch
        {
            DungeonDifficulty.Normal => offer switch
            {
                0 => EquipmentRarity.Uncommon,
                1 => EquipmentRarity.Rare,
                _ => EquipmentRarity.Epic
            },
            DungeonDifficulty.Hard => offer switch
            {
                0 => EquipmentRarity.Rare,
                1 => EquipmentRarity.Epic,
                _ => EquipmentRarity.Legendary
            },
            DungeonDifficulty.Nightmare => offer switch
            {
                0 => EquipmentRarity.Epic,
                _ => EquipmentRarity.Legendary
            },
            _ => Rarities[offer]
        };
    }

    private void RefreshPrompt()
    {
        if (prompt == null) return;
        if (nearbyPlayer == null) prompt.text = "상인 판매";
        else if (!shopOpen) prompt.text = "[F] 상인 판매";
        else
        {
            string rarity = EquipmentRarityUtility.KoreanName(GetOfferRarity(selectedOffer));
            string state = sold[selectedOffer] ? "판매 완료" : $"{Prices[selectedOffer]} GOLD";
            prompt.text = $"<{rarity} 직업 장비>  {state}\n[←/→] 상품 선택  [F] 구매  [ESC] 닫기";
        }
        prompt.color = shopOpen
            ? new Color(0.42f, 1f, 0.68f)
            : new Color(1f, 0.92f, 0.38f);
    }

    private TextMesh CreatePrompt()
    {
        GameObject label = new("Merchant Prompt", typeof(TextMesh));
        label.transform.SetParent(transform, false);

        SpriteRenderer merchantRenderer = GetComponent<SpriteRenderer>();
        float halfHeight = merchantRenderer != null && merchantRenderer.sprite != null
            ? merchantRenderer.sprite.bounds.extents.y : 0.65f;
        float worldScale = Mathf.Max(0.01f, transform.lossyScale.y);
        label.transform.localPosition = new Vector3(0f, halfHeight + 0.28f / worldScale, -0.1f);
        label.transform.localScale = Vector3.one *
            (0.85f / Mathf.Max(0.01f, transform.lossyScale.x));

        TextMesh text = label.GetComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.1f;
        text.fontSize = 64;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        label.GetComponent<MeshRenderer>().sortingOrder = 20;
        return text;
    }
}
