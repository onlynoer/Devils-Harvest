# Pause Menu & Settings

## Add the pause menu to a scene
1. Drag `Assets/UI/PauseMenu/PauseMenu.prefab` into the scene.
2. Make sure the scene has exactly one **EventSystem** using **Input System UI Input Module**. If it has none: **GameObject ▸ UI ▸ Event System**. The prefab doesn't include one, so you never get two.
3. That's it. Esc pauses the game, and the menu's **Main Menu** button loads the scene named in **Main Menu Scene** (default `MainMenu`).

The prefab brings its own **SettingsManager** (the audio volumes) and **UIAudioManager** (click sounds). Don't add a second copy of either to the scene.

## What pausing does (`GamePause`)
- Stops time and pauses game audio. UI click sounds keep playing.
- Turns off gameplay input and frees the cursor.
- Escape works like this:
  - In the Settings panel, it goes back to the pause menu.
  - In the pause menu, it resumes the game.
  - While another menu is open (the dealer shop), Escape closes that menu instead of pausing. Menus like that call `GamePause.OverlayOpened()` and `OverlayClosed()`.

## Settings panel
`Assets/UI/Settings/SettingsPanel.prefab` is shared by the main menu and the pause menu. Edit the prefab to restyle settings everywhere. If a scene has no settings panel, the pause menu spawns this prefab itself.

### Audio
- **Master** scales everything you hear: it sets `AudioListener.volume`.
- **Music**, **SFX** and **UI** set their groups on `Assets/Audio/MainAudioMixer`, on top of Master.
- **New sounds:** set the AudioSource's **Output** to the Music, SFX or UI group. Any AudioSource left without a group is routed to **SFX** when the scene loads. For AudioSources created from code, call `SettingsManager.Route(source, AudioChannel.SFX)` (or Music or UI).

### Controls
- **Where keys come from:** all rebindable keys live in `Assets/systems/Input/GameInput.cs`. In gameplay code, use `GameInput.IsHeld(GameAction.Jump)` or `GameInput.WasPressed(...)`. Don't read `Keyboard.current.xKey` directly, or that key can't be rebound.
- **Adding an action:** add it to the `GameAction` enum and to the `Definitions` table (label and default key). It then appears on the Controls page automatically.
- **Saving:** bindings are saved in PlayerPrefs, one key per action, and shared by every scene.
- **Mouse Sensitivity:** the **Horizontal Sensitivity** and **Vertical Sensitivity** sliders at the top of the Controls list set `ControlSettings.HorizontalSensitivity` and `VerticalSensitivity` (`Assets/systems/Input/ControlSettings.cs`). Each is a multiplier from 0.1× to 3× (default 1×) on `FirstPersonLook`'s speed for that axis. Reset to Defaults resets both along with the keys.
- **Reserved keys:** Escape can't be bound because it's used for pause and cancel. The mouse wheel always switches hotbar slots.
- **Styling the Controls page:** edit `ControlsPage ▸ KeybindScroll ▸ Content ▸ KeybindRowTemplate` in the prefab. The page copies that row once per action.
