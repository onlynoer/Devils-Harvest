using UnityEngine;

/// <summary>
/// Damages the first Creature the projectile collides with, then destroys the projectile.
/// Projectiles that hit anything else stay until their lifetime runs out.
/// </summary>
public class ProjectileHit : MonoBehaviour
{
    [Tooltip("Seconds before the projectile is destroyed if it hits nothing.")]
    public float lifetime = 5f;

    private Projectile projectile;
    private Rigidbody body;

    private void Awake()
    {
        projectile = GetComponent<Projectile>();
        body = GetComponent<Rigidbody>();
    }

    private void Start() => Destroy(gameObject, lifetime);

    private void OnCollisionEnter(Collision collision)
    {
        Creature creature = collision.gameObject.GetComponentInParent<Creature>();
        if (creature == null || projectile == null) return;

        Vector3 direction = body != null && body.linearVelocity.sqrMagnitude > .01f ? body.linearVelocity.normalized : Vector3.zero;
        creature.TakeDamage(projectile.damage, direction);
        Destroy(gameObject);
    }
}
