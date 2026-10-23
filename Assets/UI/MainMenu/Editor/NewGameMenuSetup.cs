using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Editor command that creates the new/load screens and wires them to the existing menu.</summary>
public static class NewGameMenuSetup
{
    // Shared design tokens and asset path for the generated screen hierarchy.
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/FontdinerSwanky-Regular SDF.asset";
    private static readonly Color Ivory = new Color(0.88f, 0.85f, 0.79f, 1f);
    private static readonly Color Muted = new Color(0.70f, 0.69f, 0.66f, 1f);
    private static readonly Color Panel = new Color(0.16f, 0.085f, 0.095f, 0.91f);

    [MenuItem("Tools/Devils Harvest/Build New and Load Game Menus")]
    public static void Build()
    {
        // Rebuilds only the SaveGameScreens subtree; the Canvas and main menu remain in place.
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
        Sprite[] levelPreviews = EnsureLevelPreviewSprites();
        if (levelPreviews == null || levelPreviews.Length < 2 || levelPreviews[0] == null || levelPreviews[1] == null)
        { Debug.LogError("Build stopped: both level preview sprites must import successfully."); return; }

        Undo.RegisterFullObjectHierarchyUndo(canvasObject, "Build New and Load Game Menus");
        var old = canvasObject.transform.Find("SaveGameScreens");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var root = Rect("SaveGameScreens", canvasObject.transform);
        Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var newScreen = BuildScreen(root, font, "NewGameScreen", "New Game");
        var loadScreen = BuildScreen(root, font, "LoadGameScreen", "Load Game");

        var levelPrompt = Text("LevelSelectHeading", newScreen, font, "Choose Your Starting Point", 23, Ivory, TextAlignmentOptions.Center);
        Stretch(levelPrompt.rectTransform, new Vector2(.29f, .67f), new Vector2(.72f, .72f), Vector2.zero, Vector2.zero);
        var levelButtons = new Button[2];
        var levelFrames = new Image[2];
        var levelImages = new Image[2];
        var levelCaptions = new TMP_Text[2];
        string[] levelNames = { "Main Level", "Boss Fight" };
        for (int i = 0; i < 2; i++)
        {
            float left = i == 0 ? .29f : .51f;
            float right = i == 0 ? .49f : .72f;
            MakeLevelCard(newScreen, font, levelNames[i] + "Card", levelNames[i], levelPreviews[i],
                new Vector2(left, .32f), new Vector2(right, .55f), out levelButtons[i], out levelFrames[i], out levelImages[i], out levelCaptions[i]);
        }

        var nameLabel = Text("SaveNameLabel", newScreen, font, "Harvest Name", 23, Ivory, TextAlignmentOptions.Left);
        Stretch(nameLabel.rectTransform, new Vector2(.30f, .63f), new Vector2(.70f, .67f), Vector2.zero, Vector2.zero);
        var input = MakeInput(newScreen, font);
        Stretch(input.GetComponent<RectTransform>(), new Vector2(.30f, .58f), new Vector2(.70f, .63f), Vector2.zero, Vector2.zero);
        var start = MakeButton("StartHarvestButton", newScreen, font, "Start Harvest", 27, new Color(.32f, .23f, .20f, .98f));
        Stretch(start.GetComponent<RectTransform>(), new Vector2(.54f, .20f), new Vector2(.70f, .25f), Vector2.zero, Vector2.zero);
        var newBack = MakeButton("BackButton", newScreen, font, "Back", 24, new Color(.22f, .19f, .19f, .98f));
        Stretch(newBack.GetComponent<RectTransform>(), new Vector2(.30f, .20f), new Vector2(.46f, .25f), Vector2.zero, Vector2.zero);
        var newMessage = Text("Message", newScreen, font, "", 17, Muted, TextAlignmentOptions.Center);
        Stretch(newMessage.rectTransform, new Vector2(.30f, .26f), new Vector2(.70f, .29f), Vector2.zero, Vector2.zero);

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
        controller.levelButtons = levelButtons; controller.levelCardFrames = levelFrames; controller.levelPreviewImages = levelImages; controller.levelCaptions = levelCaptions;
        controller.levelIds = new[] { "main-level", "boss-fight" }; controller.levelNames = levelNames;
        controller.levelSceneNames = new[] { "", "" }; controller.levelPreviewSprites = levelPreviews;
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
        var border = Image("PanelBorder", screen, new Color(.63f, .67f, .70f, 1f), false); Stretch(border.rectTransform, new Vector2(.235f, .17f), new Vector2(.765f, .83f), Vector2.zero, Vector2.zero);
        var surface = Image("PanelSurface", border.transform, Panel, false); Stretch(surface.rectTransform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
        var heading = Text("SectionHeading", surface.transform, font, title, 39, Ivory, TextAlignmentOptions.Center);
        Stretch(heading.rectTransform, new Vector2(.08f, .78f), new Vector2(.92f, .93f), Vector2.zero, Vector2.zero);
        heading.outlineWidth = .05f; heading.outlineColor = new Color(.05f, .03f, .03f, .85f);
        return screen;
    }

    private static void MakeLevelCard(Transform parent, TMP_FontAsset font, string name, string caption, Sprite previewSprite,
        Vector2 anchorMin, Vector2 anchorMax, out Button button, out Image frame, out Image preview, out TMP_Text captionText)
    {
        var card = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);
        var cardRect = card.GetComponent<RectTransform>();
        Stretch(cardRect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        frame = card.GetComponent<Image>();
        frame.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        frame.type = UnityEngine.UI.Image.Type.Sliced;
        frame.color = name.StartsWith("Main Level", StringComparison.Ordinal)
            ? new Color32(0x99, 0x8A, 0x73, 0xFF)
            : new Color32(0x57, 0x4C, 0x4A, 0xFF);
        button = card.GetComponent<Button>();
        button.targetGraphic = frame;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };

        var previewObject = new GameObject("PreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        previewObject.transform.SetParent(card.transform, false);
        var previewRect = previewObject.GetComponent<RectTransform>();
        Stretch(previewRect, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
        preview = previewObject.GetComponent<Image>();
        preview.sprite = previewSprite;
        preview.type = UnityEngine.UI.Image.Type.Simple;
        preview.preserveAspect = true;
        preview.raycastTarget = false;

        captionText = Text("LevelCaption", parent, font, caption, 22, Ivory, TextAlignmentOptions.Center);
        Stretch(captionText.rectTransform,
            new Vector2(anchorMin.x, anchorMin.y - .028f),
            new Vector2(anchorMax.x, anchorMin.y - .004f), Vector2.zero, Vector2.zero);
        captionText.fontSizeMin = 16;
        captionText.fontSizeMax = 22;
        captionText.raycastTarget = false;
    }

    private static Sprite[] EnsureLevelPreviewSprites()
    {
        const string folder = "Assets/UI/MainMenu/LevelPreviews";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/UI/MainMenu", "LevelPreviews");
        string mainPath = folder + "/MainLevelPreview.png";
        string bossPath = folder + "/BossFightPreview.png";
        EnsurePreviewTexture(mainPath, false);
        EnsurePreviewTexture(bossPath, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>(mainPath),
            AssetDatabase.LoadAssetAtPath<Sprite>(bossPath)
        };
    }

    private static void EnsurePreviewTexture(string assetPath, bool bossFight)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string absolutePath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        if (!File.Exists(absolutePath))
        {
            const int width = 384;
            const int height = 216;
            var pixels = new Color32[width * height];
            if (bossFight) PaintBossPreview(pixels, width, height);
            else PaintMainPreview(pixels, width, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || !importer.alphaIsTransparency))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static void PaintMainPreview(Color32[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            Color32 sky = Color32.Lerp(new Color32(54, 66, 78, 255), new Color32(18, 29, 43, 255), t);
            for (int x = 0; x < width; x++) pixels[y * width + x] = sky;
        }
        DrawCircle(pixels, width, height, 296, 157, 39, new Color32(180, 143, 105, 55));
        DrawCircle(pixels, width, height, 296, 157, 26, new Color32(224, 192, 148, 255));
        var random = new System.Random(17);
        for (int i = 0; i < 45; i++)
            SetPixel(pixels, width, height, random.Next(8, width - 8), random.Next(102, height - 12), new Color32(206, 198, 172, 190));

        DrawRect(pixels, width, height, 0, 0, width, 48, new Color32(31, 31, 35, 255));
        DrawRect(pixels, width, height, 0, 44, width, 10, new Color32(49, 51, 51, 255));
        // A distant farmhouse silhouette anchors the harvest field.
        DrawRect(pixels, width, height, 56, 54, 52, 49, new Color32(27, 29, 31, 255));
        DrawLine(pixels, width, height, 48, 101, 82, 126, new Color32(24, 26, 29, 255), 7);
        DrawLine(pixels, width, height, 82, 126, 118, 101, new Color32(24, 26, 29, 255), 7);
        DrawRect(pixels, width, height, 76, 54, 13, 30, new Color32(15, 17, 20, 255));
        // Wheat rows and seed heads in the foreground.
        for (int i = 0; i < 19; i++)
        {
            int x = 8 + i * 21;
            int top = 57 + (i * 13 % 33);
            DrawLine(pixels, width, height, x, 0, x + (i % 2 == 0 ? 8 : -7), top, new Color32(145, 112, 69, 210), 2);
            DrawLine(pixels, width, height, x + (i % 2 == 0 ? 8 : -7), top, x + (i % 2 == 0 ? 1 : -13), top + 10, new Color32(186, 144, 83, 220), 2);
        }
        DrawLine(pixels, width, height, 0, 50, width, 50, new Color32(91, 83, 69, 255), 2);
    }

    private static void PaintBossPreview(Color32[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            Color32 sky = Color32.Lerp(new Color32(72, 48, 57, 255), new Color32(24, 23, 34, 255), t);
            for (int x = 0; x < width; x++) pixels[y * width + x] = sky;
        }
        DrawCircle(pixels, width, height, 192, 150, 50, new Color32(135, 55, 59, 45));
        DrawCircle(pixels, width, height, 192, 150, 31, new Color32(183, 104, 93, 255));
        // A ruined arena, with a central gate and a glowing ritual ring.
        DrawRect(pixels, width, height, 0, 0, width, 44, new Color32(29, 25, 31, 255));
        DrawLine(pixels, width, height, 30, 48, 192, 63, new Color32(76, 49, 52, 255), 3);
        DrawLine(pixels, width, height, 192, 63, 354, 48, new Color32(76, 49, 52, 255), 3);
        DrawRect(pixels, width, height, 28, 53, 29, 81, new Color32(25, 24, 30, 255));
        DrawRect(pixels, width, height, 327, 53, 29, 81, new Color32(25, 24, 30, 255));
        DrawLine(pixels, width, height, 19, 132, 43, 158, new Color32(22, 22, 29, 255), 9);
        DrawLine(pixels, width, height, 43, 158, 66, 132, new Color32(22, 22, 29, 255), 9);
        DrawLine(pixels, width, height, 318, 132, 341, 158, new Color32(22, 22, 29, 255), 9);
        DrawLine(pixels, width, height, 341, 158, 365, 132, new Color32(22, 22, 29, 255), 9);
        DrawEllipseRing(pixels, width, height, 192, 43, 120, 24, new Color32(151, 63, 58, 255), 3);
        DrawEllipseRing(pixels, width, height, 192, 43, 92, 17, new Color32(107, 51, 53, 255), 2);
        DrawRect(pixels, width, height, 181, 47, 22, 53, new Color32(48, 35, 40, 255));
        DrawLine(pixels, width, height, 171, 99, 192, 113, new Color32(96, 49, 49, 255), 4);
        DrawLine(pixels, width, height, 192, 113, 213, 99, new Color32(96, 49, 49, 255), 4);
        var random = new System.Random(43);
        for (int i = 0; i < 28; i++)
            SetPixel(pixels, width, height, random.Next(6, width - 6), random.Next(46, 103), new Color32(186, 93, 74, 195));
    }

    private static void DrawRect(Color32[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color32 color)
    {
        for (int py = Mathf.Max(0, y); py < Mathf.Min(height, y + rectHeight); py++)
            for (int px = Mathf.Max(0, x); px < Mathf.Min(width, x + rectWidth); px++) SetPixel(pixels, width, height, px, py, color);
    }

    private static void DrawCircle(Color32[] pixels, int width, int height, int cx, int cy, int radius, Color32 color)
    {
        int radiusSquared = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= radiusSquared) SetPixel(pixels, width, height, x, y, color);
            }
    }

    private static void DrawEllipseRing(Color32[] pixels, int width, int height, int cx, int cy, int rx, int ry, Color32 color, float thickness)
    {
        for (int y = Mathf.Max(0, cy - ry - 3); y <= Mathf.Min(height - 1, cy + ry + 3); y++)
            for (int x = Mathf.Max(0, cx - rx - 3); x <= Mathf.Min(width - 1, cx + rx + 3); x++)
            {
                float nx = (float)(x - cx) / rx, ny = (float)(y - cy) / ry;
                float distance = Mathf.Abs(Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(rx, ry);
                if (distance <= thickness) SetPixel(pixels, width, height, x, y, color);
            }
    }

    private static void DrawLine(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;
        while (true)
        {
            DrawRect(pixels, width, height, x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, color);
            if (x0 == x1 && y0 == y1) break;
            int twiceError = 2 * error;
            if (twiceError >= dy) { error += dy; x0 += sx; }
            if (twiceError <= dx) { error += dx; y0 += sy; }
        }
    }

    private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int index = y * width + x;
        if (color.a == 255) { pixels[index] = color; return; }
        Color32 background = pixels[index];
        int alpha = color.a;
        int inverseAlpha = 255 - alpha;
        pixels[index] = new Color32(
            (byte)((color.r * alpha + background.r * inverseAlpha) / 255),
            (byte)((color.g * alpha + background.g * inverseAlpha) / 255),
            (byte)((color.b * alpha + background.b * inverseAlpha) / 255), 255);
    }

    private static TMP_InputField MakeInput(Transform parent, TMP_FontAsset font)
    {
        var go = new GameObject("SaveNameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField)); go.transform.SetParent(parent, false);
        var bg = go.GetComponent<Image>(); bg.color = new Color(.09f, .075f, .075f, .98f);
        var text = Text("Text", go.transform, font, "", 24, Ivory, TextAlignmentOptions.Left); Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 6), new Vector2(-14, -6)); text.textWrappingMode = TextWrappingModes.NoWrap;
        var placeholder = Text("Placeholder", go.transform, font, "Enter a name for this harvest...", 22, new Color(.63f, .60f, .58f, .8f), TextAlignmentOptions.Left); Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 6), new Vector2(-14, -6)); placeholder.fontStyle = FontStyles.Italic; placeholder.textWrappingMode = TextWrappingModes.NoWrap;
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
