using System;
using UnityEngine;

public sealed class DevilDealer : MonoBehaviour, IInteractable
{
    [Serializable] public class Offer
    {
        public string id;
        public WeaponData weapon;
        [Min(1)] public int price;
        public string description;
    }

    public Offer[] stock;
    public string GetPrompt(WeaponSystem user) => "Trade with the Devil Dealer";
    public bool CanInteract(WeaponSystem user) => user.TryGetComponent(out HarvestTrade trade) && trade.dealer == this && !trade.IsOpen;
    public void Interact(WeaponSystem user) { if (CanInteract(user)) user.GetComponent<HarvestTrade>().Open(); }
}
