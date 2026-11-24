#if HARVEST_PLAYTEST
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public sealed partial class HarvestReview
{
    static IEnumerator Fight(Creature enemy)
    {
        var controller = enemy.GetComponent<CharacterController>();
        var at = controller.bounds.center;
        var id = enemy.name;
        var oldDrops = Object.FindObjectsByType<MeatPickup>().ToList();
        Debug.Log("TRAIL fight " + id + " at " + at + " from " + p.transform.position);
        yield return Walk(XZ(at) + (XZ(p.transform.position) - XZ(at)).normalized * 7f, 1f);
        Face(controller.bounds.center);
        yield return Pic(routeMode + "-" + id + "-approach");
        var hp = p.CurrentHealth;
        for (int i = 0; i < 240 && p.CurrentHealth == hp; i++)
        {
            Face(controller.bounds.center);
            yield return null;
        }

        Check(hp > p.CurrentHealth, "standing in reach cost " + (hp - p.CurrentHealth) + " health against " + id);
        var retreat = XZ(p.transform.position) - XZ(controller.bounds.center);
        if (retreat.sqrMagnitude < .01f)
            retreat = Vector2.left;
        if (Vector2.Distance(XZ(p.transform.position), XZ(controller.bounds.center)) < 4.5f)
            yield return Walk(XZ(p.transform.position) + retreat.normalized * 10f, 1.2f);
        int fired = 0, detours = 0;
        float lastHealth = Hp(enemy);
        while (enemy && Hp(enemy) > 0 && fired++ < 95)
        {
            bool missedVolley = fired % 9 == 0 && Hp(enemy) >= lastHealth;
            if (!Sight(enemy, gun.playerCamera.transform.position) || missedVolley)
            {
                if (++detours > 18)
                    throw new Exception("No firing lane to " + id);
                var center = XZ(controller.bounds.center);
                var away = (XZ(p.transform.position) - center).normalized;
                var offset = Quaternion.Euler(0, 55f, 0) * new Vector3(away.x, 0, away.y) * 8f;
                yield return Walk(center + XZ(offset), 1.25f);
            }

            if (fired % 9 == 0)
                lastHealth = Hp(enemy);
            Face(controller.bounds.center);
            MouseButtons(false, true);
            yield return Frames(3);
            MouseButtons(false, false);
            yield return Frames(2);
        }

        Debug.Log("TRAIL gunfight " + id + " hp=" + (enemy ? Hp(enemy) : 0) + " ammo=" + gun.LoadedAmmo);
        Check(!enemy || Hp(enemy) <= 0, "weapon defeated " + id);
        var colliders = enemy ? enemy.GetComponentsInChildren<Collider>(true) : Array.Empty<Collider>();
        Check(enemy && !controller.enabled && enemy.GetComponents<MonoBehaviour>().Where(a => a is MonsterAI || a is FireShooterAI).All(a => !a.enabled), id + " movement, attacks and contact disabled at lethal shot");
        yield return Frames(14);
        Check(enemy && Vector3.Angle(enemy.transform.up, Vector3.up) > 65f, id + " visibly fell before cleanup");
        yield return Pic(routeMode + "-" + id + "-death");
        yield return new WaitForSeconds(1.25f);
        var drops = Object.FindObjectsByType<MeatPickup>().Where(m => !oldDrops.Contains(m)).ToArray();
        Check(!enemy && colliders.All(c => !c || !c.enabled) && drops.Length == 1, id + " body and colliders gone, one drop after gun kill");
        var drop = drops[0];
        yield return Walk(XZ(drop.transform.position) + (XZ(p.transform.position) - XZ(drop.transform.position)).normalized * 1.45f);
        yield return Target(drop);
        int carried = trade.Meat;
        yield return Press(Key.E);
        Check(!drop && trade.Meat == carried + 5, "five meat collected after " + id + " cleanup");
    }

    static IEnumerator IslandTrail()
    {
        var boss = Object.FindAnyObjectByType<BossAI>();
        var melee = Object.FindObjectsByType<MonsterAI>().Single(m => m.name == "Monster37_01");
        var caster = Object.FindObjectsByType<FireShooterAI>().Single(c => c.name == "Monster38_01");
        var bossStart = boss.transform.position;
        foreach (var wp in BeachTrail.Split(',').Select(ParsePoint))
        {
            yield return Walk(wp);
            if (wp == new Vector2(369, 244))
            {
                var guard = Object.FindObjectsByType<FireShooterAI>().Single(c => c.name == "Shelter sentinel");
                yield return Walk(new Vector2(386, 250), 1f);
                Face(guard.transform.position + Vector3.up);
                yield return Pic("trapper-scout");
                int meat = trade.Meat;
                yield return Fight(guard.GetComponent<Creature>());
                yield return VisitCache("trapper", wp);
                Check(trade.Meat == meat + 9, "trapper rations award exactly four meat");
            }

            if (wp == new Vector2(585, 363))
                yield return Fight(melee.GetComponent<Creature>());
            if (wp == new Vector2(668, 460))
                yield return Fight(caster.GetComponent<Creature>());
        }

        foreach (var wp in SaddleTrail.Split(',').Select(ParsePoint))
        {
            yield return Walk(wp);
            if (wp == new Vector2(828, 604))
            {
                Debug.Log($"TRAIL boss on approach spawn={bossStart} actual={boss.transform.position} trail={p.transform.position} engaged={boss.IsEngaged}");
                Face(boss.transform.position + Vector3.up * 5);
                yield return Pic(routeMode + "-boss-trail");
                yield return TestGiant(boss);
            }

        }

        yield return LoadAndReturn();
    }

    static IEnumerator LoadAndReturn()
    {
        var slot = SaveGameService.ActiveSave.id;
        var meat = trade.Meat;
        yield return Press(Key.Escape);
        var pause = Object.FindAnyObjectByType<PauseMenuController>();
        var menuButton = (UnityEngine.UI.Button)typeof(PauseMenuController).GetField("mainMenuButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(pause);
        yield return ButtonClick(menuButton);
        yield return Frames(16);
        var menu = Object.FindAnyObjectByType<NewGameMenuController>();
        yield return ButtonClick(menu.loadGameButton);
        yield return ButtonClick(menu.saveListRoot.Find("Save_" + slot).GetComponent<UnityEngine.UI.Button>());
        yield return ButtonClick(menu.playSelectedSaveButton);
        yield return Frames(50);
        Player();
        var cache = Object.FindObjectsByType<PowerupPickup>(FindObjectsInactive.Include).Single(r => r.cacheId == "trapper");
        Check(!cache.gameObject.activeSelf && trade.Meat == meat, "shelter reward and balance unchanged through Load Game");
        var giant = Object.FindAnyObjectByType<BossAI>();
        foreach (var wp in BeachTrail.Split(',').Select(ParsePoint))
            yield return Walk(wp);
        foreach (var wp in SaddleTrail.Split(',').Select(ParsePoint))
        {
            yield return Walk(wp);
            if (wp != new Vector2(828, 604))
                continue;
            var hp = p.CurrentHealth;
            for (int i = 0; i < 460 && p.CurrentHealth == hp; i++)
            {
                Face(giant.transform.position + Vector3.up * 4);
                yield return null;
            }

            Check(giant.IsEngaged && p.CurrentHealth < hp, "loaded giant detected, pursued and hit on the saddle approach");
            yield break;
        }
    }

    static IEnumerator TestGiant(BossAI boss)
    {
        yield return Walk(new Vector2(815, 604), 1.5f);
        var hp = p.CurrentHealth;
        for (int i = 0; i < 460 && p.CurrentHealth == hp; i++)
        {
            Face(boss.transform.position + Vector3.up * 4);
            yield return null;
        }

        Debug.Log("TRAIL giant distance=" + Vector3.Distance(p.transform.position, boss.transform.position) + " pos=" + boss.transform.position + " engaged=" + boss.IsEngaged + " hp=" + p.CurrentHealth);
        Check(boss.IsEngaged && p.CurrentHealth < hp, "giant acquired and damaged player from walking approach");
        Face(boss.transform.position + Vector3.up * 4);
        yield return Pic("giant-swing");
        foreach (var wp in new[]
        {
            new Vector2(828, 604),
            new Vector2(842, 586),
            new Vector2(872, 556),
            new Vector2(884, 536),
            new Vector2(884, 490),
            new Vector2(900, 460)
        })
            yield return Walk(wp, 1.7f);
        Check(!boss.IsEngaged, "outdistanced giant on trail");
        foreach (var wp in new[]
        {
            new Vector2(884, 490),
            new Vector2(884, 536),
            new Vector2(872, 556),
            new Vector2(842, 586),
            new Vector2(828, 604)
        })
        {
            yield return Walk(wp, 1.7f);
            if (boss.IsEngaged)
                break;
        }

        Check(boss.IsEngaged, "giant reacquired returning player");
    }
}
#endif
