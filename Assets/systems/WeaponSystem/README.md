# Weapon System

Handles everything the player can carry and attack with: the hotbar inventory, picking up and dropping items, the model shown in the player's hand, and firing projectiles.

## Overview

```
 World pickup ──E──▶ WeaponInventory (hotbar slots) ──selected slot──▶ WeaponSystem ──click──▶ Projectile
 (WeaponPickup)  ◀──Q── drop                                            │ shows model in hand
                                                                        ▼
                                                   InventoryHud / WeaponHud (UI, read-only)
```

- **Data assets** (`WeaponData`, `InventoryItemData`) describe a *type* of item: its stats, model and icon. There is one asset per weapon, and every copy of that weapon shares it.
- **`WeaponInstance`** is one *copy* the player is carrying. It points at the data asset and stores per-copy state such as remaining ammo.
- **`WeaponInventory`** holds the instances in hotbar slots and tracks which slot is selected.
- **`WeaponSystem`** reads input and does the work: pick up, drop, select a slot, show the hand model, and fire.
- The **HUD scripts** (in `Assets/UI/hud/`) only *read* this state; they never change it.

## Folder layout

| Folder | Script | What it does |
|---|---|---|
| `Core/` | `WeaponSystem.cs` | Player component. Input, pickup/drop, hand model, firing. |
| `Core/` | `MeleeSwing.cs` | Swing animation done in code for melee weapons (used by `WeaponSystem`). |
| `Core/` | `WeaponAudio.cs` | Player component (added automatically). Plays weapon sounds through the SFX mixer group. |
| `Core/` | `SoundEffect.cs` | One sound slot: random clip from a list, volume, pitch variation. |
| `Core/` | `WeaponData.cs` | Weapon asset: type, damage, cooldown, ammo, projectile, hand pose. Also the `WeaponUseType` / `WeaponSpawnType` enums. |
| `Inventory/` | `InventoryItemData.cs` | Base asset for any carryable item: name, model prefab, HUD icon. `WeaponData` extends it. |
| `Inventory/` | `WeaponInventory.cs` | Player component. Hotbar slots (max 9), selection, add/remove. |
| `Inventory/` | `WeaponInstance.cs` | One carried item plus its ammo. |
| `Interaction/` | `IInteractable.cs` | Interface for anything the player can look at and press E on. |
| `Unarmed/` | `FistsController.cs` | First-person fists shown with an empty slot; alternating punches. |
| `Pickups/` | `WeaponPickup.cs` | Put on a world object to make it pick-up-able. |
| `Pickups/` | `AmmoPickup.cs` | One-use ammo box: refills the selected weapon, then disappears. |
| `Projectiles/` | `Projectile.cs` | Stores the damage a projectile deals. |
| `Projectiles/` | `ProjectileHit.cs` | Damages the `Creature` it hits, then destroys itself; also self-destructs after `lifetime`. |
| `BowArrow/`, `Guns/`, `Staff/` | (empty) | Reserved for weapon-type-specific scripts. |

Related scripts outside this folder:
- `Assets/UI/hud/InventoryHud.cs` and `InventorySlotView.cs`: the hotbar UI.
- `Assets/UI/hud/WeaponHud.cs`: the reticle and ammo counter.
- `Assets/UI/hud/InteractPromptHud.cs`: the "Press E to ..." prompt (`Canvas ▸ Interact Prompt`).
- `Assets/UI/hud/InventoryIconRenderer.cs` and `Editor/InventoryIconBaker.cs`: icon generation.
- Weapon assets and models: `Assets/weapons/`.

## Controls

| Input | Action |
|---|---|
| Look at something + **E** | Interact: pick up a weapon (it goes into the first empty slot and is selected), use an ammo box, and so on. |
| **Q** | Drop the selected item in front of you (it keeps its ammo). |
| **1 – 9** | Select that slot. |
| **Mouse wheel** | Previous / next slot (wraps around). |
| **Left click** | Use the selected weapon. |

These keys are in `WeaponSystem.HandleKeyboard` / `HandleMouse`.

## How to edit

### Add a new weapon
1. **Project window ▸ Create ▸ Weapons ▸ Weapon Data.** Keep it next to the weapon's model in `Assets/weapons/...`.
2. Fill in the fields:
   - **Item Name**: the display name.
   - **Item Prefab**: the model used in the hand, for dropped copies, and for the icon.
   - **Use Type**: `Gun`, `Bow` and `Staff` fire projectiles. `Melee` swings (see [Melee weapons](#melee-weapons)).
   - **Spawn Type**: which spawn point on the player the projectile leaves from (`Arrow`, `Bullet` or `Magic`).
   - **Damage, Cooldown** (seconds between shots), **Max Ammo**.
   - **Projectile Prefab** and **Projectile Speed**. The projectile prefab needs a `Rigidbody`, `Projectile` and `ProjectileHit`.
   - **Equipped Position / Rotation / Scale**: how the model sits in the hand. Position is in meters and rotation is in degrees. These update live while you hold the weapon in Play Mode, and because they're on the asset, your changes are kept when you exit.
3. Optional: assign an **Icon** sprite (see [Inventory icons](#inventory-icons)).
4. Put the weapon in the world (next section).

### Melee weapons
Set **Use Type = Melee**. Left click plays a swing in code (`Core/MeleeSwing.cs`): a wind-up, a fast slash across the screen, then a return. No Animator or animation clips are needed. At the hit moment, every `Creature` in front of the camera within reach takes **Damage** (× the player's damage amplifier), once per swing. Melee weapons don't use ammo, so leave **Max Ammo** at 0.

Tune each weapon under **Melee** on its WeaponData:

| Field | Effect |
|---|---|
| Melee Range / Melee Radius | How far and how wide the hit area is. |
| Swing Duration | Length of the whole swing in seconds. A swing can't start until the previous one ends; **Cooldown** adds extra wait if it's longer. |
| Swing Hit Time | When damage lands, as a fraction of the swing (0.4 = mid-slash). |
| Swing Angle / Swing Tilt | Width of the arc, and how diagonal it is (0 = flat). |
| Swing Reach | How far the model lunges forward mid-slash. |

Tip: you can tune these while in Play Mode. They're on the WeaponData asset, so your changes are kept when you exit, unlike changes to scene objects.

### Fists (unarmed)
When the selected slot holds no weapon, the player's fists show (`MainCamera ▸ Fists`). Left click punches, alternating right and left. Each punch damages creatures in front of the player, the same way a melee weapon does.

- **Rest pose:** `Right Fist` and `Left Fist` are empty pivots marking where the hands rest. Move them to change how the arms sit; the punch animates them from there.
- **Feel:** the **FistsController** inspector on `Fists` has:
  - **Damage:** Damage, Range, Radius.
  - **Timing:** Punch Interval, Punch Duration, Hit Time.
  - **Animation:** Reach, Inward, Lift, Twist.
- The code is in `Unarmed/FistsController.cs`. `WeaponSystem` calls it when you attack with an empty slot, through its **Fists** field.

**First-person arms** (`Unarmed/FirstPersonArmsRig.cs`, on `Fists`). When the game starts, a copy of the player's model is placed under the camera with everything except its arms hidden. Each frame its arms are posed to reach the fist pivots: they bend at the elbow when the fist is close, and stretch like rubber arms when it's farther, up to **Max Stretch**. Because the copy moves with the camera, looking around and jumping can't make it clip. Tune it in the **FirstPersonArmsRig** inspector:
- **Shoulder Center:** where the arms come from, relative to the camera. It updates live in Play Mode.
- **Elbow Direction:** which way the elbows bend.
- **Allow Stretch / Max Stretch:** how stretchy the arms are.
- **Hand Roll:** turns the fists.
- **Model Scale:** the size of the copy.

Raise **Reach** on FistsController for longer, stretchier punches. **Range** there sets how far a punch actually hits.

**The real body is never drawn for the camera.** All its renderers are *Shadows Only*, so it can't clip, and your shadow shows the full body, head included. `FirstPersonArmsIK.cs`, on the player, still moves the real body's hands to the fists, so the shadow punches too (up to arm's length). The setup depends on:
- **IK Pass** ticked on `PlayerAnimator`'s Base Layer. Without it, the shadow's hands won't move.
- The Animator's **Culling Mode** set to *Always Animate*.
- **Player scale ×1.4.** The model is only about 1.3 m tall, so it's scaled ×1.4 on `Mercenary_Swordsman` to put its eyes at camera height. The camera (scale 0.714, position adjusted) and CharacterController (height, radius, center, step, skin ÷1.4) are counter-scaled, so camera height, collision size and movement are unchanged. To change the scale, adjust all three together.
- **All body renderers** (Body, Head, Hair, Eyebrow, Scarf, Shoulder Guard) set to *Shadows Only*.
- **Camera Near Clip Plane** set to 0.05, so close-up hands aren't sliced.

**FirstPersonArmsIK** has the shadow's hand rotation, elbow bend and blend time.

### Sound effects
All weapon sounds play through `WeaponAudio` on the player, which sends them to the **SFX** mixer group. The **SFX** and **Master** sliders in Settings control them, and they pause with the game. Each sound slot takes a list of clips, and one is picked at random each time. Leave the list empty for silence.
- **Weapons** (WeaponData asset ▸ *Sounds*):
  - **Equip:** plays when the weapon is selected.
  - **Use:** plays on each attack: gunshot, bow release, staff cast or sword swing.
  - **Hit:** melee only; plays where the swing lands on a creature.
  - **Empty:** plays when attacking with no ammo.
  - **Reload:** plays when an ammo box refills the weapon.
- **Fists** (`Fists` object ▸ FistsController ▸ *Sounds*): **Punch** plays on every punch, and **Punch Hit** plays when a punch connects.
- **Sounds from other scripts:** call `weaponSystem.Audio.Play(sound)` for a sound on the player, or `weaponSystem.Audio.PlayAt(sound, position)` for a sound in the world.

### Place a weapon pickup in a level
1. Drag the weapon's model into the scene.
2. Add a **Collider** (the player's look raycast needs one) and, optionally, a **Rigidbody**.
3. Add **WeaponPickup** and assign **Item Data** (or **Weapon Data**) to the weapon's asset.
4. **Loaded Ammo**: `-1` means full; any other number is the starting ammo.

> Use only one `WeaponPickup` per object.

### Place an ammo box (reload)
1. Create any object with a **Collider**, for example **GameObject ▸ 3D Object ▸ Cube**.
2. Add **AmmoPickup**.

Looking at it shows "Press E to reload &lt;weapon&gt;". Pressing E refills the *selected* weapon to **Max Ammo** and the box disappears. If the player isn't holding a weapon, or the weapon is already full, the prompt says so and the box stays. You can change those two messages in the AmmoPickup inspector. There's an example, `Ammo Box (Reload)`, in `N_Testing`.

### Make something new interactable
Write a component that implements `IInteractable` (see `AmmoPickup.cs` for a short example) and put it on an object with a Collider:
- `GetPrompt` returns the action text, e.g. `"open chest"`. The HUD turns it into "Press E to open chest".
- `CanInteract` returns false to block the action. The prompt then shows `GetPrompt`'s text as-is in the warning colour, so return the reason, e.g. `"Locked"`.
- `Interact` does the work.

`WeaponSystem` finds the object with its look raycast (**Pickup Range**). `InteractPromptHud` shows the prompt, so no other wiring is needed.

### Style the interact prompt
It's at `Canvas ▸ Interact Prompt ▸ Prompt Panel ▸ Prompt Text`:
- **Panel and text:** edit them freely. The panel resizes to fit the text.
- **Wording and colours:** set **Prompt Format** (`{0}` is replaced by the action), **Available Color** and **Blocked Color** on **InteractPromptHud**.
- **In the editor:** the panel stays visible so you can style it. In Play Mode it's hidden until there's something to interact with.

### Change player settings
On the player object (`Mercenary_Swordsman`):
- **WeaponInventory ▸ Capacity**: number of hotbar slots (0–9). The HUD shows and hides slots to match.
- **WeaponSystem**:
  - **Hand**: parent for the equipped model.
  - **Player Camera**: used for aiming and for finding pickups.
  - **Arrow / Bullet / Magic Spawn**: muzzle points.
  - **Pickup Range** and **Drop Distance**.

### Inventory icons
- Set **Icon** on the item asset to use your own sprite.
- Leave it empty and the model is rendered to an icon automatically at runtime. It's rendered once per item type and cached; the first pickup may cause a small one-frame hitch.
- To bake an icon you can edit, right-click the item asset's component header ▸ **Bake Icon From Prefab**. Or select several item assets ▸ **Assets ▸ Inventory ▸ Bake Item Icons**. This saves `<Item>_Icon.png` next to the asset and assigns it. Baked icons cost nothing at runtime.
- **Icon Rotation** on the item sets the camera angle used for generated and baked icons.

### Style the hotbar
- The HUD lives in the scene under `Inventory HUD Canvas ▸ Inventory Slots`. Every child with an `InventorySlotView` is a slot, in hierarchy order.
- **Slots:** edit the slot objects directly (sprites, size, fonts, extra decoration). Duplicate a styled slot to reuse its look.
- **Selected / normal colours:** set these on **InventoryHud** (on the player). Turn off **Tint Backgrounds** to keep each slot's own `Image` colour.
- **Highlights:** `InventorySlotView` has optional **Selected Indicator** and **Empty Indicator** objects for borders, glows or placeholder art.
- **Preview in edit mode:** set **InventoryHud ▸ Preview Item / Preview Slot Index**.

## Rules to keep it maintainable
- **Don't rename** the scripts, their classes, or their serialized fields. Scenes, prefabs and assets reference them by name and GUID; renaming silently breaks those links. If you must rename a field, add `[FormerlySerializedAs("oldName")]`.
- **Move** scripts inside Unity (or move the `.meta` file with them).
- **UI reads, gameplay writes.** HUD scripts should only read from `WeaponSystem` / `WeaponInventory`. Put gameplay changes in the weapon system.
- Ammo belongs to the `WeaponInstance`, not the `WeaponData` asset. Changing an asset at runtime would change it for every copy and persist in the editor.

## Known gaps / TODO
- Melee hits aren't blocked by walls: a creature right behind thin cover within reach can still be hit.
- The player's body doesn't animate when attacking. Full-body swings are available in `Assets/mobs/Animations/DoubleL/` (`OneHand_Up_Attack_*`) if the body ever becomes visible.
- Reloading only happens through one-use ammo boxes. There is no reload key or spare-ammo count yet.
- The interact key is hard-coded to **E** in `WeaponSystem.HandleKeyboard`, and the prompt text says "E" in **Prompt Format**. Change both if the key changes.

## Notes
- Weapons now use the shared **Item Name / Item Prefab** fields. The old `weaponName` / `weaponPrefab` values move over automatically when an asset is loaded in the editor (`WeaponData.OnValidate`). Assets from other branches upgrade themselves the same way, so save them once afterwards (**File ▸ Save Project**).
