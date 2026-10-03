using UnityEngine;

public enum WeaponUseType
{
    Melee,
    Bow,
    Gun,
    Staff
}

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;

    [Header("Visuals")]
    public GameObject weaponPrefab;

    [Header("Weapon Type")]
    public WeaponUseType useType;

    [Header("Stats")]
    public float damage = 10f;
    public float cooldown = 0.5f;
    public int maxAmmo = 20;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 30f;
}
