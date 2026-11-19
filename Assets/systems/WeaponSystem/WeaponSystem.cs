using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSystem : MonoBehaviour
{
    public Transform hand;
    public Camera playerCamera;
    public Transform arrowSpawn;
    public Transform bulletSpawn;
    public Transform magicSpawn;
    public float pickupRange = 4f;

    private WeaponData currentWeapon;
    private GameObject currentWeaponObject;
    private WeaponPickup targetedPickup;
    private Player player;
    private HarvestTrade trade;
    private DealerWeaponSave ownedSlot;
    private int burstRemaining;
    private float nextBurstShot;
    private int currentAmmo;
    private float nextFireTime;

    // Read-only weapon state for the HUD. There is currently no reserve-ammo or reload system.
    public WeaponData EquippedWeapon => currentWeapon;
    public int LoadedAmmo => currentAmmo;

    void Start()
    {
        player = GetComponent<Player>();
        trade = GetComponent<HarvestTrade>();
    }

    void Update()
    {
        Collider target = null;
        if (playerCamera != null && Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
            out var hit, pickupRange))
            target = hit.collider;
        bool interact = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        if (trade != null && trade.HandleInput(target, interact))
        {
            targetedPickup = null;
            CancelBurst();
            return;
        }
        if (burstRemaining > 0 && Time.time >= nextBurstShot)
        {
            FireProjectile();
            burstRemaining--;
            nextBurstShot = Time.time + .075f;
        }
        targetedPickup = FindTargetedPickup(target);

        if (interact)
        {
            TryPickup();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            UseWeapon();
        }
    }

    void TryPickup()
    {
        if (targetedPickup == null || !targetedPickup.gameObject.activeInHierarchy)
        {
            return;
        }

        if (targetedPickup.weaponData == null)
        {
            Debug.LogWarning("Weapon pickup has no WeaponData assigned.");
            return;
        }

        if (targetedPickup.weaponData.weaponPrefab == null)
        {
            Debug.LogWarning("Weapon pickup data has no weapon prefab assigned.");
            return;
        }

        WeaponPickup pickup = targetedPickup;
        targetedPickup = null;
        EquipWeapon(pickup.weaponData);
        pickup.gameObject.SetActive(false);
    }

    WeaponPickup FindTargetedPickup(Collider target)
    {
        if (target == null) return null;
        WeaponPickup pickup = target.GetComponentInParent<WeaponPickup>();
        if (pickup == null || !pickup.isActiveAndEnabled || pickup.weaponData == null ||
            pickup.weaponData.weaponPrefab == null)
        {
            return null;
        }

        return pickup;
    }

    void OnGUI()
    {
        if (trade != null && trade.IsOpen) return;
        string prompt = trade != null ? trade.Prompt : null;
        if (prompt == null && targetedPickup != null && targetedPickup.gameObject.activeInHierarchy && targetedPickup.weaponData != null)
            prompt = "Press E to equip " + targetedPickup.weaponData.weaponName;
        if (prompt == null) return;

        float width = Mathf.Min(Screen.width, 440f);
        const float height = 60f;
        Rect tooltipRect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.72f, width, height);
        GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Screen.height / 35, 16, 24),
            fontStyle = FontStyle.Bold
        };
        tooltipStyle.normal.textColor = Color.white;
        GUI.Box(tooltipRect, prompt, tooltipStyle);
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

        if (hand == null)
        {
            Debug.LogWarning("WeaponSystem hand transform is missing.");
            return;
        }

        // A paid weapon stays in its saved slot. Only ordinary pickups fall back into the world.
        if (trade == null) trade = GetComponent<HarvestTrade>();
        var newSlot = trade != null ? trade.Owned(weaponData) : null;
        if (!string.IsNullOrEmpty(weaponData.dealerId) && newSlot == null) return;
        CancelBurst();
        if (ownedSlot != null)
            Destroy(currentWeaponObject);
        else if (currentWeapon != null)
            DropCurrentWeapon();
        ownedSlot = newSlot;

        currentWeapon = weaponData;
        currentAmmo = ownedSlot != null ? ownedSlot.ammo : currentWeapon.maxAmmo;
        if (trade != null) trade.RememberEquipped(weaponData);
        currentWeaponObject = Instantiate(currentWeapon.weaponPrefab, hand);
        currentWeaponObject.transform.localPosition = Vector3.zero;
        currentWeaponObject.transform.localRotation = Quaternion.Euler(currentWeapon.equippedRotation);
        currentWeaponObject.transform.localScale = currentWeapon.equippedScale;

        // An equipped weapon is visual only; it must not block pickup raycasts.
        WeaponPickup[] equippedPickups = currentWeaponObject.GetComponentsInChildren<WeaponPickup>(true);
        for (int i = 0; i < equippedPickups.Length; i++)
        {
            equippedPickups[i].enabled = false;
        }

        Collider[] equippedColliders = currentWeaponObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < equippedColliders.Length; i++)
        {
            equippedColliders[i].enabled = false;
        }

        Rigidbody[] equippedBodies = currentWeaponObject.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < equippedBodies.Length; i++)
        {
            equippedBodies[i].isKinematic = true;
        }

        Debug.Log("Equipped: " + currentWeapon.weaponName);
        Debug.Log("Ammo: " + currentAmmo + "/" + currentWeapon.maxAmmo);
    }

    void DropCurrentWeapon()
    {
        if (currentWeapon == null || currentWeapon.weaponPrefab == null)
        {
            return;
        }

        Transform dropOrigin = playerCamera != null ? playerCamera.transform : transform;
        Vector3 dropPosition = dropOrigin.position + dropOrigin.forward * 2f;
        Quaternion dropRotation = Quaternion.LookRotation(dropOrigin.forward, Vector3.up)
            * Quaternion.Euler(currentWeapon.equippedRotation);
        GameObject droppedWeapon = Instantiate(
            currentWeapon.weaponPrefab,
            dropPosition,
            dropRotation
        );
        droppedWeapon.name = currentWeapon.weaponName + " (Dropped)";
        droppedWeapon.transform.localScale = currentWeapon.equippedScale;

        WeaponPickup pickup = droppedWeapon.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            pickup = droppedWeapon.AddComponent<WeaponPickup>();
        }
        pickup.weaponData = currentWeapon;

        Collider[] colliders = droppedWeapon.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            Renderer[] renderers = droppedWeapon.GetComponentsInChildren<Renderer>(true);
            BoxCollider box = droppedWeapon.AddComponent<BoxCollider>();
            Bounds localBounds = GetLocalRendererBounds(droppedWeapon.transform, renderers);
            box.center = localBounds.center;
            box.size = localBounds.size;
        }

        Rigidbody body = droppedWeapon.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = droppedWeapon.AddComponent<Rigidbody>();
        }
        body.isKinematic = false;
        body.useGravity = true;

        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
            currentWeaponObject = null;
        }

        currentWeapon = null;
        currentAmmo = 0;
    }

    Bounds GetLocalRendererBounds(Transform root, Renderer[] renderers)
    {
        Bounds result = new Bounds(Vector3.zero, Vector3.one * 0.5f);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Bounds rendererBounds = renderer.localBounds;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 sign = new Vector3(
                            x == 0 ? -1f : 1f,
                            y == 0 ? -1f : 1f,
                            z == 0 ? -1f : 1f
                        );
                        Vector3 rendererLocalPoint = rendererBounds.center + Vector3.Scale(rendererBounds.extents, sign);
                        Vector3 rootLocalPoint = root.InverseTransformPoint(
                            renderer.transform.TransformPoint(rendererLocalPoint)
                        );

                        if (!hasBounds)
                        {
                            result = new Bounds(rootLocalPoint, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            result.Encapsulate(rootLocalPoint);
                        }
                    }
                }
            }
        }

        result.Expand(new Vector3(0.1f, 0.1f, 0.1f));
        return result;
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
                if (Time.time >= nextFireTime && burstRemaining == 0)
                {
                    int before = currentAmmo;
                    FireProjectile();
                    if (before != currentAmmo)
                    {
                        nextFireTime = Time.time + currentWeapon.cooldown;
                        burstRemaining = Mathf.Min(currentAmmo, Mathf.Max(0, currentWeapon.shotsPerTrigger - 1));
                        nextBurstShot = Time.time + .075f;
                    }
                }
                break;

            case WeaponUseType.Melee:
                // Melee attacks will be implemented separately.
                break;
        }
    }

    void FireProjectile()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }


        if (currentWeapon.projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab is missing!");
            return;
        }

        Transform spawnPoint = GetSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("Projectile spawn point is missing!");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("WeaponSystem player camera is missing.");
            return;
        }

        Ray aimRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 fireDirection = aimRay.direction;
        if (currentWeapon.useType == WeaponUseType.Gun)
        {
            // Converge the offset muzzle on the viewport aim point, rather than firing parallel to it.
            const float aimDistance = 1000f;
            Vector3 aimPoint = aimRay.GetPoint(aimDistance);
            if (Physics.Raycast(aimRay, out RaycastHit aimHit, aimDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                aimPoint = aimHit.point;
            }

            Vector3 muzzleToAimPoint = aimPoint - spawnPoint.position;
            if (Vector3.Dot(muzzleToAimPoint, aimRay.direction) > 0f)
            {
                fireDirection = muzzleToAimPoint.normalized;
            }
        }
        GameObject projectile = Instantiate(
            currentWeapon.projectilePrefab,
            spawnPoint.position,
            Quaternion.LookRotation(fireDirection)
        );

        Projectile projectileDamage = projectile.GetComponent<Projectile>();
        if (projectileDamage != null && player != null && player.playerData != null)
        {
            float finalDamage = currentWeapon.damage * player.playerData.damageAmplifier;
            projectileDamage.SetDamage(finalDamage);
        }

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (currentWeapon.useType == WeaponUseType.Gun) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = fireDirection * currentWeapon.projectileSpeed;
        }

        currentAmmo--;
        if (ownedSlot != null) ownedSlot.ammo = currentAmmo;
        Debug.Log("Ammo: " + currentAmmo + "/" + currentWeapon.maxAmmo);
    }

    public void CancelBurst() => burstRemaining = 0;

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
