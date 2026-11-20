using UnityEngine;

public sealed class AltarOffering : MonoBehaviour, IInteractable
{
    public string GetPrompt(WeaponSystem user) => user.GetComponent<HarvestTrade>()?.AltarPrompt;
    public bool CanInteract(WeaponSystem user) => user.TryGetComponent(out HarvestTrade trade) && trade.CanOffer;
    public void Interact(WeaponSystem user) { if (CanInteract(user)) user.GetComponent<HarvestTrade>().OfferMeat(); }
}
