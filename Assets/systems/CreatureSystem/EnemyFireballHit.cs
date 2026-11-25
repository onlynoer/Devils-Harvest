
using UnityEngine;

public sealed class EnemyFireballHit : MonoBehaviour
{
    public float lifetime = 5f;

    private Projectile projectile;

    private bool spent;

    void Awake()
    {
        projectile = GetComponent<Projectile>();
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    void HandleHit(Collider other)
    {
        if (spent || projectile == null || (other.isTrigger && other.GetComponentInParent<Player>() == null))
            return;

        spent = true;
        Vector3 damageDirection = Vector3.zero;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            damageDirection = rb.linearVelocity.normalized;
        }

        Creature creature = other.GetComponentInParent<Creature>();

        if (creature != null)
        {
            creature.TakeDamage(projectile.damage, damageDirection);
        }
        else
        {
            Player player = other.GetComponentInParent<Player>();

            if (player != null)
            {
                player.TakeDamage(projectile.damage);
            }
        }

        Destroy(gameObject);
    }
}
