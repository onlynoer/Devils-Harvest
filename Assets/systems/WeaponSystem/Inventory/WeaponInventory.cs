using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player's hotbar: a fixed number of slots holding <see cref="WeaponInstance"/>s.
/// Slots can hold weapons or any other <see cref="InventoryItemData"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class WeaponInventory : MonoBehaviour
{
    /// <summary>Hard limit: one slot per number key 1-9.</summary>
    public const int MaxCapacity = 9;

    [Tooltip("Number of hotbar slots the player has.")]
    [Range(0, MaxCapacity)] [SerializeField] private int capacity = 5;
    [SerializeField] private List<WeaponInstance> slots = new List<WeaponInstance>();
    [Tooltip("Selected slot index, or -1 for none.")]
    [SerializeField] private int selectedIndex = -1;

    /// <summary>Usable slot count. Never smaller than the last occupied slot, so lowering capacity can't hide items.</summary>
    public int Capacity
    {
        get
        {
            int result = Mathf.Clamp(capacity, 0, MaxCapacity);
            for (int i = slots.Count - 1; i >= result; i--)
                if (IsOccupied(i)) return Mathf.Min(i + 1, MaxCapacity);
            return result;
        }
    }

    public int Count
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++) if (IsOccupied(i)) count++;
            return count;
        }
    }

    public bool HasFreeSlot
    {
        get
        {
            for (int i = 0; i < Capacity; i++)
                if (i >= slots.Count || !IsOccupied(i)) return true;
            return false;
        }
    }

    public int SelectedIndex => selectedIndex;
    public WeaponInstance Selected => GetSlot(selectedIndex);
    public IReadOnlyList<WeaponInstance> Slots => slots;

    private void Awake() => EnsureSlots();

    private void OnValidate()
    {
        capacity = Mathf.Clamp(capacity, 0, MaxCapacity);
        if (slots == null) slots = new List<WeaponInstance>();
        InventoryHud hud = GetComponent<InventoryHud>();
        if (hud != null) hud.RefreshInEditor();
    }

    /// <summary>The item in a slot, or null if the index is out of range or the slot is empty.</summary>
    public WeaponInstance GetSlot(int index)
    {
        if (index < 0 || index >= Capacity || index >= slots.Count) return null;
        return IsOccupied(index) ? slots[index] : null;
    }

    /// <summary>Puts the item in the first empty slot and selects it. Returns false when the hotbar is full.</summary>
    /// <param name="loadedAmmo">Ammo in the weapon; -1 means full.</param>
    public bool TryAdd(InventoryItemData definition, int loadedAmmo = -1)
    {
        if (definition == null) return false;
        EnsureSlots();
        for (int i = 0; i < Capacity; i++)
        {
            if (IsOccupied(i)) continue;
            slots[i] = new WeaponInstance(definition, loadedAmmo);
            Select(i);
            return true;
        }
        return false;
    }

    public bool Select(int index)
    {
        if (index < 0 || index >= Capacity) return false;
        selectedIndex = index;
        return true;
    }

    /// <summary>Empties a slot and returns what was in it (null if it was already empty).</summary>
    public WeaponInstance Remove(int index)
    {
        WeaponInstance removed = GetSlot(index);
        if (removed != null) slots[index] = null;
        return removed;
    }

    private bool IsOccupied(int index) => slots[index] != null && slots[index].ItemDefinition != null;

    private void EnsureSlots()
    {
        if (slots == null) slots = new List<WeaponInstance>();
        while (slots.Count < Mathf.Clamp(capacity, 0, MaxCapacity)) slots.Add(null);
    }
}
