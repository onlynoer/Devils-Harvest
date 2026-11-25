using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Every rebindable gameplay action. Add new actions here and in <see cref="GameInput"/>'s table.</summary>
public enum GameAction
{
    MoveForward,
    MoveBack,
    MoveLeft,
    MoveRight,
    Jump,
    Sprint,
    Attack,
    Interact,
    Drop,
    Hotbar1,
    Hotbar2,
    Hotbar3,
    Hotbar4,
    Hotbar5,
    Hotbar6,
    Hotbar7,
    Hotbar8,
    Hotbar9,
    Aim,
    Reload,
}

/// <summary>
/// Rebindable gameplay input. Scripts ask "is Jump pressed?" instead of reading a fixed key, so the Controls
/// settings page can change any binding. Bindings are saved in PlayerPrefs and shared by every scene.
/// Escape is reserved for pausing/cancelling and can't be bound.
/// </summary>
public static class GameInput
{
    private const string SaveKey = "DevilsHarvest.Settings.Bindings";

    private struct Definition
    {
        public readonly GameAction action;
        public readonly string label;
        public readonly string defaultPath;

        public Definition(GameAction action, string label, string defaultPath)
        {
            this.action = action;
            this.label = label;
            this.defaultPath = defaultPath;
        }
    }

    // Order here is the order shown on the Controls page. Must list every GameAction once, in enum order.
    private static readonly Definition[] Definitions =
    {
        new Definition(GameAction.MoveForward, "Move Forward", "<Keyboard>/w"),
        new Definition(GameAction.MoveBack, "Move Back", "<Keyboard>/s"),
        new Definition(GameAction.MoveLeft, "Move Left", "<Keyboard>/a"),
        new Definition(GameAction.MoveRight, "Move Right", "<Keyboard>/d"),
        new Definition(GameAction.Jump, "Jump", "<Keyboard>/space"),
        new Definition(GameAction.Sprint, "Sprint", "<Keyboard>/leftShift"),
        new Definition(GameAction.Attack, "Attack / Shoot", "<Mouse>/leftButton"),
        new Definition(GameAction.Interact, "Interact / Pick Up", "<Keyboard>/e"),
        new Definition(GameAction.Drop, "Drop Item", "<Keyboard>/q"),
        new Definition(GameAction.Hotbar1, "Equip Hotbar Slot 1", "<Keyboard>/1"),
        new Definition(GameAction.Hotbar2, "Equip Hotbar Slot 2", "<Keyboard>/2"),
        new Definition(GameAction.Hotbar3, "Equip Hotbar Slot 3", "<Keyboard>/3"),
        new Definition(GameAction.Hotbar4, "Equip Hotbar Slot 4", "<Keyboard>/4"),
        new Definition(GameAction.Hotbar5, "Equip Hotbar Slot 5", "<Keyboard>/5"),
        new Definition(GameAction.Hotbar6, "Equip Hotbar Slot 6", "<Keyboard>/6"),
        new Definition(GameAction.Hotbar7, "Equip Hotbar Slot 7", "<Keyboard>/7"),
        new Definition(GameAction.Hotbar8, "Equip Hotbar Slot 8", "<Keyboard>/8"),
        new Definition(GameAction.Hotbar9, "Equip Hotbar Slot 9", "<Keyboard>/9"),
        new Definition(GameAction.Aim, "Aim Down Sights", "<Mouse>/rightButton"),
        new Definition(GameAction.Reload, "Reload", "<Keyboard>/r"),
    };

    private static InputActionMap map;
    private static InputAction[] actions;
    private static InputActionRebindingExtensions.RebindingOperation rebind;
    private static bool gameplayEnabled = true;
    private static int rebindEndedFrame = -1;

    /// <summary>Raised after any binding changes (rebind, swap or reset).</summary>
    public static event Action BindingsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        rebind?.Dispose();
        rebind = null;
        map?.Dispose();
        map = null;
        actions = null;
        gameplayEnabled = true;
        rebindEndedFrame = -1;
        BindingsChanged = null;
    }

    /// <summary>All actions in display order.</summary>
    public static IEnumerable<GameAction> AllActions
    {
        get
        {
            foreach (Definition definition in Definitions) yield return definition.action;
        }
    }

    /// <summary>The hotbar actions in slot order (1-9).</summary>
    public static readonly GameAction[] HotbarActions =
    {
        GameAction.Hotbar1, GameAction.Hotbar2, GameAction.Hotbar3, GameAction.Hotbar4, GameAction.Hotbar5,
        GameAction.Hotbar6, GameAction.Hotbar7, GameAction.Hotbar8, GameAction.Hotbar9,
    };

    /// <summary>True while waiting for the player to press the new key for a binding.</summary>
    public static bool IsRebinding => rebind != null;

    /// <summary>True during the frame a rebind finished or was cancelled (so Escape isn't also read as "pause").</summary>
    public static bool RebindEndedThisFrame => rebindEndedFrame == Time.frameCount;

    /// <summary>Gameplay actions read as "not pressed" while this is false (e.g. while paused).</summary>
    public static bool GameplayEnabled
    {
        get => gameplayEnabled;
        set
        {
            gameplayEnabled = value;
            EnsureCreated();
            if (!IsRebinding)
            {
                if (value) map.Enable();
                else map.Disable();
            }
        }
    }

    public static bool IsHeld(GameAction action) => Get(action).IsPressed();

    public static bool WasPressed(GameAction action) => Get(action).WasPressedThisFrame();

    /// <summary>Name shown on the left of the Controls page, e.g. "Move Forward".</summary>
    public static string GetLabel(GameAction action) => Definitions[(int)action].label;

    /// <summary>Current key shown on the right of the Controls page, e.g. "W" or "Left Button".</summary>
    public static string GetBindingText(GameAction action) =>
        Get(action).GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions);

    /// <summary>
    /// Waits for the next key or mouse button and binds it to <paramref name="action"/>. Escape cancels.
    /// If another action already used that key, the two swap so nothing is left sharing a key.
    /// </summary>
    public static void StartRebind(GameAction action, Action<bool> onFinished = null)
    {
        EnsureCreated();
        CancelRebind();

        InputAction target = Get(action);
        string previousPath = target.bindings[0].effectivePath;
        map.Disable(); // actions must be disabled while rebinding

        rebind = target.PerformInteractiveRebinding(0)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
            .WithControlsExcluding("<Keyboard>/anyKey")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(.05f)
            .OnComplete(_ => FinishRebind(target, previousPath, true, onFinished))
            .OnCancel(_ => FinishRebind(target, previousPath, false, onFinished))
            .Start();
    }

    public static void CancelRebind()
    {
        rebind?.Cancel();
    }

    /// <summary>Restores every action to its default key.</summary>
    public static void ResetToDefaults()
    {
        EnsureCreated();
        CancelRebind();
        map.RemoveAllBindingOverrides();
        Save();
        BindingsChanged?.Invoke();
    }

    private static void FinishRebind(InputAction target, string previousPath, bool completed, Action<bool> onFinished)
    {
        rebind?.Dispose();
        rebind = null;
        rebindEndedFrame = Time.frameCount;

        if (completed)
        {
            string newPath = target.bindings[0].effectivePath;
            foreach (InputAction other in actions)
                if (other != target && other.bindings[0].effectivePath == newPath)
                    other.ApplyBindingOverride(0, previousPath);
            Save();
        }

        if (gameplayEnabled) map.Enable();
        BindingsChanged?.Invoke();
        onFinished?.Invoke(completed);
    }

    private static InputAction Get(GameAction action)
    {
        EnsureCreated();
        return actions[(int)action];
    }

    private static void EnsureCreated()
    {
        if (map != null) return;
        map = new InputActionMap("Gameplay");
        actions = new InputAction[Definitions.Length];
        for (int i = 0; i < Definitions.Length; i++)
            actions[i] = map.AddAction(Definitions[i].action.ToString(), InputActionType.Button, Definitions[i].defaultPath);
        Load();
        if (gameplayEnabled) map.Enable();
    }

    // Saved per action name ("DevilsHarvest.Settings.Bindings.Jump" = "<Keyboard>/j"), not by binding id,
    // because the actions are rebuilt in code each session and get new ids.
    private static string KeyFor(int index) => SaveKey + "." + Definitions[index].action;

    private static void Load()
    {
        PlayerPrefs.DeleteKey(SaveKey); // older JSON format, can't be matched to rebuilt actions
        for (int i = 0; i < actions.Length; i++)
        {
            string path = PlayerPrefs.GetString(KeyFor(i), string.Empty);
            if (!string.IsNullOrEmpty(path) && path != Definitions[i].defaultPath)
                actions[i].ApplyBindingOverride(0, path);
        }
    }

    private static void Save()
    {
        for (int i = 0; i < actions.Length; i++)
        {
            string path = actions[i].bindings[0].overridePath;
            if (string.IsNullOrEmpty(path) || path == Definitions[i].defaultPath) PlayerPrefs.DeleteKey(KeyFor(i));
            else PlayerPrefs.SetString(KeyFor(i), path);
        }
        PlayerPrefs.Save();
    }
}
