using UnityEngine;

public sealed class AncientGolemProjectile : MonoBehaviour
{
    private Vector2 direction; private float speed, expires; private int damage;
    public static void Spawn(Vector2 position, Vector2 direction, int damage, float speed, Color color)
    {
        GameObject go = new("Golem Rock", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(AncientGolemProjectile));
        go.transform.position = position; go.transform.localScale = Vector3.one * .38f;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = MonsterRoster.PlaceholderSprite; renderer.color = color; renderer.sortingOrder = 10;
        go.GetComponent<CircleCollider2D>().isTrigger = true; go.GetComponent<AncientGolemProjectile>().Initialize(direction, damage, speed);
    }
    private void Initialize(Vector2 value, int amount, float velocity) { direction = value.normalized; damage = amount; speed = velocity; expires = Time.time + 4f; }
    private void Update() { transform.position += (Vector3)(direction * speed * Time.deltaTime); transform.Rotate(0, 0, 240f * Time.deltaTime); if (Time.time >= expires) Destroy(gameObject); }
    private void OnTriggerEnter2D(Collider2D other) { PlayerStats player = other.GetComponentInParent<PlayerStats>(); if (player == null || player.IsDead) return; player.TakeDamage(damage); Destroy(gameObject); }
}
