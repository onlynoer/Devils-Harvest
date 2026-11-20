using System.Collections;
using UnityEngine;

// The trigger round uses the weapon controller's normal projectile. Only the MP5's
// projectile variant schedules two follow-up rounds from that same inventory slot.
[RequireComponent(typeof(Projectile))]
public sealed class Mp5Burst : MonoBehaviour
{
    [SerializeField] private GameObject followUpProjectile;
    private const float RoundInterval = .075f;

    private void Start()
    {
        var player = FindAnyObjectByType<Player>();
        if (player == null || !player.TryGetComponent(out WeaponSystem weapons)) return;
        var slot = weapons.Inventory.Selected;
        if (slot?.Definition == null || weapons.bulletSpawn == null || weapons.playerCamera == null ||
            followUpProjectile == null || !slot.Definition.projectilePrefab.TryGetComponent<Mp5Burst>(out _)) return;

        // Preserve the controller's muzzle-to-reticle convergence and follow the
        // player's view through the burst. The trigger round already spent its ammo.
        var aim = weapons.playerCamera.transform.InverseTransformDirection(transform.forward);
        weapons.StartCoroutine(FinishBurst(player, weapons, slot, followUpProjectile, aim, GetComponent<Projectile>().damage));
    }

    private static IEnumerator FinishBurst(Player player, WeaponSystem weapons, WeaponInstance slot,
        GameObject projectilePrefab, Vector3 localAim, float damage)
    {
        float next = Time.time + RoundInterval;
        for (int round = 0; round < 2; round++)
        {
            while (Time.time < next)
            {
                if (player.IsDead || !weapons.isActiveAndEnabled || weapons.Inventory.Selected != slot) yield break;
                yield return null;
            }
            if (player.IsDead || !weapons.isActiveAndEnabled || weapons.Inventory.Selected != slot || slot.LoadedAmmo == 0) yield break;
            var direction = weapons.playerCamera.transform.TransformDirection(localAim);
            var projectile = Instantiate(projectilePrefab, weapons.bulletSpawn.position, Quaternion.LookRotation(direction));
            projectile.GetComponent<Projectile>().SetDamage(damage);
            projectile.GetComponent<Rigidbody>().linearVelocity = direction * slot.Definition.projectileSpeed;
            slot.LoadedAmmo--;
            next += RoundInterval;
        }
    }
}
