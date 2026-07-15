#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectAurora.Customization.Skins;
using ProjectAurora.UI.Menu;
using ProjectAurora.UI.Menu.Skins;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuroraSkinMenuTests
{
    private const string MainMenuScene = "Assets/_ProjectAurora/Scenes/MainMenu.unity";
    private const string CatalogPath = "Assets/_ProjectAurora/Data/Skins/AuroraSkinCatalog.asset";
    private const string RenderTexturePath =
        "Assets/_ProjectAurora/Art/Skin/RenderTextures/RT_SkinPreview.renderTexture";

    private static int assertions;

    [MenuItem("Tools/Projeto Aurora/Skins/Run Skin Menu Tests")]
    public static void RunAll()
    {
        assertions = 0;
        try
        {
            TestCatalogAndSplashArts();
            TestSelectionAndPersistence();
            TestCanonicalSceneIntegration();
            Debug.Log("[AuroraSkinMenuTests] PASS: " + assertions + " assertions.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[AuroraSkinMenuTests] FAIL apos " + assertions +
                           " assertions. " + exception);
            throw;
        }
    }

    private static void TestCatalogAndSplashArts()
    {
        AuroraSkinCatalog catalog = AssetDatabase.LoadAssetAtPath<AuroraSkinCatalog>(CatalogPath);
        Expect(catalog != null, "catalogo existe");
        Expect(catalog.Count == 6, "catalogo contem seis skins");
        Expect(catalog.CollectValidationIssues().Count == 0, "catalogo nao possui issues estruturais");

        string[] expectedIds =
        {
            "default",
            "brazil",
            "aurora-ceremonial",
            "celestia-theme",
            "corrupted",
            "post-collapse-survivor"
        };
        Expect(catalog.Skins.Select(skin => skin.Id).SequenceEqual(expectedIds),
            "ordem do catalogo e deterministica");
        Expect(catalog.Skins.Select(skin => skin.Id).Distinct(StringComparer.Ordinal).Count() == catalog.Count,
            "IDs sao unicos");
        Expect(catalog.Skins.Count(skin => skin.IsDefaultSkin) == 1,
            "existe exatamente uma skin default");

        foreach (AuroraSkinDefinition skin in catalog.Skins)
        {
            Expect(skin != null, "entrada do catalogo nao e nula");
            Expect(skin.SplashArt != null, skin.Id + " possui Splash Art");
            Texture2D texture = skin.SplashArt.texture;
            Expect(texture != null && texture.width == 1672 && texture.height == 941,
                skin.Id + " preserva dimensoes 1672x941");
            Expect(Mathf.Abs(texture.width / (float)texture.height - 16f / 9f) < 0.002f,
                skin.Id + " usa proporcao 16:9");

            string path = AssetDatabase.GetAssetPath(skin.SplashArt);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Expect(importer != null, skin.Id + " possui TextureImporter");
            Expect(importer.textureType == TextureImporterType.Sprite,
                skin.Id + " esta importada como Sprite");
            Expect(importer.spriteImportMode == SpriteImportMode.Single,
                skin.Id + " usa Sprite Mode Single");
            Expect(!importer.mipmapEnabled, skin.Id + " nao usa mipmaps");
            Expect(importer.filterMode == FilterMode.Bilinear, skin.Id + " usa filtro Bilinear");
            Expect(importer.maxTextureSize == 2048, skin.Id + " usa Max Size 2048");
            Expect(importer.textureCompression == TextureImporterCompression.CompressedHQ,
                skin.Id + " usa compressao High Quality");
        }

        AuroraSkinDefinition defaultSkin = catalog.GetDefaultSkin();
        Expect(defaultSkin != null && defaultSkin.Id == "default", "GetDefaultSkin resolve Dr. Elias");
        Expect(defaultSkin.PreviewPrefab != null, "skin default possui prefab de preview");
        Expect(defaultSkin.GameplayPrefab != null, "skin default possui prefab de gameplay preparado");
        Expect(Mathf.Approximately(defaultSkin.PreviewRotationOffset.y, 90f),
            "skin default fica frontal no preview");
        Expect(catalog.Skins.Count(skin => skin.PreviewPrefab == null) == 5,
            "cinco skins sem modelo permanecem catalogadas");
        Expect(catalog.Skins.Where(skin => skin.Id != "default").All(skin => skin.GameplayPrefab == null),
            "skins sem modelo nao recebem gameplayPrefab por busca vaga");

        GameObject previewPrefab = defaultSkin.PreviewPrefab;
        Expect(previewPrefab.GetComponentsInChildren<Renderer>(true).Length > 0,
            "prefab de preview possui renderers");
        Expect(previewPrefab.GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled),
            "colliders do prefab de preview estao desativados");
        Expect(previewPrefab.GetComponentsInChildren<AudioSource>(true).All(source => !source.enabled),
            "AudioSources do prefab de preview estao desativados");
        Expect(previewPrefab.GetComponentsInChildren<Animator>(true).All(animator => !animator.enabled),
            "Animator do prefab de preview esta desativado para manter T-pose");
    }

    private static void TestSelectionAndPersistence()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(),
            "AuroraSkinMenuTests-" + Guid.NewGuid().ToString("N"));
        string savePath = Path.Combine(tempDirectory, AuroraProgressSaveService.DefaultFileName);
        Directory.CreateDirectory(tempDirectory);

        GameObject walletObject = null;
        GameObject model = null;
        AuroraSkinCatalog catalog = null;
        var definitions = new List<AuroraSkinDefinition>();
        try
        {
            model = new GameObject("SkinModel_Test");
            AuroraSkinDefinition defaultSkin = CreateDefinition(
                "default", true, true, model, model);
            AuroraSkinDefinition alternate = CreateDefinition(
                "alternate", true, false, model, model);
            AuroraSkinDefinition locked = CreateDefinition(
                "locked", false, false, model, model);
            AuroraSkinDefinition missingModel = CreateDefinition(
                "missing-model", true, false, null, null);
            definitions.AddRange(new[] { defaultSkin, alternate, locked, missingModel });

            catalog = ScriptableObject.CreateInstance<AuroraSkinCatalog>();
            catalog.ConfigureForEditor(definitions);
            walletObject = new GameObject("AuroraSkinWallet_Test");
            AuroraCoinWallet wallet = walletObject.AddComponent<AuroraCoinWallet>();
            wallet.ConfigureForTests(new AuroraProgressSaveService(savePath));

            var selection = new AuroraSkinSelectionService(catalog, wallet);
            int eventCount = 0;
            selection.OnSelectedSkinChanged += _ => eventCount++;
            selection.LoadSelectedSkin();
            Expect(selection.SelectedSkinId == "default", "novo save restaura skin default");
            Expect(selection.GetSelectedSkin() == defaultSkin, "GetSelectedSkin retorna definicao equipada");
            Expect(selection.CanSelect("alternate"), "skin desbloqueada com modelo pode ser selecionada");
            Expect(!selection.CanSelect("locked"), "skin bloqueada nao pode ser selecionada");
            Expect(!selection.CanSelect("missing-model"), "skin sem gameplayPrefab nao pode ser selecionada");
            Expect(!selection.CanSelect("invalid"), "ID invalido nao pode ser selecionado");

            Expect(selection.TrySelect("alternate"), "SELECIONAR equipa skin valida");
            Expect(selection.SelectedSkinId == "alternate", "skin visualizada valida passa a equipada");
            Expect(eventCount == 1, "selecao valida emite um evento");
            Expect(selection.TrySelect("alternate"), "selecionar a equipada e operacao valida");
            Expect(eventCount == 1, "skin ja equipada nao emite evento ou salva novamente");
            Expect(!selection.TrySelect("locked"), "tentativa de equipar bloqueada e rejeitada");
            Expect(selection.SelectedSkinId == "alternate", "tentativa bloqueada preserva equipada");

            AuroraProgressSaveData persisted = new AuroraProgressSaveService(savePath).Load();
            Expect(persisted.selectedSkinId == "alternate", "save central persiste somente o ID");
            Expect(persisted.version == AuroraProgressSaveData.CurrentVersion,
                "save usa versao atual com selectedSkinId");

            Expect(wallet.TrySetSelectedSkinId("removed-skin"), "probe grava ID removido");
            var reloaded = new AuroraSkinSelectionService(catalog, wallet);
            reloaded.LoadSelectedSkin();
            Expect(reloaded.SelectedSkinId == "default", "ID removido volta para default");
            Expect(wallet.SelectedSkinId == "default", "fallback valido substitui ID invalido no save");

            Expect(wallet.TrySetSelectedSkinId("locked"), "probe grava ID bloqueado");
            reloaded.LoadSelectedSkin();
            Expect(reloaded.SelectedSkinId == "default", "ID bloqueado salvo volta para default");
        }
        finally
        {
            AuroraCoinWallet wallet = walletObject == null ? null : walletObject.GetComponent<AuroraCoinWallet>();
            AuroraCoinWallet.ReleaseTestInstance(wallet);
            if (walletObject != null) UnityEngine.Object.DestroyImmediate(walletObject);
            if (model != null) UnityEngine.Object.DestroyImmediate(model);
            if (catalog != null) UnityEngine.Object.DestroyImmediate(catalog);
            foreach (AuroraSkinDefinition definition in definitions)
            {
                if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
            }
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static void TestCanonicalSceneIntegration()
    {
        Scene scene = SceneManager.GetActiveScene();
        Expect(scene.path == MainMenuScene, "teste executado na MainMenu oficial");

        GameObject panel = FindSceneObjects(scene, "SkinSelectionPanel").SingleOrDefault();
        GameObject previewSystem = FindSceneObjects(scene, "SkinPreviewSystem").SingleOrDefault();
        GameObject canvasObject = FindSceneObjects(scene, "Canvas_MainMenu").SingleOrDefault();
        Expect(panel != null, "SkinSelectionPanel existe uma unica vez");
        Expect(!panel.activeSelf, "SkinSelectionPanel inicia fechado");
        Expect(previewSystem != null, "SkinPreviewSystem existe uma unica vez");
        Expect(canvasObject != null, "Canvas oficial foi preservado");

        AuroraSkinSelectionController selection = panel.GetComponent<AuroraSkinSelectionController>();
        AuroraMenuExtraController extra = panel.GetComponentInParent<AuroraMenuExtraController>(true);
        Expect(selection != null, "painel possui AuroraSkinSelectionController");
        Expect(extra != null, "painel permanece dentro do Extra oficial");

        Transform card = extra.transform.Find("Card");
        Button skinBack = panel.transform.Find("Header/Button_Retornar_SkinMenu").GetComponent<Button>();
        Expect(card != null, "card do hub Extra foi preservado");
        Expect(extra.HandlesSubpanelBackButton(skinBack),
            "botao Voltar pertence ao subpainel e retorna ao Extra");
        SerializedObject serializedExtra = new SerializedObject(extra);
        Expect(serializedExtra.FindProperty("mainCard").objectReferenceValue == card.gameObject,
            "Extra referencia o card principal");
        Expect(serializedExtra.FindProperty("skinPanel").objectReferenceValue == panel,
            "botao SKIN referencia o novo painel");

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        Expect(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
            "Canvas usa escala responsiva");
        Expect(scaler.referenceResolution == new Vector2(1920f, 1080f),
            "layout usa referencia 1920x1080");

        RectTransform rootRect = panel.transform as RectTransform;
        Expect(rootRect.anchorMin == Vector2.zero && rootRect.anchorMax == Vector2.one,
            "painel ocupa toda a area do Extra");
        Image overlay = panel.transform.Find("BackgroundOverlay").GetComponent<Image>();
        Expect(overlay.raycastTarget, "overlay bloqueia click-through");

        Image splash = panel.transform.Find("SplashArtArea/SplashFrame/SplashImage").GetComponent<Image>();
        AspectRatioFitter splashAspect = splash.GetComponent<AspectRatioFitter>();
        Expect(splash.preserveAspect, "SplashImage preserva aspecto");
        Expect(splashAspect != null && splashAspect.aspectMode == AspectRatioFitter.AspectMode.FitInParent,
            "SplashImage usa Fit In Parent");
        Expect(Mathf.Approximately(splashAspect.aspectRatio, 16f / 9f),
            "SplashImage fixa proporcao 16:9");

        RawImage rawImage = panel.transform.Find("Preview3DArea/PreviewFrame/PreviewRawImage")
            .GetComponent<RawImage>();
        Expect(!rawImage.raycastTarget, "PreviewRawImage nao intercepta input");
        Expect(rawImage.texture != null, "PreviewRawImage referencia RenderTexture");
        RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        Expect(renderTexture != null && renderTexture.width == 1024 && renderTexture.height == 1024,
            "RenderTexture usa 1024x1024");
        Expect(rawImage.texture == renderTexture, "UI usa a RenderTexture oficial");

        int previewLayer = LayerMask.NameToLayer("SkinPreview");
        Expect(previewLayer >= 0, "layer SkinPreview existe");
        AuroraSkinPreviewController preview = previewSystem.GetComponent<AuroraSkinPreviewController>();
        Camera previewCamera = previewSystem.GetComponentInChildren<Camera>(true);
        Expect(preview != null && previewCamera != null, "controller e camera de preview existem");
        Expect(!previewCamera.enabled, "PreviewCamera inicia desligada");
        Expect(previewCamera.cullingMask == 1 << previewLayer,
            "PreviewCamera renderiza somente SkinPreview");
        Expect(previewCamera.targetTexture == renderTexture,
            "PreviewCamera escreve na RenderTexture oficial");

        Camera mainCamera = FindSceneComponents<Camera>(scene)
            .FirstOrDefault(camera => camera.CompareTag("MainCamera"));
        Expect(mainCamera != null, "camera principal da MainMenu existe");
        Expect((mainCamera.cullingMask & (1 << previewLayer)) == 0,
            "camera principal exclui SkinPreview");
        Light[] previewLights = previewSystem.GetComponentsInChildren<Light>(true);
        Expect(previewLights.Length == 3, "preview usa tres luzes leves");
        Expect(previewLights.All(light => light.cullingMask == 1 << previewLayer),
            "luzes afetam somente SkinPreview");

        Rect left = GetLocalRect(panel.transform.Find("SplashArtArea") as RectTransform, rootRect);
        Rect right = GetLocalRect(panel.transform.Find("Preview3DArea") as RectTransform, rootRect);
        Expect(left.xMax + 40f <= right.xMin, "Splash Art e preview possuem espacamento sem sobreposicao");
        Expect(IsInside(panel.transform.Find("Preview3DArea/ActionArea") as RectTransform, rootRect),
            "acao permanece dentro do painel");
        Expect(IsInside(panel.transform.Find("Footer") as RectTransform, rootRect),
            "footer permanece dentro do painel");

        EventSystem eventSystem = FindSceneComponents<EventSystem>(scene).SingleOrDefault();
        Expect(eventSystem != null, "EventSystem oficial foi preservado");
        TMP_Text unavailable = panel.transform.Find("Preview3DArea/PreviewFrame/PreviewUnavailableText")
            .GetComponent<TMP_Text>();
        Expect(unavailable.text.Contains("MODELO 3D"), "fallback sem modelo esta preparado");

        bool gameplayEnabled = EditorBuildSettings.scenes.Any(buildScene =>
            buildScene.enabled && Path.GetFileNameWithoutExtension(buildScene.path) == "Beta03_Principal");
        Expect(gameplayEnabled, "Beta03_Principal continua habilitada nos Build Settings");
    }

    private static AuroraSkinDefinition CreateDefinition(
        string id,
        bool unlockedByDefault,
        bool isDefault,
        GameObject previewPrefab,
        GameObject gameplayPrefab)
    {
        AuroraSkinDefinition definition = ScriptableObject.CreateInstance<AuroraSkinDefinition>();
        definition.ConfigureForEditor(
            id,
            id,
            id,
            null,
            previewPrefab,
            gameplayPrefab,
            unlockedByDefault,
            0,
            id,
            isDefault,
            Vector3.zero,
            Vector3.zero,
            1f,
            0f,
            Color.black);
        return definition;
    }

    private static IEnumerable<GameObject> FindSceneObjects(Scene scene, string name)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(gameObject => gameObject.scene == scene && gameObject.name == name);
    }

    private static IEnumerable<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .Where(component => component.gameObject.scene == scene);
    }

    private static Rect GetLocalRect(RectTransform child, RectTransform root)
    {
        var corners = new Vector3[4];
        child.GetWorldCorners(corners);
        for (int i = 0; i < corners.Length; i++) corners[i] = root.InverseTransformPoint(corners[i]);
        return Rect.MinMaxRect(corners.Min(point => point.x), corners.Min(point => point.y),
            corners.Max(point => point.x), corners.Max(point => point.y));
    }

    private static bool IsInside(RectTransform child, RectTransform parent)
    {
        Rect childRect = GetLocalRect(child, parent);
        Rect bounds = parent.rect;
        return childRect.xMin >= bounds.xMin - 0.1f && childRect.xMax <= bounds.xMax + 0.1f &&
               childRect.yMin >= bounds.yMin - 0.1f && childRect.yMax <= bounds.yMax + 0.1f;
    }

    private static void Expect(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException("Assertion falhou: " + message);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[AuroraSkinMenuTests] Nao foi possivel limpar temporarios: " +
                             exception.Message);
        }
    }
}
#endif
