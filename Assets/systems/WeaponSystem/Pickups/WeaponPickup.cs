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

    private int reserveAmmo = -1;
    public int ReserveAmmo => reserveAmmo;
    public string OriginId { get; private set; }

    internal static string SceneKey(Transform node)
    {
        // Authored siblings can share names. Include their position in the serialized hierarchy.
        string path = "";
        for (var t = node; t != null; t = t.parent)
            path = "/" + t.name + "#" + t.GetSiblingIndex() + path;
        return node.gameObject.scene.path + path;
    }

    private void Awake() => OriginId = SceneKey(transform);

    private InventoryItemData cachedPromptItem;
    private string cachedPrompt;

    public InventoryItemData Definition => itemData != null ? itemData : weaponData;
    /// <summary>Ammo handed to the inventory; -1 means full.</summary>
    public int LoadedAmmo => loadedAmmo;

    /// <summary>Configures a pickup at runtime (used when the player drops an item).</summary>
    public void SetItem(InventoryItemData definition, int ammo, int reserve = -1, string originId = null)
    {
        itemData = definition;
        weaponData = null;
        loadedAmmo = Mathf.Max(0, ammo);
        reserveAmmo = reserve;
        OriginId = originId;
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
        if (!CanInteract(user) || !user.Inventory.TryAdd(Definition, loadedAmmo, reserveAmmo, OriginId)) return;
        gameObject.SetActive(false);
    }
}
