#if HARVEST_PLAYTEST
using System;
using System.Linq;
using UnityEngine;

// Passive telemetry for native keyboard/mouse trail runs. Never changes gameplay state.
public sealed class IslandTrailObserver : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-dh-observe") < 0)
            return;
        DontDestroyOnLoad(new GameObject("Trail observer", typeof(IslandTrailObserver)));
    }

    Player player;
    WeaponSystem gun;
    Creature[] hostiles;
    float next;
    static string Pos(Vector3 v) => FormattableString.Invariant($"{v.x:F2},{v.y:F2},{v.z:F2}");
    void LateUpdate()
    {
        if (Time.unscaledTime < next)
            return;
        next = Time.unscaledTime + .10f;
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
            if (player == null)
                return;
            gun = player.GetComponent<WeaponSystem>();
            hostiles = FindObjectsByType<Creature>().Where(c => c.GetComponent<CreatureAI>() == null).ToArray();
            foreach (var a in FindObjectsByType<Animator>())
                if (a.name.IndexOf("Monster", StringComparison.OrdinalIgnoreCase) >= 0 || a.name.IndexOf("demon", StringComparison.OrdinalIgnoreCase) >= 0)
                    Debug.Log($"MODEL {a.name} {Pos(a.transform.position)} scale={a.transform.lossyScale} creature={a.GetComponentInParent<Creature>()}");
            foreach (var h in hostiles)
                Debug.Log($"SPAWN {h.name} {Pos(h.transform.position)} bounds={h.GetComponentInChildren<Renderer>().bounds} collider={h.GetComponent<CharacterController>().bounds}");
            foreach (var item in FindObjectsByType<WeaponPickup>())
                Debug.Log($"PICK {item.name} {Pos(item.transform.position)}");
        }

        var cam = gun.playerCamera.transform;
        var target = gun.CurrentInteractable as Component;
        var movement = player.GetComponent<PlayerMovement>();
        string actors = string.Join("|", hostiles.Where(h => h).Select(h => h.name + ":" + Pos(h.GetComponent<CharacterController>().enabled ? h.GetComponent<CharacterController>().bounds.center : h.transform.position) + ":" + h.enabled + ":" + h.GetComponent<CharacterController>().enabled + ":" + (h.TryGetComponent(out BossAI b) && b.IsEngaged)));
        var drops = string.Join("|", FindObjectsByType<MeatPickup>().Select(m => Pos(m.transform.position)));
        Debug.Log($"TRAIL {Time.time:F3} {Pos(player.transform.position)} {Pos(cam.position)} {Pos(cam.eulerAngles)} {Pos(movement.GetComponent<CharacterController>().velocity)} {player.CurrentHealth} {gun.LoadedAmmo} {gun.IsAiming} {gun.GetComponent<HarvestTrade>().Meat} {actors} drops={drops} aim={(target ? target.name : "-")}");
    }
}
#endif
