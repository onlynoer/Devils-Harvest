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
        int selected = -1;
        foreach (var offer in dealer.stock)
        {
            var saved = Owned(offer);
            if (saved == null || saved.dropped || !weapons.Inventory.TryAdd(offer.weapon, saved.ammo)) continue;
            if (offer.id == progress.equippedDealerId) selected = weapons.Inventory.SelectedIndex;
        }
        if (selected >= 0) weapons.Inventory.Select(selected);
        else if (weapons.Inventory.Count > 0)
        {
            for (int i = 0; i < weapons.Inventory.Capacity; i++)
                if (weapons.Inventory.GetSlot(i) == null) { weapons.Inventory.Select(i); break; }
        }
        SaveGameService.BeforeSave += CaptureInventory;
    }

    private void OnDestroy() => SaveGameService.BeforeSave -= CaptureInventory;

    private void CaptureInventory(SaveProgress saving)
    {
        if (saving != progress) return;
        progress.equippedDealerId = "";
        foreach (var offer in dealer.stock)
        {
            var saved = Owned(offer);
            if (saved == null) continue;
            int index = FindSlot(offer.weapon);
            if (index < 0)
            {
                if (!saved.dropped)
                {
                    var dropped = FindDropped(offer.weapon);
                    if (dropped != null) saved.ammo = dropped.LoadedAmmo;
                }
                saved.dropped = true;
                continue;
            }
            saved.dropped = false;
            saved.ammo = weapons.Inventory.GetSlot(index).LoadedAmmo;
            if (weapons.Inventory.SelectedIndex == index) progress.equippedDealerId = offer.id;
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

    public bool CanOffer => Meat > 0 && (quota == null || quota.Accepting);

    public string AltarPrompt => (CanOffer ? "offer 1 meat\n" : "") +
        "Meat carried: " + Meat + "   |   Offered: " + progress.meatOffered +
        (quota == null ? "" : quota.Accepting ? "   |   Still due: " + quota.Remaining :
            progress.firstQuotaResult == 1 ? "   |   Quota met" : "   |   Deadline passed");

    public void OfferMeat()
    {
        if (!CanOffer) return;
        progress.meat--;
        progress.meatOffered++;
        if (quota != null) quota.Evaluate();
        SaveGameService.SaveActive();
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

    private static WeaponPickup FindDropped(WeaponData weapon)
    {
        foreach (var pickup in FindObjectsByType<WeaponPickup>())
            if (pickup.isActiveAndEnabled && pickup.Definition == weapon) return pickup;
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
            var dropped = saved != null ? FindDropped(offer.weapon) : null;
            int ammo = dropped != null ? dropped.LoadedAmmo : saved?.ammo ?? offer.weapon.maxAmmo;
            if (!weapons.Inventory.TryAdd(offer.weapon, ammo)) return;
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
