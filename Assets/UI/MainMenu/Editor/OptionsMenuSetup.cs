using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OptionsMenuSetup
{
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/FontdinerSwanky-Regular SDF.asset";
    private static readonly Color Ivory = new Color(0.88f, 0.85f, 0.79f, 1f);
    private static readonly Color Muted = new Color(0.70f, 0.69f, 0.66f, 1f);
    private static readonly Color Pale = new Color(0.83f, 0.81f, 0.76f, 1f);

    [MenuItem("Tools/Devils Harvest/Build Options Menu")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "MainMenu")
        {
            Debug.LogError("Options menu setup stopped: the active scene must be MainMenu.");
            return;
        }

        var canvasObject = GameObject.Find("Canvas");
        var settingsManagerObject = GameObject.Find("SettingsManager");
        var menuManagerObject = GameObject.Find("MenuManager");
        if (canvasObject == null || settingsManagerObject == null || menuManagerObject == null)
        {
            Debug.LogError("Options menu setup stopped: expected Canvas, SettingsManager, and MenuManager in MainMenu.");
            return;
        }
        var canvas = canvasObject.GetComponent<Canvas>();
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        var panelTransform = canvasObject.transform.Find("SettingsPanel");
        var mainMenuTransform = canvasObject.transform.Find("MainMenuPanel");
        var mainButtonTransform = canvasObject.transform.Find("MainMenuPanel/ButtonContainer/SettingsButton");
        var manager = settingsManagerObject.GetComponent<SettingsManager>();
        var menu = menuManagerObject.GetComponent<MenuManager>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (canvas == null || scaler == null || panelTransform == null || manager == null || menu == null || font == null)
        {
            Debug.LogError("Options menu setup stopped: missing Canvas component, existing panel/managers, or TMP font at " + FontPath + ".");
            return;
        }

        var panel = panelTransform.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(panel, "Build Options Menu");
        Undo.RecordObjects(new UnityEngine.Object[] { canvas, scaler, panel.transform, panel.GetComponent<Image>(), menu }, "Build Options Menu");
        if (mainMenuTransform != null) Undo.RecordObject(mainMenuTransform.gameObject, "Build Options Menu");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.GetComponent<GraphicRaycaster>().enabled = true;

        var rootRect = panel.GetComponent<RectTransform>();
        var oldImage = panel.GetComponent<Image>();
        if (oldImage != null) { oldImage.color = Color.clear; oldImage.raycastTarget = false; }
        var oldLayout = panel.GetComponent<VerticalLayoutGroup>();
        if (oldLayout != null) Undo.DestroyObjectImmediate(oldLayout);
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero; rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;
        for (int i = panel.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(panel.transform.GetChild(i).gameObject);

        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Image dimmer = MakeImage("BackgroundDimmer", panel.transform, sprite, new Color(0.035f, 0.045f, 0.065f, 0.47f), true);
        Stretch(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        dimmer.transform.SetAsFirstSibling();

        TMP_Text title = MakeText("OptionsTitle", panel.transform, font, "Options", 54, Ivory, TextAlignmentOptions.Center);
        Stretch(title.rectTransform, new Vector2(0.22f, 0.84f), new Vector2(0.78f, 0.95f), Vector2.zero, Vector2.zero);
        title.fontStyle = FontStyles.Normal;
        title.outlineWidth = 0.08f;
        title.outlineColor = new Color(0.05f, 0.04f, 0.04f, 0.8f);

        RectTransform tabsRoot = MakeRect("SettingsTabs", panel.transform);
        Stretch(tabsRoot, new Vector2(0.235f, 0.765f), new Vector2(0.765f, 0.82f), Vector2.zero, Vector2.zero);
        var tabsLayout = tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 10; tabsLayout.padding = new RectOffset(0, 0, 0, 0);
        tabsLayout.childAlignment = TextAnchor.MiddleCenter; tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true; tabsLayout.childForceExpandWidth = true; tabsLayout.childForceExpandHeight = true;

        var border = MakeImage("SettingsCardBorder", panel.transform, sprite, new Color(0.63f, 0.67f, 0.70f, 0.78f), false);
        Stretch(border.rectTransform, new Vector2(0.235f, 0.13f), new Vector2(0.765f, 0.75f), Vector2.zero, Vector2.zero);
        var surface = MakeImage("SettingsCardSurface", border.transform, sprite, new Color(0.16f, 0.085f, 0.095f, 0.86f), false);
        Stretch(surface.rectTransform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));

        TMP_Text section = MakeText("SectionHeading", surface.transform, font, "Audio", 34, Ivory, TextAlignmentOptions.Center);
        Stretch(section.rectTransform, new Vector2(0.06f, 0.835f), new Vector2(0.94f, 0.965f), Vector2.zero, Vector2.zero);
        section.outlineWidth = 0.04f;
        section.outlineColor = new Color(0.06f, 0.03f, 0.03f, 0.7f);

        RectTransform pageRoot = MakeRect("SettingsPages", surface.transform);
        Stretch(pageRoot, new Vector2(0.06f, 0.19f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);
        GameObject audioPage = MakePage("AudioPage", pageRoot);
        GameObject graphicsPage = MakePage("GraphicsPage", pageRoot);
        GameObject controlsPage = MakePage("ControlsPage", pageRoot);
        GameObject creditsPage = MakePage("CreditsPage", pageRoot);
        AddPlaceholder(graphicsPage.transform, font, "Graphics", "Display and quality settings will appear here.");
        AddPlaceholder(controlsPage.transform, font, "Controls", "Keyboard and controller navigation use the existing Input System.\nControl rebinding is not configured yet.");
        AddPlaceholder(creditsPage.transform, font, "Credits", "Devils Harvest");

        RectTransform rows = MakeRect("VolumeRows", audioPage.transform);
        Stretch(rows, new Vector2(0.015f, 0.02f), new Vector2(0.985f, 0.98f), Vector2.zero, Vector2.zero);
        var rowsLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 12; rowsLayout.padding = new RectOffset(8, 8, 8, 8);
        rowsLayout.childAlignment = TextAnchor.MiddleCenter; rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true; rowsLayout.childForceExpandWidth = true; rowsLayout.childForceExpandHeight = true;

        string[] labels = { "Master Volume", "Music Volume", "SFX Volume", "UI Volume" };
        var sliders = new Slider[4]; var valueLabels = new TMP_Text[4];
        for (int i = 0; i < labels.Length; i++)
            MakeVolumeRow(rows, sprite, font, labels[i], out sliders[i], out valueLabels[i]);

        Button[] buttons = new Button[4]; Image[] tabImages = new Image[4];
        string[] tabNames = { "Audio", "Graphics", "Controls", "Credits" };
        for (int i = 0; i < tabNames.Length; i++)
            MakeTab(tabsRoot, sprite, font, tabNames[i], out buttons[i], out tabImages[i]);

        var closeButton = MakeButton("BackButton", surface.transform, sprite, font, "Back", 25, new Color(0.23f, 0.20f, 0.19f, 0.96f));
        Stretch(closeButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.055f), new Vector2(0.94f, 0.16f), Vector2.zero, Vector2.zero);

        var controller = panel.GetComponent<OptionsMenuController>();
        if (controller == null) controller = Undo.AddComponent<OptionsMenuController>(panel);
        controller.settingsManager = manager;
        controller.menuManager = menu;
        controller.tabs = buttons;
        controller.tabImages = tabImages;
        controller.pages = new[] { audioPage, graphicsPage, controlsPage, creditsPage };
        controller.sectionHeading = section;
        controller.volumeSliders = sliders;
        controller.volumeValues = valueLabels;
        controller.closeButton = closeButton;
        controller.Initialize();

        menu.mainMenuPanel = mainMenuTransform != null ? mainMenuTransform.gameObject : null;
        if (menu.mainMenuPanel != null) menu.mainMenuPanel.SetActive(true);
        menu.settingsPanel = panel;
        if (mainButtonTransform != null) menu.openSettingsButton = mainButtonTransform.GetComponent<Button>();
        if (menu.openSettingsButton == null)
            Debug.LogWarning("Settings menu built, but MainMenuPanel/ButtonContainer/SettingsButton was not found; call MenuManager.OpenSettings from your menu flow.");

        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Options menu built in MainMenu. Audio, Graphics, Controls, and Credits tabs are connected.");
    }

    private static GameObject MakePage(string name, Transform parent)
    {
        var page = MakeRect(name, parent);
        Stretch(page, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return page.gameObject;
    }

    private static void AddPlaceholder(Transform page, TMP_FontAsset font, string title, string body)
    {
        var heading = MakeText(title + "Heading", page, font, title, 36, Ivory, TextAlignmentOptions.Center);
        Stretch(heading.rectTransform, new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero);
        var description = MakeText(title + "Description", page, font, body, 25, Muted, TextAlignmentOptions.Center);
        Stretch(description.rectTransform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);
        description.enableWordWrapping = true;
    }

    private static void MakeVolumeRow(Transform parent, Sprite sprite, TMP_FontAsset font, string label,
        out Slider slider, out TMP_Text valueText)
    {
        var row = MakeRect(label.Replace(" ", "") + "Row", parent);
        var element = row.gameObject.AddComponent<LayoutElement>(); element.minHeight = 48; element.preferredHeight = 62; element.flexibleHeight = 1;
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12; layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlWidth = true;
        layout.childControlHeight = true; layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;

        var labelText = MakeText("Label", row, font, label, 27, Ivory, TextAlignmentOptions.Left);
        var labelElement = labelText.gameObject.AddComponent<LayoutElement>(); labelElement.flexibleWidth = 4f; labelElement.minWidth = 180;

        var sliderRect = MakeRect("Slider", row);
        var sliderElement = sliderRect.gameObject.AddComponent<LayoutElement>(); sliderElement.flexibleWidth = 4.3f; sliderElement.minWidth = 180; sliderElement.minHeight = 44;
        slider = sliderRect.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1; slider.wholeNumbers = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        var hitArea = MakeImage("HitArea", sliderRect, sprite, new Color(1f, 1f, 1f, 0f), true);
        Stretch(hitArea.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var background = MakeImage("Track", sliderRect, sprite, new Color(0.22f, 0.23f, 0.24f, 1f), true);
        Stretch(background.rectTransform, new Vector2(0, 0.46f), new Vector2(1, 0.54f), Vector2.zero, Vector2.zero);
        var fillArea = MakeRect("Fill Area", sliderRect);
        Stretch(fillArea, new Vector2(0, 0.35f), new Vector2(1, 0.65f), new Vector2(6, 0), new Vector2(-6, 0));
        var fill = MakeImage("Fill", fillArea, sprite, Pale, false);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var handleArea = MakeRect("Handle Slide Area", sliderRect);
        Stretch(handleArea, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(7, -8), new Vector2(-7, 8));
        var handle = MakeImage("Handle", handleArea, sprite, Pale, true);
        Stretch(handle.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(-7, 0), new Vector2(7, 0));
        handle.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = hitArea;
        slider.direction = Slider.Direction.LeftToRight;


        handle.rectTransform.sizeDelta = new Vector2(14, 0);
        handle.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

        valueText = MakeText("Value", row, font, "0.50", 26, Ivory, TextAlignmentOptions.Right);
        var valueElement = valueText.gameObject.AddComponent<LayoutElement>(); valueElement.flexibleWidth = 1.7f; valueElement.minWidth = 86; valueElement.preferredWidth = 90;
    }

    private static void MakeTab(Transform parent, Sprite sprite, TMP_FontAsset font, string text, out Button button, out Image image)
    {
        var go = new GameObject(text + "Tab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.localScale = Vector3.one;
        var element = go.GetComponent<LayoutElement>(); element.minHeight = 48; element.preferredHeight = 56; element.flexibleWidth = 1;
        image = go.GetComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Sliced; image.color = new Color(0.19f, 0.19f, 0.20f, 0.94f);
        button = go.GetComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        var block = button.colors; block.normalColor = Color.white; block.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1); block.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1); button.colors = block;
        var label = MakeText("Label", go.transform, font, text, 27, Muted, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 2), new Vector2(-8, -2));
        label.raycastTarget = false;
    }

    private static Button MakeButton(string name, Transform parent, Sprite sprite, TMP_FontAsset font, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Sliced; image.color = color;
        var button = go.GetComponent<Button>(); button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        var label = MakeText("Label", go.transform, font, text, size, Ivory, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); label.raycastTarget = false;
        return button;
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.sprite = sprite; image.type = sprite == null ? Image.Type.Simple : Image.Type.Sliced;
        image.color = color; image.raycastTarget = raycast;
        return image;
    }

    private static TMP_Text MakeText(string name, Transform parent, TMP_FontAsset font, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.font = font; tmp.text = text; tmp.fontSize = size;
        tmp.color = color; tmp.alignment = alignment; tmp.enableAutoSizing = true; tmp.fontSizeMin = Mathf.Max(16, size - 4); tmp.fontSizeMax = size;
        tmp.raycastTarget = false; tmp.margin = new Vector4(2, 1, 2, 1);
        return tmp;
    }

    private static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.localScale = Vector3.one; return rect;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }
}






