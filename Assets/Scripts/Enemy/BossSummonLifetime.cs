using UnityEngine;

public sealed class BossSummonLifetime : MonoBehaviour
{
    public void SetLifetime(float lifetime) => Destroy(gameObject, Mathf.Max(0.1f, lifetime));
}
