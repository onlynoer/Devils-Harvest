using UnityEngine;

/// <summary>Shared definition for an inventory item. WeaponData extends this for weapons.</summary>
[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Inventory/Item Data")]
public class InventoryItemData : ScriptableObject
{
    [Header("Item")]
    [Tooltip("Display name. Falls back to the asset name when empty.")]
    [SerializeField] private string itemName;
    [Tooltip("Model used in the hand, for dropped copies and for the inventory icon.")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Inventory Icon")]
    [Tooltip("Icon shown in the inventory HUD. Leave empty to render the model prefab automatically at runtime. " +
             "Right-click this asset's component header > 'Bake Icon From Prefab' to generate an editable PNG.")]
    [SerializeField] private Sprite icon;
    [Tooltip("Model rotation used when rendering or baking the icon from the prefab.")]
    [SerializeField] private Vector3 iconRotation = new Vector3(12f, -32f, 0f);

    public string DisplayName => string.IsNullOrWhiteSpace(itemName) ? name : itemName;
    public virtual GameObject ModelPrefab => itemPrefab;
    public Sprite Icon => icon;
    public Vector3 IconRotation => iconRotation;

    /// <summary>For subclasses moving old field values into the shared item fields. Only fills empty fields.</summary>
    /// <returns>True if anything changed.</returns>
    protected bool AdoptLegacyValues(string legacyName, GameObject legacyPrefab)
    {
        bool changed = false;
        if (string.IsNullOrWhiteSpace(itemName) && !string.IsNullOrWhiteSpace(legacyName))
        {
            itemName = legacyName;
            changed = true;
        }
        if (itemPrefab == null && legacyPrefab != null)
        {
            itemPrefab = legacyPrefab;
            changed = true;
        }
        return changed;
    }
}
