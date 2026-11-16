using UnityEngine;

/// <summary>How the weapon attacks when the player clicks.</summary>
public enum WeaponUseType
{
    Melee,
    Bow,
    Gun,
    Staff
}

/// <summary>Which spawn point on the player a projectile leaves from.</summary>
public enum WeaponSpawnType
{
    None,
    Arrow,
    Bullet,
    Magic
}

/// <summary>Definition of a weapon type (one asset per weapon, shared by every copy of it).</summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponData : InventoryItemData
{
    [Header("Basic Info")]
    [Tooltip("Display name. Falls back to the asset name when empty.")]
    public string weaponName;

    [Header("Visuals")]
    [Tooltip("Model used in the hand, for dropped copies and for the inventory icon.")]
    public GameObject weaponPrefab;

    [Header("Weapon Type")]
    public WeaponUseType useType;

    [Header("Projectile Spawn")]
    public WeaponSpawnType spawnType;

    [Header("Stats")]
    public float damage = 10f;
    [Tooltip("Seconds between shots.")]
    public float cooldown = 0.5f;
    [Tooltip("Ammo in a full weapon. Each shot uses one.")]
    public int maxAmmo = 20;

    [Header("Projectile")]
    [Tooltip("Prefab with a Rigidbody, Projectile and ProjectileHit.")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 30f;

    [Header("Equipped Appearance")]
    [Tooltip("Rotation of the model in the player's hand (also applied to dropped copies).")]
    public Vector3 equippedRotation = Vector3.zero;
    public Vector3 equippedScale = Vector3.one;

    // Weapons keep their own name/prefab fields (existing assets use them) instead of the base item fields.
    public override string DisplayName => string.IsNullOrWhiteSpace(weaponName) ? name : weaponName;
    public override GameObject ModelPrefab => weaponPrefab;
}
