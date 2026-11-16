using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Drives the authored inventory bar. All layout and styling lives in the scene: every child of
/// <see cref="slotContainer"/> with an <see cref="InventorySlotView"/> is a slot, in hierarchy order.
/// This script never creates UI; it only shows/hides slots and writes icons, numbers and selection state.
/// </summary>
[DisallowMultipleComponent]
public sealed class InventoryHud : MonoBehaviour
{
    public const int MaxSlots = WeaponInventory.MaxCapacity;
    private static readonly Rect FullUv = new Rect(0f, 0f, 1f, 1f);
    private static readonly string[] SlotNumbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    [Header("References")]
    [SerializeField] private WeaponSystem weaponSystem;
    [Tooltip("Parent of the slot objects (e.g. 'Inventory HUD Canvas/Inventory Slots').")]
    [SerializeField] private RectTransform slotContainer;

    [Header("Slot Style")]
    [Tooltip("When on, slot backgrounds are tinted with the colors below. Turn off to keep each slot's authored Image color.")]
    [SerializeField] private bool tintBackgrounds = true;
    [SerializeField] private Color normalColor = new Color(.08f, .07f, .07f, .82f);
    [SerializeField] private Color selectedColor = new Color(.72f, .36f, .12f, .96f);
    [Tooltip("When on, slot labels are set to 1..9. Turn off to keep authored label text.")]
    [SerializeField] private bool numberSlots = true;
    [Tooltip("Hide slots past the WeaponInventory capacity.")]
    [SerializeField] private bool hideSlotsBeyondCapacity = true;

    [Header("Generated Icons")]
    [Tooltip("Resolution for icons rendered at runtime for items that have no authored icon.")]
    [SerializeField, Range(64, 1024)] private int generatedIconResolution = 256;

    [Header("Editor Preview (edit mode only)")]
    [SerializeField] private InventoryItemData previewItem;
    [SerializeField, Range(0, MaxSlots - 1)] private int previewSlotIndex;

    private static readonly Dictionary<InventoryItemData, Texture2D> GeneratedIcons = new Dictionary<InventoryItemData, Texture2D>();
    private readonly List<InventorySlotView> slots = new List<InventorySlotView>();

    private WeaponInventory Inventory => weaponSystem != null ? weaponSystem.Inventory : null;

    private void Awake()
    {
        ResolveReferences();
        CollectSlots();
    }

    private void LateUpdate() => Refresh();

    /// <summary>Re-reads the slot children. Call after adding/removing slot objects at runtime.</summary>
    public void CollectSlots()
    {
        slots.Clear();
        if (slotContainer == null) return;
        for (int i = 0; i < slotContainer.childCount && slots.Count < MaxSlots; i++)
        {
            InventorySlotView view = slotContainer.GetChild(i).GetComponent<InventorySlotView>();
            if (view == null) continue;
            view.AutoAssign();
            slots.Add(view);
        }
    }

    private void ResolveReferences()
    {
        if (weaponSystem == null) weaponSystem = GetComponent<WeaponSystem>();
    }

    private void Refresh()
    {
        WeaponInventory inventory = Inventory;
        int capacity = inventory != null ? inventory.Capacity : slots.Count;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotView view = slots[i];
            if (view == null) continue;

            bool shown = !hideSlotsBeyondCapacity || i < capacity;
            view.SetVisible(shown);
            if (!shown) continue;

            GetSlotContent(inventory, i, out InventoryItemData item, out bool selected);
            if (numberSlots) view.SetLabel(SlotNumbers[i]);
            Texture texture = GetIcon(item, out Rect uv);
            view.SetIcon(texture, uv);
            view.SetSelected(selected, tintBackgrounds, normalColor, selectedColor);
        }
    }

    private void GetSlotContent(WeaponInventory inventory, int index, out InventoryItemData item, out bool selected)
    {
        item = null;
        selected = false;
        if (Application.isPlaying)
        {
            if (inventory == null) return;
            WeaponInstance instance = inventory.GetSlot(index);
            item = instance != null ? instance.ItemDefinition : null;
            selected = inventory.SelectedIndex == index;
        }
        else if (index == previewSlotIndex && previewItem != null)
        {
            item = previewItem;
            selected = true;
        }
    }

    private Texture GetIcon(InventoryItemData item, out Rect uv)
    {
        uv = FullUv;
        if (item == null) return null;

        Sprite sprite = item.Icon;
        if (sprite != null)
        {
            Texture2D atlas = sprite.texture;
            Rect r = sprite.textureRect;
            uv = new Rect(r.x / atlas.width, r.y / atlas.height, r.width / atlas.width, r.height / atlas.height);
            return atlas;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Edit mode: Unity's asset preview thumbnail, never a live render.
            if (item.ModelPrefab == null) return null;
            Texture2D preview = AssetPreview.GetAssetPreview(item.ModelPrefab);
            if (preview == null && AssetPreview.IsLoadingAssetPreview(item.ModelPrefab.GetEntityId()))
                QueueEditorRefresh();
            return preview;
        }
#endif

        if (GeneratedIcons.TryGetValue(item, out Texture2D cached) && cached != null) return cached;
        Texture2D generated = InventoryIconRenderer.Render(item.ModelPrefab, generatedIconResolution, item.IconRotation);
        GeneratedIcons[item] = generated;
        return generated;
    }

#if UNITY_EDITOR
    // NonSerialized: domain reload preserves private fields but drops delayCall, which would leave this stuck true.
    [System.NonSerialized] private bool editorRefreshQueued;

    private void OnEnable()
    {
        if (!Application.isPlaying) QueueEditorRefresh();
    }

    private void OnValidate()
    {
        previewSlotIndex = Mathf.Clamp(previewSlotIndex, 0, MaxSlots - 1);
        QueueEditorRefresh();
    }

    /// <summary>Updates the edit-mode preview. Safe to call from OnValidate: the work runs after it returns.</summary>
    public void RefreshInEditor() => QueueEditorRefresh();

    [ContextMenu("Refresh Preview")]
    private void QueueEditorRefresh()
    {
        if (Application.isPlaying || editorRefreshQueued) return;
        editorRefreshQueued = true;
        EditorApplication.delayCall += EditorRefresh;
    }

    private void EditorRefresh()
    {
        editorRefreshQueued = false;
        if (this == null || Application.isPlaying) return;
        ResolveReferences();
        CollectSlots();
        Refresh();
    }
#else
    public void RefreshInEditor() { }
#endif
}
