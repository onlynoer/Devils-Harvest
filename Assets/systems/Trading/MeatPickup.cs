using UnityEngine;

public sealed class MeatPickup : MonoBehaviour, IInteractable
{
    public int Amount { get; private set; }
    public void SetYield(int amount) => Amount = amount;

    public string GetPrompt(WeaponSystem user) => "collect " + Amount + " meat";
    public bool CanInteract(WeaponSystem user) => Amount > 0 && user.TryGetComponent<HarvestTrade>(out _);
    public void Interact(WeaponSystem user) { if (CanInteract(user)) user.GetComponent<HarvestTrade>().Collect(this); }

    public int Collect()
    {
        int meat = Amount;
        Amount = 0;
        Destroy(gameObject);
        return meat;
    }
}
