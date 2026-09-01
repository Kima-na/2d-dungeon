using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public sealed class EquipmentPickup : MonoBehaviour
{
    [SerializeField, Min(0f)] private float attractionRange = 2.2f;
    [SerializeField, Min(0f)] private float attractionSpeed = 5f;
    private EquipmentItem item;
    private PlayerStats player;
    private bool collected;

    public static EquipmentPickup Spawn(Vector2 position, EquipmentRarity? rarity = null)
    {
        GameObject go = new("Equipment Drop", typeof(SpriteRenderer), typeof(CircleCollider2D),
            typeof(Rigidbody2D), typeof(EquipmentPickup));
        go.transform.position = position; go.transform.localScale = Vector3.one * 0.72f;
        LootVisualDatabase visuals = Resources.Load<LootVisualDatabase>("LootVisualDatabase");
        EquipmentInventory inventory = FindAnyObjectByType<PlayerStats>()?.GetComponent<EquipmentInventory>();
        EquipmentItem rolledItem = inventory != null ? inventory.CreateRandomLoot(rarity) : null;
        EquipmentRarity displayRarity = rolledItem != null ? rolledItem.rarity : rarity ?? EquipmentRarity.Common;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = visuals != null ? visuals.GetEquipmentChest(displayRarity) : MonsterRoster.PlaceholderSprite;
        renderer.color = visuals != null ? visuals.GetEquipmentChestColor(displayRarity) : EquipmentRarityUtility.Color(displayRarity);
        renderer.sortingOrder = 4;
        CircleCollider2D trigger = go.GetComponent<CircleCollider2D>(); trigger.isTrigger = true; trigger.radius = 0.48f;
        Rigidbody2D body = go.GetComponent<Rigidbody2D>(); body.gravityScale = 0f; body.bodyType = RigidbodyType2D.Kinematic;
        EquipmentPickup pickup = go.GetComponent<EquipmentPickup>(); pickup.item = rolledItem;
        go.name = $"Equipment Drop [{displayRarity}]";
        if (displayRarity == EquipmentRarity.Legendary) go.transform.localScale = Vector3.one * 0.88f;
        Destroy(go, 45f); return pickup;
    }

    private void Update()
    {
        if (player == null) player = FindAnyObjectByType<PlayerStats>();
        if (player == null || player.IsDead) return;
        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance <= 0.6f) { Collect(player); return; }
        if (distance <= attractionRange)
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position,
                attractionSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStats collector = other.GetComponentInParent<PlayerStats>();
        if (collector != null) Collect(collector);
    }

    private void Collect(PlayerStats collector)
    {
        if (collected) return;
        EquipmentInventory inventory = collector.GetComponent<EquipmentInventory>();
        if (inventory == null) return;
        if (item == null) item = inventory.CreateRandomLoot();
        if (!inventory.Add(item)) return;
        collected = true; Destroy(gameObject);
    }
}
