using System;
using UnityEngine;

[Serializable]
public sealed class WeaponInstance
{
    [SerializeField] private string instanceId;
    [SerializeField] private InventoryItemData itemDefinition;
    [SerializeField] private int loadedAmmo;
    [SerializeField] private int reserveAmmo;

    public string InstanceId => instanceId;
    public string OriginId { get; }
    public InventoryItemData ItemDefinition => itemDefinition;
    public WeaponData Definition => itemDefinition as WeaponData;
    public int LoadedAmmo
    {
        get => loadedAmmo;
        set => loadedAmmo = Definition ? Mathf.Clamp(value, 0, Definition.Capacity) : 0;
    }
    public int ReserveAmmo => reserveAmmo;
    public bool NeedsAmmo => Definition && loadedAmmo + reserveAmmo < Definition.maxAmmo;
    public bool CanReload => Definition && Definition.useType == WeaponUseType.Gun && loadedAmmo < Definition.Capacity && reserveAmmo > 0;

    // A missing reserve denotes a legacy total, including on authored world pickups.
    public WeaponInstance(InventoryItemData definition, int ammo, int reserve = -1, string originId = null)
    {
        instanceId = Guid.NewGuid().ToString("N");
        itemDefinition = definition;
        OriginId = originId;
        if (!Definition) return;
        int total = ammo < 0 ? Definition.maxAmmo : Mathf.Clamp(ammo, 0, Definition.maxAmmo);
        LoadedAmmo = total;
        if (Definition.useType == WeaponUseType.Gun)
            reserveAmmo = reserve < 0 ? total - loadedAmmo : Mathf.Clamp(reserve, 0, Definition.maxAmmo - loadedAmmo);
    }

    public void Refill()
    {
        if (!Definition) return;
        if (Definition.useType == WeaponUseType.Gun) reserveAmmo = Definition.maxAmmo - loadedAmmo;
        else LoadedAmmo = Definition.maxAmmo;
    }

    public void CompleteReload()
    {
        if (!CanReload) return;
        int rounds = Mathf.Min(Definition.Capacity - loadedAmmo, reserveAmmo);
        loadedAmmo += rounds;
        reserveAmmo -= rounds;
    }
}
