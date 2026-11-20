using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// In-game pause menu. Escape opens it (pausing the game), goes back from Settings, and closes it.
/// Reuses the main menu's settings panel (OptionsMenuController) for audio and controls.
/// Put this on an always-active object (e.g. the pause canvas); the panels it shows are its children.
/// </summary>
public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("Everything shown while paused (dimmer + panels). Hidden while playing.")]
    [SerializeField] private GameObject pauseRoot;
    [Tooltip("The Resume / Settings / Main Menu / Quit panel.")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("The settings panel in this scene. Leave empty to spawn Settings Panel Prefab instead.")]
    [SerializeField] private OptionsMenuController settingsPanel;
    [Tooltip("Shared settings panel (Assets/UI/Settings/SettingsPanel.prefab), spawned under Pause Root when " +
             "Settings Panel is empty. Edit the prefab to restyle settings everywhere.")]
    [SerializeField] private OptionsMenuController settingsPanelPrefab;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Scenes")]
    [Tooltip("Scene loaded by the Main Menu button. Must be in the Build Profile's scene list.")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool SettingsOpen => settingsPanel != null && settingsPanel.gameObject.activeSelf;

    private void Awake()
    {
        if (settingsPanel == null && settingsPanelPrefab != null)
        {
            Transform parent = pauseRoot != null ? pauseRoot.transform : transform;
            settingsPanel = Instantiate(settingsPanelPrefab, parent, false);
            settingsPanel.name = settingsPanelPrefab.name;
        }
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (settingsPanel != null) settingsPanel.Closed += CloseSettings;
        HideAll();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
        // Escape is also "cancel" for rebinding and closes other menus like the dealer shop.
        if (GameInput.IsRebinding || GameInput.RebindEndedThisFrame || GamePause.OverlayOpen) return;

        if (!GamePause.IsPaused) Open();
        else if (SettingsOpen) CloseSettings();
        else Resume();
    }

    public void Open()
    {
        GamePause.Pause();
        if (pauseRoot != null) pauseRoot.SetActive(true);
        ShowPausePanel();
    }

    public void Resume()
    {
        HideAll();
        GamePause.Resume();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(ReapplyCursor(Cursor.lockState, Cursor.visible));
    }

    /// <summary>
    /// The Unity editor frees the cursor whenever Escape is pressed, after scripts run, so resuming with Escape
    /// would leave the mouse loose. Re-applying the resumed cursor state on the next frames fixes that.
    /// </summary>
    private System.Collections.IEnumerator ReapplyCursor(CursorLockMode lockState, bool visible)
    {
        for (int i = 0; i < 2; i++)
        {
            yield return null;
            if (GamePause.IsPaused) yield break;
            Cursor.lockState = lockState;
            Cursor.visible = visible;
        }
    }

    // Click sounds come from each button's PressedEvent component, so this script plays none.
    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.gameObject.SetActive(true);
            settingsPanel.SelectTab(0);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null && settingsPanel.gameObject.activeSelf) settingsPanel.gameObject.SetActive(false);
        ShowPausePanel();
    }

    private void OnResumeClicked() => Resume();

    private void GoToMainMenu()
    {
        SaveAndLeave();
        // Resuming restores gameplay's locked, hidden cursor; menus need it free.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuScene);
    }

    private void QuitGame()
    {
        SaveAndLeave();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SaveAndLeave()
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.SavePreferences();
        SaveGameService.SaveActive();
        GamePause.Resume(); // never leave time stopped for the next scene
    }

    private void ShowPausePanel()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        if (resumeButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    private void HideAll()
    {
        if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseRoot != null) pauseRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (settingsPanel != null) settingsPanel.Closed -= CloseSettings;
        if (GamePause.IsPaused) GamePause.Resume();
    }
}
