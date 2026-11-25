#if HARVEST_PLAYTEST
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public sealed partial class HarvestReview
{
    static IEnumerator ApproachHostile(string kind)
    {
        Vector2 arrival = kind == "orange" ? new Vector2(585,363) :
            kind == "red" ? new Vector2(668,460) : new Vector2(828,604);
        foreach (var point in (BeachTrail + "," + SaddleTrail).Split(',').Select(ParsePoint))
        {
            yield return Walk(point, .85f);
            if (point == arrival) yield break;
        }
    }

    static Creature PlacedHostile(string kind)
    {
        string id = kind == "orange" ? "Monster37_01" : kind == "red" ? "Monster38_01" : "Monster39_01";
        return Object.FindObjectsByType<Creature>().Single(c => c.name == id);
    }

    static bool Cue(Creature enemy)
    {
        if (enemy.TryGetComponent(out MonsterAI melee)) return melee.WindingUp;
        if (enemy.TryGetComponent(out FireShooterAI caster)) return caster.IsMarking;
        return enemy.GetComponent<BossAI>().WindingUp;
    }

    static IEnumerator EncounterPressure()
    {
        string[] parts=routeMode.Split('-');
        string kind=parts[1];
        bool dodge=parts[2]=="dodge";
        var enemy=PlacedHostile(kind);
        var target=enemy.GetComponent<CharacterController>();
        bool cover=parts[2]=="cover";
        bool film=kind=="red" && parts[2]=="ignore";
        if (film)
        {
            Face(gun.playerCamera.transform.position+new Vector3(0,50,20));
            yield return ShootDownTo(2);
        }
        yield return ApproachHostile(kind);
        for (int i=0;i<380 && !Cue(enemy);i++)
        {
            Face(target.bounds.center);
            float range=Vector2.Distance(XZ(p.transform.position),XZ(enemy.transform.position));
            if (range>(kind=="red" ? 19 : kind=="giant" ? 32 : 13)) Keys(Key.W);
            else Keys();
            yield return null;
        }
        Keys();
        Check(Cue(enemy) && !p.IsDead,kind+" committed on the placed route");
        Face(target.bounds.center);
        var hp=p.CurrentHealth;
        var start=p.transform.position;
        if (cover)
        {
            yield return CoverCases(enemy.GetComponent<FireShooterAI>());
            yield break;
        }
        if (film)
        {
            p.StartCoroutine(CaptureClip("reload",86,"PressureCapture"));
            MouseButtons(false,true);
            for (int i=0;i<21 && gun.LoadedAmmo>0;i++) { Face(target.bounds.center); yield return null; }
            MouseButtons(false,false); yield return Frames(2);
            AmmoCounts(0,120,"magazine emptied with caster active");
            Check(Hp(enemy)>0,"marked caster survived the last round");
            yield return Frames(7);
            yield return Press(Key.R);
            while (gun.IsReloading) { Face(target.bounds.center); yield return null; }
            yield return Frames(2);
            AmmoCounts(30,90,"reload under caster pressure");
            Face(target.bounds.center);
            yield return ShootDownTo(29);
            yield return Frames(93);
        }
        else
        {
            if (dodge) Keys(Key.D);
            else if (kind=="orange") Keys(Key.S);
            yield return Frames(dodge ? 29 : 48);
            Keys();
            if (dodge) yield return Frames(12);
        }
        Debug.Log($"ENCOUNTER {kind} dodge={dodge} hp {hp}->{p.CurrentHealth}, player {start}->{p.transform.position}, enemy {enemy.transform.position}");
        Check(dodge ? p.CurrentHealth==hp : p.CurrentHealth<hp,kind+(dodge?" cue sidestepped":" ignored attack cost health"));
        if (dodge && kind!="red")
        {
            bool recovery=kind=="orange" ? enemy.GetComponent<MonsterAI>().Recovering : enemy.GetComponent<BossAI>().Recovering;
            Check(recovery,kind+" committed miss left a recovery window");
        }
        yield return Pic(routeMode);
    }

    static IEnumerator CoverCases(FireShooterAI caster)
    {
        // Put a physical barrier on this natural shot line at controlled points in the cast.
        var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var capsule = p.GetComponent<CharacterController>();
        var muzzle = caster.fireballSpawn;
        Vector3 target = capsule.bounds.center;
        barrier.transform.position = Vector3.Lerp(muzzle.position, target, .69f);
        barrier.transform.rotation = Quaternion.LookRotation(target - muzzle.position);
        barrier.transform.localScale = new Vector3(3, 4, .25f);
        Physics.SyncTransforms();
        var health = p.CurrentHealth;
        yield return Frames(43);
        Check(!caster.IsMarking && !Object.FindAnyObjectByType<EnemyFireballHit>() && p.CurrentHealth == health,
            "cover interrupted the marked cast before release");

        barrier.SetActive(false);
        EnemyFireballHit shot = null;
        for (int i=0;i<160 && !shot;i++)
        {
            if (caster.IsMarking) Face(caster.transform.position + Vector3.up);
            yield return null;
            shot=Object.FindAnyObjectByType<EnemyFireballHit>();
        }
        Check(shot, "caster released a real fireball in the clear lane");
        var direction = shot.GetComponent<Rigidbody>().linearVelocity.normalized;
        float range = Vector3.Distance(shot.transform.position, target);
        Check(range > 1f, "projectile has remaining travel to the player");
        barrier.transform.position = shot.transform.position + direction * (range * .5f);
        barrier.transform.rotation = Quaternion.LookRotation(direction);
        barrier.SetActive(true);
        Physics.SyncTransforms();
        yield return Frames(15);
        Check(!shot && p.CurrentHealth == health, "solid cover stopped the in-flight fireball");
        Object.Destroy(barrier);
    }

    static IEnumerator PassingHostile(string kind)
    {
        var enemy = PlacedHostile(kind);
        var target = enemy.GetComponent<CharacterController>();
        yield return ApproachHostile(kind);
        var delta = XZ(p.transform.position)-XZ(enemy.transform.position);
        if (kind=="giant")
        {
            for (int i=0;i<120 && Vector2.Distance(XZ(p.transform.position),XZ(enemy.transform.position))>14;i++)
                yield return null;
        }
        else if (delta.magnitude>10) yield return Walk(XZ(enemy.transform.position)+delta.normalized*9,1.4f);
        foreach (bool sprint in new[] {false,true})
        {
            var line = target.bounds.center;
            if (sprint) line += Vector3.Cross(Vector3.up,(line-p.transform.position).normalized) * (kind=="giant" ? 4f : 1f);
            Face(line);
            var health = p.CurrentHealth;
            var playerStart = p.transform.position;
            var enemyStart = enemy.transform.position;
            float start = Time.time;
            if (sprint) Keys(Key.W,Key.LeftShift);
            else { Keys(Key.S); MouseButtons(false,true); }
            yield return Frames(sprint ? 48 : 27);
            Keys(); MouseButtons(false,false);
            Debug.Log($"PASSING {kind} sprint={sprint} duration={Time.time-start:F2} player={playerStart}->{p.transform.position} enemy={enemyStart}->{enemy.transform.position} hp={health}->{p.CurrentHealth} ammo={gun.LoadedAmmo}");
        }
        var before=p.transform.position;
        yield return Press(Key.R);
        Check(Vector3.Distance(before,p.transform.position)<2 && !p.IsDead,"R has no respawn effect on island");
    }
}
#endif
