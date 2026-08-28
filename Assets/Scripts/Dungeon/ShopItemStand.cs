using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ShopItemStand : MonoBehaviour
{
    private int price;
    private bool sold;
    private EquipmentRarity rarity;
    private TextMesh label;

    public static ShopItemStand Spawn(Transform parent, Vector2 localPosition, int price, string itemName,
        EquipmentRarity rarity)
    {
        GameObject stand = new($"Shop Offer - {itemName}", typeof(BoxCollider2D), typeof(ShopItemStand));
        stand.transform.SetParent(parent, false);
        stand.transform.localPosition = localPosition;
        BoxCollider2D trigger = stand.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.8f, 1.4f);
        ShopItemStand shop = stand.GetComponent<ShopItemStand>();
        shop.price = price;
        shop.rarity = rarity;
        GameObject textObject = new("Price", typeof(TextMesh));
        textObject.transform.SetParent(stand.transform, false);
        textObject.transform.localPosition = new Vector3(0f, .65f, 0f);
        shop.label = textObject.GetComponent<TextMesh>();
        shop.label.text = $"{itemName}\n{price} GOLD";
        shop.label.anchor = TextAnchor.MiddleCenter;
        shop.label.alignment = TextAlignment.Center;
        shop.label.characterSize = .12f;
        shop.label.fontSize = 38;
        shop.label.color = new Color(1f, .85f, .3f);
        shop.label.GetComponent<MeshRenderer>().sortingOrder = 5;
        return shop;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (sold) return;
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        EquipmentInventory inventory = other.GetComponentInParent<EquipmentInventory>();
        if (player == null || inventory == null) return;
        if (!player.TrySpendGold(price))
        {
            if (label != null) label.text = $"골드 부족\n{price} GOLD";
            return;
        }
        EquipmentItem item = inventory.RollRandomLoot(rarity);
        if (item == null) { player.AddGold(price); return; }
        sold = true;
        if (label != null) { label.text = "판매 완료"; label.color = Color.gray; }
        GetComponent<Collider2D>().enabled = false;
    }
}
