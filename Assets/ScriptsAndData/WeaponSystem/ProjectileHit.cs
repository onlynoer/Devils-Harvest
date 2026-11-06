using UnityEngine;

public class ProjectileHit : MonoBehaviour
{
    public float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Projectile projectile = GetComponent<Projectile>();

        if (projectile == null)
            return;

        Vector3 damageDirection = Vector3.zero;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            damageDirection = rb.linearVelocity.normalized;
        }

        Creature creature = collision.gameObject
            .GetComponentInParent<Creature>();

        if (creature != null)
        {
            creature.TakeDamage(projectile.damage, damageDirection);
            Destroy(gameObject);
        }
    }
}