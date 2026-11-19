using UnityEngine;

/// <summary>
/// Makes a world object pick-up-able. The object needs a Collider so the player's look raycast can hit it.
/// Assign either field below; <see cref="itemData"/> wins if both are set.
/// </summary>
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [Tooltip("Weapon this pickup gives. (Older field, kept so existing scenes keep working.)")]
    public WeaponData weaponData;
    [Tooltip("Any inventory item, including weapons.")]
    public InventoryItemData itemData;

    [Tooltip("Ammo in the weapon. -1 = full. Dropped weapons keep their remaining ammo.")]
    [SerializeField] private int loadedAmmo = -1;

    private InventoryItemData cachedPromptItem;
    private string cachedPrompt;

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

    public string GetPrompt(WeaponSystem user)
    {
        InventoryItemData definition = Definition;
        if (definition == null) return null;
        if (definition.ModelPrefab == null) return definition.DisplayName + " has no model prefab";
        if (!user.Inventory.HasFreeSlot) return "Inventory full";
        if (cachedPromptItem != definition)
        {
            cachedPromptItem = definition;
            cachedPrompt = "pick up " + definition.DisplayName;
        }
        return cachedPrompt;
    }

    public bool CanInteract(WeaponSystem user)
    {
        InventoryItemData definition = Definition;
        return definition != null && definition.ModelPrefab != null && user.Inventory.HasFreeSlot;
    }

    public void Interact(WeaponSystem user)
    {
        if (!CanInteract(user) || !user.Inventory.TryAdd(Definition, loadedAmmo)) return;
        gameObject.SetActive(false);
    }
}
