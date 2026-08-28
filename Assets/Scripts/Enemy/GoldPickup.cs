using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class GoldPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int amount = 1;
    [SerializeField, Min(0f)] private float attractionRange = 2.8f;
    [SerializeField, Min(0f)] private float attractionSpeed = 7f;
    private PlayerStats player;
    private bool collected;

    public static GoldPickup Spawn(Vector2 position, int goldAmount)
    {
        GameObject go = new("Gold", typeof(SpriteRenderer), typeof(CircleCollider2D),
            typeof(Rigidbody2D), typeof(GoldPickup));
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        LootVisualDatabase visuals = Resources.Load<LootVisualDatabase>("LootVisualDatabase");
        renderer.sprite = visuals != null && visuals.coin != null ? visuals.coin : MonsterRoster.PlaceholderSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 3;
        CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.38f;
        Rigidbody2D body = go.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        GoldPickup pickup = go.GetComponent<GoldPickup>();
        pickup.amount = Mathf.Max(1, goldAmount);
        Destroy(go, 30f);
        return pickup;
    }

    private void Update()
    {
        if (player == null) player = FindAnyObjectByType<PlayerStats>();
        if (player == null || player.IsDead) return;
        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance <= 0.55f) { Collect(player); return; }
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
        collected = true;
        collector.AddGold(amount);
        Destroy(gameObject);
    }
}
