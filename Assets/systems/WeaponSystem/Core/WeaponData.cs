using UnityEngine;
using UnityEngine.Serialization;

/// <summary>How the weapon attacks when the player clicks.</summary>
public enum WeaponUseType
{
    Melee,
    Bow,
    Gun,
    Staff
}

public enum GunSightStyle { Iron, Aperture, Scope }

/// <summary>Which spawn point on the player a projectile leaves from.</summary>
public enum WeaponSpawnType
{
    None,
    Arrow,
    Bullet,
    Magic
}

/// <summary>
/// Definition of a weapon type (one asset per weapon, shared by every copy of it).
/// Name, model prefab and icon are the shared Item fields at the top of the inspector.
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponData : InventoryItemData
{
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

    [Header("Gun handling")]
    [Min(1)] public int magazineSize = 30;
    [Min(.1f)] public float reloadDuration = 2f;
    public int Capacity => useType == WeaponUseType.Gun ? Mathf.Clamp(magazineSize, 1, Mathf.Max(1, maxAmmo)) : maxAmmo;
    public GunSightStyle sightStyle;
    [Tooltip("Top of the physical sight in the equipped model's local coordinates.")]
    public Vector3 sightReference;
    [Range(.15f, 1f)] public float aimFieldOfViewScale = .78f;
    [Range(.1f, 5f)] public float recoilDegrees = 1.2f;
    public bool automatic;

    [Header("Projectile")]
    [Tooltip("Prefab with a Rigidbody, Projectile and ProjectileHit.")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 30f;

    [Header("Melee (Use Type = Melee)")]
    [Tooltip("How far in front of the camera the swing reaches, in meters.")]
    [Min(0f)] public float meleeRange = 2.2f;
    [Tooltip("Width of the swing hit area. Bigger = more forgiving aim.")]
    [Min(0f)] public float meleeRadius = .5f;
    [Tooltip("Length of the whole swing animation, in seconds. The next swing can't start until it ends.")]
    [Min(.05f)] public float swingDuration = .45f;
    [Tooltip("When the hit lands, as a fraction of the swing (0 = start, 1 = end). Mid-slash is about 0.4.")]
    [Range(0f, 1f)] public float swingHitTime = .4f;
    [Tooltip("Horizontal arc of the slash, in degrees.")]
    [Range(0f, 180f)] public float swingAngle = 100f;
    [Tooltip("Diagonal tilt of the slash, in degrees. 0 = flat horizontal swing.")]
    [Range(-90f, 90f)] public float swingTilt = 25f;
    [Tooltip("How far the model lunges forward mid-slash, in meters.")]
    [Min(0f)] public float swingReach = .15f;

    [Header("Sounds (play through the SFX volume slider)")]
    [Tooltip("When the weapon is selected on the hotbar.")]
    public SoundEffect equipSound = new SoundEffect();
    [Tooltip("Each attack: gunshot, bow release, staff cast, sword swing.")]
    public SoundEffect useSound = new SoundEffect();
    [Tooltip("Melee only: when the swing hits a creature.")]
    public SoundEffect hitSound = new SoundEffect();
    [Tooltip("Attacking with no ammo left (dry fire click).")]
    public SoundEffect emptySound = new SoundEffect();
    [Tooltip("When the weapon is refilled from an ammo box.")]
    public SoundEffect reloadSound = new SoundEffect();

    [Header("Equipped Appearance")]
    [Tooltip("Offset of the model from the Hand, in the Hand's local space (meters). Updates live in Play Mode.")]
    public Vector3 equippedPosition = Vector3.zero;
    [Tooltip("Rotation of the model in the player's hand, in degrees (also applied to dropped copies). Updates live in Play Mode.")]
    public Vector3 equippedRotation = Vector3.zero;
    [Tooltip("Scale of the model in the hand and of dropped copies. Updates live in Play Mode.")]
    public Vector3 equippedScale = Vector3.one;

    // Old weapon-only name/prefab fields. Their values move into the Item fields when the asset is loaded in
    // the editor (OnValidate); until that asset is saved, ModelPrefab still falls back to the old prefab.
    [SerializeField, HideInInspector, FormerlySerializedAs("weaponName")] private string legacyWeaponName;
    [SerializeField, HideInInspector, FormerlySerializedAs("weaponPrefab")] private GameObject legacyWeaponPrefab;

    public override GameObject ModelPrefab
    {
        get
        {
            GameObject prefab = base.ModelPrefab;
            return prefab != null ? prefab : legacyWeaponPrefab;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bool hadLegacy = !string.IsNullOrEmpty(legacyWeaponName) || legacyWeaponPrefab != null;
        if (!hadLegacy) return;
        AdoptLegacyValues(legacyWeaponName, legacyWeaponPrefab);
        legacyWeaponName = null;
        legacyWeaponPrefab = null;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
