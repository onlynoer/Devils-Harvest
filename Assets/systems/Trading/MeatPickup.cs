using UnityEngine;

public sealed class MeatPickup : MonoBehaviour
{
    public int Amount { get; private set; }
    public void SetYield(int amount) => Amount = amount;

    public int Collect()
    {
        int meat = Amount;
        Amount = 0;
        Destroy(gameObject);
        return meat;
    }
}
