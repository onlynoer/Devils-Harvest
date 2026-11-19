using UnityEngine;

/// <summary>
/// One-use ammo box: refills the player's selected weapon to max ammo, then disappears.
/// Needs a Collider so the player's look raycast can hit it.
/// </summary>
public class AmmoPickup : MonoBehaviour, IInteractable
{
    [Tooltip("Prompt when the player isn't holding a weapon.")]
    [SerializeField] private string noWeaponPrompt = "Equip a weapon to reload";
    [Tooltip("Prompt when the selected weapon is already full.")]
    [SerializeField] private string fullPrompt = "Ammo full";

    private WeaponData cachedPromptWeapon;
    private string cachedPrompt;

    public string GetPrompt(WeaponSystem user)
    {
        WeaponInstance selected = user.Inventory.Selected;
        WeaponData weapon = selected != null ? selected.Definition : null;
        if (weapon == null) return noWeaponPrompt;
        if (!selected.NeedsAmmo) return fullPrompt;
        if (cachedPromptWeapon != weapon)
        {
            cachedPromptWeapon = weapon;
            cachedPrompt = "reload " + weapon.DisplayName;
        }
        return cachedPrompt;
    }

    public bool CanInteract(WeaponSystem user)
    {
        WeaponInstance selected = user.Inventory.Selected;
        return selected != null && selected.NeedsAmmo;
    }

    public void Interact(WeaponSystem user)
    {
        if (!CanInteract(user)) return;
        user.Inventory.Selected.Refill();
        gameObject.SetActive(false);
    }
}
