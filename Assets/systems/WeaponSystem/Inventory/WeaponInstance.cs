using System;
using UnityEngine;

/// <summary>
/// One specific item the player is carrying (e.g. "this AK47 with 12 bullets left").
/// The shared stats come from <see cref="ItemDefinition"/>; per-copy state like ammo lives here.
/// </summary>
[Serializable]
public sealed class WeaponInstance
{
    [SerializeField] private string instanceId;
    [SerializeField] private InventoryItemData itemDefinition;
    [SerializeField] private int loadedAmmo;

    public string InstanceId => instanceId;
    public InventoryItemData ItemDefinition => itemDefinition;
    /// <summary>The definition as weapon data, or null for non-weapon items.</summary>
    public WeaponData Definition => itemDefinition as WeaponData;

    public int LoadedAmmo
    {
        get => loadedAmmo;
        set => loadedAmmo = Definition != null ? Mathf.Clamp(value, 0, Definition.maxAmmo) : 0;
    }

    /// <summary>True for a weapon that has less than its max ammo.</summary>
    public bool NeedsAmmo => Definition != null && loadedAmmo < Definition.maxAmmo;

    /// <summary>Fills the weapon back up to its max ammo.</summary>
    public void Refill()
    {
        if (Definition != null) LoadedAmmo = Definition.maxAmmo;
    }

    /// <param name="loadedAmmo">Ammo in the weapon; -1 means full.</param>
    public WeaponInstance(InventoryItemData definition, int loadedAmmo)
    {
        instanceId = Guid.NewGuid().ToString("N");
        itemDefinition = definition;
        LoadedAmmo = definition is WeaponData weapon && loadedAmmo < 0 ? weapon.maxAmmo : loadedAmmo;
    }
}
