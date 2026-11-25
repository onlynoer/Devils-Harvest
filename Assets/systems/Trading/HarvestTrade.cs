using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HarvestTrade : MonoBehaviour
{
    public DevilDealer dealer;
    public DealerShopView shop;
    private SaveProgress progress;
    private FirstQuotaDay quota;
    private WeaponSystem weapons;
    private Player player;
    private PlayerMovement movement;
    private FirstPersonLook look;
    private bool restoreMovement, restoreLook, restoreWeapons;
    private CursorLockMode previousLock;
    private bool previousCursor;
    private int openedFrame;
    private bool resumeInput;
    public bool IsOpen { get; private set; }
    public int Meat => progress.meat;

    private void Awake()
    {
        weapons = GetComponent<WeaponSystem>();
        player = GetComponent<Player>();
        movement = GetComponent<PlayerMovement>();
        look = weapons.playerCamera.GetComponent<FirstPersonLook>();
        progress = SaveGameService.ActiveSave?.progress ?? new SaveProgress();
        quota = GetComponent<FirstQuotaDay>();
        if (quota != null) quota.Bind(progress);
        if (progress.dealerWeapons == null) progress.dealerWeapons = new List<DealerWeaponSave>();
    }

    private void Start()
    {
        shop.Bind(this, dealer);
        if (progress.fieldGuns == null) progress.fieldGuns = new List<DealerWeaponSave>();
        var pickups = FindObjectsByType<WeaponPickup>();
        foreach (var saved in progress.fieldGuns)
        {
            var original = System.Array.Find(pickups, pickup => pickup.OriginId == saved.id);
            if (!original || !(original.Definition is WeaponData definition)) continue;
            original.gameObject.SetActive(false);
            RestoreWeapon(saved, definition);
        }
        foreach (var offer in dealer.stock)
        {
            var saved = Owned(offer);
            if (saved != null) RestoreWeapon(saved, offer.weapon);
        }
        int selected = SlotWithOrigin(progress.equippedDealerId);
        if (selected >= 0) weapons.Inventory.Select(selected);
        else if (weapons.Inventory.Count > 0)
            for (int i = 0; i < weapons.Inventory.Capacity; i++)
                if (weapons.Inventory.GetSlot(i) == null) { weapons.Inventory.Select(i); break; }
        SaveGameService.BeforeSave += CaptureInventory;
    }

    private void RestoreWeapon(DealerWeaponSave saved, WeaponData definition)
    {
        if (!saved.dropped)
        {
            weapons.Inventory.TryAdd(definition, saved.ammo, saved.SavedReserve, saved.id, saved.hasMagazine ? saved.slot : -1);
            return;
        }
        if (!saved.hasMagazine) return; // Old dropped purchases are reclaimable at the counter.
        var item = new WeaponInstance(definition, saved.ammo, saved.reserve, saved.id);
        var pickup = weapons.SpawnDropped(item);
        if (pickup) pickup.transform.SetPositionAndRotation(saved.dropPosition, saved.dropRotation);
    }

    private int SlotWithOrigin(string id)
    {
        if (string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < weapons.Inventory.Capacity; i++)
            if (weapons.Inventory.GetSlot(i)?.OriginId == id) return i;
        return -1;
    }

    private void OnDestroy() => SaveGameService.BeforeSave -= CaptureInventory;

    private DealerWeaponSave Record(string id)
    {
        var record = progress.dealerWeapons.Find(s => s.id == id) ?? progress.fieldGuns.Find(s => s.id == id);
        if (record != null) return record;
        record = new DealerWeaponSave { id = id };
        progress.fieldGuns.Add(record);
        return record;
    }

    private void CaptureInventory(SaveProgress saving)
    {
        if (saving != progress) return;
        progress.equippedDealerId = weapons.Inventory.Selected?.OriginId;
        for (int i = 0; i < weapons.Inventory.Capacity; i++)
        {
            var item = weapons.Inventory.GetSlot(i);
            if (item?.Definition == null || item.Definition.useType != WeaponUseType.Gun || string.IsNullOrEmpty(item.OriginId)) continue;
            var saved = Record(item.OriginId);
            saved.ammo = item.LoadedAmmo;
            saved.reserve = item.ReserveAmmo;
            saved.hasMagazine = true;
            saved.dropped = false;
            saved.slot = i;
        }
        foreach (var pickup in FindObjectsByType<WeaponPickup>())
        {
            if (!pickup.isActiveAndEnabled || pickup.ReserveAmmo < 0 || string.IsNullOrEmpty(pickup.OriginId) ||
                !(pickup.Definition is WeaponData gun) || gun.useType != WeaponUseType.Gun) continue;
            var saved = Record(pickup.OriginId);
            saved.ammo = pickup.LoadedAmmo;
            saved.reserve = pickup.ReserveAmmo;
            saved.hasMagazine = true;
            saved.dropped = true;
            saved.dropPosition = pickup.transform.position;
            saved.dropRotation = pickup.transform.rotation;
        }
    }

    public int FindSlot(WeaponData weapon)
    {
        for (int i = 0; i < weapons.Inventory.Capacity; i++)
            if (weapons.Inventory.GetSlot(i)?.Definition == weapon) return i;
        return -1;
    }

    public DealerWeaponSave Owned(DevilDealer.Offer offer) =>
        progress.dealerWeapons.Find(item => item.id == offer.id);

    public void Collect(MeatPickup pickup)
    {
        int amount = pickup.Collect();
        progress.meat += amount;
        progress.harvestedItems += amount;
        SaveGameService.SaveActive();
    }

    public int OfferAmount => quota != null && quota.Accepting ? Mathf.Min(Meat, quota.Remaining) : 0;
    public string AltarPrompt => OfferAmount > 0 ? $"offer {OfferAmount} meat" :
        quota != null && quota.Result == 1 ? "Offering accepted. The quota is complete." :
        quota != null && quota.Result == 2 ? "Nightfall. The altar is closed." :
        $"No meat to offer. Still owed: {quota?.Remaining ?? 0}";

    public int OfferMeat()
    {
        int amount = OfferAmount;
        if (amount <= 0) return 0;
        progress.meat -= amount;
        progress.meatOffered += amount;
        if (quota.Remaining == 0) progress.firstQuotaResult = 1;
        SaveGameService.SaveActive();
        return amount;
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return;
        var keyboard = Keyboard.current;
        if (player.IsDead || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || GameInput.WasPressed(GameAction.Interact))
            Close();
    }

    private void LateUpdate()
    {
        if (!resumeInput || player.IsDead) return;
        resumeInput = false;
        weapons.enabled = restoreWeapons;
    }

    public void Open()
    {
        if (IsOpen || player.IsDead) return;
        openedFrame = Time.frameCount;
        IsOpen = true;
        GamePause.OverlayOpened(); // Escape closes the shop instead of opening the pause menu
        previousLock = Cursor.lockState;
        previousCursor = Cursor.visible;
        restoreMovement = movement.enabled;
        restoreLook = look.enabled;
        restoreWeapons = weapons.enabled;
        movement.enabled = look.enabled = weapons.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        shop.Show();
    }

    public void CloseForRespawn()
    {
        Close();
        resumeInput = false;
        weapons.enabled = restoreWeapons;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        GamePause.OverlayClosed();
        shop.gameObject.SetActive(false);
        movement.enabled = restoreMovement && !player.IsDead;
        look.enabled = restoreLook;
        Cursor.lockState = previousLock;
        Cursor.visible = previousCursor;
        resumeInput = true;
    }

    private static WeaponPickup FindDropped(string id)
    {
        foreach (var pickup in FindObjectsByType<WeaponPickup>())
            if (pickup.isActiveAndEnabled && pickup.OriginId == id) return pickup;
        return null;
    }

    public void SelectOffer(int index)
    {
        if (!IsOpen || player.IsDead || index < 0 || index >= dealer.stock.Length) return;
        var offer = dealer.stock[index];
        if (offer.weapon == null || offer.weapon.ModelPrefab == null || offer.price < 1) return;
        var saved = Owned(offer);
        int existing = FindSlot(offer.weapon);
        if (existing >= 0) weapons.Inventory.Select(existing);
        else
        {
            if (saved == null && Meat < offer.price) return;
            // A paid item dropped with Q can also be reclaimed at the counter.
            var dropped = saved != null ? FindDropped(offer.id) : null;
            int ammo = dropped != null ? dropped.LoadedAmmo : saved?.ammo ?? offer.weapon.maxAmmo;
            int reserve = dropped != null ? dropped.ReserveAmmo : saved?.SavedReserve ?? -1;
            if (!weapons.Inventory.TryAdd(offer.weapon, ammo, reserve, offer.id)) return;
            if (dropped != null) dropped.gameObject.SetActive(false);
            if (saved == null)
            {
                progress.meat -= offer.price;
                progress.dealerWeapons.Add(new DealerWeaponSave { id = offer.id, ammo = ammo });
            }
        }
        CaptureInventory(progress);
        SaveGameService.SaveActive();
        shop.Refresh();
    }
}
