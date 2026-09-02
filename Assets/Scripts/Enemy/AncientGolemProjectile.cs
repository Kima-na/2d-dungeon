using UnityEngine;

public sealed class AncientGolemProjectile : MonoBehaviour
{
    private static Sprite[] phaseOneSprites, phaseTwoSprites;
    private Vector2 direction; private float speed, expires; private int damage;
    public static void Spawn(Vector2 position, Vector2 direction, int damage, float speed, Color color, bool phaseTwo)
    {
        GameObject go = new("Golem Rock", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(AncientGolemProjectile));
        go.transform.position = position;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        Sprite[] sprites;
        if(phaseTwo)
        {
            if(phaseTwoSprites==null)
            {
                phaseTwoSprites=new Sprite[7];
                for(int i=0;i<phaseTwoSprites.Length;i++)phaseTwoSprites[i]=Resources.Load<Sprite>($"AncientGolem/P2Projectile_{i+1:00}");
            }
            sprites=phaseTwoSprites;
        }
        else
        {
            if(phaseOneSprites==null)
            {
                phaseOneSprites=new Sprite[4];
                for(int i=0;i<phaseOneSprites.Length;i++)phaseOneSprites[i]=Resources.Load<Sprite>($"AncientGolem/P1Projectile_{i+1:00}");
            }
            sprites=phaseOneSprites;
        }
        renderer.sprite=sprites[Random.Range(0,sprites.Length)];renderer.color=Color.white;
        go.transform.localScale=Vector3.one*(1.25f/Mathf.Max(.01f,renderer.sprite.bounds.size.x));
        float rotation=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
        go.transform.rotation=Quaternion.Euler(0f,0f,rotation);
        renderer.flipX=true;
        renderer.sortingOrder = 10;
        go.GetComponent<CircleCollider2D>().isTrigger = true; go.GetComponent<AncientGolemProjectile>().Initialize(direction, damage, speed);
    }
    private void Initialize(Vector2 value, int amount, float velocity) { direction = value.normalized; damage = amount; speed = velocity; expires = Time.time + 4f; }
    private void Update() { transform.position += (Vector3)(direction * speed * Time.deltaTime); if (Time.time >= expires) Destroy(gameObject); }
    private void OnTriggerEnter2D(Collider2D other) { PlayerStats player = other.GetComponentInParent<PlayerStats>(); if (player == null || player.IsDead) return; player.TakeDamage(damage); Destroy(gameObject); }
}
