using UnityEngine;

/// <summary>
/// Damages what the projectile collides with, then destroys the projectile.
/// Player shots (no owner set) only damage Creatures. Shots with an owner (e.g. an armed animal) damage
/// anything <see cref="IDamageable"/>, including the player, except the owner itself.
/// Projectiles that hit nothing damageable stay until their lifetime runs out.
/// </summary>
public class ProjectileHit : MonoBehaviour
{
    [Tooltip("Seconds before the projectile is destroyed if it hits nothing.")]
    public float lifetime = 5f;

    private Projectile projectile;
    private Rigidbody body;
    private GameObject owner;
    private bool hostile;

    private void Awake()
    {
        projectile = GetComponent<Projectile>();
        body = GetComponent<Rigidbody>();
    }

    private void Start() => Destroy(gameObject, lifetime);

    /// <summary>Marks the shot as fired by <paramref name="shooter"/> and stops it colliding with the shooter.</summary>
    public void SetOwner(GameObject shooter, Collider[] shooterColliders)
    {
        owner = shooter;
        hostile = true;
        if (shooterColliders == null) return;
        foreach (Collider mine in GetComponentsInChildren<Collider>())
        foreach (Collider theirs in shooterColliders)
            if (mine != null && theirs != null) Physics.IgnoreCollision(mine, theirs);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (projectile == null) return;
        Vector3 direction = body != null && body.linearVelocity.sqrMagnitude > .01f ? body.linearVelocity.normalized : Vector3.zero;

        if (!hostile)
        {
            Creature creature = collision.gameObject.GetComponentInParent<Creature>();
            if (creature == null) return;
            creature.TakeDamage(projectile.damage, direction);
            Destroy(gameObject);
            return;
        }

        if (owner != null && collision.transform.IsChildOf(owner.transform)) return;
        IDamageable target = collision.gameObject.GetComponentInParent<IDamageable>();
        if (target == null || !target.IsAlive) return;
        target.ApplyDamage(new DamageInfo(projectile.damage, direction, owner));
        Destroy(gameObject);
    }
}
