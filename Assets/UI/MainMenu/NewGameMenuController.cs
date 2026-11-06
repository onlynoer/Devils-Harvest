using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls new/load game screens, level selection, and save-list interactions.</summary>
public sealed class NewGameMenuController : MonoBehaviour
{
    [Header("Screens and messages")]
    public GameObject buttonContainer;
    public GameObject newGameScreen;
    public GameObject loadGameScreen;
    public TMP_InputField saveNameInput;
    public TMP_Text messageText;
    public TMP_Text loadMessageText;
    public Transform saveListRoot;
    public TMP_FontAsset bodyFont;

    [Header("Buttons")]
    public Button newGameButton;
    public Button loadGameButton;
    public Button startHarvestButton;
    public Button newGameBackButton;
    public Button loadGameBackButton;

    [Header("Load screen selection and delete confirmation")]
    public GameObject loadSaveActions;
    public Button playSelectedSaveButton;
    public Button deleteSelectedSaveButton;
    public GameObject deleteConfirmationPopup;
    public TMP_Text deleteConfirmationText;
    public Button cancelDeleteButton;
    public Button confirmDeleteButton;

    [Header("Level selection buttons")]
    public Button[] levelButtons;

    [Header("Level card presentation")]
    public Image[] levelCardFrames;
    public Image[] levelPreviewImages;
    public TMP_Text[] levelCaptions;
    public string[] levelIds = { "main-level", "boss-fight" };
    public string[] levelNames = { "Main Level", "Boss Fight" };
    public string[] levelSceneNames = { "", "" };
    public Sprite[] levelPreviewSprites;
    public int selectedLevelIndex;
    [Tooltip("Leave empty until the real gameplay/world scene is ready and included in Build Settings.")]
    public string gameplaySceneName = "";

    private readonly List<GameObject> generatedRows = new List<GameObject>();
    private readonly Dictionary<string, Image> saveRowImages = new Dictionary<string, Image>();
    private readonly Dictionary<string, Button> saveRowButtons = new Dictionary<string, Button>();
    private string selectedSaveId;
    private string selectedSaveName;
    private static readonly Color Ivory = new Color(0.88f, 0.85f, 0.79f, 1f);
    private static readonly Color Muted = new Color(0.70f, 0.69f, 0.66f, 1f);
    private static readonly Color SelectedLevelColor = new Color32(0x99, 0x8A, 0x73, 0xFF);
    private static readonly Color IdleLevelColor = new Color32(0x57, 0x4C, 0x4A, 0xFF);
    private UnityEngine.Events.UnityAction[] levelButtonHandlers;

    private void OnEnable() => Bind();

    public void Bind()
    {
        Listen(newGameButton, OpenNewGame);
        Listen(loadGameButton, OpenLoadGame);
        Listen(startHarvestButton, StartHarvest);
        Listen(newGameBackButton, ReturnToMenu);
        Listen(loadGameBackButton, ReturnToMenu);
        Listen(playSelectedSaveButton, PlaySelectedSave);
        Listen(deleteSelectedSaveButton, ShowDeleteConfirmation);
        Listen(cancelDeleteButton, CancelDelete);
        Listen(confirmDeleteButton, ConfirmDeleteSelectedSave);
        BindLevelButtons();
        SelectLevel(0);
        if (newGameScreen != null) newGameScreen.SetActive(false);
        if (loadGameScreen != null) loadGameScreen.SetActive(false);
        if (deleteConfirmationPopup != null) deleteConfirmationPopup.SetActive(false);
        if (loadSaveActions != null) loadSaveActions.SetActive(false);
        if (buttonContainer != null) buttonContainer.SetActive(true);
    }

    private static void Listen(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null) return;
        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    private void BindLevelButtons()
    {
        if (levelButtons == null) return;
        if (levelButtonHandlers == null || levelButtonHandlers.Length != levelButtons.Length)
            levelButtonHandlers = new UnityEngine.Events.UnityAction[levelButtons.Length];
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int index = i;
            if (levelButtons[i] == null) continue;
            if (levelButtonHandlers[i] != null) levelButtons[i].onClick.RemoveListener(levelButtonHandlers[i]);
            levelButtonHandlers[i] = () => SelectLevel(index);
            levelButtons[i].onClick.AddListener(levelButtonHandlers[i]);
        }
    }

    public void SelectLevel(int index)
    {
        // Keep the cards' serialized arrays index-aligned: button, frame, preview, caption, and level data.
        int count = levelButtons != null ? levelButtons.Length : 0;
        if (count == 0) { selectedLevelIndex = 0; return; }
        selectedLevelIndex = Mathf.Clamp(index, 0, count - 1);
        for (int i = 0; i < count; i++)
        {
            if (levelCardFrames != null && i < levelCardFrames.Length && levelCardFrames[i] != null)
                levelCardFrames[i].color = i == selectedLevelIndex ? SelectedLevelColor : IdleLevelColor;
            if (levelPreviewImages != null && i < levelPreviewImages.Length && levelPreviewImages[i] != null && levelPreviewSprites != null && i < levelPreviewSprites.Length)
                levelPreviewImages[i].sprite = levelPreviewSprites[i];
            if (levelCaptions != null && i < levelCaptions.Length && levelCaptions[i] != null)
            {
                string title = levelNames != null && i < levelNames.Length ? levelNames[i] : "Level " + (i + 1);
                levelCaptions[i].text = title;
            }
        }
    }

    public void OpenNewGame()
    {
        SetScreen(newGameScreen);
        SelectLevel(0);
        if (messageText != null) messageText.text = "";
        if (saveNameInput != null) { saveNameInput.text = ""; saveNameInput.Select(); saveNameInput.ActivateInputField(); }
    }

    public void OpenLoadGame()
    {
        SetScreen(loadGameScreen);
        if (loadMessageText != null) loadMessageText.text = "";
        ClearSaveSelection();
        RefreshSaveList();
    }

    private void SetScreen(GameObject screen)
    {
        if (buttonContainer != null) buttonContainer.SetActive(false);
        if (newGameScreen != null) newGameScreen.SetActive(screen == newGameScreen);
        if (loadGameScreen != null) loadGameScreen.SetActive(screen == loadGameScreen);
    }

    public void ReturnToMenu()
    {
        if (newGameScreen != null) newGameScreen.SetActive(false);
        if (loadGameScreen != null) loadGameScreen.SetActive(false);
        if (deleteConfirmationPopup != null) deleteConfirmationPopup.SetActive(false);
        ClearSaveSelection();
        if (buttonContainer != null) buttonContainer.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void StartHarvest()
    {
        try
        {
            string requestedName = saveNameInput != null ? saveNameInput.text : "";
            string levelId = GetSelectedLevelId();
            string levelName = GetLevelName(levelId);
            var save = SaveGameService.Create(requestedName, levelId);
            SaveGameService.SelectAsActive(save);
            string sceneName = GetSceneNameForLevel(levelId);
            if (string.IsNullOrWhiteSpace(sceneName))
                SetMessage("“" + save.saveName + "” saved for " + levelName + ". This level's gameplay scene is not configured yet.");
            else TryLoadGameplayScene(sceneName);
        }
        catch (ArgumentException exception) { SetMessage(exception.Message); }
        catch (Exception exception) { Debug.LogException(exception); SetMessage("The save could not be created."); }
    }

    public void RefreshSaveList()
    {
        // Only generated rows are tracked and replaced; the serialized scroll view and its layout stay intact.
        if (saveListRoot == null) return;
        for (int i = 0; i < generatedRows.Count; i++)
            if (generatedRows[i] != null) Destroy(generatedRows[i]);
        generatedRows.Clear();
        saveRowImages.Clear();
        saveRowButtons.Clear();
        ClearSaveSelection();
        var saves = SaveGameService.GetAll();
        if (saves.Count == 0) { AddSaveRow(null, "No harvests saved yet."); return; }
        foreach (var save in saves)
        {
            string details = save.saveName + "   •   " + GetLevelName(save.progress.startingLevelId) + "   •   Day " + save.progress.day + "   •   " + save.progress.harvestedItems + " harvested   •   " + FormatTime(save.timePlayedSeconds) + "   •   " + FormatDate(save.lastPlayedUtc);
            AddSaveRow(save, details);
        }
    }

    private void AddSaveRow(SaveGameData save, string labelText)
    {
        var row = new GameObject("Save_" + (save != null ? save.id : "Empty"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        row.transform.SetParent(saveListRoot, false);
        generatedRows.Add(row);
        var image = row.GetComponent<Image>(); image.color = save == null ? new Color(0.12f, 0.10f, 0.10f, 0.55f) : new Color(0.29f, 0.24f, 0.23f, 0.90f);
        var button = row.GetComponent<Button>(); button.targetGraphic = image; button.interactable = save != null;
        var element = row.GetComponent<LayoutElement>(); element.minHeight = 54; element.preferredHeight = 60;
        var textObject = new GameObject("SaveInfo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(row.transform, false);
        var rect = textObject.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(16, 4); rect.offsetMax = new Vector2(-16, -4);
        var text = textObject.GetComponent<TMP_Text>(); text.font = bodyFont; text.text = labelText; text.color = save != null ? Ivory : Muted; text.fontSize = 22; text.alignment = TextAlignmentOptions.MidlineLeft; text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 22; text.raycastTarget = false;
        if (save != null)
        {
            saveRowImages[save.id] = image;
            saveRowButtons[save.id] = button;
            string saveId = save.id;
            button.onClick.AddListener(() => SelectSave(saveId));
        }
    }

    /// <summary>Selects a row without loading its world, then exposes the Play and Delete actions.</summary>
    public void SelectSave(string id)
    {
        SaveGameData selected = SaveGameService.GetAll().Find(save => save.id == id);
        if (selected == null) return;

        selectedSaveId = selected.id;
        selectedSaveName = selected.saveName;
        //if (selectedSaveLabel != null) selectedSaveLabel.text = selected.saveName;
        if (loadSaveActions != null) loadSaveActions.SetActive(true);
        UpdateSaveRowStyles();

        if (EventSystem.current != null && saveRowButtons.TryGetValue(id, out Button rowButton))
            EventSystem.current.SetSelectedGameObject(rowButton.gameObject);
    }

    private void PlaySelectedSave()
    {
        if (!string.IsNullOrEmpty(selectedSaveId)) LoadSave(selectedSaveId);
    }

    private void ShowDeleteConfirmation()
    {
        if (string.IsNullOrEmpty(selectedSaveId) || deleteConfirmationPopup == null) return;
        if (deleteConfirmationText != null)
            deleteConfirmationText.text = "Delete \"" + selectedSaveName + "\"?";
        deleteConfirmationPopup.SetActive(true);
        if (EventSystem.current != null && cancelDeleteButton != null)
            EventSystem.current.SetSelectedGameObject(cancelDeleteButton.gameObject);
    }

    private void CancelDelete()
    {
        if (deleteConfirmationPopup != null) deleteConfirmationPopup.SetActive(false);
        if (EventSystem.current != null && !string.IsNullOrEmpty(selectedSaveId) && saveRowButtons.TryGetValue(selectedSaveId, out Button rowButton))
            EventSystem.current.SetSelectedGameObject(rowButton.gameObject);
    }

    private void ConfirmDeleteSelectedSave()
    {
        if (string.IsNullOrEmpty(selectedSaveId))
        {
            CancelDelete();
            return;
        }

        try
        {
            if (!SaveGameService.Delete(selectedSaveId))
            {
                if (loadMessageText != null) loadMessageText.text = "That save could not be found.";
            }
            else if (loadMessageText != null)
            {
                loadMessageText.text = "Deleted \"" + selectedSaveName + "\".";
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (loadMessageText != null) loadMessageText.text = "The save could not be deleted.";
        }

        if (deleteConfirmationPopup != null) deleteConfirmationPopup.SetActive(false);
        RefreshSaveList();
    }

    private void ClearSaveSelection()
    {
        selectedSaveId = null;
        selectedSaveName = null;
        if (loadSaveActions != null) loadSaveActions.SetActive(false);
        if (deleteConfirmationPopup != null) deleteConfirmationPopup.SetActive(false);
        //if (selectedSaveLabel != null) selectedSaveLabel.text = "Select a save to play or delete";
        UpdateSaveRowStyles();
    }

    private void UpdateSaveRowStyles()
    {
        foreach (var entry in saveRowImages)
            if (entry.Value != null)
                entry.Value.color = entry.Key == selectedSaveId
                    ? new Color(0.42f, 0.35f, 0.31f, 0.98f)
                    : new Color(0.29f, 0.24f, 0.23f, 0.90f);
    }

    public void LoadSave(string id)
    {
        if (!SaveGameService.Load(id)) { SetMessage("That save could not be loaded."); RefreshSaveList(); return; }
        var save = SaveGameService.ActiveSave;
        SaveGameService.SelectAsActive(save);
        string sceneName = !string.IsNullOrEmpty(save.progress.currentScene) ? save.progress.currentScene : GetSceneNameForLevel(save.progress.startingLevelId);
        if (string.IsNullOrWhiteSpace(sceneName)) SetMessage("“" + save.saveName + "” selected at " + GetLevelName(save.progress.startingLevelId) + ". This level's gameplay scene is not configured yet.");
        else TryLoadGameplayScene(sceneName);
    }

    private string GetSelectedLevelId()
    {
        return levelIds != null && selectedLevelIndex >= 0 && selectedLevelIndex < levelIds.Length && !string.IsNullOrEmpty(levelIds[selectedLevelIndex])
            ? levelIds[selectedLevelIndex] : "main-level";
    }

    private string GetLevelName(string id)
    {
        if (levelIds != null && levelNames != null)
            for (int i = 0; i < levelIds.Length && i < levelNames.Length; i++)
                if (levelIds[i] == id) return levelNames[i];
        return id == "boss-fight" ? "Boss Fight" : "Main Level";
    }

    private string GetSceneNameForLevel(string id)
    {
        if (levelIds != null && levelSceneNames != null)
            for (int i = 0; i < levelIds.Length && i < levelSceneNames.Length; i++)
                if (levelIds[i] == id && !string.IsNullOrWhiteSpace(levelSceneNames[i])) return levelSceneNames[i];
        return id == "main-level" ? gameplaySceneName : "";
    }

    private bool TryLoadGameplayScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
            { SceneManager.LoadScene(sceneName); return true; }
        }
        SetMessage("Level scene “" + sceneName + "” is not included in Build Settings.");
        return false;
    }

    private void SetMessage(string message)
    {
        TMP_Text target = loadGameScreen != null && loadGameScreen.activeSelf ? loadMessageText : messageText;
        if (target != null) target.text = message;
    }
    private static string FormatDate(string iso) { DateTime date; return DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out date) ? date.ToLocalTime().ToString("MMM d, yyyy") : "Unknown date"; }
    private static string FormatTime(double seconds) { var t = TimeSpan.FromSeconds(Math.Max(0, seconds)); return t.TotalHours >= 1 ? ((int)t.TotalHours) + "h " + t.Minutes + "m" : t.ToString(@"m\m\ ss\s"); }
}
