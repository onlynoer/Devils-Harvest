# Devil's Harvest: island map release

## Open the map

`Assets/DevilsHarvest/Scenes/DevilsHarvest_Island.unity`

Use **Tools > Devils Harvest > Island Release > Open Release Map Alone**. Unity prompts to save other unsaved scenes. This is the approved Tropical Polished 1 scene moved with its original scene GUID (`4e02a5a090e4ee64b938f8707a8b3cbe`), not a replacement map generation.

Controls: WASD, mouse look, hold Left Shift to sprint, Space to jump, Escape to release cursor, click to capture, R for beach respawn. Walking/sprint speeds are configurable on `WildernessTestPlayer` (5.5/8.8 m/s).

## Release polish

- The altar sits in the saddle at approximately `(793, 137.365, 757)`, with a natural standing area at `(789.901, 137.067, 757.064)` and a clear contour connector. Its footing conforms to terrain. The table and offering stones use actual mesh/collider support; tabletop height is approximately 1.26 m above the standing area.
- Corrected 301 object placements using geometry footprints and actual TerrainCollider/solid-support queries. Existing correctly supported props, stacked grotto furniture, walls/roof, overhangs, lanterns, pit geometry and rockfall source positions were retained. The final active-prop audit covers 4,802 logical props and reports zero unclassified floating or unsupported candidates.
- Terrain heights and holes were not sculpted. The seven natural routes, both peaks, altar integration markers, dealer markers, checkpoints and hazards remain available. The release terrain baseline is a copy of the archive-verified approved terrain; the obsolete construction-era comparison field was retired.

The editable terrain is under `00 Landscape - Editable Terrain and Ocean`. Required materials, meshes, prefabs, terrain, layers, textures and lighting are consolidated under `Assets/DevilsHarvest/Environment`. Existing regional hierarchy and teammate integration markers are retained.

## Cleanup and recovery

Unity dependency closure checks included the release scene, its validation baseline, and unrelated/team assets. 147 required assets were moved with GUID checks. 659 unnecessary generated files were removed, including 11 obsolete/incomplete generated scenes and superseded generation tools/reports. Completed preparation-only scripts/status files were subsequently archived and removed. One release island remains alongside the four unchanged team scenes (`Main`, `K_Testing`, `N_Testing`, `V_Testing`).

Verified recovery ZIPs were created outside the repository before deletion, including asset metadata and SHA-256 manifests. Their local paths/checksums are in ignored `Logs/Release/recovery.json` and `recovery-supplement.json`; no recovery archive is part of the proposed commit. `.vscode` and pre-existing unrelated work were preserved.

## Executed validation

- Unity **6000.5.5f1** compilation; no Unity/URP version changes.
- Saved-scene reopen; missing-script, missing-prefab, serialized-reference/GUID and shader checks; matching enabled TerrainCollider; safe player spawn/checkpoints; one gameplay camera/listener.
- Native CharacterController traversal of all seven routes, both peak destinations and the new altar connector.
- Focused graphical Input System tests: walking, measured **1.60x sprint**, release to walk, actual Space jump (**1.24 m**) and grounded landing.
- Live rockfall warning, physical burst reaching the trail, shelter, hit/reset, cap, cooldown, cleanup and retrigger; real spike-pit fall/trigger/reset.
- Windows development-player smoke: map load, Direct3D12 and URP shader support, all routes, altar access, dealer entrance/interior access, both hazards and offscreen GPU rendering.
- GPU captures reviewed for the altar standing area/approach and island/hazard views. The overview with fog disabled is diagnostic only; normal scene fog was not changed.

Development player: `Builds/DevilsHarvest_Development/DevilsHarvest.exe`. The complete local build folder is required. Build result: succeeded, zero errors, approximately 186 MB output. One pre-existing warning remains: the optional `com.unity.pipeline` command-bridge package has no RuntimePipelineConfig, so its runtime command bridge is disabled. This is separate from URP, which rendered successfully. Obsolete API warnings in the smoke helper were fixed.

Build settings include this release map first. The confirmed missing template `Assets/Scenes/SampleScene.unity` entry was replaced; valid team entries and the existing input configuration were preserved. Build-generated serialization/prefilter changes to unrelated URP/PlayerSettings assets were restored to their pre-task contents.

Reports and captures are local, ignored outputs under `Logs/Release`; builds are ignored under `Builds`. Final status, reference checks and dependency lists are repeated after the last scene save/reopen. Compilation alone is not the basis for these results.

## Repeat checks

- **Tools > Devils Harvest > Island Release > Validate Open Release Map**: native collision/reference checks.
- **Tools > Devils Harvest > Island Release > Audit Grounding**: read-only footprint/support report.
- **Tools > Devils Harvest > Island Release > Hazards > Select and Frame Rockfall / Spike Pit**: Editor-only scene framing and gizmos.
- Command-line Editor methods: `DevilsHarvest.Editor.IslandReleaseTools.BuildDevelopment`, `IslandReleasePlayChecks.Run` (headless for physics; graphical for input), and `IslandReleaseCapture.Run`. These batch methods close their validation Editor when finished; do not run another Editor on an already-open project.
- The development player accepts `-dh-island-smoke` for its opt-in automated smoke run and writes to `Logs/Release`. The helper is compiled only for Editor/development builds and does nothing during normal play without this flag.

Manual review still recommended: normal keyboard/mouse/cursor feel, longer exploration and vegetation sightlines, hazard readability, and FPS on intended student PCs. Automated traversal and render captures are not a full human playthrough or a performance benchmark. Gameplay systems remain teammate-owned.

## Release Git scope

Release scene/environment, required runtime scripts and metadata, release Editor tools, Terrain Physics package dependency, release build entry, this handoff, and narrow binary attributes for native TerrainData/texture assets. No file exceeds 50 MiB; the largest is the approximately 17.6 MiB scene. Existing Git LFS rules are retained; no LFS migration or history rewrite was performed.

Library, Temp, Logs, Obj, builds, recovery archives and credentials are excluded. Pre-existing `.vscode/` and `ProjectSettings/SceneTemplateSettings.json` are deliberately outside the staged release changes. Publication is limited to victor-map after validation/review. No merge, force-push or history rewrite is part of this release workflow.
