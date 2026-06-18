using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuroraGameplayHudBuilder
{
    private const string SourceScene = "Assets/Scenes/Game.unity";
    private const string TargetScene =
        "Assets/_ProjectAurora/Scenes/FASE 01 - Laboratório Limpo A/" +
        "Fase01_SetorA_LaboratorioLimpo.unity";
    private const string PrefabPath = "Assets/_ProjectAurora/Prefabs/UI/HUD_Fase01.prefab";
    private const string PortraitPath = "Assets/_ProjectAurora/Art/UI/CelestiaNormal.png";
    private const string EliasReferencePath = "Assets/_ProjectAurora/Art/Characters/Dr.Elias.png";
    private const string GeneratedUiPath = "Assets/_ProjectAurora/Art/UI/Generated";
    private const string CircleSpritePath = GeneratedUiPath + "/HUD_Circle.png";
    private const string HexSpritePath = GeneratedUiPath + "/HUD_Hex.png";
    private const string FontPath = GeneratedUiPath + "/AuroraHUD_Font.asset";
    private const string FontSourcePath =
        "Assets/_ProjectAurora/Art/UI/Fonts/Inter-Regular.otf";
    private const string FontMaterialPath = GeneratedUiPath + "/AuroraTMPUI.mat";
    private const string FontShaderName = "TextMeshPro/Mobile/Distance Field";

    private static readonly Color Cyan = new Color(0.04f, 0.86f, 1f);
    private static readonly Color CyanSoft = new Color(0.28f, 0.92f, 1f);
    private static readonly Color Panel = new Color(0.015f, 0.075f, 0.12f, 0.82f);
    private static readonly Color PanelStrong = new Color(0.008f, 0.045f, 0.075f, 0.93f);
    private static readonly Color White = new Color(0.86f, 0.96f, 1f);
    private static TMP_FontAsset hudFont;

    [MenuItem("Tools/Projeto Aurora/Fase 01/Build Gameplay HUD")]
    public static void Build()
    {
        EnsureFolder("Assets/_ProjectAurora/Art/UI");
        EnsureFolder(GeneratedUiPath);
        EnsureFolder("Assets/_ProjectAurora/Prefabs/UI");
        EnsureFolder("Assets/_ProjectAurora/Scenes/FASE01");

        ConfigureReferenceSprite(PortraitPath);
        ConfigureReferenceSprite(EliasReferencePath);
        CreateMaskSprite(CircleSpritePath, false);
        CreateMaskSprite(HexSpritePath, true);
        hudFont = GetOrCreateFont();

        Scene scene;
        if (File.Exists(TargetScene))
        {
            scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, TargetScene, true);
            scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        }

        BuildHud(scene);
        ConfigureSceneReferences();
        ConfigureCamera();
        AddSceneToBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PROJETO:AURORA - HUD de gameplay da Fase 01 criada.");
    }

    [MenuItem("Tools/Projeto Aurora/Fase 01/Configurar Fluxo de Finalizacao")]
    public static void PatchEndStateActions()
    {
        hudFont = GetOrCreateFont();
        Scene scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        AuroraGameplayHUDController hud =
            Object.FindAnyObjectByType<AuroraGameplayHUDController>();
        UIManager ui = Object.FindAnyObjectByType<UIManager>();
        if (hud == null || ui == null)
        {
            throw new System.InvalidOperationException(
                "HUD ou UIManager nao encontrado na cena da Fase 01.");
        }

        DestroyPanel(hud.failurePanel, hud.transform.Find("Integrity Failure"));
        DestroyPanel(hud.finalPanel, hud.transform.Find("Final Panel"));

        hud.failurePanel = CreateOverlay(
            hud.transform,
            "Integrity Failure",
            "INTEGRIDADE COMPROMETIDA",
            "Os três segmentos de integridade foram perdidos");
        ConfigureDecisionOverlay(
            hud.failurePanel,
            "TENTAR NOVAMENTE",
            out ui.gameOverRestartButton,
            out ui.gameOverMenuButton);
        ui.gameOverPanel = hud.failurePanel;

        hud.finalPanel = CreateOverlay(
            hud.transform,
            "Final Panel",
            "MISSÃO CONCLUÍDA",
            "Terminal Central alcançado");
        ConfigureDecisionOverlay(
            hud.finalPanel,
            "JOGAR NOVAMENTE",
            out ui.finalRestartButton,
            out ui.finalMenuButton);
        ui.finalPanel = hud.finalPanel;

        hud.failurePanel.transform.SetAsLastSibling();
        hud.finalPanel.transform.SetAsLastSibling();
        hud.failurePanel.SetActive(false);
        hud.finalPanel.SetActive(false);

        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScene);
        PrefabUtility.SaveAsPrefabAsset(hud.gameObject, PrefabPath);
        AssetDatabase.SaveAssets();

        ValidateEndStateActions(hud, ui);
        Debug.Log(
            "PROJETO:AURORA - Fluxos de derrota e conclusao configurados com reinicio e menu.");
    }

    private static void DestroyPanel(GameObject referencedPanel, Transform fallback)
    {
        GameObject panel = referencedPanel != null
            ? referencedPanel
            : fallback != null ? fallback.gameObject : null;
        if (panel != null)
        {
            Object.DestroyImmediate(panel);
        }
    }

    private static void ValidateEndStateActions(
        AuroraGameplayHUDController hud,
        UIManager ui)
    {
        if (hud.failurePanel == null ||
            hud.finalPanel == null ||
            ui.gameOverRestartButton == null ||
            ui.gameOverMenuButton == null ||
            ui.finalRestartButton == null ||
            ui.finalMenuButton == null)
        {
            throw new System.InvalidOperationException(
                "Os paineis finais nao possuem todas as acoes obrigatorias.");
        }

        if (hud.failurePanel.GetComponentsInChildren<Button>(true).Length != 2 ||
            hud.finalPanel.GetComponentsInChildren<Button>(true).Length != 2)
        {
            throw new System.InvalidOperationException(
                "Cada painel final precisa conter exatamente dois botoes.");
        }
    }

    private static void BuildHud(Scene scene)
    {
        GameObject oldHud = GameObject.Find("HUD Canvas");
        if (oldHud != null)
        {
            Object.DestroyImmediate(oldHud);
        }

        GameObject canvasObject = new GameObject(
            "HUD Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        AuroraGameplayHUDController hud = canvasObject.AddComponent<AuroraGameplayHUDController>();
        UIManager ui = canvasObject.AddComponent<UIManager>();
        ui.auroraHud = hud;

        BuildSectorPanel(canvasObject.transform, hud);
        BuildIntegrityPanel(canvasObject.transform, hud);
        BuildDistancePanel(canvasObject.transform, hud);
        BuildCelestIAPanel(canvasObject.transform, hud);
        BuildUtilityPanels(canvasObject.transform, hud, ui);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }
        PrefabUtility.SaveAsPrefabAsset(canvasObject, PrefabPath);
    }

    private static void BuildSectorPanel(Transform parent, AuroraGameplayHUDController hud)
    {
        Image panel = CreatePanel(
            "Sector Identification",
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(34f, -28f),
            new Vector2(520f, 132f),
            Panel);
        AddPanelChrome(panel.rectTransform);

        hud.sectorText = CreateText(
            "Sector Name",
            panel.transform,
            "SETOR A: Laboratório Limpo",
            24f,
            TextAlignmentOptions.Left,
            new Vector2(22f, -12f),
            new Vector2(474f, 48f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            FontStyles.Bold,
            White);

        hud.objectiveText = CreateText(
            "Objective",
            panel.transform,
            "Escape do setor",
            24f,
            TextAlignmentOptions.Left,
            new Vector2(66f, -76f),
            new Vector2(410f, 38f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            FontStyles.Normal,
            CyanSoft);
        Image objectiveIcon = CreateImage(
            "Objective Diamond",
            panel.transform,
            new Vector2(42f, -95f),
            new Vector2(18f, 18f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Cyan,
            null);
        objectiveIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        objectiveIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image objectiveIconCore = CreateImage(
            "Objective Diamond Core",
            objectiveIcon.transform,
            Vector2.zero,
            new Vector2(10f, 10f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Panel,
            null);
        objectiveIconCore.rectTransform.anchoredPosition = Vector2.zero;
        hud.SetObjective("Escape do setor");
    }

    private static void BuildIntegrityPanel(Transform parent, AuroraGameplayHUDController hud)
    {
        Image panel = CreatePanel(
            "Integrity System",
            parent,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -26f),
            new Vector2(420f, 82f),
            PanelStrong);
        AddPanelChrome(panel.rectTransform);

        hud.integrityLabel = CreateText(
            "Integrity Label",
            panel.transform,
            "INTEGRIDADE",
            23f,
            TextAlignmentOptions.Left,
            new Vector2(24f, -21f),
            new Vector2(190f, 38f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            FontStyles.Bold,
            White);

        Sprite hex = AssetDatabase.LoadAssetAtPath<Sprite>(HexSpritePath);
        hud.integritySegments = new Image[3];
        for (int i = 0; i < hud.integritySegments.Length; i++)
        {
            Image segment = CreateImage(
                "Integrity Segment " + (i + 1),
                panel.transform,
                new Vector2(250f + i * 48f, -19f),
                new Vector2(36f, 42f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                Cyan,
                hex);
            Outline outline = segment.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.95f, 1f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);
            hud.integritySegments[i] = segment;
        }
    }

    private static void BuildDistancePanel(Transform parent, AuroraGameplayHUDController hud)
    {
        Image panel = CreatePanel(
            "Distance System",
            parent,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-34f, -28f),
            new Vector2(560f, 128f),
            Panel);
        AddPanelChrome(panel.rectTransform);

        CreateText(
            "Distance Label",
            panel.transform,
            "DISTÂNCIA",
            23f,
            TextAlignmentOptions.Left,
            new Vector2(25f, -14f),
            new Vector2(190f, 34f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            FontStyles.Bold,
            White);

        hud.distanceValueText = CreateText(
            "Distance Value",
            panel.transform,
            "0 m",
            38f,
            TextAlignmentOptions.Left,
            new Vector2(25f, -52f),
            new Vector2(190f, 56f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            FontStyles.Bold,
            CyanSoft);

        Image track = CreateImage(
            "Distance Track",
            panel.transform,
            new Vector2(-55f, 39f),
            new Vector2(300f, 8f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Color(0.3f, 0.7f, 0.82f, 0.42f),
            null);
        hud.distanceTrack = track.rectTransform;

        Image fill = CreateImage(
            "Distance Fill",
            track.transform,
            Vector2.zero,
            Vector2.zero,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            Cyan,
            null);
        Stretch(fill.rectTransform);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        hud.distanceProgressFill = fill;

        Image start = CreateImage(
            "Start Marker",
            track.transform,
            new Vector2(-150f, 0f),
            new Vector2(5f, 24f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            White,
            null);
        start.rectTransform.anchoredPosition = new Vector2(-150f, 0f);

        Image marker = CreateImage(
            "Progress Marker",
            track.transform,
            new Vector2(-150f, 0f),
            new Vector2(16f, 16f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Cyan,
            null);
        marker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        hud.distanceMarker = marker.rectTransform;

        CreateFinishFlag(panel.transform, new Vector2(-24f, 38f));
    }

    private static void BuildCelestIAPanel(Transform parent, AuroraGameplayHUDController hud)
    {
        Image panel = CreatePanel(
            "CelestIA Communication",
            parent,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-34f, 34f),
            new Vector2(760f, 270f),
            PanelStrong);
        AddPanelChrome(panel.rectTransform);

        CelestIACommPanel comm = panel.gameObject.AddComponent<CelestIACommPanel>();
        hud.commPanel = comm;

        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        Sprite portrait = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);

        Image portraitRing = CreateImage(
            "Portrait Ring",
            panel.transform,
            new Vector2(28f, 28f),
            new Vector2(214f, 214f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            Cyan,
            circle);
        Outline ringGlow = portraitRing.gameObject.AddComponent<Outline>();
        ringGlow.effectColor = new Color(0.05f, 0.9f, 1f, 0.62f);
        ringGlow.effectDistance = new Vector2(4f, -4f);

        Image maskImage = CreateImage(
            "Portrait Mask",
            portraitRing.transform,
            Vector2.zero,
            new Vector2(194f, 194f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Color.white,
            circle);
        maskImage.rectTransform.anchoredPosition = Vector2.zero;
        Mask mask = maskImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        Image portraitImage = CreateImage(
            "CelestIA Portrait",
            maskImage.transform,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            Color.white,
            portrait);
        Stretch(portraitImage.rectTransform);
        portraitImage.preserveAspect = true;
        comm.portraitImage = portraitImage;

        comm.nameText = CreateText(
            "CelestIA Name",
            panel.transform,
            "CELESTIA",
            28f,
            TextAlignmentOptions.Left,
            new Vector2(276f, -26f),
            new Vector2(180f, 42f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            FontStyles.Bold,
            CyanSoft);

        comm.statusText = CreateText(
            "CelestIA Status",
            panel.transform,
            "STATUS: NORMAL",
            18f,
            TextAlignmentOptions.Right,
            new Vector2(-42f, -31f),
            new Vector2(230f, 32f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            FontStyles.Bold,
            CyanSoft);

        comm.signalIcon = CreateImage(
            "Signal Icon",
            panel.transform,
            new Vector2(-25f, -38f),
            new Vector2(7f, 22f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            Cyan,
            null);
        for (int i = 1; i <= 2; i++)
        {
            CreateImage(
                "Signal Bar " + i,
                panel.transform,
                new Vector2(-25f - i * 10f, -38f),
                new Vector2(6f, 14f + i * 6f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                Cyan,
                null);
        }

        CreateImage(
            "Header Divider",
            panel.transform,
            new Vector2(268f, -72f),
            new Vector2(460f, 2f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.65f),
            null);

        comm.messageText = CreateText(
            "CelestIA Message",
            panel.transform,
            "Doutor Elias, mantenha a rota. Detectando obstáculos à frente.",
            25f,
            TextAlignmentOptions.TopLeft,
            new Vector2(278f, -91f),
            new Vector2(440f, 108f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            FontStyles.Normal,
            White);
        comm.messageText.textWrappingMode = TextWrappingModes.Normal;

        List<Image> bars = new List<Image>();
        for (int i = 0; i < 26; i++)
        {
            Image bar = CreateImage(
                "Transmission " + i,
                panel.transform,
                new Vector2(288f + i * 14f, 25f),
                new Vector2(5f, 12f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                Cyan,
                null);
            bars.Add(bar);
        }
        comm.waveformBars = bars.ToArray();
    }

    private static void BuildUtilityPanels(
        Transform parent,
        AuroraGameplayHUDController hud,
        UIManager ui)
    {
        Image prompt = CreatePanel(
            "Interaction Prompt",
            parent,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 58f),
            new Vector2(380f, 64f),
            PanelStrong);
        AddPanelChrome(prompt.rectTransform);
        hud.interactionPrompt = prompt.gameObject;
        hud.interactionText = CreateText(
            "Interaction Text",
            prompt.transform,
            "Pressione E",
            24f,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            FontStyles.Bold,
            White);
        Stretch(hud.interactionText.rectTransform);
        prompt.gameObject.SetActive(false);

        Image sectorCard = CreatePanel(
            "Sector Card",
            parent,
            new Vector2(0.5f, 0.72f),
            new Vector2(0.5f, 0.72f),
            Vector2.zero,
            new Vector2(560f, 82f),
            PanelStrong);
        sectorCard.gameObject.AddComponent<CanvasGroup>();
        hud.sectorCard = sectorCard.gameObject;
        hud.sectorCardText = CreateText(
            "Sector Card Text",
            sectorCard.transform,
            "SETOR A: LABORATÓRIO LIMPO",
            28f,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            FontStyles.Bold,
            CyanSoft);
        Stretch(hud.sectorCardText.rectTransform);
        sectorCard.gameObject.SetActive(false);

        hud.pausePanel = CreateOverlay(parent, "Pause Panel", "PAUSADO", "Pressione ESC para continuar");
        hud.failurePanel = CreateOverlay(
            parent,
            "Integrity Failure",
            "INTEGRIDADE COMPROMETIDA",
            "Falha crítica no traje do Dr. Elias");
        ConfigureDecisionOverlay(
            hud.failurePanel,
            "TENTAR NOVAMENTE",
            out ui.gameOverRestartButton,
            out ui.gameOverMenuButton);
        ui.gameOverPanel = hud.failurePanel;

        hud.finalPanel = CreateOverlay(
            parent,
            "Final Panel",
            "PROTOCOLO AURORA CONTINUA",
            "Terminal Central alcançado");
        ConfigureDecisionOverlay(
            hud.finalPanel,
            "JOGAR NOVAMENTE",
            out ui.finalRestartButton,
            out ui.finalMenuButton);
        ui.finalPanel = hud.finalPanel;

        Image intro = CreatePanel(
            "Intro Panel",
            parent,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.005f, 0.012f, 0.02f, 0.97f));
        Stretch(intro.rectTransform);
        hud.introPanel = intro.gameObject;
        hud.introText = CreateText(
            "Intro Text",
            intro.transform,
            string.Empty,
            34f,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.zero,
            new Vector2(0.15f, 0.35f),
            new Vector2(0.85f, 0.65f),
            FontStyles.Bold,
            CyanSoft);
        hud.introText.rectTransform.offsetMin = Vector2.zero;
        hud.introText.rectTransform.offsetMax = Vector2.zero;
        intro.gameObject.SetActive(false);
    }

    private static GameObject CreateOverlay(Transform parent, string name, string title, string subtitle)
    {
        Image overlay = CreatePanel(
            name,
            parent,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.005f, 0.02f, 0.035f, 0.9f));
        Stretch(overlay.rectTransform);

        Image card = CreatePanel(
            "Message",
            overlay.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(720f, 220f),
            PanelStrong);
        AddPanelChrome(card.rectTransform);

        TMP_Text titleText = CreateText(
            "Title",
            card.transform,
            title,
            38f,
            TextAlignmentOptions.Center,
            new Vector2(0f, -42f),
            new Vector2(640f, 64f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            FontStyles.Bold,
            CyanSoft);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -42f);

        TMP_Text subtitleText = CreateText(
            "Subtitle",
            card.transform,
            subtitle,
            23f,
            TextAlignmentOptions.Center,
            new Vector2(0f, 42f),
            new Vector2(640f, 44f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            FontStyles.Normal,
            White);
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, 42f);
        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }

    private static void ConfigureDecisionOverlay(
        GameObject overlay,
        string restartLabel,
        out Button restartButton,
        out Button menuButton)
    {
        RectTransform card = overlay.transform.Find("Message") as RectTransform;
        if (card == null)
        {
            throw new System.InvalidOperationException(
                "Card de mensagem nao encontrado no painel " + overlay.name + ".");
        }

        card.sizeDelta = new Vector2(760f, 340f);

        TMP_Text subtitle = card.Find("Subtitle")?.GetComponent<TMP_Text>();
        if (subtitle != null)
        {
            subtitle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, 24f);
            subtitle.rectTransform.sizeDelta = new Vector2(650f, 48f);
        }

        restartButton = CreateActionButton(
            card,
            "Restart Button",
            restartLabel,
            new Vector2(-155f, -88f),
            true);
        menuButton = CreateActionButton(
            card,
            "Menu Button",
            "VOLTAR AO MENU",
            new Vector2(155f, -88f),
            false);
    }

    private static Button CreateActionButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        bool primary)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(270f, 64f);

        Image image = go.GetComponent<Image>();
        image.color = primary
            ? new Color(0.035f, 0.45f, 0.58f, 0.96f)
            : new Color(0.02f, 0.12f, 0.18f, 0.96f);
        image.raycastTarget = true;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, primary ? 0.95f : 0.68f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.72f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.45f, 0.82f, 0.9f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.35f, 0.45f, 0.48f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText(
            "Label",
            go.transform,
            label,
            21f,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            FontStyles.Bold,
            White);
        Stretch(text.rectTransform);
        return button;
    }

    private static void ConfigureSceneReferences()
    {
        UIManager ui = Object.FindAnyObjectByType<UIManager>();
        GameManager game = Object.FindAnyObjectByType<GameManager>();
        CelestIAController celestia = Object.FindAnyObjectByType<CelestIAController>();
        SectorManager sectors = Object.FindAnyObjectByType<SectorManager>();
        DialogueManager dialogue = Object.FindAnyObjectByType<DialogueManager>();

        if (game != null)
        {
            game.ui = ui;
            EditorUtility.SetDirty(game);
        }
        if (celestia != null)
        {
            celestia.ui = ui;
            EditorUtility.SetDirty(celestia);
        }
        if (sectors != null)
        {
            sectors.ui = ui;
            sectors.celestIAHud = null;
            EditorUtility.SetDirty(sectors);
        }
        if (dialogue != null)
        {
            dialogue.ui = ui;
            dialogue.celestIAHud = null;
            EditorUtility.SetDirty(dialogue);
        }
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.fieldOfView = 61f;
        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow != null)
        {
            follow.offset = new Vector3(0.35f, 4.2f, -8.7f);
            follow.lookOffset = new Vector3(1.35f, 1.15f, 7.5f);
            follow.positionSmooth = 9f;
            follow.rotationSmooth = 8f;
            EditorUtility.SetDirty(follow);
        }
        EditorUtility.SetDirty(camera);
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool exists = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == TargetScene)
            {
                scenes[i] = new EditorBuildSettingsScene(TargetScene, true);
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            scenes.Add(new EditorBuildSettingsScene(TargetScene, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Image CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        Image image = CreateImage(
            name,
            parent,
            anchoredPosition,
            size,
            anchorMin,
            anchorMax,
            color,
            null);
        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.52f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return image;
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color,
        Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(
            Mathf.Approximately(anchorMin.x, 1f) ? 1f : Mathf.Approximately(anchorMin.x, 0f) ? 0f : 0.5f,
            Mathf.Approximately(anchorMin.y, 1f) ? 1f : Mathf.Approximately(anchorMin.y, 0f) ? 0f : 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        FontStyles style,
        Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(
            Mathf.Approximately(anchorMin.x, 1f) ? 1f : Mathf.Approximately(anchorMin.x, 0f) ? 0f : 0.5f,
            Mathf.Approximately(anchorMin.y, 1f) ? 1f : Mathf.Approximately(anchorMin.y, 0f) ? 0f : 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = hudFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Truncate;
        return tmp;
    }

    private static void AddPanelChrome(RectTransform panel)
    {
        CreateImage(
            "Top Glow",
            panel,
            new Vector2(0f, -1f),
            new Vector2(-18f, 2f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            Cyan,
            null);
        RectTransform top = panel.Find("Top Glow").GetComponent<RectTransform>();
        top.offsetMin = new Vector2(9f, -3f);
        top.offsetMax = new Vector2(-9f, -1f);

        CreateCorner(panel, "Corner TL H", new Vector2(10f, -10f), new Vector2(28f, 3f), new Vector2(0f, 1f));
        CreateCorner(panel, "Corner TL V", new Vector2(10f, -10f), new Vector2(3f, 22f), new Vector2(0f, 1f));
        CreateCorner(panel, "Corner BR H", new Vector2(-10f, 10f), new Vector2(28f, 3f), new Vector2(1f, 0f));
        CreateCorner(panel, "Corner BR V", new Vector2(-10f, 10f), new Vector2(3f, 22f), new Vector2(1f, 0f));
    }

    private static void CreateCorner(RectTransform parent, string name, Vector2 position, Vector2 size, Vector2 anchor)
    {
        CreateImage(name, parent, position, size, anchor, anchor, Cyan, null);
    }

    private static void CreateFinishFlag(Transform parent, Vector2 position)
    {
        GameObject flag = new GameObject("Finish Flag", typeof(RectTransform));
        flag.transform.SetParent(parent, false);
        RectTransform rect = flag.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(30f, 30f);

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                CreateImage(
                    "Check " + x + y,
                    flag.transform,
                    new Vector2(x * 8f, y * 8f),
                    new Vector2(8f, 8f),
                    Vector2.zero,
                    Vector2.zero,
                    (x + y) % 2 == 0 ? White : Cyan,
                    null);
            }
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ConfigureReferenceSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void CreateMaskSprite(string path, bool hexagon)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y) - center;
                bool inside;
                if (hexagon)
                {
                    float qx = Mathf.Abs(point.x) / radius;
                    float qy = Mathf.Abs(point.y) / radius;
                    inside = qy <= 0.88f && qx * 0.58f + qy <= 1f;
                }
                else
                {
                    inside = point.sqrMagnitude <= radius * radius;
                }
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ConfigureReferenceSprite(path);
    }

    private static TMP_FontAsset GetOrCreateFont()
    {
        Font source = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
        if (source == null)
        {
            throw new FileNotFoundException("Fonte Inter de origem nao encontrada.", FontSourcePath);
        }

        Shader shader = Shader.Find(FontShaderName);
        if (shader == null)
        {
            throw new System.InvalidOperationException("Shader da fonte TMP da HUD nao encontrado.");
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            font = TMP_FontAsset.CreateFontAsset(source);
            if (font == null)
            {
                throw new System.InvalidOperationException("Nao foi possivel criar a fonte TMP da HUD.");
            }
            font.name = "AuroraHUD Font";
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(FontMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            material.name = "Aurora TMP UI";
            AssetDatabase.CreateAsset(material, FontMaterialPath);
        }
        else
        {
            material.shader = shader;
        }
        material.mainTexture = font.atlasTexture;
        material.SetColor("_FaceColor", Color.white);
        font.material = material;

        font.TryAddCharacters(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            " .,:;!?-/()[]+_<>%#&" +
            "\u00C0\u00C1\u00C2\u00C3\u00C7\u00C9\u00CA\u00CD\u00D3\u00D4\u00D5\u00DA" +
            "\u00E0\u00E1\u00E2\u00E3\u00E7\u00E9\u00EA\u00ED\u00F3\u00F4\u00F5\u00FA" +
            "\u2026");
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(font);
        return font;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
