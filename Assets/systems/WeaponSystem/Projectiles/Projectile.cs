using UnityEngine;

/// <summary>Holds a projectile's damage. Set by WeaponSystem when fired (weapon damage x player damage amplifier).</summary>
public class Projectile : MonoBehaviour
{
    public float damage;

    public void SetDamage(float newDamage) => damage = newDamage;
}
