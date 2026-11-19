using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DealerShopView : MonoBehaviour
{
    [Serializable] public class Row
    {
        public TMP_Text title, detail, price, action;
        public Button button;
    }

    public TMP_Text balance;
    public Button leave;
    public Row[] rows;
    private HarvestTrade trade;
    private DevilDealer dealer;

    public void Bind(HarvestTrade owner, DevilDealer merchant)
    {
        trade = owner;
        dealer = merchant;
        leave.onClick.AddListener(trade.Close);
        for (int i = 0; i < rows.Length; i++)
        {
            int slot = i;
            rows[i].button.onClick.AddListener(() => trade.SelectOffer(slot));
        }
    }

    public void Show()
    {
        Refresh();
        gameObject.SetActive(true);
    }

    public void Refresh()
    {
        balance.text = trade.Meat + " MEAT";
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var item = dealer.stock[i];
            var owned = trade.Owned(item);
            bool equipped = trade.GetComponent<WeaponSystem>().EquippedWeapon == item.weapon;
            row.title.text = item.weapon.DisplayName;
            row.detail.text = item.description;
            row.price.text = item.price + " MEAT";
            if (owned != null) row.action.text = equipped ? "OWNED  /  EQUIPPED" : "OWNED  /  EQUIP";
            else row.action.text = trade.Meat >= item.price ? "BUY & EQUIP" : "NEED " + (item.price - trade.Meat) + " MORE";
            bool full = trade.FindSlot(item.weapon) < 0 && !trade.GetComponent<WeaponSystem>().Inventory.HasFreeSlot;
            if (full) row.action.text = "HOTBAR FULL";
            row.button.interactable = !full && (owned != null ? !equipped : trade.Meat >= item.price);
        }
    }
}
