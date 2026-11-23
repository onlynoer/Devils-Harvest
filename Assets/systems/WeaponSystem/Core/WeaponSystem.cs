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

    [Header("Interaction")]
    [Tooltip("How far (in meters) the player can look at a pickup or other interactable and still use it.")]
    [Min(0f)] public float pickupRange = 4f;
    [Tooltip("Distance in front of the camera where dropped items spawn.")]
    [Min(0f)] [SerializeField] private float dropDistance = 2f;

    [Header("Unarmed")]
    [Tooltip("Fists shown and used when the selected slot holds no weapon. Optional.")]
    [SerializeField] private FistsController fists;


    private static readonly Collider[] MeleeHits = new Collider[32];
    private readonly MeleeSwing swing = new MeleeSwing();
    private readonly System.Collections.Generic.HashSet<Creature> meleeVictims = new System.Collections.Generic.HashSet<Creature>();
    private WeaponData swingingWeapon;

    private WeaponInventory inventory;
    private WeaponAudio weaponAudio;
    private Player player;
    private IInteractable targetedInteractable;
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
    /// <summary>What the player is looking at and can press E on, or null. Read by InteractPromptHud.</summary>
    public IInteractable CurrentInteractable => targetedInteractable;

    /// <summary>Plays weapon sounds through the SFX volume. Also used by fists and ammo boxes.</summary>
    public WeaponAudio Audio
    {
        get
        {
            if (weaponAudio == null) weaponAudio = GetComponent<WeaponAudio>();
            if (weaponAudio == null) weaponAudio = gameObject.AddComponent<WeaponAudio>();
            return weaponAudio;
        }
    }

    private void Awake()
    {
        inventory = GetComponent<WeaponInventory>();
        player = GetComponent<Player>();
        weaponAudio = Audio;
    }

    private void Update()
    {
        targetedInteractable = FindTargetedInteractable();
        HandleInput();
        SyncEquippedVisual();
        ApplyEquippedPose();
        if (swing.Tick()) ResolveMeleeHit(swingingWeapon);
    }

    private PlayerPowerups powerups;

    /// <summary>Player's damage amplifier times any Damage powerup.</summary>
    private float DamageMultiplier
    {
        get
        {
            float amplifier = player != null && player.playerData != null ? player.playerData.damageAmplifier : 1f;
            if (powerups == null) powerups = PlayerPowerups.For(gameObject);
            return amplifier * powerups.GetMultiplier(PowerupStat.Damage);
        }
    }

    // ---------------------------------------------------------------- Input

    /// <summary>Keys come from the Controls settings (GameInput); nothing here fires while the game is paused.</summary>
    private void HandleInput()
    {
        if (!GameInput.GameplayEnabled) return;
        if (GameInput.WasPressed(GameAction.Interact)) Interact();
        if (GameInput.WasPressed(GameAction.Drop)) DropSelected();

        int slotCount = Mathf.Min(inventory.Capacity, GameInput.HotbarActions.Length);
        for (int i = 0; i < slotCount; i++)
            if (GameInput.WasPressed(GameInput.HotbarActions[i])) inventory.Select(i);

        if (Mouse.current != null) SelectFromScrollWheel(Mouse.current.scroll.ReadValue().y);
        if (GameInput.WasPressed(GameAction.Attack)) UseWeapon();
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

    // ---------------------------------------------------------------- Interaction / drop

    private IInteractable FindTargetedInteractable()
    {
        if (playerCamera == null) return null;
        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange)) return null;
        IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
        return target is Behaviour behaviour && behaviour.isActiveAndEnabled ? target : null;
    }

    private void Interact()
    {
        if (targetedInteractable == null || !targetedInteractable.CanInteract(this)) return;
        targetedInteractable.Interact(this);
        targetedInteractable = null;
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

        WeaponPickup.SpawnWorldPickup(definition, item.LoadedAmmo, position, rotation);
    }

    // ---------------------------------------------------------------- Equipped model

    /// <summary>Swaps the model in the hand whenever the selected slot changes.</summary>
    private void SyncEquippedVisual()
    {
        WeaponInstance selected = inventory.Selected;
        if (selected == renderedInstance) return;

        swing.Cancel();
        if (equippedVisual != null) Destroy(equippedVisual);
        equippedVisual = null;
        renderedInstance = selected;

        WeaponData weapon = selected != null ? selected.Definition : null;
        if (weapon == null || weapon.ModelPrefab == null || hand == null) return;

        equippedVisual = Instantiate(weapon.ModelPrefab, hand);
        ApplyEquippedPose();
        Audio.Play(weapon.equipSound);

        // The hand model is visual only: it must not be picked up, collide or simulate physics.
        foreach (WeaponPickup pickup in equippedVisual.GetComponentsInChildren<WeaponPickup>(true)) pickup.enabled = false;
        foreach (Collider collider in equippedVisual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in equippedVisual.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
    }

    /// <summary>
    /// Puts the hand model at its WeaponData pose. Runs every frame (except mid-swing, which animates from this
    /// pose), so editing Equipped Position / Rotation / Scale in Play Mode shows up immediately.
    /// </summary>
    private void ApplyEquippedPose()
    {
        if (equippedVisual == null || swing.IsSwinging) return;
        WeaponData weapon = renderedInstance != null ? renderedInstance.Definition : null;
        if (weapon == null) return;
        Transform model = equippedVisual.transform;
        model.localPosition = weapon.equippedPosition;
        model.localRotation = Quaternion.Euler(weapon.equippedRotation);
        model.localScale = weapon.equippedScale;
    }

    // ---------------------------------------------------------------- Attacking

    private void UseWeapon()
    {
        WeaponInstance item = inventory.Selected;
        if (item == null || item.Definition == null)
        {
            if (fists != null) fists.TryPunch();
            return;
        }
        switch (item.Definition.useType)
        {
            case WeaponUseType.Bow:
            case WeaponUseType.Gun:
            case WeaponUseType.Staff:
                FireProjectile(item);
                break;
            case WeaponUseType.Melee:
                StartMeleeSwing(item.Definition);
                break;
        }
    }

    private void StartMeleeSwing(WeaponData weapon)
    {
        if (Time.time < nextFireTime || swing.IsSwinging) return;
        nextFireTime = Time.time + Mathf.Max(weapon.cooldown, weapon.swingDuration);
        swingingWeapon = weapon;
        Audio.Play(weapon.useSound);
        // Without a hand model the swing has nothing to animate, but the hit still lands at the hit time.
        if (equippedVisual != null) swing.Start(equippedVisual.transform, weapon);
        else ResolveMeleeHit(weapon);
    }

    private void ResolveMeleeHit(WeaponData weapon)
    {
        if (weapon != null) DealMeleeDamage(weapon.meleeRange, weapon.meleeRadius, weapon.damage, weapon.hitSound);
    }

    /// <summary>
    /// Damages every Creature inside a capsule from the camera out to <paramref name="range"/>, once each.
    /// Base damage is multiplied by the player's damage amplifier. Used by melee weapons and fists.
    /// If anything was hit, <paramref name="hitSound"/> plays where the first hit landed. Returns how many were hit.
    /// </summary>
    public int DealMeleeDamage(float range, float radius, float baseDamage, SoundEffect hitSound = null)
    {
        if (playerCamera == null) return 0;
        Transform view = playerCamera.transform;
        Vector3 start = view.position;
        Vector3 end = start + view.forward * range;
        int count = Physics.OverlapCapsuleNonAlloc(start, end, radius, MeleeHits,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        meleeVictims.Clear();
        float damage = baseDamage * DamageMultiplier;
        for (int i = 0; i < count; i++)
        {
            Creature creature = MeleeHits[i].GetComponentInParent<Creature>();
            if (creature == null || creature.transform.IsChildOf(transform) || !meleeVictims.Add(creature)) continue;
            if (meleeVictims.Count == 1) Audio.PlayAt(hitSound, MeleeHits[i].ClosestPoint(start));
            creature.TakeDamage(damage, view.forward);
        }
        return meleeVictims.Count;
    }

    private void FireProjectile(WeaponInstance item)
    {
        WeaponData weapon = item.Definition;
        if (Time.time < nextFireTime) return;
        if (item.LoadedAmmo <= 0)
        {
            Audio.Play(weapon.emptySound);
            nextFireTime = Time.time + weapon.cooldown; // so holding the clicks doesn't stack dry-fire sounds
            return;
        }
        Transform spawn = GetSpawnPoint(weapon.spawnType);
        if (weapon.projectilePrefab == null || spawn == null || playerCamera == null) return;

        nextFireTime = Time.time + weapon.cooldown;
        Vector3 direction = GetAimDirection(weapon, spawn);

        GameObject projectile = Instantiate(weapon.projectilePrefab, spawn.position, Quaternion.LookRotation(direction));
        Projectile damage = projectile.GetComponent<Projectile>();
        if (damage != null) damage.SetDamage(weapon.damage * DamageMultiplier);
        Rigidbody body = projectile.GetComponent<Rigidbody>();
        if (body != null) body.linearVelocity = direction * weapon.projectileSpeed;
        item.LoadedAmmo--;
        Audio.Play(weapon.useSound);
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
