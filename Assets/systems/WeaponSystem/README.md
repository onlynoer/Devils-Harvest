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
| `Core/` | `WeaponData.cs` | Weapon asset: type, damage, cooldown, ammo, projectile, hand pose. Also the `WeaponUseType` / `WeaponSpawnType` enums. |
| `Inventory/` | `InventoryItemData.cs` | Base asset for any carryable item: name, model prefab, HUD icon. `WeaponData` extends it. |
| `Inventory/` | `WeaponInventory.cs` | Player component. Hotbar slots (max 9), selection, add/remove. |
| `Inventory/` | `WeaponInstance.cs` | One carried item plus its ammo. |
| `Pickups/` | `WeaponPickup.cs` | Put on a world object to make it pick-up-able. |
| `Projectiles/` | `Projectile.cs` | Stores the damage a projectile deals. |
| `Projectiles/` | `ProjectileHit.cs` | Damages the `Creature` it hits, then destroys itself; also self-destructs after `lifetime`. |
| `BowArrow/`, `Guns/`, `Staff/` | (empty) | Reserved for weapon-type-specific scripts. |

Related scripts outside this folder:
- `Assets/UI/hud/InventoryHud.cs` and `InventorySlotView.cs`: the hotbar UI.
- `Assets/UI/hud/WeaponHud.cs`: the reticle and ammo counter.
- `Assets/UI/hud/InventoryIconRenderer.cs` and `Editor/InventoryIconBaker.cs`: icon generation.
- Weapon assets and models: `Assets/weapons/`.

## Controls

| Input | Action |
|---|---|
| Look at a pickup + **E** | Pick it up (goes into the first empty slot and is selected). |
| **Q** | Drop the selected item in front of you (it keeps its ammo). |
| **1 – 9** | Select that slot. |
| **Mouse wheel** | Previous / next slot (wraps around). |
| **Left click** | Use the selected weapon. |

These keys are in `WeaponSystem.HandleKeyboard` / `HandleMouse`.

## How to edit

### Add a new weapon
1. **Project window ▸ Create ▸ Weapons ▸ Weapon Data.** Keep it next to the weapon's model in `Assets/weapons/...`.
2. Fill in the fields:
   - **Weapon Name**: the display name.
   - **Weapon Prefab**: the model used in the hand, for dropped copies, and for the icon.
   - **Use Type**: `Gun`, `Bow` and `Staff` fire projectiles. `Melee` does nothing yet.
   - **Spawn Type**: which spawn point on the player the projectile leaves from (`Arrow`, `Bullet` or `Magic`).
   - **Damage, Cooldown** (seconds between shots), **Max Ammo**.
   - **Projectile Prefab** and **Projectile Speed**. The projectile prefab needs a `Rigidbody`, `Projectile` and `ProjectileHit`.
   - **Equipped Rotation / Scale**: how the model sits in the hand. Tweak these in Play Mode, copy the values, then paste them back after you exit Play Mode.
3. Optional: assign an **Icon** sprite (see [Inventory icons](#inventory-icons)).
4. Put the weapon in the world (next section).

### Place a weapon pickup in a level
1. Drag the weapon's model into the scene.
2. Add a **Collider** (the player's look raycast needs one) and, optionally, a **Rigidbody**.
3. Add **WeaponPickup** and assign **Item Data** (or **Weapon Data**) to the weapon's asset.
4. **Loaded Ammo**: `-1` means full; any other number is the starting ammo.

> Use only one `WeaponPickup` per object.

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
- `Melee` weapons don't attack yet (`WeaponSystem.UseWeapon`).
- There is no reloading. Ammo only comes from picking a weapon up.
- The pickup prompt is drawn with `OnGUI` (`WeaponSystem.OnGUI`); it could move to the HUD canvas.
- `WeaponData` keeps its own `weaponName` / `weaponPrefab` fields instead of the base `InventoryItemData` name/prefab fields, so existing assets keep working. For weapons, fill in the weapon fields.
