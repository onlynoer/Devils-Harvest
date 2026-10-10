using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSystem : MonoBehaviour
{
    public Transform hand;
    public Camera playerCamera;
    public Transform arrowSpawn;
    public Transform bulletSpawn;
    public Transform magicSpawn;

    private WeaponData currentWeapon;
    private GameObject currentWeaponObject;
    private Player player;

    private int currentAmmo;
    private float nextFireTime;

    void Start()
    {
        player = GetComponent<Player>();
    }
    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickup();
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            UseWeapon();
        }
    }

    void TryPickup()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            WeaponPickup pickup = hit.collider.GetComponentInParent<WeaponPickup>();

            if (pickup != null)
            {
                EquipWeapon(pickup.weaponData);

                pickup.gameObject.SetActive(false);
            }
        }
    }

    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            Debug.LogWarning("No weapon data provided.");
            return;
        }

        if (weaponData.weaponPrefab == null)
        {
            Debug.LogWarning("Weapon prefab is missing!");
            return;
        }

        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }

        currentWeapon = weaponData;
        currentAmmo = currentWeapon.maxAmmo;

        currentWeaponObject = Instantiate(
            currentWeapon.weaponPrefab,
            hand
        );

        currentWeaponObject.transform.localPosition = Vector3.zero;
        currentWeaponObject.transform.localRotation = Quaternion.identity;
        currentWeaponObject.transform.localScale = currentWeapon.equippedScale;
        Debug.Log("Equipped: " + currentWeapon.weaponName);
        Debug.Log("Ammo: " + currentAmmo + "/" + currentWeapon.maxAmmo);
    }

    void UseWeapon()
    {
        if (currentWeapon == null)
        {
            return;
        }

        switch (currentWeapon.useType)
        {
            case WeaponUseType.Bow:
            case WeaponUseType.Gun:
            case WeaponUseType.Staff:
                FireProjectile();
                break;

            case WeaponUseType.Melee:
                // Melee attacks will be implemented separately.
                break;
        }
    }

    void FireProjectile(){
    if (currentAmmo <= 0)
    {
        Debug.Log("Out of ammo!");
        return;
    }

    if (Time.time < nextFireTime)
    {
        return;
    }

    if (currentWeapon.projectilePrefab == null)
    {
        Debug.LogWarning("Projectile prefab is missing!");
        return;
    }

    nextFireTime = Time.time + currentWeapon.cooldown;

    Ray aimRay = playerCamera.ViewportPointToRay(
        new Vector3(0.5f, 0.5f, 0f)
    );

    Transform spawnPoint = GetSpawnPoint();

    if (spawnPoint == null)
    {
        Debug.LogWarning("Projectile spawn point is missing!");
        return;
    }

    GameObject projectile = Instantiate(
        currentWeapon.projectilePrefab,
        spawnPoint.position,
        Quaternion.LookRotation(aimRay.direction)
    );

    Projectile projectileDamage = projectile.GetComponent<Projectile>();
    
    if (projectileDamage != null && player != null)
    {
        float finalDamage =
            currentWeapon.damage *
            player.playerData.damageAmplifier;

        projectileDamage.SetDamage(finalDamage);
    }


    Rigidbody rb = projectile.GetComponent<Rigidbody>();

    if (rb != null)
    {
        rb.linearVelocity =
            aimRay.direction * currentWeapon.projectileSpeed;
    }

        currentAmmo--;

        Debug.Log("Ammo: " + currentAmmo + "/" + currentWeapon.maxAmmo);
    }
    Transform GetSpawnPoint()
    {
        switch (currentWeapon.spawnType)
        {
            case WeaponSpawnType.Arrow:
                return arrowSpawn;

            case WeaponSpawnType.Bullet:
                return bulletSpawn;

            case WeaponSpawnType.Magic:
                return magicSpawn;

            default:
                return null;
        }
    }
}