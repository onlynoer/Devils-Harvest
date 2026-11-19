using System;
using UnityEngine;

public sealed class DevilDealer : MonoBehaviour
{
    [Serializable]
    public class Offer
    {
        public WeaponData weapon;
        [Min(1)] public int price;
        public string description;
    }

    public Offer[] stock;
}
