# Powerups

Timed boosts the player collects by walking into them (for example, faster movement for a few seconds).

| Script | What it does |
|---|---|
| `PowerupData.cs` | Powerup asset: name, icon, color, duration, stat modifiers, collect/expire sounds. Also the `PowerupStat` enum. |
| `PlayerPowerups.cs` | On the player (added automatically). Tracks active powerups and timers; `GetMultiplier(stat)`. |
| `PowerupPickup.cs` | World pickup. Trigger collider, spins/bobs, optional respawn. |
| `Editor/PowerupExamplesMenu.cs` | **Tools ▸ Powerups ▸ Create Example Powerups** makes Speed Boost and High Jump in `Data/`. |

## Make a new powerup
1. Create the asset: **Create ▸ Powerups ▸ Powerup Data**.
2. Set its **Duration** and add **Modifiers**, for example Move Speed ×1.5. A multiplier of 1 means no change, and 2 means double. One powerup can boost several stats.
3. Put a model in the scene, add **PowerupPickup**, and assign the asset. It needs a collider; the script makes it a trigger, or adds a sphere collider if there is none.
4. **Respawn Time** 0 means the pickup works once. Any other value brings it back after that many seconds.

## Rules
- **Same powerup again:** collecting a powerup that's already running restarts its timer. It doesn't stack.
- **Different powerups:** they stack by multiplying. Two ×1.5 speed boosts make ×2.25.
- **Pausing:** timers pause with the game.
- **Dying:** clears every active powerup.
- **Sounds:** they play through the **SFX** volume slider.

## Stats and where they're applied
| Stat | Applied in | Notes |
|---|---|---|
| `MoveSpeed` | `PlayerMovement` | Multiplies walk and sprint speed. |
| `JumpHeight` | `PlayerMovement` | Multiplies jump *height*: ×2 jumps twice as high. |
| `Damage` | `WeaponSystem` | Multiplies all weapon and fist damage, on top of the PlayerData damage amplifier. |

## Add a new stat
1. Add it to the `PowerupStat` enum in `PowerupData.cs`. Add it at the **end**: assets store stats as numbers, so inserting in the middle changes existing assets.
2. In the script that uses the stat, multiply by `PlayerPowerups.For(gameObject).GetMultiplier(PowerupStat.YourStat)`. Cache the `PlayerPowerups` reference in `Start`.

A non-stat effect (like a shield or healing) can watch `PlayerPowerups.Started` and `Ended` and check `data`. For a HUD timer, read `ActivePowerups`: each entry has `data`, `Remaining` and `Fraction`.

## HUD
Active powerups appear to the right of the hotbar: an icon with the time left under it, one box per powerup, side by side.
- **Where it lives:** `Inventory HUD Canvas ▸ Powerup HUD`, driven by `Assets/UI/hud/PowerupHud.cs` and `PowerupIndicator.cs`.
- **Restyling:** edit the `PowerupIndicatorTemplate` child; every indicator is a copy of it. It stays visible in the editor as a preview and hides when the game starts.
- **Icons:** set each powerup's **Icon** sprite. If a powerup has no icon, its box shows the powerup's **Color** with the first letter of its name.
- **Timer:** under 10 s the time shows tenths (4.2s), and under 2 s it turns red. Both thresholds are on the template's `PowerupIndicator`.
