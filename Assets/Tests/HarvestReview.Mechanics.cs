#if HARVEST_PLAYTEST
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed partial class HarvestReview
{
    private static void MouseButtons(bool aim, bool fire)
    {
        var input = new MouseState().WithButton(MouseButton.Right, aim).WithButton(MouseButton.Left, fire);
        InputSystem.QueueStateEvent(mouse, input);
    }

    private static IEnumerator Mechanics()
    {
        var world = SaveGameService.ActiveSave.progress;
        var quota = p.GetComponent<FirstQuotaDay>();
        var cam = gun.playerCamera;
        float normal = cam.fieldOfView;
        // Isolate the one-meat boundary. Natural first-day prey drop five at a time.
        world.meat = 9;
        yield return Frames(2);
        Check(!quota.ReadyToOffer && !GameObject.Find("Destination bearings"), "9 carried hides directions");
        var definition = Object.FindObjectsByType<Creature>().First(c => c.creatureData.meatDrop != null).creatureData;
        var here = p.transform.position + p.transform.forward * 2f;
        here.y = Terrain.activeTerrain.SampleHeight(here) + Terrain.activeTerrain.transform.position.y + .05f;
        var drop = Instantiate(definition.meatDrop, here, Quaternion.identity);
        drop.SetYield(1);
        yield return Target(drop);
        string expectedPrompt = "[" + GameInput.GetBindingText(GameAction.Interact) + "]";
        var prompt = Object.FindAnyObjectByType<InteractPromptHud>().GetComponentsInChildren<TMP_Text>().Single(t => t.isActiveAndEnabled);
        Check(prompt.text.Contains(expectedPrompt) && prompt.text.Contains("meat"), "meat prompt displays the actual bound input");
        yield return Press(Key.E);
        Check(world.meat == 10 && quota.ReadyToOffer && GameObject.Find("Destination bearings"), "9 -> 10 through pickup and HUD");
        var altar = Object.FindAnyObjectByType<AltarOffering>();
        Face(altar.transform.position);
        yield return Frames(3);
        var pointer = GameObject.Find("Destination bearings").transform.GetChild(0);
        float facing = pointer.localEulerAngles.z;
        Face(cam.transform.position - cam.transform.forward * 100f);
        yield return Frames(3);
        Check(Mathf.Abs(Mathf.DeltaAngle(facing, pointer.localEulerAngles.z)) > 165f, "behind indicator turns 180 degrees");
        yield return Pic("behind-bearings");
        Stage(793, 754.4f);
        yield return Frames(6);
        yield return Target(altar);
        yield return Press(Key.E);
        Check(world.meatOffered == 0 && world.meat == 10 && quota.ReadyToOffer, "a tap keeps meat and directions");
        Stage(895, 440.95f, 27.15f);
        yield return Frames(6);
        yield return Open();
        yield return ButtonClick(trade.shop.rows[0].button);
        yield return Press(Key.Escape);
        Check(world.meat == 6 && !quota.ReadyToOffer && !GameObject.Find("Destination bearings"), "optional purchase hides directions below remaining cost");
        var bow = Object.FindObjectsByType<WeaponPickup>().First(w => w.isActiveAndEnabled && w.Definition is WeaponData d && d.useType == WeaponUseType.Bow);
        var at = bow.transform.position;
        Stage(at.x, at.z - 1.7f);
        yield return Frames(6);
        yield return Target(bow);
        yield return Press(Key.E);
        var ammo = GameObject.Find("Ammo Values").GetComponent<TMP_Text>();
        Check(gun.EquippedWeapon.useType == WeaponUseType.Bow && GameObject.Find("Ammo Meaning").GetComponent<TMP_Text>().text == "ARROWS" && gun.LoadedAmmo == 30, "bow equipped with real 30-arrow HUD");
        gun.Inventory.Selected.LoadedAmmo = 1;
        yield return Click(Vector2.zero);
        yield return Frames(18);
        yield return Click(Vector2.zero);
        Check(gun.LoadedAmmo == 0 && ammo.text.Contains("0 / 30"), "last arrow consumed and empty bow stays empty");
        yield return Pic("bow-empty");
        // Buy the remaining firearms using a test balance and fire each through normal controls.
        world.meat = 50;
        Stage(895, 440.95f, 27.15f);
        yield return Frames(6);
        for (int i = 1; i < 3; i++)
        {
            yield return Open();
            yield return ButtonClick(trade.shop.rows[i].button);
            yield return Press(Key.Escape);
        }

        Stage(331, 140);
        yield return Frames(5);
        var weapons = gun.Inventory;
        for (int i = 0; i < weapons.Capacity; i++)
        {
            var item = weapons.GetSlot(i);
            if (item?.Definition == null || item.Definition.useType != WeaponUseType.Gun)
                continue;
            yield return Press((Key)((int)Key.Digit1 + i));
            Check(gun.Inventory.Selected == item && GameObject.Find("Ammo Meaning").GetComponent<TMP_Text>().text != "ARROWS", "gun ammo restored for " + item.Definition.DisplayName);
            Face(cam.transform.position + new Vector3(.15f, .2f, 1f) * 50f);
            MouseButtons(true, false);
            yield return Frames(10);
            Check(Mathf.Abs(cam.fieldOfView - normal * item.Definition.aimFieldOfViewScale) < .15f, $"aim FOV {item.Definition.DisplayName} measured={cam.fieldOfView}, base={normal}, scale={item.Definition.aimFieldOfViewScale}, held={GameInput.IsHeld(GameAction.Aim)}, blend={gun.AimBlend}");
            yield return Pic("sight-" + item.Definition.name);
            int loaded = gun.LoadedAmmo;
            var before = cam.transform.localRotation;
            var centerRay = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            Vector3 sightPoint = Physics.Raycast(centerRay, out var impact, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? impact.point : centerRay.GetPoint(1000f);
            Vector3 expected = (sightPoint - gun.bulletSpawn.position).normalized;
            MouseButtons(true, true);
            yield return Frames(2);
            Check(Quaternion.Angle(before, cam.transform.localRotation) > .05f && gun.LoadedAmmo < loaded, "shot and camera recoil " + item.Definition.DisplayName);
            var shot = Object.FindObjectsByType<Projectile>().Where(b => b.name == item.Definition.projectilePrefab.name + "(Clone)").OrderBy(b => Vector3.Distance(b.transform.position, gun.bulletSpawn.position)).First();
            Check(Vector3.Angle(shot.transform.forward, expected) < 1f, "projectile followed center sight ray");
            Keys(Key.W);
            yield return Frames(32);
            Keys();
            if (item.Definition.automatic)
                Check(loaded - gun.LoadedAmmo >= 4, "held automatic fire spent multiple rounds while moving");
            Check(Quaternion.Angle(before, cam.transform.localRotation) < 6f, "bounded moving recoil");
            MouseButtons(false, false);
            yield return Frames(22);
            Check(Mathf.Abs(cam.fieldOfView - normal) < .1f && Quaternion.Angle(before, cam.transform.localRotation) < .2f, "release and recoil recovery");
            int movingStart = gun.LoadedAmmo;
            Vector3 start = p.transform.position;
            Keys(Key.D);
            yield return Frames(2);
            MouseButtons(true, true);
            yield return Frames(2);
            MouseButtons(true, false);
            yield return Frames(8);
            Keys();
            MouseButtons(false, false);
            yield return Frames(8);
            Check(gun.LoadedAmmo < movingStart && Vector3.Distance(p.transform.position, start) > .25f, "fired while strafing with " + item.Definition.DisplayName);
        }

        MouseButtons(true, false);
        yield return Frames(8);
        yield return Press(Key.Digit1);
        Check(!gun.IsAiming && Mathf.Abs(cam.fieldOfView - normal) < .1f, "swap while aiming restores view");
        MouseButtons(false, false);
        yield return Frames(4);
        var rewards = Object.FindObjectsByType<PowerupPickup>().Where(r => !string.IsNullOrEmpty(r.cacheId)).ToArray();
        Check(rewards.Length == 3, "three exploration rewards");
        foreach (var reward in rewards)
        {
            var dest = XZ(reward.transform.position);
            Stage(dest.x, dest.y - 5f);
            yield return Frames(5);
            Face(reward.transform.position);
            yield return Pic(reward.cacheId);
            p.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(reward.transform.position - p.transform.position, Vector3.up));
            Keys(Key.W);
            for (int f = 0; f < 45 && reward.gameObject.activeSelf; f++)
                yield return null;
            Keys();
            yield return Frames(4);
            var modifier = reward.Powerup.modifiers[0];
            Check(world.exploredCaches.Contains(reward.cacheId) && !reward.gameObject.activeSelf && PlayerPowerups.For(p.gameObject).GetMultiplier(modifier.stat) >= modifier.multiplier, $"walked to {reward.cacheId}, saved={world.exploredCaches.Contains(reward.cacheId)} active={reward.gameObject.activeSelf} multiplier={PlayerPowerups.For(p.gameObject).GetMultiplier(modifier.stat)} player={p.transform.position} reward={reward.transform.position}");
        }

        world.firstDayElapsed = 240;
        yield return Frames(4);
        yield return Pic("midday");
        SaveGameService.SaveActive();
        var id = SaveGameService.ActiveSave.id;
        Check(SaveGameService.Load(id), "opened mid-cycle save");
        SceneManager.LoadScene("DevilsHarvest_Island");
        yield return Frames(15);
        Player();
        cam = gun.playerCamera;
        world = SaveGameService.ActiveSave.progress;
        quota = p.GetComponent<FirstQuotaDay>();
        Check(Mathf.Abs(quota.DayFraction - .5f) < .02f && world.exploredCaches.Count == 3 && !Object.FindObjectsByType<PowerupPickup>().Any(r => !string.IsNullOrEmpty(r.cacheId)) && PlayerPowerups.For(p.gameObject).ActivePowerups.Count > 0, "reload restored clock, used caches and remaining boost");
        var sky = RenderSettings.skybox;
        float dayExposure = sky.GetFloat("_Exposure");
        world.firstDayElapsed = 446;
        yield return Frames(3);
        Stage(792, 742);
        yield return Frames(5);
        Face(altarPosition());
        yield return Frames(3);
        Check(sky.GetFloat("_Exposure") < dayExposure * .35f && RenderSettings.sun.intensity > 0f, "visible night illumination and distinct sky");
        yield return Pic("night-shrine");
        world.firstDayElapsed = 479.95f;
        yield return Frames(5);
        Check(world.firstQuotaResult == 2 && !quota.ReadyToOffer, "end of day closes offering");
        var caster = Object.FindObjectsByType<FireShooterAI>().Single(c => c.name == "Monster38_01");
        var hostile = caster.GetComponent<Creature>();
        yield return Approach(hostile, 7);
        float health = p.CurrentHealth;
        bool warning = false;
        for (int f = 0; f < 220 && p.CurrentHealth == health; f++)
        {
            if (caster.castingCue.activeSelf && !warning)
            {
                warning = true;
                Face(caster.transform.position + Vector3.up);
                yield return Pic("cast");
            }

            yield return null;
        }

        Check(warning && p.CurrentHealth < health, "ranged windup and projectile hit");
        int beforeDrop = Object.FindObjectsByType<MeatPickup>().Length;
        Face(hostile.transform.position + Vector3.up);
        hostile.TakeDamage(10000f, cam.transform.forward);
        hostile.TakeDamage(10000f, cam.transform.forward);
        yield return Frames(12);
        Check(hostile && hostile.deathClip != null && !caster.enabled && !caster.castingCue.activeSelf && Object.FindObjectsByType<MeatPickup>().Length == beforeDrop + 1, "caster plays death clip with one drop and casting stopped");
        yield return Pic("caster-death");
        yield return new WaitForSeconds(hostile.deathClip.length);
        Check(!hostile, "caster removed after clip");
        var monster = Object.FindAnyObjectByType<MonsterAI>();
        yield return Approach(monster.GetComponent<Creature>(), 5);
        health = p.CurrentHealth;
        for (int f = 0; f < 220 && p.CurrentHealth == health; f++)
            yield return null;
        Check(p.CurrentHealth < health, "melee monster detected, pursued and hit");
        var boss = Object.FindAnyObjectByType<BossAI>();
        int rifle = trade.FindSlot(trade.dealer.stock[2].weapon);
        yield return Press((Key)((int)Key.Digit1 + rifle));
        MouseButtons(true, false);
        yield return Frames(9);
        Check(gun.IsAiming, "scoped before lethal encounter");
        yield return Approach(boss.GetComponent<Creature>(), 3);
        Face(boss.transform.position + Vector3.up);
        for (int f = 0; f < 480 && !p.IsDead; f++)
            yield return null;
        Check(p.IsDead && !gun.enabled && !gun.IsAiming && Mathf.Abs(cam.fieldOfView - normal) < .1f, "hostile damage triggered player death");
        yield return Frames(15);
        yield return Pic("player-death");
        yield return Frames(35);
        Check(!p.IsDead && p.CurrentHealth == p.MaxHealth && gun.enabled && gun.playerCamera.GetComponent<FirstPersonLook>().enabled, "respawn restored health, weapons and look");
        MouseButtons(false, false);
        var prey = Object.FindObjectsByType<CreatureAI>().First().GetComponent<Creature>();
        yield return Approach(prey, 5);
        Face(prey.transform.position + Vector3.up);
        yield return Frames(2);
        int loot = Object.FindObjectsByType<MeatPickup>().Length;
        prey.TakeDamage(10000f, cam.transform.forward);
        prey.TakeDamage(10000f, cam.transform.forward);
        yield return Frames(7);
        Check(prey && !prey.GetComponent<CreatureAI>().enabled && Object.FindObjectsByType<MeatPickup>().Length == loot + 1, "one loot drop, AI disabled during visible fall");
        yield return Pic("mob-death");
        yield return Frames(40);
        Check(!prey, "corpse finished transition");
        var water = Object.FindAnyObjectByType<WaterDeathZone>().GetComponent<BoxCollider>().bounds;
        Stage(water.center.x, water.center.z, water.max.y - .6f);
        for (int f = 0; f < 24 && !p.IsDead; f++)
            yield return null;
        Check(p.IsDead, "water volume triggered player death");
        yield return Frames(50);
        Check(!p.IsDead && gun.enabled && p.CurrentHealth == p.MaxHealth, "drowning returned control at the beach");
    }

    private static Vector3 altarPosition() => Object.FindAnyObjectByType<AltarOffering>().transform.position + Vector3.up * 3f;
}
#endif
