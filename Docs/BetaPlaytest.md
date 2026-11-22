# Devil's Harvest playtest handoff

## Current Windows follow-up

This pass starts at `164dcebc31fdea273537d68e898c887cdae8c88b` on the same local branch. Run `Builds/Windows/DevilsHarvest.exe` with the complete Windows folder. The earlier beta handoff below records the starting build.

| Report | Finding and change | Windows evidence |
| --- | --- | --- |
| Sprint | The controller stored the held Shift input in a local variable and applied speed from a different property. The held binding now drives the property used by movement and prey hearing. Aiming disables the sprint bonus. | On the same beach lane, W and W + Left Shift measured 15.01 and 14.99 m/s before the fix. Afterward they measured 15.01 and 22.49 m/s. Release returned to 15.01. With the AK, sprint measured 22.49, then 22.48 after releasing aim and 22.48 after drowning and respawning. |
| Armored giant | Its player reference was valid. The 24 m acquisition check missed the saddle trail, its unbounded patrol could drift, and weapon hits never alerted its AI. Its patrol now stays around the placed spawn, it detects within 56 m, and a hit alerts it. A committed swing has a 0.85 s windup. | Walked in from the grotto with no shot or target assignment. The giant pursued and dealt 39 damage. Retreat broke contact and a return approach restarted pursuit. Repeated natural acquisition and damage after Load Game. |
| Little red demon | The red, two-horned caster played a 4.63 s clip through the outer Animator. The old body did eventually despawn in the reproduction, but still looked upright after a lethal AK shot. The clip now targets the model rig, the body tips immediately, and it clears in about 1.15 s. | Shot the island caster and shelter guard with the beach AK. The movement controller, attack AI and colliders disabled on the lethal hit. Both fell, vanished, and left one collectible five-meat drop. |
| Signs | Kept both Hunting Grounds signs and the Brass Trail, Saddle Trail and Offering Altar signs. Each faces its incoming trail, with lettering raised clear of both faces, a dark board, larger type, and a return destination on the back. | Read all five at player eye height from roughly 8 to 13 m. The quota arrows are still present. [Beach approach screenshot](BeachTrailSign.png) |
| Threat | The orange melee creature closes quickly and commits to a short straight lunge. The red caster marks a target before firing, and the giant commits to its facing before impact. | Standing in reach drew hits of 18, 15 and 39 damage respectively. The AK killed the smaller enemies on foot. Retreat and re-entry worked against the giant. |
| Guarded rations | A lantern, crate and red sentinel mark the spur beyond the hunting blind. Collect the green pickup for four meat and the existing High Jump boost. The guard drops five more meat if defeated. | Visited and claimed the cache during a fresh harvest. Load Game retained the balance and the spent cache. A separate day skipped the shelter, hunted 15 meat, bought the four-meat sidearm, offered ten and reloaded with one left. |

**Try the loop.** Pick up the AK on the beach and hold Shift on the straight approach to the first Hunting Grounds board. At the hunter's blind, leave the main trail toward the lantern and standing stone. Return to the brass markers, pass the melee hunter and the red caster, and follow the route to the dealer grotto. Take the Saddle Trail uphill toward the Offering Altar. Watch for the giant as the trail bends back across the hillside.

**Verification.** The native keyboard and mouse run sent Windows events to the focused game window. Optional `-dh-observe` sampled controller and world state without driving the player. The `-dh-review trail` standalone replay used movement, normal weapon fire and interaction from New Game, with automated steering. It exercised each placed hostile, all five signs, the cache, and a return trip after loading. Combat on that route used no teleport, direct damage, forced target, or direct attack invocation. The older `mechanics` replay still stages edge cases. Final records are `Builds/followup-day.log` (21 checks), `Builds/followup-trail.log` (32 checks), and `Builds/followup-mechanics.log` (67 checks).

## Starting point

The lightweight remote tag `Beta` resolves to `f7642a2287d3eaaf8347fdfee303fd3f736aca52`. It labels the team's combined `Dev (#12)` commit, including the first quota day, pause menu, power-ups and restored island UI. The only other fetched tag was `Alpha`. The local branch `victor-beta-playtest-feedback` started at the verified Beta SHA in a separate worktree.

The build opens `Assets/Scenes/MainMenu/MainMenu.unity` and loads `Assets/Scenes/DevilsHarvest_Island.unity` for Main Level. The tagged New Game screen presents the name field, level cards and Start Harvest together.

## Baseline

Unity 6000.5.5f1 initially failed to compile the tag's editor-only KitWright dependency at `ObjectIdCodec.cs:78`. The existing checkout already had a single version-guard correction. Applying the equivalent correction to the worktree's installed editor package allowed the tagged gameplay to run. The package manifest and lockfile retain the team's versions.

The automated Editor playthrough clicked New Game, named a fresh harvest and used Start Harvest. It walked from the beach to the AK, hunted three prey worth five meat each, collected the drops using the interaction input, bought the four-meat sidearm in the grotto, and offered ten times at the saddle table. It travelled about 1.54 km in 120 simulated seconds. Reload restored a completed quota, the equipped sidearm and one spare meat.

Live inspection found empty player references on the melee monster, fire caster and boss. Both existing island boosts were within two metres of each other near the beach. Source inspection showed that the bow consumed one of 30 arrows per shot while the ammo HUD only displayed guns. Creatures dropped loot and disappeared immediately. Combat and pit deaths scheduled recovery, while the water volume called the terminal death method. The baseline route exercised hunting and quota completion. The baseline bow and death findings came from inspection.

## Play the Windows build

Run `Builds/Windows/DevilsHarvest.exe` from this worktree. Keep the complete Windows folder together. Choose **New Game**, enter a harvest name, select **Main Level**, then click **Start Harvest**. Pressing Enter in the name field confirms the currently selected level. Aim defaults to the right mouse button. The Controls menu supports rebinding it.

To reproduce the review build on Windows, close this worktree's editor and run `.\Tools\BuildWindows.ps1` from its root. The script uses the Unity Hub editor recorded in ProjectVersion.txt. On a fresh import it handles the specific tagged KitWright compiler error before retrying. Builds and local test captures are ignored by Git.

The executable has two optional automated replays. Run it with `-dh-review day` for a fresh hunt through the dealer, altar and Load Game menu, or `-dh-review mechanics` for focused boundary and combat checks. Use the default controls. Both runs use generated harvest names and simulate input. The focused checks stage encounter approaches, a one-meat drop, purchase money, remaining arrows and clock positions. Captures are saved beside the executable in `PlaytestEvidence`. Launching without arguments opens the normal main menu.

## Changed gameplay

| Feedback | Implementation |
| --- | --- |
| Altar and dealer directions | Two color-coded HUD arrows show the signed horizontal bearing through 360 degrees. They appear when carried meat covers the unpaid quota. The altar instruction and optional dealer label state their roles. |
| Name confirmation and interaction | TMP submit shares Start Harvest validation and a one-frame guard. The interaction HUD shows one contextual hint or actionable prompt, uses the bound interaction key, and shares its target with the interaction controller. |
| Shrine and island routes | Taller basalt horns mark the saddle. The enlarged offering surface retains the grounded foundation. Five physical signs mark the hunting trail, brass trail and saddle approaches. |
| Firearms and bow | Four gun assets define sight anchors, zoom and recoil. AK and Five-Seven use iron sights, MP5 uses an aperture and M82 uses a scope. The AK supports held automatic fire, and MP5 retains its three-round burst. The bow displays its existing finite count down to zero. |
| Exploration | Tidewatch supplies, Trapper's shelter and Ridge survey station reuse island props and the existing speed and jump effects. Each cache is single-use per harvest. Saves retain consumed caches and remaining boost time. |
| Time of day | The eight-minute saved quota clock drives the sun, sky exposure, ambient colors and fog. The scene freezes at the outcome's time of day. |
| Threat and death | The three hostile actors have a live player target. The melee monster detects nearby players, the caster retains its marked cast and windup, and the boss has an escape radius and lower pursuit speed. Monster38 uses its matching death clip. Other mobs visibly fall, then despawn. The player camera rolls and fades before checkpoint recovery. |

## Windows acceptance

The standalone built with zero errors. Its two warnings were a deprecated lookup in CreatureAudio and a missing optional RuntimePipelineConfig. The player launched in a separate native-input smoke test. A Windows mouse click, typed harvest name and Enter opened the island and created one save. Screenshots of those screens are in `Builds/Windows/PlaytestEvidence`.

| Run | Verified in the graphical Windows player |
| --- | --- |
| Opening day, 22 checks | Walked 1.54 km from the beach, picked up the AK, hunted three prey, collected 15 meat, saw both arrows at 10, visited all three cache detours and bought the sidearm. Ten offerings completed the quota. Load Game restored the sidearm, completed quota and one remaining meat. |
| Focused mechanics, 67 checks | Exercised 9 to 10, turning away from both destinations, partial offerings, spending, completion and loading. Checked all four gun sights and recoil standing and moving, last arrow and empty fire, switching weapons, the three cache effects and persisted state, and the clock at the start, midpoint, late night and expiry. |
| Combat and recovery within the focused run | Triggered melee, caster and boss attacks. Confirmed the cast warning and hit, caster death clip, prey fall, single loot drop, and combat and drowning recovery. Dying while scoped restored normal view and control. |

The local records are `Builds/WindowsBuild.log`, `Builds/windows-acceptance-day.log`, `Builds/windows-acceptance-mechanics.log` and `Builds/normal-launch.log`. Both acceptance replays finished with `REVIEW complete=True`. The opening-day routes used the character controller throughout. The mechanics run staged balances, encounter approaches and clock positions so its edge cases are reproducible.

For classroom review, have a first-time player find the hunting trail and grotto unaided, judge the boss escape window and sustained-fire kick, and check late-night trail visibility on the display used in class. The bow retains the beta's finite ammunition. Drowning now returns the player to the beach.
