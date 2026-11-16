using UnityEngine;

/// <summary>Shared definition for an inventory item.</summary>
[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Inventory/Item Data")]
public class InventoryItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private GameObject itemPrefab;

    [Header("Inventory Icon")]
    [Tooltip("Icon shown in the inventory HUD. Leave empty to render the model prefab automatically at runtime. " +
             "Right-click this asset's component header > 'Bake Icon From Prefab' to generate an editable PNG.")]
    [SerializeField] private Sprite icon;
    [Tooltip("Model rotation used when rendering or baking the icon from the prefab.")]
    [SerializeField] private Vector3 iconRotation = new Vector3(12f, -32f, 0f);

    public virtual string DisplayName => string.IsNullOrWhiteSpace(itemName) ? name : itemName;
    public virtual GameObject ModelPrefab => itemPrefab;
    public Sprite Icon => icon;
    public Vector3 IconRotation => iconRotation;
}
