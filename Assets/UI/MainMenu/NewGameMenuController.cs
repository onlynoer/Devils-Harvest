using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class NewGameMenuController : MonoBehaviour
{
    public GameObject buttonContainer;
    public GameObject newGameScreen;
    public GameObject loadGameScreen;
    public TMP_InputField saveNameInput;
    public TMP_Text messageText;
    public TMP_Text loadMessageText;
    public Transform saveListRoot;
    public TMP_FontAsset bodyFont;
    public Button newGameButton;
    public Button loadGameButton;
    public Button startHarvestButton;
    public Button newGameBackButton;
    public Button loadGameBackButton;
    [Tooltip("Leave empty until the real gameplay/world scene is ready and included in Build Settings.")]
    public string gameplaySceneName = "";

    private readonly List<GameObject> generatedRows = new List<GameObject>();
    private static readonly Color Ivory = new Color(0.88f, 0.85f, 0.79f, 1f);
    private static readonly Color Muted = new Color(0.70f, 0.69f, 0.66f, 1f);

    private void OnEnable() => Bind();

    public void Bind()
    {
        Listen(newGameButton, OpenNewGame);
        Listen(loadGameButton, OpenLoadGame);
        Listen(startHarvestButton, StartHarvest);
        Listen(newGameBackButton, ReturnToMenu);
        Listen(loadGameBackButton, ReturnToMenu);
        if (newGameScreen != null) newGameScreen.SetActive(false);
        if (loadGameScreen != null) loadGameScreen.SetActive(false);
        if (buttonContainer != null) buttonContainer.SetActive(true);
    }

    private static void Listen(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null) return;
        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    public void OpenNewGame()
    {
        SetScreen(newGameScreen);
        if (messageText != null) messageText.text = "";
        if (saveNameInput != null) { saveNameInput.text = ""; saveNameInput.Select(); saveNameInput.ActivateInputField(); }
    }

    public void OpenLoadGame()
    {
        SetScreen(loadGameScreen);
        if (loadMessageText != null) loadMessageText.text = "";
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
        if (buttonContainer != null) buttonContainer.SetActive(true);
    }

    public void StartHarvest()
    {
        try
        {
            string requestedName = saveNameInput != null ? saveNameInput.text : "";
            var save = SaveGameService.Create(requestedName);
            SaveGameService.SelectAsActive(save);
            if (!TryLoadGameplayScene())
                SetMessage("“" + save.saveName + "” saved. Gameplay scene is not configured yet.");
        }
        catch (ArgumentException exception) { SetMessage(exception.Message); }
        catch (Exception exception) { Debug.LogException(exception); SetMessage("The save could not be created."); }
    }

    public void RefreshSaveList()
    {
        if (saveListRoot == null) return;
        for (int i = 0; i < generatedRows.Count; i++)
            if (generatedRows[i] != null) Destroy(generatedRows[i]);
        generatedRows.Clear();
        var saves = SaveGameService.GetAll();
        if (saves.Count == 0) { AddSaveRow(null, "No harvests saved yet."); return; }
        foreach (var save in saves)
        {
            string details = save.saveName + "   •   Day " + save.progress.day + "   •   " + save.progress.harvestedItems + " harvested   •   " + FormatTime(save.timePlayedSeconds) + "   •   " + FormatDate(save.lastPlayedUtc);
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
        if (save != null) button.onClick.AddListener(() => LoadSave(save.id));
    }

    public void LoadSave(string id)
    {
        if (!SaveGameService.Load(id)) { SetMessage("That save could not be loaded."); RefreshSaveList(); return; }
        SaveGameService.SelectAsActive(SaveGameService.ActiveSave);
        if (!TryLoadGameplayScene()) SetMessage("“" + SaveGameService.ActiveSave.saveName + "” selected. Gameplay scene is not configured yet.");
    }

    private bool TryLoadGameplayScene()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName)) return false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == gameplaySceneName)
            { SceneManager.LoadScene(gameplaySceneName); return true; }
        }
        SetMessage("Gameplay scene “" + gameplaySceneName + "” is not included in Build Settings.");
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
