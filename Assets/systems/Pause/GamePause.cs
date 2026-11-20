using System;
using UnityEngine;

/// <summary>
/// Global pause state: stops time, pauses game audio (UI sounds that ignore listener pause keep playing),
/// disables gameplay input and frees the cursor. Used by PauseMenuController.
/// Other full-screen menus (e.g. the dealer shop) report themselves as overlays so Escape closes them
/// instead of opening the pause menu.
/// </summary>
public static class GamePause
{
    private static float previousTimeScale = 1f;
    private static CursorLockMode previousLock;
    private static bool previousCursorVisible;
    private static int overlayCount;
    private static int overlayChangedFrame = -1;

    public static bool IsPaused { get; private set; }

    /// <summary>Raised with true when the game pauses and false when it resumes.</summary>
    public static event Action<bool> PauseChanged;

    /// <summary>True while another menu is open, or closed this frame.</summary>
    public static bool OverlayOpen => overlayCount > 0 || overlayChangedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsPaused = false;
        previousTimeScale = 1f;
        overlayCount = 0;
        overlayChangedFrame = -1;
        PauseChanged = null;
    }

    public static void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        GameInput.GameplayEnabled = false;
        previousLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PauseChanged?.Invoke(true);
    }

    public static void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = false;
        GameInput.GameplayEnabled = true;
        Cursor.lockState = previousLock;
        Cursor.visible = previousCursorVisible;
        PauseChanged?.Invoke(false);
    }

    public static void OverlayOpened()
    {
        overlayCount++;
        overlayChangedFrame = Time.frameCount;
    }

    public static void OverlayClosed()
    {
        overlayCount = Mathf.Max(0, overlayCount - 1);
        overlayChangedFrame = Time.frameCount;
    }
}
