using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage;

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}