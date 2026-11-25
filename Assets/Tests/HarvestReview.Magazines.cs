#if HARVEST_PLAYTEST
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed partial class HarvestReview
{
    static IEnumerator ShootDownTo(int left)
    {
        var item = gun.Inventory.Selected;
        int attempts = 0;
        while (item.LoadedAmmo > left && attempts++ < 160)
        {
            MouseButtons(false,true);
            yield return Frames(2);
            MouseButtons(false,false);
            yield return new WaitForSeconds(Mathf.Max(.24f,item.Definition.cooldown));
        }
        Check(item.LoadedAmmo == left, $"fired to {left} loaded in {item.Definition.DisplayName}");
    }

    static IEnumerator ReloadFully()
    {
        yield return Press(Key.R);
        yield return new WaitForSeconds(gun.EquippedWeapon.reloadDuration + .1f);
    }

    static void AmmoCounts(int loaded, int reserve, string reason)
    {
        string actual = GameObject.Find("Ammo Values").GetComponent<TMP_Text>().text;
        string expected = loaded + " / " + reserve;
        Check(actual == expected && gun.LoadedAmmo == loaded && gun.Inventory.Selected.ReserveAmmo == reserve,
            reason + " HUD " + actual + " (expected " + expected + ")");
    }

    static IEnumerator ReloadReviewSave(bool legacyFixture = false)
    {
        string slot = SaveGameService.ActiveSave.id;
        yield return Press(Key.Escape);
        var pause = Object.FindAnyObjectByType<PauseMenuController>();
        var button = (Button)typeof(PauseMenuController).GetField("mainMenuButton",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(pause);
        yield return ButtonClick(button);
        yield return Frames(20);
        var menu = Object.FindAnyObjectByType<NewGameMenuController>();
        if (legacyFixture)
        {
            // Recreate the old on-disk schema only for the throwaway save this replay created.
            var record = SaveGameService.ActiveSave;
            Check(record.id == slot && record.saveName == save && System.Guid.TryParseExact(slot, "N", out _),
                "legacy fixture targets this replay's save");
            string file = System.IO.Path.Combine(Application.persistentDataPath, "DevilsHarvest/Saves", slot + ".json");
            string json = "{\"id\":\"" + slot + "\",\"saveName\":\"" + save + "\",\"lastPlayedUtc\":\"" + record.lastPlayedUtc + "\",\"progress\":{" +
                "\"startingLevelId\":\"main-level\",\"equippedDealerId\":\"grotto-burst\"," +
                "\"dealerWeapons\":[{\"id\":\"grotto-burst\",\"ammo\":47,\"dropped\":false}]}}";
            System.IO.File.WriteAllText(file, json);
        }
        yield return ButtonClick(menu.loadGameButton);
        yield return ButtonClick(menu.saveListRoot.Find("Save_"+slot).GetComponent<Button>());
        yield return ButtonClick(menu.playSelectedSaveButton);
        yield return Frames(40);
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "DevilsHarvest_Island", "Load Game opened the island");
        Player();
    }

    static IEnumerator MagazineCases()
    {
        AmmoCounts(30,120,"beach pickup");
        Face(gun.playerCamera.transform.position + Vector3.up * 20 + Vector3.forward * 30);
        yield return ShootDownTo(23);
        AmmoCounts(23,120,"partial magazine");
        yield return Press(Key.R);
        yield return Press(Key.R);
        yield return Press(Key.Escape);
        int before = gun.LoadedAmmo;
        yield return Frames(85);
        Check(gun.LoadedAmmo == before,"pause cannot finish reload");
        yield return Press(Key.Escape);
        Keys(Key.R);
        yield return Frames(83);
        Keys(); yield return Frames(3);
        AmmoCounts(30,113,"one partial reload on repeated and held R");
        yield return ShootDownTo(0);
        AmmoCounts(0,113,"empty");
        Check(GameObject.Find("Ammo Meaning").GetComponent<TMP_Text>().text.Contains(GameInput.GetBindingText(GameAction.Reload)),"empty prompt uses bound key");
        yield return Click(Vector2.zero);
        AmmoCounts(0,113,"dry trigger");
        yield return ReloadFully();
        AmmoCounts(30,83,"empty reload");
        yield return ShootDownTo(26);
        yield return Press(Key.R);
        yield return Press(Key.Digit2);
        yield return Frames(78);
        yield return Press(Key.Digit1);
        AmmoCounts(26,83,"swap cancels reload");
        yield return Press(Key.R);
        yield return ReloadReviewSave();
        AmmoCounts(26,83,"partially spent pickup after Load Game during reload");
        Check(!Object.FindObjectsByType<WeaponPickup>().Any(x=>x.isActiveAndEnabled && x.Definition==gun.EquippedWeapon),"collected beach rifle stayed collected");
        yield return Press(Key.R);
        p.KillAndRecover();
        yield return Frames(95);
        AmmoCounts(26,83,"death cancels reload");

        var box=Object.FindAnyObjectByType<AmmoPickup>();
        var boxAt=box.transform.position;
        Stage(boxAt.x,boxAt.z-1.75f);
        yield return Frames(5);
        yield return Target(box);
        yield return Press(Key.E);
        AmmoCounts(26,124,"ammo box replenished reserve");
        string origin=gun.Inventory.Selected.OriginId;
        Stage(333,119);
        yield return Frames(4);
        Face(gun.playerCamera.transform.position + Vector3.forward * 10);
        yield return Press(Key.Q);
        yield return Frames(35);
        yield return ReloadReviewSave();
        Check(!Object.FindAnyObjectByType<AmmoPickup>(), "spent ammo box stayed collected");
        var dropped=Object.FindObjectsByType<WeaponPickup>().Single(x=>x.isActiveAndEnabled && x.OriginId==origin);
        var dropAt=dropped.transform.position;
        yield return Walk(XZ(dropAt)+(XZ(p.transform.position)-XZ(dropAt)).normalized*1.65f);
        yield return Target(dropped);
        yield return Press(Key.E);
        AmmoCounts(26,124,"dropped AK recovered after load");

        // Isolated shop fixtures exercise all three prices without changing the quota playthrough.
        SaveGameService.ActiveSave.progress.meat = 50;
        Stage(895,440.95f,27.15f);
        yield return Frames(8);
        for (int i=0;i<3;i++)
        {
            yield return Open();
            yield return ButtonClick(trade.shop.rows[i].button);
            yield return Press(Key.Escape);
            yield return Frames(4);
            var item=gun.Inventory.Selected;
            int capacity=item.Definition.Capacity, reserve=item.Definition.maxAmmo-capacity;
            AmmoCounts(capacity,reserve,item.Definition.DisplayName+" purchase");
            Face(gun.playerCamera.transform.position + Vector3.up * 70 + Vector3.forward * 20);
            int used=i==1 ? 3 : 1;
            yield return ShootDownTo(capacity-used);
            yield return ReloadFully();
            AmmoCounts(capacity,reserve-used,item.Definition.DisplayName+" top-up");
            if (i==0)
            {
                yield return ShootDownTo(0);
                yield return ReloadFully();
                AmmoCounts(reserve-1,0,"last reserve smaller than magazine");
                yield return ShootDownTo(0);
                yield return ReloadFully();
                AmmoCounts(0,0,"out of rounds");
            }
        }
        var selected=gun.Inventory.Selected;
        yield return ShootDownTo(selected.LoadedAmmo-1);
        int keep=selected.LoadedAmmo, spare=selected.ReserveAmmo;
        yield return Press(Key.Q);
        yield return Open();
        yield return ButtonClick(trade.shop.rows[2].button);
        yield return Press(Key.Escape);
        yield return Frames(5);
        AmmoCounts(keep,spare,"dealer reclaim");
        yield return ReloadReviewSave();
        AmmoCounts(keep,spare,"purchased rifle after load");
        yield return ReloadReviewSave(true);
        AmmoCounts(30,17,"pre-magazine save loaded from disk");
    }
}
#endif
