using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BossMovement), typeof(BossHealth), typeof(AncientGolemAnimator))]
public sealed class AncientGolemCombat : MonoBehaviour
{
    private BossMovement movement; private BossHealth health; private AncientGolemAnimator animatorComponent;
    private float nextAttack, damageScale = 1f; private bool phaseTwo;
    public bool IsAttacking { get; private set; }
    private void Awake() { movement = GetComponent<BossMovement>(); health = GetComponent<BossHealth>(); animatorComponent = GetComponent<AncientGolemAnimator>(); }
    public void Configure(float scale) => damageScale = Mathf.Max(.1f, scale);
    private void Update()
    {
        if (!phaseTwo && health.CurrentHealth <= health.MaxHealth / 2) { phaseTwo = true; animatorComponent.SetPhaseTwo(); BossAttackEffect.Spawn(null, transform.position, Vector2.one * 4f, .7f, 0, new Color(.2f,.75f,1f,.8f)); }
        if (IsAttacking || health.IsDead || movement.Target == null || Time.time < nextAttack || movement.DistanceToTarget > 9f) return;
        if (movement.DistanceToTarget < 2.15f) StartCoroutine(Slam()); else if (phaseTwo && Random.value < .45f) StartCoroutine(GroundCrack()); else StartCoroutine(ThrowRocks());
    }
    private IEnumerator Slam() { IsAttacking = true; animatorComponent.PlaySlam(); BossAttackEffect.Spawn(null, transform.position, Vector2.one * 3.2f, .55f, 0, new Color(.25f,.65f,1f,.7f)); yield return new WaitForSeconds(.52f); DamageRadius(transform.position, phaseTwo ? 2f : 1.65f, phaseTwo ? 72 : 55); yield return new WaitForSeconds(.25f); Finish(); }
    private IEnumerator ThrowRocks() { IsAttacking = true; animatorComponent.PlayRanged(); yield return new WaitForSeconds(.38f); Transform target = movement.Target; if (target != null) { Vector2 aim = ((Vector2)target.position - (Vector2)transform.position).normalized; int count = phaseTwo ? 5 : 1; for (int i=0;i<count;i++) AncientGolemProjectile.Spawn(transform.position, Quaternion.Euler(0,0,(i-(count-1)*.5f)*12f)*aim, Scale(phaseTwo?42:48), phaseTwo?6.5f:5.5f, new Color(.35f,.8f,1f)); } yield return new WaitForSeconds(.28f); Finish(); }
    private IEnumerator GroundCrack() { IsAttacking = true; animatorComponent.PlayCrack(); Vector2 origin = transform.position; Vector2 direction = movement.Target == null ? Vector2.right : ((Vector2)movement.Target.position-origin).normalized; for(int i=1;i<=4;i++){ Vector2 point=origin+direction*i*1.25f; BossAttackEffect.Spawn(null, point, Vector2.one*1.45f, .32f, 0, new Color(.1f,.55f,1f,.75f)); yield return new WaitForSeconds(.13f); DamageRadius(point,.75f,48); } Finish(); }
    private int Scale(int value) => Mathf.RoundToInt(value * damageScale);
    private void DamageRadius(Vector2 point,float radius,int amount) { Transform target=movement.Target; if(target==null || Vector2.Distance(point,target.position)>radius) return; target.GetComponent<PlayerStats>()?.TakeDamage(Scale(amount)); }
    private void Finish() { animatorComponent.EndAction(); IsAttacking=false; nextAttack=Time.time+(phaseTwo?.72f:1.05f); }
    private void OnDisable() { StopAllCoroutines(); IsAttacking=false; }
}
