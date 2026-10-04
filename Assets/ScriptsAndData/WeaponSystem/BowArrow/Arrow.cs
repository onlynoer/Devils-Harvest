using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float lifetime = 5f;
    public float damage = 10f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Creature creature = collision.gameObject.GetComponentInParent<Creature>();

        if (creature != null)
        {
            Vector3 damageDirection = Vector3.zero;

            Rigidbody rb = GetComponent<Rigidbody>();

            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                damageDirection = rb.linearVelocity.normalized;
            }

            Projectile projectile = GetComponent<Projectile>();

            float finalDamage = damage;

            if (projectile != null)
            {
                finalDamage = projectile.damage;
            }

            creature.TakeDamage(finalDamage, damageDirection);

            Destroy(gameObject);
        }
    }
}