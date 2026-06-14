using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuroraGameOverBuilder
{
    private const string ScenePath =
        "Assets/_ProjectAurora/Scenes/FASE 01 - Laboratório Limpo A/" +
        "Fase01_SetorA_LaboratorioLimpo.unity";
    private const string MusicPath =
        "Assets/_ProjectAurora/Audio/Music/GameOver.mp3";
    private const string FontPath =
        "Assets/_ProjectAurora/Art/UI/Generated/AuroraHUD_Font.asset";
    private const string PrefabPath =
        "Assets/_ProjectAurora/Prefabs/UI/Canvas_GameOver.prefab";

    private static readonly Color Cyan = new Color(0.06f, 0.88f, 1f);
    private static readonly Color Red = new Color(1f, 0.08f, 0.12f);
    private static readonly Color White = new Color(0.9f, 0.96f, 1f);
    private static TMP_FontAsset font;

    [MenuItem("Tools/Projeto Aurora/Fase 01/Build Game Over Cinematico")]
    public static void Build()
    {
        AssetDatabase.ImportAsset(MusicPath, ImportAssetOptions.ForceSynchronousImport);
        ConfigureAudioImporter();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        if (music == null)
        {
            throw new System.IO.FileNotFoundException(
                "GameOver.mp3 nao foi importado.", MusicPath);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject existing = GameObject.Find("Canvas_GameOver");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject canvasObject = new GameObject(
            "Canvas_GameOver",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(GameOverManager));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject root = CreateUiObject(
            "Panel_GameOverRoot",
            canvasObject.transform,
            typeof(CanvasGroup),
            typeof(Image));
        Stretch(root.GetComponent<RectTransform>());
        CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;
        Image rootBlocker = root.GetComponent<Image>();
        rootBlocker.color = Color.clear;
        rootBlocker.raycastTarget = true;

        Image fade = CreateImage(
            "Image_FadeBackground",
            root.transform,
            Color.black);
        Stretch(fade.rectTransform);
        fade.raycastTarget = true;

        Image flash = CreateImage(
            "Image_ImpactFlash",
            root.transform,
            new Color(0.8f, 0.01f, 0.02f, 0f));
        Stretch(flash.rectTransform);

        BuildScanLines(root.transform);
        BuildFrame(root.transform);

        TMP_Text status = CreateText(
            "Text_StatusSmall",
            root.transform,
            "SINAL VITAL PERDIDO",
            26f,
            FontStyles.Bold,
            Cyan,
            new Vector2(0f, 162f),
            new Vector2(920f, 48f));
        status.characterSpacing = 8f;

        TMP_Text title = CreateText(
            "Text_GameOverTitle",
            root.transform,
            "GAME OVER",
            92f,
            FontStyles.Bold,
            White,
            new Vector2(0f, 58f),
            new Vector2(1200f, 130f));
        title.characterSpacing = 5f;
        Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(1f, 0.02f, 0.06f, 0.72f);
        titleShadow.effectDistance = new Vector2(4f, -2f);

        TMP_Text message = CreateText(
            "Text_CelestIAMessage",
            root.transform,
            "CELESTIA: Dr. Elias não responde. Encerrando protocolo de evacuação.",
            28f,
            FontStyles.Normal,
            White,
            new Vector2(0f, -58f),
            new Vector2(1220f, 90f));
        message.textWrappingMode = TextWrappingModes.Normal;

        TMP_Text diagnostic = CreateText(
            "Text_Diagnostic",
            root.transform,
            "// verificando sinais vitais...",
            20f,
            FontStyles.Normal,
            Cyan,
            new Vector2(0f, -144f),
            new Vector2(900f, 42f));
        diagnostic.characterSpacing = 3f;

        GameObject buttonsPanel = CreateUiObject(
            "Panel_Buttons",
            root.transform,
            typeof(CanvasGroup));
        RectTransform buttonsRect = buttonsPanel.GetComponent<RectTransform>();
        Center(buttonsRect, new Vector2(0f, -278f), new Vector2(760f, 82f));
        CanvasGroup buttonsGroup = buttonsPanel.GetComponent<CanvasGroup>();
        buttonsGroup.alpha = 0f;
        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;

        Button retry = CreateButton(
            "Button_Retry",
            buttonsPanel.transform,
            "TENTAR NOVAMENTE",
            new Vector2(-190f, 0f),
            Red,
            true);
        Button menu = CreateButton(
            "Button_Menu",
            buttonsPanel.transform,
            "VOLTAR AO MENU",
            new Vector2(190f, 0f),
            Cyan,
            false);

        GameObject audioObject = new GameObject(
            "Audio_GameOverMusic",
            typeof(AudioSource));
        audioObject.transform.SetParent(canvasObject.transform, false);
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
        audioSource.clip = music;

        PlayerRunner player = Object.FindAnyObjectByType<PlayerRunner>();
        PlayerInteraction interaction =
            player == null ? null : player.GetComponent<PlayerInteraction>();
        Behaviour[] systemsToDisable = Object
            .FindObjectsByType<ObstacleSpawner>(
                FindObjectsInactive.Include)
            .Cast<Behaviour>()
            .ToArray();
        Collider[] hazardColliders = FindHazardColliders();

        GameOverManager manager = canvasObject.GetComponent<GameOverManager>();
        SerializedObject managerSerialized = new SerializedObject(manager);
        SetObject(managerSerialized, "gameOverCanvas", canvas);
        SetObject(managerSerialized, "panelRoot", root);
        SetObject(managerSerialized, "rootGroup", rootGroup);
        SetObject(managerSerialized, "fadeBackground", fade);
        SetObject(managerSerialized, "impactFlash", flash);
        SetObject(managerSerialized, "statusText", status);
        SetObject(managerSerialized, "titleText", title);
        SetObject(managerSerialized, "celestIAMessageText", message);
        SetObject(managerSerialized, "diagnosticText", diagnostic);
        SetObject(managerSerialized, "buttonsGroup", buttonsGroup);
        SetObject(managerSerialized, "retryButton", retry);
        SetObject(managerSerialized, "menuButton", menu);
        SetObject(managerSerialized, "gameOverMusicSource", audioSource);
        SetObject(managerSerialized, "gameOverMusic", music);
        SetObject(managerSerialized, "player", player);
        SetObject(managerSerialized, "playerInteraction", interaction);
        SetArray(managerSerialized, "systemsToDisable", systemsToDisable);
        SetArray(managerSerialized, "hazardColliders", hazardColliders);
        managerSerialized.FindProperty("totalSequenceDuration").floatValue = 14f;
        managerSerialized.FindProperty("gameplayMusicFadeDuration").floatValue = 0.75f;
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameManager game = Object.FindAnyObjectByType<GameManager>();
        if (game == null)
        {
            throw new System.InvalidOperationException(
                "GameManager nao encontrado na Fase 01.");
        }
        SerializedObject gameSerialized = new SerializedObject(game);
        SetObject(gameSerialized, "gameOverManager", manager);
        gameSerialized.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(game);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        GameObject oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (oldPrefab != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }
        PrefabUtility.SaveAsPrefabAsset(canvasObject, PrefabPath);
        AssetDatabase.SaveAssets();

        Validate(manager, game, music);
        Debug.Log(
            "PROJETO:AURORA - Sequencia cinematografica de Game Over configurada.");
    }

    private static void ConfigureAudioImporter()
    {
        AudioImporter importer = AssetImporter.GetAtPath(MusicPath) as AudioImporter;
        if (importer == null)
        {
            return;
        }

        importer.forceToMono = false;
        importer.loadInBackground = false;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.preloadAudioData = true;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    private static void BuildScanLines(Transform parent)
    {
        for (int i = 0; i < 13; i++)
        {
            Image line = CreateImage(
                "Diagnostic Scanline " + (i + 1),
                parent,
                new Color(0.12f, 0.75f, 0.82f, i % 3 == 0 ? 0.055f : 0.025f));
            RectTransform rect = line.rectTransform;
            rect.anchorMin = new Vector2(0f, i / 12f);
            rect.anchorMax = new Vector2(1f, i / 12f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, i % 3 == 0 ? 2f : 1f);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    private static void BuildFrame(Transform parent)
    {
        CreateFrameLine(parent, "Frame Top", new Vector2(0.5f, 1f), new Vector2(1180f, 3f), new Vector2(0f, -86f), Cyan);
        CreateFrameLine(parent, "Frame Bottom", new Vector2(0.5f, 0f), new Vector2(1180f, 3f), new Vector2(0f, 86f), Red);
        CreateFrameLine(parent, "Frame Left", new Vector2(0f, 0.5f), new Vector2(3f, 640f), new Vector2(126f, 0f), Cyan);
        CreateFrameLine(parent, "Frame Right", new Vector2(1f, 0.5f), new Vector2(3f, 640f), new Vector2(-126f, 0f), Red);
    }

    private static void CreateFrameLine(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        Image line = CreateImage(name, parent, new Color(color.r, color.g, color.b, 0.68f));
        RectTransform rect = line.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        Color accent,
        bool primary)
    {
        GameObject go = CreateUiObject(
            name,
            parent,
            typeof(Image),
            typeof(Button),
            typeof(Outline));
        RectTransform rect = go.GetComponent<RectTransform>();
        Center(rect, position, new Vector2(340f, 72f));

        Image image = go.GetComponent<Image>();
        image.color = primary
            ? new Color(accent.r * 0.34f, accent.g * 0.34f, accent.b * 0.34f, 0.96f)
            : new Color(0.025f, 0.07f, 0.09f, 0.96f);
        image.raycastTarget = true;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.72f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.5f, 0.78f, 0.82f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.32f, 0.38f, 0.4f, 0.56f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText(
            "Label",
            go.transform,
            label,
            21f,
            FontStyles.Bold,
            White,
            Vector2.zero,
            Vector2.zero);
        Stretch(text.rectTransform);
        return button;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float size,
        FontStyles style,
        Color color,
        Vector2 position,
        Vector2 dimensions)
    {
        GameObject go = CreateUiObject(
            name,
            parent,
            typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        Center(rect, position, dimensions);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
        }
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = CreateUiObject(name, parent, typeof(Image));
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static GameObject CreateUiObject(
        string name,
        Transform parent,
        params System.Type[] components)
    {
        List<System.Type> types = new List<System.Type>
        {
            typeof(RectTransform),
            typeof(CanvasRenderer)
        };
        types.AddRange(components);
        GameObject go = new GameObject(name, types.Distinct().ToArray());
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Collider[] FindHazardColliders()
    {
        HashSet<Collider> colliders = new HashSet<Collider>();
        foreach (Obstacle obstacle in Object.FindObjectsByType<Obstacle>(
            FindObjectsInactive.Include))
        {
            foreach (Collider collider in obstacle.GetComponents<Collider>())
            {
                colliders.Add(collider);
            }
        }

        foreach (LaserHazard laser in Object.FindObjectsByType<LaserHazard>(
            FindObjectsInactive.Include))
        {
            if (laser.damageCollider != null)
            {
                colliders.Add(laser.damageCollider);
            }
            foreach (Collider collider in laser.GetComponents<Collider>())
            {
                colliders.Add(collider);
            }
        }
        return colliders.ToArray();
    }

    private static void SetObject(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                "Propriedade serializada nao encontrada: " + propertyName);
        }
        property.objectReferenceValue = value;
    }

    private static void SetArray<T>(
        SerializedObject serialized,
        string propertyName,
        T[] values)
        where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void Validate(
        GameOverManager manager,
        GameManager game,
        AudioClip music)
    {
        SerializedObject serialized = new SerializedObject(manager);
        string[] required =
        {
            "gameOverCanvas",
            "panelRoot",
            "rootGroup",
            "fadeBackground",
            "statusText",
            "titleText",
            "celestIAMessageText",
            "buttonsGroup",
            "retryButton",
            "menuButton",
            "gameOverMusicSource",
            "gameOverMusic"
        };
        foreach (string propertyName in required)
        {
            if (serialized.FindProperty(propertyName).objectReferenceValue == null)
            {
                throw new System.InvalidOperationException(
                    "GameOverManager sem referencia: " + propertyName);
            }
        }

        if (game.gameOverManager != manager || music.length < 12f)
        {
            throw new System.InvalidOperationException(
                "Integracao do Game Over nao foi concluida corretamente.");
        }
    }

    private static void Center(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
