using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player-side weapon controller: reads input, picks up / drops items, shows the equipped model in the hand
/// and fires projectiles. What the player is carrying lives in <see cref="WeaponInventory"/>.
/// </summary>
[RequireComponent(typeof(WeaponInventory))]
public sealed class WeaponSystem : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Parent for the equipped weapon model.")]
    public Transform hand;
    [Tooltip("Used for aiming and for the pickup raycast.")]
    public Camera playerCamera;

    [Header("Projectile Spawn Points")]
    public Transform arrowSpawn;
    public Transform bulletSpawn;
    public Transform magicSpawn;

    [Header("Pickups")]
    [Tooltip("How far (in meters) the player can look at a pickup and still grab it.")]
    [Min(0f)] public float pickupRange = 4f;
    [Tooltip("Distance in front of the camera where dropped items spawn.")]
    [Min(0f)] [SerializeField] private float dropDistance = 2f;

    private static readonly Key[] SlotKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
    };

    private WeaponInventory inventory;
    private Player player;
    private WeaponPickup targetedPickup;
    private GameObject equippedVisual;
    private WeaponInstance renderedInstance;
    private float nextFireTime;

    public WeaponInventory Inventory
    {
        get
        {
            if (inventory == null) inventory = GetComponent<WeaponInventory>();
            return inventory;
        }
    }

    /// <summary>The selected item's weapon data, or null if the slot is empty or holds a non-weapon item.</summary>
    public WeaponData EquippedWeapon => Inventory.Selected != null ? Inventory.Selected.Definition : null;
    public int LoadedAmmo => Inventory.Selected != null ? Inventory.Selected.LoadedAmmo : 0;

    private void Awake()
    {
        inventory = GetComponent<WeaponInventory>();
        player = GetComponent<Player>();
    }

    private void Update()
    {
        targetedPickup = FindTargetedPickup();
        HandleKeyboard(Keyboard.current);
        HandleMouse(Mouse.current);
        SyncEquippedVisual();
    }

    // ---------------------------------------------------------------- Input

    private void HandleKeyboard(Keyboard keyboard)
    {
        if (keyboard == null) return;
        if (keyboard.eKey.wasPressedThisFrame) TryPickup();
        if (keyboard.qKey.wasPressedThisFrame) DropSelected();

        int slotCount = Mathf.Min(inventory.Capacity, SlotKeys.Length);
        for (int i = 0; i < slotCount; i++)
            if (keyboard[SlotKeys[i]].wasPressedThisFrame) inventory.Select(i);
    }

    private void HandleMouse(Mouse mouse)
    {
        if (mouse == null) return;
        SelectFromScrollWheel(mouse.scroll.ReadValue().y);
        if (mouse.leftButton.wasPressedThisFrame) UseWeapon();
    }

    /// <summary>Scroll up = next slot, scroll down = previous slot, wrapping around the hotbar.</summary>
    private void SelectFromScrollWheel(float scroll)
    {
        int capacity = inventory.Capacity;
        if (capacity <= 0 || Mathf.Approximately(scroll, 0f)) return;

        int step = scroll < 0f ? -1 : 1;
        int current = inventory.SelectedIndex < 0 ? (step > 0 ? -1 : 0) : inventory.SelectedIndex;
        inventory.Select(((current + step) % capacity + capacity) % capacity);
    }

    // ---------------------------------------------------------------- Pickup / drop

    private WeaponPickup FindTargetedPickup()
    {
        if (playerCamera == null) return null;
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange)) return null;
        WeaponPickup pickup = hit.collider.GetComponentInParent<WeaponPickup>();
        return pickup != null && pickup.isActiveAndEnabled && pickup.Definition != null ? pickup : null;
    }

    private void TryPickup()
    {
        if (targetedPickup == null) return;
        if (targetedPickup.Definition.ModelPrefab == null)
        {
            Debug.LogWarning($"Pickup '{targetedPickup.name}' has no model prefab on its item data.", targetedPickup);
            return;
        }
        if (!inventory.TryAdd(targetedPickup.Definition, targetedPickup.LoadedAmmo)) return;
        targetedPickup.gameObject.SetActive(false);
        targetedPickup = null;
    }

    public void DropSelected()
    {
        WeaponInstance item = inventory.Remove(inventory.SelectedIndex);
        if (item == null) return;
        SpawnDropped(item);
        SyncEquippedVisual();
    }

    private void SpawnDropped(WeaponInstance item)
    {
        InventoryItemData definition = item.ItemDefinition;
        if (definition == null || definition.ModelPrefab == null) return;
        WeaponData weapon = item.Definition;

        Transform origin = playerCamera != null ? playerCamera.transform : transform;
        Vector3 position = origin.position + origin.forward * dropDistance;
        Quaternion rotation = Quaternion.LookRotation(origin.forward, Vector3.up);
        if (weapon != null) rotation *= Quaternion.Euler(weapon.equippedRotation);

        GameObject dropped = Instantiate(definition.ModelPrefab, position, rotation);
        dropped.name = definition.DisplayName + " (Dropped)";
        if (weapon != null) dropped.transform.localScale = weapon.equippedScale;

        WeaponPickup pickup = dropped.GetComponent<WeaponPickup>();
        if (pickup == null) pickup = dropped.AddComponent<WeaponPickup>();
        pickup.SetItem(definition, item.LoadedAmmo);

        if (dropped.GetComponentInChildren<Collider>(true) == null)
        {
            Bounds bounds = GetLocalRendererBounds(dropped.transform, dropped.GetComponentsInChildren<Renderer>(true));
            var box = dropped.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }

        Rigidbody body = dropped.GetComponent<Rigidbody>();
        if (body == null) body = dropped.AddComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
    }

    private static Bounds GetLocalRendererBounds(Transform root, Renderer[] renderers)
    {
        var result = new Bounds(Vector3.zero, Vector3.one * .5f);
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            Bounds bounds = renderer.localBounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                Vector3 local = root.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                if (!found) { result = new Bounds(local, Vector3.zero); found = true; }
                else result.Encapsulate(local);
            }
        }
        result.Expand(Vector3.one * .1f);
        return result;
    }

    private void OnGUI()
    {
        if (targetedPickup == null || !targetedPickup.gameObject.activeInHierarchy) return;
        const float width = 160f, height = 21f;
        var rect = new Rect((Screen.width - width) * .5f, Screen.height * .72f, width, height);
        GUI.Box(rect, "Press E to pick up");
    }

    // ---------------------------------------------------------------- Equipped model

    /// <summary>Swaps the model in the hand whenever the selected slot changes.</summary>
    private void SyncEquippedVisual()
    {
        WeaponInstance selected = inventory.Selected;
        if (selected == renderedInstance) return;

        if (equippedVisual != null) Destroy(equippedVisual);
        equippedVisual = null;
        renderedInstance = selected;

        WeaponData weapon = selected != null ? selected.Definition : null;
        if (weapon == null || weapon.weaponPrefab == null || hand == null) return;

        equippedVisual = Instantiate(weapon.weaponPrefab, hand);
        equippedVisual.transform.localPosition = Vector3.zero;
        equippedVisual.transform.localRotation = Quaternion.Euler(weapon.equippedRotation);
        equippedVisual.transform.localScale = weapon.equippedScale;

        // The hand model is visual only: it must not be picked up, collide or simulate physics.
        foreach (WeaponPickup pickup in equippedVisual.GetComponentsInChildren<WeaponPickup>(true)) pickup.enabled = false;
        foreach (Collider collider in equippedVisual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in equippedVisual.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
    }

    // ---------------------------------------------------------------- Attacking

    private void UseWeapon()
    {
        WeaponInstance item = inventory.Selected;
        if (item == null || item.Definition == null) return;
        switch (item.Definition.useType)
        {
            case WeaponUseType.Bow:
            case WeaponUseType.Gun:
            case WeaponUseType.Staff:
                FireProjectile(item);
                break;
            case WeaponUseType.Melee:
                // Not implemented yet.
                break;
        }
    }

    private void FireProjectile(WeaponInstance item)
    {
        WeaponData weapon = item.Definition;
        if (item.LoadedAmmo <= 0 || Time.time < nextFireTime) return;
        Transform spawn = GetSpawnPoint(weapon.spawnType);
        if (weapon.projectilePrefab == null || spawn == null || playerCamera == null) return;

        nextFireTime = Time.time + weapon.cooldown;
        Vector3 direction = GetAimDirection(weapon, spawn);

        GameObject projectile = Instantiate(weapon.projectilePrefab, spawn.position, Quaternion.LookRotation(direction));
        Projectile damage = projectile.GetComponent<Projectile>();
        if (damage != null)
        {
            float multiplier = player != null && player.playerData != null ? player.playerData.damageAmplifier : 1f;
            damage.SetDamage(weapon.damage * multiplier);
        }
        Rigidbody body = projectile.GetComponent<Rigidbody>();
        if (body != null) body.linearVelocity = direction * weapon.projectileSpeed;
        item.LoadedAmmo--;
    }

    /// <summary>
    /// Bows and staffs fire straight along the camera. Guns aim from the muzzle at whatever the crosshair is on,
    /// so bullets hit the reticle even though the muzzle is offset from the camera.
    /// </summary>
    private Vector3 GetAimDirection(WeaponData weapon, Transform spawn)
    {
        Ray aimRay = playerCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        if (weapon.useType != WeaponUseType.Gun) return aimRay.direction;

        const float aimDistance = 1000f;
        Vector3 aimPoint = aimRay.GetPoint(aimDistance);
        if (Physics.Raycast(aimRay, out RaycastHit hit, aimDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;
        Vector3 muzzleToAim = aimPoint - spawn.position;
        return Vector3.Dot(muzzleToAim, aimRay.direction) > 0f ? muzzleToAim.normalized : aimRay.direction;
    }

    private Transform GetSpawnPoint(WeaponSpawnType type)
    {
        switch (type)
        {
            case WeaponSpawnType.Arrow: return arrowSpawn;
            case WeaponSpawnType.Bullet: return bulletSpawn;
            case WeaponSpawnType.Magic: return magicSpawn;
            default: return null;
        }
    }
}
