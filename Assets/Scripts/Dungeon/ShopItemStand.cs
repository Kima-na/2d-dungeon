using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ShopItemStand : MonoBehaviour
{
    private static Sprite cardSprite;
    private int price;
    private bool sold;
    private EquipmentRarity rarity;
    private TextMesh titleLabel, priceLabel, actionLabel;
    private SpriteRenderer panel, accent, icon;
    private PlayerStats nearbyPlayer;
    private EquipmentInventory nearbyInventory;
    private ShopMerchant merchant;
    private bool lastShopOpen;

    public static ShopItemStand Spawn(Transform parent, Vector2 localPosition, int price, string itemName,
        EquipmentRarity rarity)
    {
        GameObject stand = new($"Shop Offer - {rarity}", typeof(BoxCollider2D), typeof(ShopItemStand));
        stand.transform.SetParent(parent, false);
        stand.transform.localPosition = localPosition;
        BoxCollider2D trigger = stand.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.35f, 1.75f);
        ShopItemStand shop = stand.GetComponent<ShopItemStand>();
        shop.price = price;
        shop.rarity = rarity;
        shop.merchant = parent.GetComponentInChildren<ShopMerchant>();
        shop.BuildCard();
        shop.RefreshCard();
        return shop;
    }

    private void Update()
    {
        bool shopOpen = nearbyPlayer != null && merchant != null && merchant.IsOpenFor(nearbyPlayer);
        if (shopOpen != lastShopOpen) { lastShopOpen = shopOpen; RefreshCard(); }
        if (!sold && shopOpen && nearbyInventory != null && Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame) Purchase();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        EquipmentInventory inventory = other.GetComponentInParent<EquipmentInventory>();
        if (player == null || inventory == null) return;
        ClearNearbyPlayer();
        nearbyPlayer = player;
        nearbyInventory = inventory;
        nearbyPlayer.GoldChanged += OnGoldChanged;
        RefreshCard();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyPlayer != null && other.GetComponentInParent<PlayerStats>() == nearbyPlayer)
            ClearNearbyPlayer();
    }

    private void Purchase()
    {
        if (!nearbyPlayer.TrySpendGold(price))
        {
            actionLabel.text = $"NEED {price - nearbyPlayer.Gold} MORE GOLD";
            actionLabel.color = new Color(1f, 0.36f, 0.32f);
            return;
        }
        EquipmentItem item = nearbyInventory.RollShopLoot(rarity, nearbyPlayer.CurrentClass);
        if (item == null)
        {
            nearbyPlayer.AddGold(price);
            actionLabel.text = "INVENTORY FULL";
            actionLabel.color = new Color(1f, 0.36f, 0.32f);
            return;
        }
        sold = true;
        GetComponent<Collider2D>().enabled = false;
        titleLabel.text = "SOLD";
        priceLabel.text = "THANK YOU";
        actionLabel.text = "ITEM DELIVERED";
        Color disabled = new(0.24f, 0.26f, 0.3f, 0.94f);
        panel.color = disabled;
        accent.color = disabled;
        icon.color = disabled;
        ClearNearbyPlayer();
    }

    private void ClearNearbyPlayer()
    {
        if (nearbyPlayer != null) nearbyPlayer.GoldChanged -= OnGoldChanged;
        nearbyPlayer = null;
        nearbyInventory = null;
        if (!sold) RefreshCard();
    }

    private void OnGoldChanged(int _) => RefreshCard();

    private void BuildCard()
    {
        Color rarityColor = RarityColor();
        CreateBlock("Card Shadow", new Vector3(0.07f, -0.08f), new Vector2(2.14f, 1.44f),
            new Color(0f, 0f, 0f, 0.56f), 3);
        panel = CreateBlock("Card Panel", Vector3.zero, new Vector2(2.08f, 1.38f),
            new Color(0.055f, 0.07f, 0.105f, 0.97f), 4);
        accent = CreateBlock("Rarity Accent", new Vector3(0f, 0.64f), new Vector2(2.08f, 0.1f), rarityColor, 5);
        icon = CreateBlock("Item Icon", new Vector3(0f, 0.14f), new Vector2(0.34f, 0.34f), rarityColor, 6);
        icon.sprite = RuntimeCombatSprites.Circle;
        titleLabel = CreateLabel("Item Name", new Vector3(0f, 0.46f), 42, Color.white, 7);
        priceLabel = CreateLabel("Price", new Vector3(0f, -0.22f), 38, new Color(1f, 0.78f, 0.24f), 7);
        actionLabel = CreateLabel("Action", new Vector3(0f, -0.52f), 27, new Color(0.62f, 0.72f, 0.84f), 7);
    }

    private void RefreshCard()
    {
        if (sold || titleLabel == null) return;
        string className = nearbyPlayer != null ? nearbyPlayer.CurrentClass.ToString().ToUpperInvariant() + " " : "";
        titleLabel.text = className + (rarity switch
        {
            EquipmentRarity.Rare => "RARE GEAR",
            EquipmentRarity.Uncommon => "FINE GEAR",
            _ => "COMMON GEAR"
        });
        priceLabel.text = $"{price} GOLD";
        if (nearbyPlayer == null)
        {
            actionLabel.text = "APPROACH TO INSPECT";
            actionLabel.color = new Color(0.62f, 0.72f, 0.84f);
            panel.color = new Color(0.055f, 0.07f, 0.105f, 0.97f);
            return;
        }
        bool shopOpen = merchant != null && merchant.IsOpenFor(nearbyPlayer);
        if (!shopOpen)
        {
            actionLabel.text = "TALK TO MERCHANT [F]";
            actionLabel.color = new Color(0.62f, 0.72f, 0.84f);
            panel.color = new Color(0.055f, 0.07f, 0.105f, 0.97f);
            return;
        }
        bool affordable = nearbyPlayer.Gold >= price;
        actionLabel.text = affordable ? $"[F] BUY  |  OWNED {nearbyPlayer.Gold}" : $"LOCKED  |  OWNED {nearbyPlayer.Gold}";
        actionLabel.color = affordable ? new Color(0.42f, 1f, 0.68f) : new Color(1f, 0.36f, 0.32f);
        panel.color = affordable ? new Color(0.065f, 0.105f, 0.12f, 0.98f) : new Color(0.11f, 0.06f, 0.075f, 0.98f);
    }

    private SpriteRenderer CreateBlock(string name, Vector3 position, Vector2 size, Color color, int order)
    {
        GameObject block = new(name, typeof(SpriteRenderer));
        block.transform.SetParent(transform, false);
        block.transform.localPosition = position;
        block.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
        renderer.sprite = CardSprite();
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private TextMesh CreateLabel(string name, Vector3 position, int size, Color color, int order)
    {
        GameObject textObject = new(name, typeof(TextMesh));
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = position;
        TextMesh text = textObject.GetComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.055f;
        text.fontSize = size;
        text.color = color;
        textObject.GetComponent<MeshRenderer>().sortingOrder = order;
        return text;
    }

    private Color RarityColor() => rarity switch
    {
        EquipmentRarity.Rare => new Color(0.24f, 0.62f, 1f),
        EquipmentRarity.Uncommon => new Color(0.3f, 0.92f, 0.54f),
        _ => new Color(0.82f, 0.86f, 0.92f)
    };

    private static Sprite CardSprite()
    {
        if (cardSprite != null) return cardSprite;
        Texture2D texture = new(4, 4, TextureFormat.RGBA32, false)
        { name = "Runtime Shop Card", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        cardSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f, 4f);
        cardSprite.name = "Shop Card";
        return cardSprite;
    }

    private void OnDestroy() => ClearNearbyPlayer();
}
