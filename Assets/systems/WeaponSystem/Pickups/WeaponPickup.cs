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

    /// <summary>
    /// Spawns a physical, pick-up-able copy of <paramref name="definition"/> in the world (player drops, creature
    /// loot). Adds a collider from the renderer bounds if the model has none. Returns null if it has no model.
    /// </summary>
    public static WeaponPickup SpawnWorldPickup(InventoryItemData definition, int ammo, Vector3 position, Quaternion rotation)
    {
        if (definition == null || definition.ModelPrefab == null) return null;

        GameObject dropped = Instantiate(definition.ModelPrefab, position, rotation);
        dropped.name = definition.DisplayName + " (Dropped)";
        if (definition is WeaponData weapon) dropped.transform.localScale = weapon.equippedScale;

        WeaponPickup pickup = dropped.GetComponent<WeaponPickup>();
        if (pickup == null) pickup = dropped.AddComponent<WeaponPickup>();
        pickup.enabled = true;
        pickup.SetItem(definition, ammo);

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
        return pickup;
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
