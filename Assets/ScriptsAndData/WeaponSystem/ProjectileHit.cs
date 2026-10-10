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
        Creature creature =
            collision.gameObject.GetComponentInParent<Creature>();

        if (creature != null)
        {
            Projectile projectile = GetComponent<Projectile>();

            if (projectile != null)
            {
                Vector3 damageDirection = Vector3.zero;

                Rigidbody rb = GetComponent<Rigidbody>();

                if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
                {
                    damageDirection = rb.linearVelocity.normalized;
                }

                creature.TakeDamage(projectile.damage, damageDirection);
            }

            Destroy(gameObject);
        }
    }
}