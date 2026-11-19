using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HarvestTrade : MonoBehaviour
{
    public DevilDealer dealer;
    public DealerShopView shop;
    public InputActionReference previousWeapon;
    public InputActionReference nextWeapon;
    private SaveProgress progress;
    private WeaponSystem weapons;
    private Player player;
    private PlayerMovement movement;
    private FirstPersonLook look;
    private bool restoreMovement, restoreLook;
    private CursorLockMode previousLock;
    private bool previousCursor;
    private int closedFrame = -1;
    public bool IsOpen { get; private set; }
    public string Prompt { get; private set; }
    public int Meat => progress.meat;

    void Awake()
    {
        weapons = GetComponent<WeaponSystem>();
        player = GetComponent<Player>();
        movement = GetComponent<PlayerMovement>();
        look = weapons.playerCamera.GetComponent<FirstPersonLook>();
        progress = SaveGameService.ActiveSave?.progress ?? new SaveProgress();
        if (progress.dealerWeapons == null) progress.dealerWeapons = new List<DealerWeaponSave>();
    }

    void OnEnable()
    {
        if (previousWeapon != null) previousWeapon.action.Enable();
        if (nextWeapon != null) nextWeapon.action.Enable();
    }

    void Start()
    {
        shop.Bind(this, dealer);
        foreach (var offer in dealer.stock)
            if (offer.weapon.dealerId == progress.equippedDealerId && Owned(offer.weapon) != null)
                weapons.EquipWeapon(offer.weapon);
    }

    public DealerWeaponSave Owned(WeaponData weapon)
    {
        if (weapon == null || string.IsNullOrEmpty(weapon.dealerId)) return null;
        return progress.dealerWeapons.Find(item => item.id == weapon.dealerId);
    }

    public void RememberEquipped(WeaponData weapon)
    {
        progress.equippedDealerId = weapon.dealerId;
        SaveGameService.SaveActive();
    }

    public bool HandleInput(Collider target, bool interact)
    {
        Prompt = null;
        if (IsOpen)
        {
            if (player.IsDead || (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || interact))) Close();
            return true;
        }
        if (player.IsDead || closedFrame == Time.frameCount) return true;
        if (previousWeapon != null && previousWeapon.action.WasPressedThisFrame())
            return CycleOwned(-1);
        if (nextWeapon != null && nextWeapon.action.WasPressedThisFrame())
            return CycleOwned(1);
        if (target == null) return false;
        var trader = target.GetComponentInParent<DevilDealer>();
        if (trader != null && trader == dealer)
        {
            Prompt = "Press E to Trade with the Devil Dealer";
            if (interact) Open();
            return interact;
        }
        var pickup = target.GetComponentInParent<MeatPickup>();
        if (pickup != null && pickup.Amount > 0)
        {
            Prompt = "Press E to collect " + pickup.Amount + " meat";
            if (interact)
            {
                int amount = pickup.Collect();
                progress.meat += amount;
                progress.harvestedItems += amount;
                SaveGameService.SaveActive();
            }
            return interact;
        }
        if (target.GetComponentInParent<AltarOffering>() != null)
        {
            Prompt = "Meat carried: " + Meat + "   |   Offered: " + progress.meatOffered;
            if (Meat > 0)
            {
                Prompt += "\nPress E to offer 1 meat";
                if (interact)
                {
                    progress.meat--;
                    progress.meatOffered++;
                    SaveGameService.SaveActive();
                }
            }
            return interact;
        }
        return false;
    }

    private bool CycleOwned(int direction)
    {
        var stock = dealer.stock;
        int slot = System.Array.FindIndex(stock, offer => offer.weapon == weapons.EquippedWeapon);
        if (slot < 0 && direction < 0) slot = 0;
        for (int i = 0; i < stock.Length; i++)
        {
            slot = (slot + direction + stock.Length) % stock.Length;
            var weapon = stock[slot].weapon;
            if (Owned(weapon) == null) continue;
            if (weapons.EquippedWeapon != weapon) weapons.EquipWeapon(weapon);
            return true;
        }
        return false;
    }

    void Open()
    {
        IsOpen = true;
        weapons.CancelBurst();
        previousLock = Cursor.lockState;
        previousCursor = Cursor.visible;
        restoreMovement = movement.enabled;
        restoreLook = look.enabled;
        movement.enabled = false;
        look.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        shop.Show();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        closedFrame = Time.frameCount;
        if (shop != null) shop.gameObject.SetActive(false);
        if (movement != null && player != null) movement.enabled = restoreMovement && !player.IsDead;
        if (look != null) look.enabled = restoreLook;
        Cursor.lockState = previousLock;
        Cursor.visible = previousCursor;
    }

    void OnDisable()
    {
        if (IsOpen) Close();
        if (previousWeapon != null) previousWeapon.action.Disable();
        if (nextWeapon != null) nextWeapon.action.Disable();
    }

    public void SelectOffer(int index)
    {
        if (!IsOpen || player.IsDead || index < 0 || index >= dealer.stock.Length) return;
        var offer = dealer.stock[index];
        var w = offer.weapon;
        if (w == null || w.weaponPrefab == null || w.projectilePrefab == null || weapons.hand == null ||
            string.IsNullOrEmpty(w.dealerId) || offer.price < 1) return;
        if (Owned(w) == null)
        {
            if (Meat < offer.price) return;
            progress.meat -= offer.price;
            progress.dealerWeapons.Add(new DealerWeaponSave { id = w.dealerId, ammo = w.maxAmmo });
        }
        if (weapons.EquippedWeapon != w) weapons.EquipWeapon(w);
        shop.Refresh();
    }
}
