using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjectAurora.UI.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class AuroraMenuCardsBuilder
{
    private const string BasePath = "Assets/_ProjectAurora";
    private const string SourceMenuScenePath = BasePath + "/Scenes/MainMenu.unity";
    private const string MenuScenePath = BasePath + "/Scenes/MainMenu.unity";
    private const string CardsArtPath = BasePath + "/Art/UI/Menu/Cards";
    private const string MenuPrefabPath = BasePath + "/Prefabs/UI/Menu/PF_AuroraMenuCard.prefab";
    private const string RequestPath = "Temp/AuroraMenuCardsBuild.request";
    private const string ResultPath = "Temp/AuroraMenuCardsBuild.result";

    private static readonly Color32 DarkA = Hex("07111F");
    private static readonly Color32 DarkB = Hex("0A1A2F");
    private static readonly Color32 DarkC = Hex("0D2740");
    private static readonly Color32 Border = Hex("1B4F7A");
    private static readonly Color32 InnerBorder = Hex("123D61");
    private static readonly Color32 Cyan = Hex("00C8FF");
    private static readonly Color32 White = Hex("FFFFFF");

    private sealed class CardDefinition
    {
        public string Key;
        public string Label;
        public AuroraMenuCardAction Action;
        public IconKind IconKind;
    }

    private enum IconKind
    {
        Play,
        Settings,
        Gem,
        UsersRound,
        Power
    }

    private static readonly CardDefinition[] Cards =
    {
        new CardDefinition { Key = "jogar", Label = "JOGAR", Action = AuroraMenuCardAction.StartGame, IconKind = IconKind.Play },
        new CardDefinition { Key = "configuracoes", Label = "CONFIGURA\u00C7\u00D5ES", Action = AuroraMenuCardAction.OpenSettings, IconKind = IconKind.Settings },
        new CardDefinition { Key = "extra", Label = "EXTRA", Action = AuroraMenuCardAction.OpenExtras, IconKind = IconKind.Gem },
        new CardDefinition { Key = "creditos", Label = "CR\u00C9DITOS", Action = AuroraMenuCardAction.OpenCredits, IconKind = IconKind.UsersRound },
        new CardDefinition { Key = "sair", Label = "SAIR", Action = AuroraMenuCardAction.QuitGame, IconKind = IconKind.Power }
    };

    static AuroraMenuCardsBuilder()
    {
        if (File.Exists(AbsolutePath(RequestPath)))
        {
            EditorApplication.delayCall += RunRequestedBuild;
        }
    }

    [MenuItem("Tools/Projeto Aurora/Menu/Build PNG Card Menu")]
    public static void BuildMenuCards()
    {
        EnsureFolders();
        GenerateCardAssets();
        GameObject prefab = CreateCardPrefab();
        BuildMenuScene(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PROJETO:AURORA - MainMenu com cards PNG gerado.");
    }

    private static void RunRequestedBuild()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunRequestedBuild;
            return;
        }

        try
        {
            File.Delete(AbsolutePath(RequestPath));
            BuildMenuCards();
            File.WriteAllText(AbsolutePath(ResultPath), "SUCCESS: MainMenu PNG cards generated.", Encoding.UTF8);
        }
        catch (Exception exception)
        {
            File.WriteAllText(AbsolutePath(ResultPath), "FAILED: " + exception, Encoding.UTF8);
            Debug.LogException(exception);
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder(BasePath + "/Art");
        EnsureFolder(BasePath + "/Art/UI");
        EnsureFolder(BasePath + "/Art/UI/Menu");
        EnsureFolder(CardsArtPath);
        EnsureFolder(BasePath + "/Prefabs");
        EnsureFolder(BasePath + "/Prefabs/UI");
        EnsureFolder(BasePath + "/Prefabs/UI/Menu");
        EnsureFolder(BasePath + "/Scripts");
        EnsureFolder(BasePath + "/Scripts/UI");
        EnsureFolder(BasePath + "/Scripts/UI/Menu");
        EnsureFolder(BasePath + "/Scenes");
    }

    private static void GenerateCardAssets()
    {
        foreach (CardDefinition card in Cards)
        {
            WritePng(card, false);
            WritePng(card, true);
        }

        AssetDatabase.Refresh();
        foreach (CardDefinition card in Cards)
        {
            ConfigureSprite(PngPath(card, false));
            ConfigureSprite(PngPath(card, true));
        }
        AssetDatabase.Refresh();
    }


    private static void WritePng(CardDefinition card, bool active)
    {
        Texture2D texture = RenderCardTexture(card, active, 2);
        File.WriteAllBytes(AbsolutePath(PngPath(card, active)), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static GameObject CreateCardPrefab()
    {
        GameObject root = new GameObject("PF_AuroraMenuCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(AuroraMenuCard));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 52f);

        Image image = root.GetComponent<Image>();
        image.raycastTarget = true;
        image.preserveAspect = true;

        Button button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredWidth = 320f;
        layout.preferredHeight = 52f;
        layout.minWidth = 320f;
        layout.minHeight = 52f;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(root.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(74f, 0f);
        labelRect.offsetMax = new Vector2(-18f, 0f);

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "JOGAR";
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = White;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        AuroraMenuCard card = root.GetComponent<AuroraMenuCard>();
        Sprite inactive = LoadSprite(PngPath(Cards[0], false));
        Sprite active = LoadSprite(PngPath(Cards[0], true));
        card.Configure(Cards[0].Label, Cards[0].Action, inactive, active, true);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildMenuScene(GameObject cardPrefab)
    {
        Scene scene = File.Exists(AbsolutePath(SourceMenuScenePath))
            ? EditorSceneManager.OpenScene(SourceMenuScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        if (!EditorSceneManager.SaveScene(scene, MenuScenePath, true))
        {
            throw new InvalidOperationException("Nao foi possivel atualizar MainMenu.unity.");
        }

        scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        Canvas canvas = FindOrCreateCanvas();
        RectTransform menuRoot = FindOrCreateMenuRoot(canvas.transform);

        RemoveOldMenuController(canvas.gameObject);
        RemoveOldButtons(menuRoot);
        RectTransform container = CreateCardsContainer(menuRoot);
        GameObject settings = FindSceneObject("Panel_Settings");
        GameObject extras = FindSceneObject("Panel_Extra");
        GameObject credits = FindSceneObject("Panel_Credits");

        AuroraMainMenuController controller = canvas.gameObject.GetComponent<AuroraMainMenuController>();
        if (controller == null)
        {
            controller = canvas.gameObject.AddComponent<AuroraMainMenuController>();
        }

        for (int i = 0; i < Cards.Length; i++)
        {
            AuroraMenuCard card = CreateCardInstance(cardPrefab, container, Cards[i], i == 0);
            controller.RegisterCard(card);
        }

        controller.SetCardsContainer(container);
        controller.SetPanels(settings, extras, credits);
        EnsureEventSystem();
        AddSceneToBuildSettingsAsFirst(MenuScenePath);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
    }

    private static AuroraMenuCard CreateCardInstance(GameObject prefab, Transform parent, CardDefinition definition, bool active)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException("Nao foi possivel instanciar PF_AuroraMenuCard.");
        }

        instance.name = "Card_" + definition.Label;
        AuroraMenuCard card = instance.GetComponent<AuroraMenuCard>();
        card.Configure(
            definition.Label,
            definition.Action,
            LoadSprite(PngPath(definition, false)),
            LoadSprite(PngPath(definition, true)),
            active);
        EditorUtility.SetDirty(card);
        return card;
    }

    private static RectTransform CreateCardsContainer(RectTransform menuRoot)
    {
        GameObject existing = FindChild(menuRoot, "AuroraMenuCards_Container");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject container = new GameObject("AuroraMenuCards_Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
        container.transform.SetParent(menuRoot, false);
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(116f, -382f);
        rect.sizeDelta = new Vector2(320f, 308f);

        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);
        return rect;
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).FirstOrDefault(item => item.name == "Canvas_MainMenu");
        if (canvas != null)
        {
            return canvas;
        }

        GameObject canvasObject = new GameObject("Canvas_MainMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static RectTransform FindOrCreateMenuRoot(Transform canvas)
    {
        Transform existing = canvas.Find("MenuRoot_16x9");
        if (existing != null)
        {
            return existing.GetComponent<RectTransform>();
        }

        GameObject root = new GameObject("MenuRoot_16x9", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1920f, 1080f);
        return rect;
    }

    private static void RemoveOldMenuController(GameObject canvasObject)
    {
        ProjectAurora.UI.MainMenuController old = canvasObject.GetComponent<ProjectAurora.UI.MainMenuController>();
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }
    }

    private static void RemoveOldButtons(RectTransform menuRoot)
    {
        string[] names =
        {
            "Button_Jogar",
            "Button_Configuracoes",
            "Button_Extra",
            "Button_Creditos",
            "Button_Sair",
            "AuroraMenuCards_Container"
        };

        foreach (string name in names)
        {
            GameObject found = FindChild(menuRoot, name);
            if (found != null)
            {
                Object.DestroyImmediate(found);
            }
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static GameObject FindSceneObject(string name)
    {
        return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Select(item => item.gameObject)
            .FirstOrDefault(item => item.name == name);
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private static Texture2D RenderCardTexture(CardDefinition card, bool active, int scale)
    {
        int width = 320 * scale;
        int height = 52 * scale;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32[] pixels = Enumerable.Repeat(clear, width * height).ToArray();
        texture.SetPixels32(pixels);

        Vector2[] body = ScalePolygon(scale, new Vector2(4, 6), new Vector2(18, 6), new Vector2(23, 1), new Vector2(286, 1), new Vector2(319, 26), new Vector2(286, 51), new Vector2(23, 51), new Vector2(18, 46), new Vector2(4, 46), new Vector2(1, 43), new Vector2(1, 9));
        Vector2[] inner = ScalePolygon(scale, new Vector2(9, 10), new Vector2(27, 10), new Vector2(31, 6), new Vector2(282, 6), new Vector2(310, 26), new Vector2(282, 46), new Vector2(31, 46), new Vector2(27, 42), new Vector2(9, 42));
        Vector2[] iconBox = ScalePolygon(scale, new Vector2(32, 14), new Vector2(48, 14), new Vector2(56, 26), new Vector2(48, 38), new Vector2(32, 38), new Vector2(24, 26));

        FillGradient(texture, body, DarkA, DarkB, DarkC, active ? (byte)255 : (byte)238);
        Stroke(texture, body, active ? Cyan : Border, active ? 2 * scale : scale, active ? (byte)230 : (byte)210, true);
        FillPolygon(texture, inner, new Color32(Cyan.r, Cyan.g, Cyan.b, active ? (byte)36 : (byte)22));
        Stroke(texture, inner, InnerBorder, scale, active ? (byte)230 : (byte)190, true);

        if (active)
        {
            Vector2[] highlight = ScalePolygon(scale, new Vector2(8, 8), new Vector2(28, 8), new Vector2(33, 3), new Vector2(284, 3), new Vector2(315, 26), new Vector2(284, 49), new Vector2(33, 49), new Vector2(28, 44), new Vector2(8, 44));
            Stroke(texture, highlight, Cyan, 2 * scale, 210, false);
            DrawLine(texture, 64 * scale, 18 * scale, 67 * scale, 26 * scale, Cyan, scale, 220);
            DrawLine(texture, 67 * scale, 26 * scale, 64 * scale, 34 * scale, Cyan, scale, 220);
        }

        DrawLine(texture, 42 * scale, 7 * scale, 136 * scale, 7 * scale, Cyan, scale, active ? (byte)150 : (byte)90);
        DrawLine(texture, 188 * scale, 45 * scale, 270 * scale, 45 * scale, Cyan, scale, active ? (byte)135 : (byte)70);
        DrawLine(texture, 12 * scale, 17 * scale, 20 * scale, 17 * scale, Cyan, scale, active ? (byte)130 : (byte)85);
        DrawLine(texture, 12 * scale, 35 * scale, 20 * scale, 35 * scale, Cyan, scale, active ? (byte)130 : (byte)85);
        DrawLine(texture, 294 * scale, 17 * scale, 306 * scale, 26 * scale, Cyan, scale, active ? (byte)130 : (byte)75);
        DrawLine(texture, 306 * scale, 26 * scale, 294 * scale, 35 * scale, Cyan, scale, active ? (byte)130 : (byte)75);

        FillPolygon(texture, iconBox, new Color32(6, 17, 29, active ? (byte)230 : (byte)190));
        Stroke(texture, iconBox, Cyan, scale, active ? (byte)230 : (byte)165, true);
        DrawIcon(texture, card.IconKind, scale, active ? (byte)255 : (byte)210);

        texture.Apply();
        return texture;
    }

    private static void DrawIcon(Texture2D texture, IconKind kind, int scale, byte alpha)
    {
        switch (kind)
        {
            case IconKind.Play:
                Stroke(texture, ScalePolygon(scale, new Vector2(36, 18), new Vector2(50, 26), new Vector2(36, 34)), Cyan, 2 * scale, alpha, true);
                break;
            case IconKind.Settings:
                DrawCircle(texture, 40 * scale, 26 * scale, 8 * scale, Cyan, 2 * scale, alpha);
                DrawCircle(texture, 40 * scale, 26 * scale, 3 * scale, Cyan, 2 * scale, alpha);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI * 0.25f;
                    DrawLine(texture, Mathf.RoundToInt((40 + Mathf.Cos(a) * 11) * scale), Mathf.RoundToInt((26 + Mathf.Sin(a) * 11) * scale), Mathf.RoundToInt((40 + Mathf.Cos(a) * 14) * scale), Mathf.RoundToInt((26 + Mathf.Sin(a) * 14) * scale), Cyan, 2 * scale, alpha);
                }
                break;
            case IconKind.Gem:
                Stroke(texture, ScalePolygon(scale, new Vector2(28, 23), new Vector2(34, 17), new Vector2(46, 17), new Vector2(52, 23), new Vector2(40, 36)), Cyan, 2 * scale, alpha, true);
                DrawLine(texture, 28 * scale, 23 * scale, 52 * scale, 23 * scale, Cyan, 2 * scale, alpha);
                DrawLine(texture, 34 * scale, 17 * scale, 40 * scale, 36 * scale, Cyan, scale, alpha);
                DrawLine(texture, 46 * scale, 17 * scale, 40 * scale, 36 * scale, Cyan, scale, alpha);
                break;
            case IconKind.UsersRound:
                DrawCircle(texture, 38 * scale, 23 * scale, 5 * scale, Cyan, 2 * scale, alpha);
                DrawArc(texture, 38 * scale, 38 * scale, 11 * scale, Mathf.PI, Mathf.PI * 2f, Cyan, 2 * scale, alpha);
                DrawArc(texture, 50 * scale, 38 * scale, 8 * scale, Mathf.PI * 1.15f, Mathf.PI * 1.85f, Cyan, 2 * scale, alpha);
                break;
            case IconKind.Power:
                DrawLine(texture, 40 * scale, 17 * scale, 40 * scale, 27 * scale, Cyan, 2 * scale, alpha);
                DrawArc(texture, 40 * scale, 27 * scale, 11 * scale, Mathf.PI * 0.72f, Mathf.PI * 2.28f, Cyan, 2 * scale, alpha);
                break;
        }
    }

    private static void ConfigureSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }


    private static string PngPath(CardDefinition card, bool active)
    {
        return $"{CardsArtPath}/card_{card.Key}_{(active ? "active" : "inactive")}.png";
    }

    private static string AbsolutePath(string assetPath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
    }

    private static void AddSceneToBuildSettingsAsFirst(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(item => item.path == scenePath || item.path == BasePath + "/Scenes/MenuPrincipal.unity");
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Vector2[] ScalePolygon(int scale, params Vector2[] points)
    {
        Vector2[] result = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            result[i] = points[i] * scale;
        }
        return result;
    }

    private static void FillGradient(Texture2D texture, Vector2[] polygon, Color32 left, Color32 middle, Color32 right, byte alpha)
    {
        int width = texture.width;
        int height = texture.height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!PointInPolygon(new Vector2(x, y), polygon))
                {
                    continue;
                }

                float t = x / (float)(width - 1);
                Color32 color = t < 0.45f
                    ? Lerp(left, middle, t / 0.45f)
                    : Lerp(middle, right, (t - 0.45f) / 0.55f);
                color.a = alpha;
                Blend(texture, x, height - 1 - y, color);
            }
        }
    }

    private static void FillPolygon(Texture2D texture, Vector2[] polygon, Color32 color)
    {
        int width = texture.width;
        int height = texture.height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (PointInPolygon(new Vector2(x, y), polygon))
                {
                    Blend(texture, x, height - 1 - y, color);
                }
            }
        }
    }

    private static void Stroke(Texture2D texture, Vector2[] polygon, Color32 color, int thickness, byte alpha, bool close)
    {
        for (int i = 0; i < polygon.Length - 1; i++)
        {
            DrawLine(texture, Mathf.RoundToInt(polygon[i].x), Mathf.RoundToInt(polygon[i].y), Mathf.RoundToInt(polygon[i + 1].x), Mathf.RoundToInt(polygon[i + 1].y), color, thickness, alpha);
        }
        if (close)
        {
            DrawLine(texture, Mathf.RoundToInt(polygon[polygon.Length - 1].x), Mathf.RoundToInt(polygon[polygon.Length - 1].y), Mathf.RoundToInt(polygon[0].x), Mathf.RoundToInt(polygon[0].y), color, thickness, alpha);
        }
    }

    private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color, int thickness, byte alpha)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = -Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            DrawBrush(texture, x0, y0, color, thickness, alpha);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }
            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color32 color, int thickness, byte alpha)
    {
        DrawArc(texture, cx, cy, radius, 0f, Mathf.PI * 2f, color, thickness, alpha);
    }

    private static void DrawArc(Texture2D texture, int cx, int cy, int radius, float start, float end, Color32 color, int thickness, byte alpha)
    {
        int steps = Mathf.Max(12, Mathf.CeilToInt(radius * Mathf.Abs(end - start) * 0.5f));
        Vector2 previous = Vector2.zero;
        for (int i = 0; i <= steps; i++)
        {
            float t = Mathf.Lerp(start, end, i / (float)steps);
            Vector2 current = new Vector2(cx + Mathf.Cos(t) * radius, cy + Mathf.Sin(t) * radius);
            if (i > 0)
            {
                DrawLine(texture, Mathf.RoundToInt(previous.x), Mathf.RoundToInt(previous.y), Mathf.RoundToInt(current.x), Mathf.RoundToInt(current.y), color, thickness, alpha);
            }
            previous = current;
        }
    }

    private static void DrawBrush(Texture2D texture, int x, int y, Color32 color, int thickness, byte alpha)
    {
        int radius = Mathf.Max(1, thickness) / 2;
        Color32 c = color;
        c.a = alpha;
        for (int oy = -radius; oy <= radius; oy++)
        {
            for (int ox = -radius; ox <= radius; ox++)
            {
                Blend(texture, x + ox, texture.height - 1 - (y + oy), c);
            }
        }
    }

    private static void Blend(Texture2D texture, int x, int y, Color32 src)
    {
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height || src.a == 0)
        {
            return;
        }

        Color32 dst = texture.GetPixel(x, y);
        float sa = src.a / 255f;
        float da = dst.a / 255f;
        float outA = sa + da * (1f - sa);
        if (outA <= 0f)
        {
            texture.SetPixel(x, y, Color.clear);
            return;
        }

        byte r = (byte)Mathf.RoundToInt((src.r * sa + dst.r * da * (1f - sa)) / outA);
        byte g = (byte)Mathf.RoundToInt((src.g * sa + dst.g * da * (1f - sa)) / outA);
        byte b = (byte)Mathf.RoundToInt((src.b * sa + dst.b * da * (1f - sa)) / outA);
        byte a = (byte)Mathf.RoundToInt(outA * 255f);
        texture.SetPixel(x, y, new Color32(r, g, b, a));
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if (polygon[i].y > point.y != polygon[j].y > point.y &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private static Color32 Lerp(Color32 a, Color32 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
    }

    private static Color32 Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }

}



