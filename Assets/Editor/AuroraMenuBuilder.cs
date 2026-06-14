using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Setup do Menu Principal organizado de PROJETO:AURORA.
/// Reconstrói Assets/_ProjectAurora/Scenes/MainMenu.unity reproduzindo o estilo
/// da arte de referência MenuFull.png, mas com botões REAIS da UI do Unity:
/// borda ciano, ícone textual, fundo translúcido e feedback de hover/click/foco.
/// A coluna de botões desenhada na arte é coberta por Panel_ButtonAreaCleaner.
/// Não altera a cena/menu legados existentes nem o gameplay.
/// </summary>
public static class AuroraMenuBuilder
{
    private const string Base = "Assets/_ProjectAurora";
    private const string ScenePath = Base + "/Scenes/MainMenu.unity";
    private const string BackgroundPath = Base + "/Art/Menu/References/MenuFull.png";
    private const string CharacterPath = Base + "/Art/Menu/Characters/Dr.Elias_Menu.png";
    private const string MusicPath = Base + "/Audio/Music/Aurora.mp3";

    private static readonly Color Cyan = new Color(0.10f, 0.85f, 1f);

    [MenuItem("Tools/Projeto Aurora/Setup Main Menu (Organizado)")]
    public static void SetupMainMenu()
    {
        EnsureFolders();
        MoveRootAssetsIfNeeded();
        ConfigureSpriteImport(BackgroundPath);
        ConfigureSpriteImport(CharacterPath);
        ConfigureMusicImport(MusicPath);
        AssetDatabase.Refresh();

        BuildScene();
        AddSceneToBuildSettingsAsFirst(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PROJETO:AURORA - Menu Principal reconstruído em " + ScenePath);
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            Base, Base + "/Scenes", Base + "/Scripts", Base + "/Scripts/UI",
            Base + "/Art", Base + "/Art/Menu", Base + "/Art/Menu/References", Base + "/Art/Menu/Characters",
            Base + "/Audio", Base + "/Audio/Music", Base + "/Prefabs", Base + "/Prefabs/UI"
        };
        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }
    }

    // Se algum asset ainda estiver na raiz do projeto, move para o destino correto (sem sobrescrever).
    private static void MoveRootAssetsIfNeeded()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        MoveIfInRoot(projectRoot, "MenuFull.png", BackgroundPath);
        MoveIfInRoot(projectRoot, "Dr.Elias_Menu.png", CharacterPath);
        MoveIfInRoot(projectRoot, "Aurora.mp3", MusicPath);
    }

    private static void MoveIfInRoot(string projectRoot, string fileName, string destAssetPath)
    {
        string src = Path.Combine(projectRoot, fileName);
        string dest = Path.Combine(projectRoot, destAssetPath);
        if (!File.Exists(src)) return; // já movido ou inexistente
        if (File.Exists(dest))
        {
            Debug.LogWarning($"PROJETO:AURORA - '{destAssetPath}' já existe. Preservando o existente; '{fileName}' permanece na raiz.");
            return;
        }
        File.Move(src, dest);
        Debug.Log($"PROJETO:AURORA - Movido da raiz: {fileName} -> {destAssetPath}");
    }

    private static void ConfigureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Textura não encontrada para importar como Sprite: " + path);
            return;
        }
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureMusicImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Áudio não encontrado para configurar import: " + path);
            return;
        }
        AudioImporterSampleSettings s = importer.defaultSampleSettings;
        s.loadType = AudioClipLoadType.CompressedInMemory;
        s.preloadAudioData = true;
        importer.defaultSampleSettings = s;
        importer.loadInBackground = false;
        importer.SaveAndReimport();
    }

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        // Câmera + AudioListener (necessário para ouvir a música).
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.02f, 0.035f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 1f, -10f);

        // Luz direcional (boa prática para novas cenas).
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        CreateEventSystem();

        // Canvas principal.
        Canvas canvas = CreateCanvas("Canvas_MainMenu");
        var controller = canvas.gameObject.AddComponent<ProjectAurora.UI.MainMenuController>();

        // Raiz 16:9 fixa (1920x1080) centralizada. Mantém botões e fundo no MESMO
        // espaço de coordenadas, escalados juntos pelo CanvasScaler -> alinhamento estável.
        RectTransform menuRoot = CreateRect("MenuRoot_16x9", canvas.transform);
        menuRoot.anchorMin = new Vector2(0.5f, 0.5f);
        menuRoot.anchorMax = new Vector2(0.5f, 0.5f);
        menuRoot.pivot = new Vector2(0.5f, 0.5f);
        menuRoot.anchoredPosition = Vector2.zero;
        menuRoot.sizeDelta = new Vector2(1920f, 1080f);

        // Fundo: MenuFull.png preenchendo o MenuRoot, sem bloquear cliques.
        Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        Image bg = CreateImage(menuRoot, "Image_MenuBackground", Color.white);
        StretchFull(bg.rectTransform);
        bg.sprite = background;
        bg.type = Image.Type.Simple;
        bg.preserveAspect = true;
        bg.raycastTarget = false;
        if (background == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Sprite de fundo não carregado: " + BackgroundPath);
        }

        // Cobre toda a coluna desenhada na arte (incl. a caixa larga e destacada do "Jogar", ~x48..848)
        // e a faixa vertical onde ficam os botões reais. Funciona como painel-sidebar sci-fi.
        Image cleaner = CreateImage(menuRoot, "Panel_ButtonAreaCleaner", new Color(0.012f, 0.03f, 0.055f, 0.80f));
        SetTopLeft(cleaner.rectTransform, 46f, -345f, 812f, 515f);
        cleaner.raycastTarget = false;
        // Acento ciano vertical à esquerda do painel, para integrar ao visual sci-fi.
        Image accent = CreateImage(cleaner.rectTransform, "Accent_Left", new Color(Cyan.r, Cyan.g, Cyan.b, 0.85f));
        SetTopLeft(accent.rectTransform, 0f, 0f, 4f, 515f);
        accent.raycastTarget = false;

        // Botões reais.
        controller.playButton     = CreateSciFiButton(menuRoot, "Button_Jogar",         "▶", "JOGAR",         -385f);
        controller.settingsButton = CreateSciFiButton(menuRoot, "Button_Configuracoes", "⚙", "CONFIGURAÇÕES", -482f);
        controller.extraButton    = CreateSciFiButton(menuRoot, "Button_Extra",         "▣", "EXTRA",         -579f);
        controller.creditsButton  = CreateSciFiButton(menuRoot, "Button_Creditos",      "◉", "CRÉDITOS",      -676f);
        controller.quitButton     = CreateSciFiButton(menuRoot, "Button_Sair",          "⏻", "SAIR",          -773f);

        // Painéis (filhos do MenuRoot, centralizados, começam desativados).
        controller.settingsPanel = CreatePanel(menuRoot, "Panel_Settings", "CONFIGURAÇÕES",
            "Configurações de áudio, controles e customização serão implementadas nas próximas versões.",
            out controller.settingsBackButton);
        controller.extraPanel = CreatePanel(menuRoot, "Panel_Extra", "EXTRA",
            "O Projeto Aurora foi criado para controlar fenômenos atmosféricos artificiais. " +
            "Durante a falha de contenção, seus sistemas autônomos passaram a priorizar a " +
            "continuidade do protocolo acima da vida humana.",
            out controller.extraBackButton);
        controller.creditsPanel = CreatePanel(menuRoot, "Panel_Credits", "CRÉDITOS",
            "PROJETO:AURORA — Falha de Contenção\n\nDesenvolvimento: Matheus Fernandes\n" +
            "Disciplina: Computação Gráfica\n\n2026",
            out controller.creditsBackButton);

        controller.settingsPanel.SetActive(false);
        controller.extraPanel.SetActive(false);
        controller.creditsPanel.SetActive(false);

        // Música do menu (objeto raiz, sem DontDestroyOnLoad).
        GameObject audioObject = new GameObject("Audio_MenuMusic");
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        source.playOnAwake = true;
        source.loop = true;
        source.volume = 0.55f;
        source.spatialBlend = 0f;
        if (source.clip == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Clipe de música não carregado: " + MusicPath);
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath);
    }

    // ---------- Botão sci-fi: borda ciano + fill translúcido + ícone + texto ----------

    private static Button CreateSciFiButton(Transform parent, string name, string icon, string label, float y)
    {
        // Fill translúcido escuro = targetGraphic (a arte aparece sutilmente atrás).
        // Largura aumentada para 720 (vs. 560 base) para combinar com os botões largos da arte.
        Image fill = CreateImage(parent, name, Color.white);
        SetTopLeft(fill.rectTransform, 100f, y, 720f, 76f);

        Button button = fill.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = fill; // fill branco => a cor do estado é aplicada diretamente
        ColorBlock colors = button.colors;
        colors.normalColor      = new Color(0.03f, 0.08f, 0.13f, 0.55f); // escuro translúcido (sem azul sólido)
        colors.highlightedColor = new Color(0.10f, 0.85f, 1.00f, 0.28f); // ciano translúcido leve (hover)
        colors.pressedColor     = new Color(0.10f, 0.85f, 1.00f, 0.48f); // ciano mais forte (click)
        colors.selectedColor    = new Color(0.06f, 0.45f, 0.62f, 0.28f); // foco discreto (sem virar card)
        colors.disabledColor    = new Color(0.03f, 0.08f, 0.13f, 0.25f);
        colors.colorMultiplier  = 1f;
        colors.fadeDuration     = 0.1f;
        button.colors = colors;

        // Moldura ciano fina de 4 arestas (não vaza para o centro).
        AddCyanBorder(fill.rectTransform, 2f);

        // Ícone textual à esquerda.
        Text iconText = CreateText(fill.rectTransform, "Icon", icon, 30, TextAnchor.MiddleCenter,
            new Vector2(0.03f, 0f), new Vector2(0.16f, 1f), Cyan);
        // Rótulo.
        Text labelText = CreateText(fill.rectTransform, "Label", label, 26, TextAnchor.MiddleLeft,
            new Vector2(0.19f, 0f), new Vector2(0.97f, 1f), new Color(0.90f, 0.96f, 1f));
        labelText.fontStyle = FontStyle.Bold;
        iconText.raycastTarget = false;
        labelText.raycastTarget = false;

        return button;
    }

    // Cria uma moldura ciano com 4 arestas finas dentro do RectTransform informado.
    private static void AddCyanBorder(RectTransform target, float thickness)
    {
        Color c = new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f);
        // Topo
        Edge(target, "Border_Top", c, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -thickness), new Vector2(0f, 0f));
        // Base
        Edge(target, "Border_Bottom", c, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, thickness));
        // Esquerda
        Edge(target, "Border_Left", c, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(thickness, 0f));
        // Direita
        Edge(target, "Border_Right", c, new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-thickness, 0f), new Vector2(0f, 0f));
    }

    private static void Edge(RectTransform parent, string name, Color color, Vector2 aMin, Vector2 aMax,
        Vector2 oMin, Vector2 oMax)
    {
        Image e = CreateImage(parent, name, color);
        RectTransform r = e.rectTransform;
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = oMin; r.offsetMax = oMax;
        e.raycastTarget = false;
    }

    // ---------- Painel modal centralizado ----------

    private static GameObject CreatePanel(Transform parent, string name, string title, string body, out Button backButton)
    {
        // Backdrop escuro translúcido cobrindo a tela (bloqueia cliques atrás).
        Image backdrop = CreateImage(parent, name, new Color(0f, 0f, 0f, 0.6f));
        StretchFull(backdrop.rectTransform);
        backdrop.raycastTarget = true;

        // Moldura ciano centralizada.
        Image border = CreateImage(backdrop.rectTransform, "Border", new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f));
        RectTransform br = border.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.pivot = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = Vector2.zero;
        br.sizeDelta = new Vector2(820f, 520f);

        // Painel interno escuro (inset 4px).
        Image card = CreateImage(border.rectTransform, "Card", new Color(0.02f, 0.05f, 0.09f, 0.98f));
        StretchFull(card.rectTransform);
        card.rectTransform.offsetMin = new Vector2(4f, 4f);
        card.rectTransform.offsetMax = new Vector2(-4f, -4f);

        CreateText(card.rectTransform, "Title", title, 36, TextAnchor.MiddleCenter,
            new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.95f), Cyan);
        CreateText(card.rectTransform, "Body", body, 22, TextAnchor.UpperCenter,
            new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.78f), Color.white);

        backButton = CreateLabeledButton(card.rectTransform, "Button_Voltar", "VOLTAR",
            new Vector2(0.36f, 0.07f), new Vector2(0.64f, 0.19f));

        return backdrop.gameObject;
    }

    private static Button CreateLabeledButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        Image fill = CreateImage(parent, name, Color.white);
        RectTransform rt = fill.rectTransform;
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        Button button = fill.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = fill;
        ColorBlock colors = button.colors;
        colors.normalColor      = new Color(0.03f, 0.10f, 0.16f, 0.7f);
        colors.highlightedColor = new Color(0.10f, 0.85f, 1.00f, 0.35f);
        colors.pressedColor     = new Color(0.10f, 0.85f, 1.00f, 0.55f);
        colors.selectedColor    = new Color(0.10f, 0.85f, 1.00f, 0.35f);
        colors.fadeDuration     = 0.1f;
        button.colors = colors;

        AddCyanBorder(fill.rectTransform, 2f);

        Text t = CreateText(fill.rectTransform, "Label", label, 22, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Color.white);
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        return button;
    }

    // ---------- Helpers genéricos ----------

    private static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Posiciona com âncora/pivot top-left no sistema 1920x1080.
    private static void SetTopLeft(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor,
        Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void AddSceneToBuildSettingsAsFirst(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath))
        {
            return; // já presente, não duplica
        }
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
