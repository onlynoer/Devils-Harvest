using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class NewGameMenuSetup
{
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/FontdinerSwanky-Regular SDF.asset";
    private static readonly Color Ivory = new Color(0.88f, 0.85f, 0.79f, 1f);
    private static readonly Color Muted = new Color(0.70f, 0.69f, 0.66f, 1f);
    private static readonly Color Panel = new Color(0.16f, 0.085f, 0.095f, 0.91f);

    [MenuItem("Tools/Devils Harvest/Build New and Load Game Menus")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "MainMenu") { Debug.LogError("Open the MainMenu scene before building the save screens."); return; }
        var canvasObject = GameObject.Find("Canvas");
        var menuObject = GameObject.Find("MenuManager");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (canvasObject == null || menuObject == null || font == null) { Debug.LogError("Build stopped: Canvas, MenuManager, or the project FontdinerSwanky TMP font is missing."); return; }
        var buttons = canvasObject.transform.Find("MainMenuPanel/ButtonContainer");
        var newButtonTransform = canvasObject.transform.Find("MainMenuPanel/ButtonContainer/NewGameButton");
        var loadButtonTransform = canvasObject.transform.Find("MainMenuPanel/ButtonContainer/LoadGameButton");
        if (buttons == null || newButtonTransform == null || loadButtonTransform == null) { Debug.LogError("Build stopped: expected MainMenuPanel/ButtonContainer and its NewGameButton and LoadGameButton."); return; }
        var newButton = newButtonTransform.GetComponent<Button>();
        var loadButton = loadButtonTransform.GetComponent<Button>();
        if (newButton == null || loadButton == null) { Debug.LogError("Build stopped: NewGameButton and LoadGameButton need Unity Button components."); return; }

        Undo.RegisterFullObjectHierarchyUndo(canvasObject, "Build New and Load Game Menus");
        var old = canvasObject.transform.Find("SaveGameScreens");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var root = Rect("SaveGameScreens", canvasObject.transform);
        Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var newScreen = BuildScreen(root, font, "NewGameScreen", "New Game");
        var loadScreen = BuildScreen(root, font, "LoadGameScreen", "Load Game");

        var nameLabel = Text("SaveNameLabel", newScreen, font, "Harvest Name", 25, Ivory, TextAlignmentOptions.Left);
        Stretch(nameLabel.rectTransform, new Vector2(.30f, .546f), new Vector2(.70f, .606f), Vector2.zero, Vector2.zero);
        var input = MakeInput(newScreen, font);
        Stretch(input.GetComponent<RectTransform>(), new Vector2(.30f, .474f), new Vector2(.70f, .546f), Vector2.zero, Vector2.zero);
        var start = MakeButton("StartHarvestButton", newScreen, font, "Start Harvest", 27, new Color(.32f, .23f, .20f, .98f));
        Stretch(start.GetComponent<RectTransform>(), new Vector2(.54f, .249f), new Vector2(.70f, .335f), Vector2.zero, Vector2.zero);
        var newBack = MakeButton("BackButton", newScreen, font, "Back", 24, new Color(.22f, .19f, .19f, .98f));
        Stretch(newBack.GetComponent<RectTransform>(), new Vector2(.30f, .249f), new Vector2(.46f, .335f), Vector2.zero, Vector2.zero);
        var newMessage = Text("Message", newScreen, font, "", 19, Muted, TextAlignmentOptions.Center);
        Stretch(newMessage.rectTransform, new Vector2(.30f, .36f), new Vector2(.70f, .434f), Vector2.zero, Vector2.zero);

        var frame = Image("SaveListFrame", loadScreen, new Color(.61f, .63f, .64f, .88f), false);
        Stretch(frame.rectTransform, new Vector2(.258f, .35f), new Vector2(.742f, .68f), Vector2.zero, Vector2.zero);
        var viewport = Rect("SaveListViewport", frame.transform);
        Stretch(viewport, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
        var viewportImage = viewport.gameObject.AddComponent<Image>(); viewportImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); viewportImage.type = UnityEngine.UI.Image.Type.Sliced; viewportImage.color = new Color(.105f, .065f, .07f, .97f); viewportImage.raycastTarget = true;
        var roundedMask = viewport.gameObject.AddComponent<Mask>(); roundedMask.showMaskGraphic = true;
        // Keep a hard viewport clip as well as the rounded stencil mask, so rows never draw outside the frame.
        viewport.gameObject.AddComponent<RectMask2D>();
        var list = Rect("SaveList", viewport);
        // Keep row cards inset from the rounded frame and leave a clear gutter for the scrollbar.
        list.anchorMin = new Vector2(.04f, 1); list.anchorMax = new Vector2(.89f, 1); list.pivot = new Vector2(.5f, 1); list.sizeDelta = Vector2.zero;
        var layout = list.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 10; layout.padding = new RectOffset(0, 0, 18, 18); layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        var contentSize = list.gameObject.AddComponent<ContentSizeFitter>(); contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scrollbar = MakeScrollbar(viewport);
        Stretch(scrollbar.GetComponent<RectTransform>(), new Vector2(.93f, .035f), new Vector2(.965f, .965f), Vector2.zero, Vector2.zero);
        var scroll = loadScreen.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = list; scroll.verticalScrollbar = scrollbar; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent; scroll.scrollSensitivity = 28;
        var loadBack = MakeButton("BackButton", loadScreen, font, "Back", 24, new Color(.22f, .19f, .19f, .98f));
        Stretch(loadBack.GetComponent<RectTransform>(), new Vector2(.30f, .249f), new Vector2(.46f, .335f), Vector2.zero, Vector2.zero);
        var loadMessage = Text("Message", loadScreen, font, "", 19, Muted, TextAlignmentOptions.Center);
        Stretch(loadMessage.rectTransform, new Vector2(.30f, .34f), new Vector2(.70f, .365f), Vector2.zero, Vector2.zero);

        var controller = menuObject.GetComponent<NewGameMenuController>();
        if (controller == null) controller = Undo.AddComponent<NewGameMenuController>(menuObject);
        Undo.RecordObject(controller, "Connect save menus");
        controller.buttonContainer = buttons.gameObject; controller.newGameScreen = newScreen.gameObject; controller.loadGameScreen = loadScreen.gameObject;
        controller.saveNameInput = input; controller.messageText = newMessage; controller.loadMessageText = loadMessage; controller.saveListRoot = list; controller.bodyFont = font;
        controller.newGameButton = newButton; controller.loadGameButton = loadButton; controller.startHarvestButton = start;
        controller.newGameBackButton = newBack; controller.loadGameBackButton = loadBack;
        controller.Bind();
        newScreen.gameObject.SetActive(false); loadScreen.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("New Game and Load Game save screens built and connected in MainMenu.");
    }

    private static RectTransform BuildScreen(Transform root, TMP_FontAsset font, string name, string title)
    {
        var screen = Rect(name, root);
        Stretch(screen, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var dim = Image("Dimmer", screen, new Color(.025f, .035f, .05f, .57f), true); Stretch(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var border = Image("PanelBorder", screen, new Color(.63f, .67f, .70f, .82f), false); Stretch(border.rectTransform, new Vector2(.235f, .17f), new Vector2(.765f, .83f), Vector2.zero, Vector2.zero);
        var surface = Image("PanelSurface", border.transform, Panel, false); Stretch(surface.rectTransform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
        var heading = Text("SectionHeading", surface.transform, font, title, 39, Ivory, TextAlignmentOptions.Center);
        Stretch(heading.rectTransform, new Vector2(.08f, .78f), new Vector2(.92f, .93f), Vector2.zero, Vector2.zero);
        heading.outlineWidth = .05f; heading.outlineColor = new Color(.05f, .03f, .03f, .85f);
        return screen;
    }

    private static TMP_InputField MakeInput(Transform parent, TMP_FontAsset font)
    {
        var go = new GameObject("SaveNameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField)); go.transform.SetParent(parent, false);
        var bg = go.GetComponent<Image>(); bg.color = new Color(.09f, .075f, .075f, .98f);
        var text = Text("Text", go.transform, font, "", 24, Ivory, TextAlignmentOptions.Left); Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 6), new Vector2(-14, -6)); text.enableWordWrapping = false;
        var placeholder = Text("Placeholder", go.transform, font, "Enter a name for this harvest...", 22, new Color(.63f, .60f, .58f, .8f), TextAlignmentOptions.Left); Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 6), new Vector2(-14, -6)); placeholder.fontStyle = FontStyles.Italic; placeholder.enableWordWrapping = false;
        var field = go.GetComponent<TMP_InputField>(); field.textViewport = go.GetComponent<RectTransform>(); field.textComponent = text; field.placeholder = placeholder; field.characterLimit = 32; field.lineType = TMP_InputField.LineType.SingleLine; field.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        return field;
    }

    private static Button MakeButton(string name, Transform parent, TMP_FontAsset font, string label, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.color = color; var button = go.GetComponent<Button>(); button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        var text = Text("Label", go.transform, font, label, size, Ivory, TextAlignmentOptions.Center); Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 2), new Vector2(-6, -2)); text.raycastTarget = false;
        return button;
    }

    private static Scrollbar MakeScrollbar(Transform parent)
    {
        var go = new GameObject("SaveListScrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar)); go.transform.SetParent(parent, false);
        var track = go.GetComponent<Image>(); track.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); track.type = UnityEngine.UI.Image.Type.Sliced; track.color = new Color(.15f, .13f, .13f, .95f); track.raycastTarget = true;
        var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); handleObject.transform.SetParent(go.transform, false);
        var handle = handleObject.GetComponent<Image>(); handle.sprite = track.sprite; handle.type = UnityEngine.UI.Image.Type.Sliced; handle.color = new Color(.72f, .69f, .64f, .95f); handle.raycastTarget = true;
        var handleRect = handleObject.GetComponent<RectTransform>(); handleRect.anchorMin = Vector2.zero; handleRect.anchorMax = Vector2.one; handleRect.offsetMin = new Vector2(3, 3); handleRect.offsetMax = new Vector2(-3, -3);
        var scrollbar = go.GetComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop; scrollbar.handleRect = handleRect; scrollbar.targetGraphic = handle; scrollbar.value = 1; scrollbar.size = .25f; scrollbar.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        return scrollbar;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment; text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = size; text.raycastTarget = false; text.margin = new Vector4(2, 1, 2, 1); return text;
    }
    private static Image Image(string name, Transform parent, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent, false); var image = go.GetComponent<UnityEngine.UI.Image>(); image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color; image.raycastTarget = raycast; return image;
    }
    private static RectTransform Rect(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); go.transform.localScale = Vector3.one; return go.GetComponent<RectTransform>(); }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; rect.localScale = Vector3.one; }
}
