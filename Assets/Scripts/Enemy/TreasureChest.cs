using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class TreasureChest : MonoBehaviour
{
    private bool opened;

    public static TreasureChest Spawn(Transform parent, Vector2 localPosition)
    {
        GameObject chestObject = new("Treasure Chest", typeof(SpriteRenderer),
            typeof(BoxCollider2D), typeof(TreasureChest));
        chestObject.transform.SetParent(parent, false);
        chestObject.transform.localPosition = localPosition;
        chestObject.transform.localScale = Vector3.one * 2.4f;

        LootVisualDatabase visuals = Resources.Load<LootVisualDatabase>("LootVisualDatabase");
        SpriteRenderer renderer = chestObject.GetComponent<SpriteRenderer>();
        renderer.sprite = visuals != null ? visuals.GetRandomChest() : MonsterRoster.PlaceholderSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;
        BoxCollider2D collider = chestObject.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.75f, 0.65f);
        return chestObject.GetComponent<TreasureChest>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened) return;
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player == null) return;
        opened = true;
        player.AddGold(Random.Range(12, 31));
        player.GetComponent<EquipmentInventory>()?.RollRandomLoot();
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        Destroy(gameObject, 0.35f);
    }
}
