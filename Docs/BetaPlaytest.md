# Devil's Harvest playtest handoff

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
