# Creature System

Handles everything alive that isn't the player: health, damage, death and loot, animal AI (wandering, fleeing, warning, hunting, fighting back), creature attacks, rare "secret" spawn variants, and creature sounds. Monsters and the boss also use `Creature` for health, but they still have their own older AI scripts. See [Monsters and the boss](#monsters-and-the-boss).

## Overview

```
 CreatureData (asset, one per species)
   │  stats, temperament, radii, combat, secrets, sounds, loot
   ▼
 Creature ──────────────── health, damage flash, death, loot, rolls a secret at spawn
   │ Damaged / Died events          │ secret with a weapon?
   ▼                                ▼
 CreatureAI (animals) ◀── uses ── ICreatureAttack ── CreatureMeleeAttack (default bite/charge)
   │ SetInput / SyncFacing                         └─ ArmedCreature     (secret: shoots a real weapon)
   ▼
 CreatureMover (ithappy) ── moves the CharacterController and drives the Vert/State blend tree
```

- **`CreatureData`** describes a *species*. Every deer shares one asset, so changing it changes all deer.
- **`Creature`** is the per-animal component for health and dying. Anything that deals damage calls it, through `IDamageable` or `TakeDamage`.
- **`CreatureAI`** is the animal brain. It decides *what* to do, and `CreatureMover` does the actual walking and animating.
- **Attacks** are swappable `ICreatureAttack` objects. The brain moves the animal into range and then hands control to the attack.
- **`IDamageable`** is shared by `Player` and `Creature`. Creature attacks and hostile projectiles damage anything that implements it.

## Folder layout

| Path | Script | What it does |
|---|---|---|
| `systems/CreatureSystem/` | `CreatureData.cs` | Species asset: stats, combat, behaviour, secrets, audio, loot, damage flash. |
| | `Creature.cs` | Health, defense, damage flash, death and loot drop. Rolls secrets. Raises `Damaged` / `Died` events. |
| | `CreatureAI.cs` | Animal brain (state machine). Needs `Creature` and `CreatureMover`. |
| | `CreatureBehaviourTypes.cs` | `CreatureTemperament` enum and the `ICreatureAttack` interface. |
| | `CreatureMeleeAttack.cs` | Default attack: charge in, hit if still in reach and in front, recover. |
| | `CreatureSecret.cs` | One rare spawn variant (chance, temperament, weapon, shooting settings). |
| | `ArmedCreature.cs` | Added at runtime for secrets with a weapon: shows the gun, shoots bursts, drops it on death. |
| | `IDamageable.cs` | `IDamageable` interface and `DamageInfo` (amount, direction, source). |
| | `CreatureAudio.cs` | Added automatically when the data has a **Call** clip. Calls, footsteps, warnings, hurt and death sounds. |
| | `MonsterAI.cs`, `BossAI.cs`, `FireShooterAI.cs` | Older enemy AI (see below). |
| | `EnemyFireballHit.cs` | Fireball projectile: damages the creature or player it hits. |
| `mobs/animals/_Shared/Scripts/` | `CreatureMover.cs` | Third-party (ithappy) mover, with one addition: `SyncFacing()`. |
| `mobs/animals/<Species>/` | prefabs, animators, `Data/` | Per-species models and assets. |
| `mobs/enemies/` | prefabs, `data/` | Monster and boss models and data. |

## How an animal thinks

`CreatureAI` is a state machine. It checks its surroundings 5 times a second (spread across frames) and acts every frame.

| State | What it does | Leaves when |
|---|---|---|
| **Idle** | Stands still for *Min/Max Idle Time*. | Timer ends → Wander. |
| **Wander** | Walks to a random point within *Wander Radius* of its spawn point. | Arrives or *Min/Max Walk Time* runs out → Idle. |
| **Alert** | Defensive only. Stares at the player and plays the **Warning** sound once. | Player comes within *Aggro Radius* or stays past *Alert Patience* → Engage. Player backs off past 1.25 × *Alert Radius* → Idle. |
| **Engage** | Runs at the target until it's in attack range and visible. Backs away if closer than the attack's minimum range. | Attack ready → Attack. Target too far (*Give Up Distance*) or animal too far from home (*Leash Radius*) → Return. |
| **Attack** | The attack controls the animal (charges, or stands and aims). | The attack finishes → Engage. |
| **Flee** | Sprints away and picks a clear direction every ~0.4 s, avoiding walls, cliffs and water. | At least *Flee Time* has passed and it's *Flee Distance* away → Idle. |
| **Return** | Walks home and ignores the player for *Calm Duration* (unless hurt). | Back near home → Idle. |

### Temperament (set on CreatureData)

| Temperament | Noticing the player | When hurt | Example |
|---|---|---|---|
| **Skittish** | Flees within *Notice Radius* (*Sprint Notice Radius* if the player is sprinting). | Flees. Has a *Retaliate Chance* of fighting back instead. | deer, chicken |
| **Passive** | Ignores them. | Flees, with the same *Retaliate Chance*. | horse, cow |
| **Defensive** | Warns within *Alert Radius*, attacks within *Aggro Radius* or after *Alert Patience* seconds. | Fights back. | bear |
| **Aggressive** | Hunts within *Notice Radius*. | Fights back. | wolf |

Extra rules:
- **Line of sight:** with *Require Line Of Sight* on, Defensive and Aggressive animals only react to a player they can see. Skittish animals "hear" the player and don't need to see them.
- **Losing nerve:** when health falls to *Flee Health Fraction* (0 turns this off), the animal flees and acts Skittish for the rest of its life.
- **Needs an attack:** an animal with no attack (Damage 0 and no weapon) never fights. It flees instead.
- **Groups:** when an animal flees or attacks, others *of the same CreatureData asset* within *Group Alert Radius* join in. A herd scatters together and a pack attacks together. Set it to 0 to turn this off.
- **Unknown attacker:** damage without a source (all player weapons today) is treated as coming from the player.

## Attacks

The brain asks an attack for its range (`MinRange`, `MaxRange`), measured as the **gap between the two bodies' edges**, not center to center. That lets big and small animals use the same numbers.

**Melee (`CreatureMeleeAttack`)** is used automatically when **Damage > 0** and the animal isn't armed:
1. **Start:** fires the Animator trigger named in *Attack Animator Trigger*, if the controller has one, and plays the **Attack** sound.
2. **Charge:** runs at the target until *Attack Hit Delay* has passed.
3. **Hit:** deals **Damage**, but only if the target is still within *Attack Range* + 0.5 m and in front of the animal.
4. **Recover:** waits until *Attack Duration* has passed, then *Attack Cooldown* before the next attack.

> The animal animators (`Chicken`, `Deer`, `Horse`, `Tiger`) only have a walk/run blend tree. They have no attack clip yet, so the charge is the only visible attack. To add one: add an `Attack` **Trigger** parameter, add an attack state with a transition from *Any State* on that trigger and an exit back to the blend tree.

**Custom attacks:** write a class or component that implements `ICreatureAttack`.
- If it's a component on the creature, the brain picks it up instead of the melee attack.
- Set `Lunging` to true while the animal should run at the target. Otherwise the brain keeps it standing and facing the target.
- Call `brain.GapTo(target)`, `brain.IsFacing(target, dot)` and `CreatureAI.AimPoint(target)` for range and aiming checks.

## Secrets (rare variants)

Each `CreatureData` has a **Secrets** list. When a creature spawns, the list is checked top to bottom, and the first secret whose **Chance** roll succeeds is applied. A secret can:
- change the **Temperament** (an armed deer usually becomes Aggressive) and the **Health Multiplier**;
- give the animal a **Weapon** (any `WeaponData`, e.g. the AK47), which adds `ArmedCreature`.

An armed animal:
- **Carries the gun:** it shows the weapon's model on **Mount Bone**, offset by **Mount Offset / Rotation / Scale**. Leave the bone empty to attach it to the root.
- **Keeps its distance:** it closes to **Max Range**, then backs off if the player is closer than **Min Range**.
- **Shoots:** it aims for **Aim Time**, which is the player's warning, then fires **Burst Count** real projectiles from the weapon.
  - Each shot does weapon damage × **Damage Multiplier**, with **Spread Degrees** of aim error.
  - The gap between shots is the weapon cooldown × **Shot Interval Multiplier**.
  - It waits **Burst Cooldown** (random between x and y) between bursts and plays the weapon's **Use** sound.
- **Who its bullets hit:** the player and other creatures, but never the animal that fired them.
- **Death:** with **Drop Weapon On Death** on, it leaves the gun as a normal pickup with a random amount of ammo.

**Testing a secret:** set **Force Secret Index** on a `Creature` in the scene, for example `0` for the first secret, so that animal always rolls it. Set it back to `-1` when you're done. The console log shows the secret's name when an animal spawns with one, e.g. `Deer [Deer With A Gun] spawned with 100 health.`

**Mounting tip:** enter Play Mode with a forced secret, then tweak **Mount Offset / Rotation** on the CreatureData asset. The gun is placed once at spawn, so restart Play Mode to see each change. Your values are kept because they're on the asset.

## How to edit

### Add a new animal (e.g. wolf, bear)
1. **Model:** put it in `Assets/mobs/animals/<Species>_001/`. Use one of the existing animals as a template. It needs:
   - a **CharacterController** sized to the body;
   - an **Animator** whose controller has `Vert` and `State` float parameters (copy `_Shared/Animations/CreatureAnimator.controller` or a species controller and swap the clips);
   - **CreatureMover**: set its Walk / Run / Rotate speeds here;
   - **Creature** and **CreatureAI**. Drag the CreatureMover into **CreatureAI ▸ Mover**, or leave it empty and it finds it.
2. **Project window ▸ Create ▸ Creatures ▸ Creature Data**. Save it in `<Species>_001/Data/` and assign it to **Creature ▸ Creature Data**.
3. Pick a **Temperament** and tune the fields. Starting points:

| Field | Chicken | Wolf | Bear |
|---|---|---|---|
| Temperament | Skittish | Aggressive | Defensive |
| Max Health / Defense | 20 / 0 | 60 / 0 | 250 / 5 |
| Damage | 2 | 10 | 30 |
| Attack Range / Hit Delay / Duration / Cooldown | 0.4 / 0.3 / 0.6 / 1 | 0.8 / 0.35 / 0.8 / 1.2 | 1.2 / 0.6 / 1.4 / 2.5 |
| Notice / Sprint Notice Radius | 6 / 12 | 22 / 30 | 18 / 26 |
| Alert / Aggro Radius / Alert Patience | — | — | 14 / 6 / 4 |
| Give Up Distance / Leash Radius | — | 35 / 60 | 20 / 30 |
| Flee Health Fraction | 0 | 0.25 | 0.15 |
| Retaliate Chance | 0.05 (angry chicken) | — | — |
| Group Alert Radius | 8 | 25 | 0 |

4. Fill in the sounds (below) and loot.
5. Make it a prefab and place it. Its **spawn position is its home**: it wanders around it and returns there.

### Sounds
On **CreatureData ▸ Positional Audio**:

| Clip | When it plays |
|---|---|
| Call | Every so often while idle. **Required:** without a Call clip, `CreatureAudio` isn't added and none of the other sounds play. |
| Step | Footsteps; *Stride* sets the distance between steps. |
| Warning | Entering Alert or Engage, e.g. a growl. |
| Attack | Start of each melee attack. |
| Hurt / Death | When damaged / killed. |

All sounds go through the creature data's **Sound Group**, or the **SFX** mixer group if none is set.

### Per-animal settings (CreatureAI inspector)
- **Min/Max Idle Time, Min/Max Walk Time, Wander Radius:** how the animal roams.
- **Flee Time, Flee Distance:** how long and how far it runs.
- **Min Ground Height:** with a Terrain in the scene, the animal never walks onto ground lower than this, which keeps it out of the water. The default (18.4) matches the island's water level.
- **Seeing the radii:** select the animal in the Scene view. Blue = wander area, yellow = notice radius, orange/red = alert/aggro radius (Defensive only).

### Damage from new sources
- **Hitting anything:** call `GetComponentInParent<IDamageable>()?.ApplyDamage(new DamageInfo(amount, direction, attackerGameObject))`. Passing the attacker lets animals fight back against the right thing.
- **Projectiles:** for a projectile that should hurt the player, call `projectileHit.SetOwner(shooter, shooterColliders)`. Without an owner, `ProjectileHit` keeps its player-weapon behaviour and only damages creatures.
- **Reacting to damage or death:** subscribe to `creature.Damaged` / `creature.Died`.

## Monsters and the boss

These still use their own scripts, which repeat much of the same movement code. They're next in line for the revamp.

| Script | Behaviour |
|---|---|
| `MonsterAI` | Wanders. Becomes aggressive only when hurt (`Provoke`), chases and melees, and gives up past *Lose Target Range*. |
| `BossAI` | Wanders. Chases and melees within *Detection Range* and plays the warning sound. `IsEngaged` silences its idle calls. |
| `FireShooterAI` | Keeps its distance, strafes, and shoots fireballs after a visible wind-up. Casts are spaced out across all shooters. |

They move the CharacterController themselves, use an Animator with `Speed` (float) and `IsAttacking` (bool), and damage the player through `Player.TakeDamage`.

## Rules to keep it maintainable
- **Don't rename** these scripts, their classes or their serialized fields. The Deer prefab and the scenes reference them by name and GUID. If a rename is unavoidable, add `[FormerlySerializedAs("oldName")]`.
- **Species settings go on CreatureData; per-animal settings go on CreatureAI.** Don't change CreatureData at runtime: it's shared by every animal of that species, and in the editor the change stays after you exit Play Mode.
- **The brain decides, the mover moves.** Animals should only move through `CreatureMover.SetInput`. If you rotate an animal yourself while it stands still, call `mover.SyncFacing()` afterwards so the mover doesn't turn it back.
- **New attacks implement `ICreatureAttack`.** Don't add attack code to `CreatureAI`.

## Known gaps / TODO
- **Animations:** animals have no attack, hurt or death clips. Death removes the animal instantly.
- **Targets:** animals only target the player. Predator vs. prey (wolves hunting deer) needs target selection in `CreatureAI`; `IDamageable` and the attacks already support any target.
- **Navigation:** there's no NavMesh. Animals steer with short probes, so they can get stuck in concave obstacles or dense forest.
- **Unknown attackers:** player weapons don't pass a damage source, so an animal hit by a monster's fireball blames the player.
- **Gun mounting:** armed animals have no aiming pose. The gun stays where it's mounted and bullets fly from its front, toward the player.
