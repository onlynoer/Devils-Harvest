using UnityEngine;

/// <summary>
/// Added at spawn by <see cref="Creature"/> when a secret with a weapon is rolled. Shows the weapon model on the
/// creature, fires its real projectile at the target in aimed bursts, and drops it as a pickup on death.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArmedCreature : MonoBehaviour, ICreatureAttack
{
    private CreatureSecret secret;
    private WeaponData weapon;
    private GameObject model;
    private Transform muzzle;
    private AudioSource voice;
    private Collider[] ownColliders;

    private bool running;
    private float readyAt;
    private float nextShotAt;
    private int shotsLeft;
    private bool warnedNoProjectile;

    public WeaponData Weapon => weapon;
    public float MinRange => secret != null ? secret.minRange : 0f;
    public float MaxRange => secret != null ? secret.maxRange : 0f;
    public bool IsReady => !running && Time.time >= readyAt && weapon != null;
    public bool IsRunning => running;
    public bool Lunging => false;

    public void Setup(CreatureSecret rolledSecret)
    {
        secret = rolledSecret;
        weapon = rolledSecret.weapon;
        ownColliders = GetComponentsInChildren<Collider>(true);
        if (weapon == null || weapon.ModelPrefab == null) return;

        Transform mount = FindChild(transform, secret.mountBone);
        if (mount == null)
        {
            if (!string.IsNullOrEmpty(secret.mountBone))
                Debug.LogWarning(name + ": secret mount bone '" + secret.mountBone + "' not found, using the root.", this);
            mount = transform;
        }

        model = Instantiate(weapon.ModelPrefab, mount);
        model.name = weapon.DisplayName + " (Creature)";
        model.transform.localPosition = secret.mountOffset;
        model.transform.localRotation = Quaternion.Euler(secret.mountRotation);
        model.transform.localScale = weapon.equippedScale * secret.mountScale;

        // Same treatment as the player's hand model: visual only.
        foreach (WeaponPickup pickup in model.GetComponentsInChildren<WeaponPickup>(true)) pickup.enabled = false;
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;

        // Muzzle: the front of the model along the creature's facing.
        var bounds = new Bounds(model.transform.position, Vector3.zero);
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
        Vector3 forward = transform.forward;
        float reach = Mathf.Abs(forward.x) * bounds.extents.x + Mathf.Abs(forward.y) * bounds.extents.y + Mathf.Abs(forward.z) * bounds.extents.z;
        muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(model.transform, false);
        muzzle.position = bounds.center + forward * reach;

        voice = model.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 1f;
        voice.rolloffMode = AudioRolloffMode.Linear;
        voice.minDistance = 2f;
        voice.maxDistance = 60f;
        SettingsManager.Route(voice, AudioChannel.SFX);
    }

    public void Begin(Transform target)
    {
        running = true;
        shotsLeft = secret.burstCount;
        nextShotAt = Time.time + secret.aimTime;
    }

    public void Tick(Transform target)
    {
        if (target == null) { Cancel(); return; }
        if (Time.time < nextShotAt) return;

        if (shotsLeft > 0)
        {
            Fire(target);
            shotsLeft--;
            nextShotAt = Time.time + Mathf.Max(weapon.cooldown, .05f) * secret.shotIntervalMultiplier;
            return;
        }

        running = false;
        readyAt = Time.time + Random.Range(secret.burstCooldown.x, Mathf.Max(secret.burstCooldown.x, secret.burstCooldown.y));
    }

    public void Cancel()
    {
        if (!running) return;
        running = false;
        readyAt = Time.time + secret.burstCooldown.x;
    }

    private void Fire(Transform target)
    {
        if (weapon.projectilePrefab == null)
        {
            if (!warnedNoProjectile) Debug.LogWarning(weapon.DisplayName + " has no projectile prefab; " + name + " can't shoot.", this);
            warnedNoProjectile = true;
            return;
        }

        Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up;
        Vector3 aim = CreatureAI.AimPoint(target) - origin;
        Vector2 error = Random.insideUnitCircle * secret.spreadDegrees;
        Quaternion rotation = Quaternion.LookRotation(aim) * Quaternion.Euler(error.y, error.x, 0f);

        GameObject shot = Instantiate(weapon.projectilePrefab, origin, rotation);
        if (shot.TryGetComponent(out Projectile projectile)) projectile.SetDamage(weapon.damage * secret.damageMultiplier);
        if (!shot.TryGetComponent(out ProjectileHit hit)) hit = shot.AddComponent<ProjectileHit>();
        hit.SetOwner(gameObject, ownColliders);
        if (shot.TryGetComponent(out Rigidbody body)) body.linearVelocity = rotation * Vector3.forward * weapon.projectileSpeed;

        SoundEffect sound = weapon.useSound;
        if (voice != null && sound != null && sound.HasClip)
        {
            voice.pitch = 1f + Random.Range(-sound.pitchVariation, sound.pitchVariation);
            voice.PlayOneShot(sound.PickClip(), sound.volume);
        }
    }

    /// <summary>Called by Creature on death.</summary>
    public void DropWeapon()
    {
        if (secret == null || !secret.dropWeaponOnDeath || weapon == null || model == null) return;
        Vector3 position = model.transform.position + Vector3.up * .3f;
        Quaternion rotation = Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(weapon.equippedRotation);
        WeaponPickup.SpawnWorldPickup(weapon, Random.Range(0, weapon.maxAmmo + 1), position, rotation);
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (string.IsNullOrEmpty(childName)) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == childName) return child;
        return null;
    }
}
