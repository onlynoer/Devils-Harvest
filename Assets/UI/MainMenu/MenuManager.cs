using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Switches between the main menu and options panel and handles quitting.</summary>
public class MenuManager : MonoBehaviour
{
    [Header("Menu panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    [Header("Main menu actions")]
    public Button openSettingsButton;
    public Button quitButton;

    private void Awake()
    {
        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.RemoveListener(OpenSettings);
            openSettingsButton.onClick.AddListener(OpenSettings);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        ClearUISelection();
    }

    private static void ClearUISelection()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void QuitGame()
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.SavePreferences();
        SaveGameService.SaveActive();

    #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
        Debug.Log("Quit requested. Build and launch the game to test the quit button outside the Editor.");
    #else
        Application.Quit();
    #endif
    }

    private void OnDestroy()
    {
        if (openSettingsButton != null) openSettingsButton.onClick.RemoveListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
    }
}
