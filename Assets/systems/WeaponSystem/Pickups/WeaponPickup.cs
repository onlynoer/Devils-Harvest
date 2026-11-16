using UnityEngine;

/// <summary>
/// Makes a world object pick-up-able. The object needs a Collider so the player's look raycast can hit it.
/// Assign either field below; <see cref="itemData"/> wins if both are set.
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [Tooltip("Weapon this pickup gives. (Older field, kept so existing scenes keep working.)")]
    public WeaponData weaponData;
    [Tooltip("Any inventory item, including weapons.")]
    public InventoryItemData itemData;

    [Tooltip("Ammo in the weapon. -1 = full. Dropped weapons keep their remaining ammo.")]
    [SerializeField] private int loadedAmmo = -1;

    public InventoryItemData Definition => itemData != null ? itemData : weaponData;
    /// <summary>Ammo handed to the inventory; -1 means full.</summary>
    public int LoadedAmmo => loadedAmmo;

    /// <summary>Configures a pickup at runtime (used when the player drops an item).</summary>
    public void SetItem(InventoryItemData definition, int ammo)
    {
        itemData = definition;
        weaponData = null;
        loadedAmmo = Mathf.Max(0, ammo);
    }
}
