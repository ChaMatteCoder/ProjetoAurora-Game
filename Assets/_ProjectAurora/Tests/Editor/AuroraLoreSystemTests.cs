#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectAurora.Editor.Lore;
using ProjectAurora.Lore;
using ProjectAurora.UI.Menu;
using ProjectAurora.UI.Menu.Lore;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuroraLoreSystemTests
{
    private const string MainMenuScene = "Assets/_ProjectAurora/Scenes/MainMenu.unity";
    private const string CatalogPath = "Assets/_ProjectAurora/Data/Lore/AuroraLoreCatalog.asset";
    private const string PrefabPath =
        "Assets/_ProjectAurora/Prefabs/Collectibles/PF_Aurora_DataFile.prefab";
    private static readonly string[] LegacyLoreIds =
    {
        "LORE_001", "LORE_003", "LORE_005", "LORE_006",
        "LORE_011", "LORE_013", "LORE_014", "LORE_017",
        "LORE_018", "LORE_019", "LORE_022", "LORE_023"
    };
    private static int assertions;

    [MenuItem("Tools/Projeto Aurora/Lore/Run Lore System Tests")]
    public static void RunAll()
    {
        assertions = 0;
        try
        {
            AuroraLoreCatalog catalog = TestCatalogFilesAndEncoding();
            TestUnlockPurchaseAndPersistence(catalog);
            TestLegacyDataFileSequenceAndPersistence(catalog);
            TestCollectiblePrefabAndPermanence(catalog);
            TestCanonicalMenuIntegration(catalog);
            Debug.Log("[AuroraLoreTests] PASS: " + assertions + " checks.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[AuroraLoreTests] FAIL após " + assertions +
                           " assertions. " + exception);
            throw;
        }
    }

    private static void TestLegacyDataFileSequenceAndPersistence(AuroraLoreCatalog catalog)
    {
        string directory = Path.Combine(Path.GetTempPath(), "AuroraLegacyDataFileTests-" + Guid.NewGuid().ToString("N"));
        string savePath = Path.Combine(directory, AuroraProgressSaveService.DefaultFileName);
        Directory.CreateDirectory(directory);
        GameObject walletObject = null;
        GameObject managerObject = null;
        AuroraLoreService service = null;
        try
        {
            walletObject = new GameObject("AuroraLegacyDataFileWallet_Test");
            AuroraCoinWallet wallet = walletObject.AddComponent<AuroraCoinWallet>();
            wallet.ConfigureForTests(new AuroraProgressSaveService(savePath));
            service = AuroraLoreService.Initialize(catalog, wallet);

            managerObject = new GameObject("AuroraLegacyDataFileManager_Test");
            DataFileManager manager = managerObject.AddComponent<DataFileManager>();
            manager.ConfigureLoreCatalogForEditor(catalog);

            for (int i = 0; i < LegacyLoreIds.Length; i++)
            {
                string dataFileId = "DF_" + (i + 1).ToString("00");
                string loreId = DataFileManager.ResolveLegacyLoreId(dataFileId);
                Expect(loreId == LegacyLoreIds[i], dataFileId + " resolve para " + LegacyLoreIds[i]);
                Expect(catalog.GetById(loreId).UnlockType == AuroraLoreUnlockType.GameplayCollectible,
                    loreId + " é coletável oficial");
                Expect(manager.Collect(dataFileId), dataFileId + " é coletado pela ponte legada");
            }

            Expect(manager.CollectedCount == 12, "os 12 DataFiles são contabilizados na corrida");
            Expect(!manager.Collect("DF_01"), "coleta duplicada é recusada");
            Expect(DataFileManager.ResolveLegacyLoreId("DF_13") == string.Empty,
                "ID legado fora da faixa é rejeitado");

            wallet.Load();
            AuroraLoreService reloaded = new AuroraLoreService(catalog, wallet);
            foreach (string loreId in LegacyLoreIds)
                Expect(reloaded.IsUnlocked(loreId), loreId + " persiste após reload");
        }
        finally
        {
            if (managerObject != null) UnityEngine.Object.DestroyImmediate(managerObject);
            if (service != null) AuroraLoreService.ReleaseTestInstance(service);
            if (walletObject != null)
            {
                AuroraCoinWallet wallet = walletObject.GetComponent<AuroraCoinWallet>();
                AuroraCoinWallet.ReleaseTestInstance(wallet);
                UnityEngine.Object.DestroyImmediate(walletObject);
            }
            TryDeleteDirectory(directory);
        }
    }

    private static AuroraLoreCatalog TestCatalogFilesAndEncoding()
    {
        AuroraLoreCatalog catalog = AssetDatabase.LoadAssetAtPath<AuroraLoreCatalog>(CatalogPath);
        Expect(catalog != null, "catálogo existe");
        Expect(catalog.Count == 24, "catálogo contém 24 entradas");
        Expect(catalog.CollectValidationIssues().Count == 0, "catálogo não possui issues estruturais");
        Expect(AuroraLoreCatalogBuilder.CollectValidationIssues(catalog, false).Count == 0,
            "arquivos e encoding passam no validador");
        Expect(catalog.Entries.Select(entry => entry.Id).SequenceEqual(
            Enumerable.Range(1, 24).Select(number => "LORE_" + number.ToString("000"))),
            "ordem LORE_001 até LORE_024 é determinística");
        Expect(catalog.Entries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count() == 24,
            "IDs são únicos");
        Expect(catalog.Entries.Count(entry => entry.UnlockType == AuroraLoreUnlockType.Default) == 2,
            "existem dois arquivos default");
        Expect(catalog.Entries.Count(entry => entry.UnlockType == AuroraLoreUnlockType.GameplayCollectible) == 12,
            "existem doze arquivos coletáveis");
        Expect(catalog.Entries.Count(entry => entry.UnlockType == AuroraLoreUnlockType.AuroraCoinPurchase) == 8,
            "existem oito arquivos compráveis");
        Expect(catalog.Entries.Count(entry => entry.UnlockType == AuroraLoreUnlockType.SecretMission) == 2,
            "existem dois arquivos secretos");

        Expect(catalog.GetById("LORE_008").UnlockedByDefault, "LORE_008 é default");
        Expect(catalog.GetById("LORE_009").UnlockedByDefault, "LORE_009 é default");
        Expect(catalog.GetById("LORE_020").IsSecret, "LORE_020 é secreto");
        Expect(catalog.GetById("LORE_024").IsSecret, "LORE_024 é secreto");
        Expect(catalog.GetById("LORE_020").AuroraCoinPrice == 0, "LORE_020 não possui preço");
        Expect(catalog.GetById("LORE_024").AuroraCoinPrice == 0, "LORE_024 não possui preço");

        var expectedPrices = new Dictionary<string, int>
        {
            { "LORE_002", 10 }, { "LORE_004", 10 }, { "LORE_007", 15 }, { "LORE_010", 15 },
            { "LORE_012", 15 }, { "LORE_015", 20 }, { "LORE_016", 20 }, { "LORE_021", 20 }
        };
        foreach (KeyValuePair<string, int> price in expectedPrices)
            Expect(catalog.GetById(price.Key).AuroraCoinPrice == price.Value,
                price.Key + " possui preço provisório correto");

        foreach (AuroraLoreDefinition entry in catalog.Entries)
        {
            Expect(entry.FullText != null && !string.IsNullOrWhiteSpace(entry.FullText.text),
                entry.Id + " possui TextAsset não vazio");
            Expect(!ContainsMojibake(entry.FullText.text), entry.Id + " não contém mojibake");
            Expect(entry.SourceFileName == entry.Id + ".txt", entry.Id + " preserva nome-fonte");
        }

        string lore001 = catalog.GetById("LORE_001").FullText.text;
        string lore008 = catalog.GetById("LORE_008").FullText.text;
        Expect(lore001.Contains("Início"), "LORE_001 preserva acento em Início");
        Expect(lore008.Contains("Versão Inicial"), "LORE_008 preserva cedilha/acento");
        string formatted = AuroraLoreTextFormatter.FormatForDisplay(lore008);
        Expect(!formatted.Contains("**") && !formatted.StartsWith("#"),
            "formatador remove marcação Markdown visual");
        Expect(formatted.Contains("função inicial"), "formatador preserva conteúdo PT-BR");
        return catalog;
    }

    private static void TestUnlockPurchaseAndPersistence(AuroraLoreCatalog catalog)
    {
        string directory = Path.Combine(Path.GetTempPath(), "AuroraLoreTests-" + Guid.NewGuid().ToString("N"));
        string savePath = Path.Combine(directory, AuroraProgressSaveService.DefaultFileName);
        Directory.CreateDirectory(directory);
        GameObject walletObject = null;
        AuroraLoreService service = null;
        try
        {
            walletObject = new GameObject("AuroraLoreWallet_Test");
            AuroraCoinWallet wallet = walletObject.AddComponent<AuroraCoinWallet>();
            wallet.ConfigureForTests(new AuroraProgressSaveService(savePath));
            wallet.SetBalanceForTests(30);
            service = AuroraLoreService.Initialize(catalog, wallet);

            Expect(service.IsUnlocked("LORE_008"), "LORE_008 inicia desbloqueado");
            Expect(service.IsUnlocked("LORE_009"), "LORE_009 inicia desbloqueado");
            Expect(service.UnlockedCount == 2, "novo save inicia com 2/24");
            Expect(!service.IsUnlocked("LORE_002"), "LORE_002 inicia bloqueado");
            Expect(service.CanPurchase("LORE_002"), "LORE_002 pode ser comprado com saldo");
            Expect(service.TryPurchase("LORE_002"), "compra de LORE_002 funciona");
            Expect(wallet.Balance == 20, "compra desconta 10 AuroraCoins");
            Expect(service.IsUnlocked("LORE_002"), "compra desbloqueia LORE_002");
            Expect(!service.TryPurchase("LORE_002"), "compra duplicada é impedida");
            Expect(wallet.Balance == 20, "compra duplicada não desconta saldo");

            wallet.SetBalanceForTests(0);
            Expect(!service.CanPurchase("LORE_004"), "saldo insuficiente bloqueia compra");
            Expect(!service.TryPurchase("LORE_004"), "compra sem saldo falha");
            Expect(wallet.Balance == 0 && !service.IsUnlocked("LORE_004"),
                "falha não desconta nem desbloqueia");
            Expect(!service.TryPurchase("LORE_001"), "coletável não pode ser comprado");
            Expect(!service.TryPurchase("LORE_020"), "secreto não pode ser comprado");

            Expect(service.TryUnlockFromGameplay("LORE_001"), "coleta desbloqueia LORE_001");
            Expect(!service.TryUnlockFromGameplay("LORE_001"), "coleta duplicada é impedida");
            Expect(!service.TryUnlockFromGameplay("LORE_002"), "API de coleta rejeita comprável");
            Expect(!service.TryUnlockSecret("LORE_020", "MISSÃO_INCORRETA"),
                "missão incorreta não libera secreto");
            Expect(!service.IsUnlocked("LORE_020"), "LORE_020 permanece bloqueado sem missão oficial");
            Expect(!service.IsUnlocked("LORE_024"), "LORE_024 permanece bloqueado");
            Expect(service.TryUnlockSecret("LORE_020", "SECRET_MISSION_LORE_020"),
                "API futura aceita somente missionId oficial em save isolado");

            wallet.Load();
            AuroraLoreService reloaded = new AuroraLoreService(catalog, wallet);
            Expect(reloaded.IsUnlocked("LORE_002"), "compra persiste após reload");
            Expect(reloaded.IsUnlocked("LORE_001"), "coleta persiste após reload");
            Expect(reloaded.IsUnlocked("LORE_020"), "API secreta persiste no save temporário");
        }
        finally
        {
            if (service != null) AuroraLoreService.ReleaseTestInstance(service);
            if (walletObject != null)
            {
                AuroraCoinWallet wallet = walletObject.GetComponent<AuroraCoinWallet>();
                AuroraCoinWallet.ReleaseTestInstance(wallet);
                UnityEngine.Object.DestroyImmediate(walletObject);
            }
            TryDeleteDirectory(directory);
        }
    }

    private static void TestCollectiblePrefabAndPermanence(AuroraLoreCatalog catalog)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Expect(prefab != null, "prefab oficial existe");
        AuroraDataFileCollectible prefabCollectible = prefab.GetComponent<AuroraDataFileCollectible>();
        Expect(prefabCollectible != null, "prefab possui AuroraDataFileCollectible");
        Expect(prefabCollectible.LoreId == "LORE_001", "prefab base usa exemplo LORE_001");
        Expect(prefabCollectible.CollectOncePerSave, "prefab coleta uma vez por save");
        Expect(prefabCollectible.LoreCatalog == catalog, "prefab referencia catálogo oficial");
        Expect(prefab.GetComponent<Collider>() != null && prefab.GetComponent<Collider>().isTrigger,
            "prefab possui trigger");
        Expect(prefab.GetComponent<Rigidbody>() != null && prefab.GetComponent<Rigidbody>().isKinematic,
            "prefab possui Rigidbody cinemático");
        Expect(prefab.transform.Find("VisualRoot") != null, "prefab possui visual tecnológico");
        Expect(prefab.GetComponentsInChildren<Renderer>(true).Length >= 8,
            "visual possui múltiplas peças e não é cubo branco bruto");

        string directory = Path.Combine(Path.GetTempPath(), "AuroraDataFileTests-" + Guid.NewGuid().ToString("N"));
        string savePath = Path.Combine(directory, AuroraProgressSaveService.DefaultFileName);
        Directory.CreateDirectory(directory);
        GameObject walletObject = null;
        GameObject first = null;
        GameObject second = null;
        AuroraLoreService service = null;
        try
        {
            walletObject = new GameObject("AuroraDataFileWallet_Test");
            AuroraCoinWallet wallet = walletObject.AddComponent<AuroraCoinWallet>();
            wallet.ConfigureForTests(new AuroraProgressSaveService(savePath));
            service = AuroraLoreService.Initialize(catalog, wallet);
            first = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Expect(first != null, "prefab instancia para teste");
            Expect(first.GetComponent<AuroraDataFileCollectible>().TryCollect(),
                "coletável oficial desbloqueia pelo serviço");
            Expect(!first.activeSelf, "coletável some após coleta");
            Expect(service.IsUnlocked("LORE_001"), "estado permanente foi salvo");

            second = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            second.GetComponent<AuroraDataFileCollectible>().RefreshAvailability();
            Expect(!second.activeSelf, "nova corrida não reapresenta DataFile já coletado");
        }
        finally
        {
            if (service != null) AuroraLoreService.ReleaseTestInstance(service);
            if (first != null) UnityEngine.Object.DestroyImmediate(first);
            if (second != null) UnityEngine.Object.DestroyImmediate(second);
            if (walletObject != null)
            {
                AuroraCoinWallet wallet = walletObject.GetComponent<AuroraCoinWallet>();
                AuroraCoinWallet.ReleaseTestInstance(wallet);
                UnityEngine.Object.DestroyImmediate(walletObject);
            }
            TryDeleteDirectory(directory);
        }
    }

    private static void TestCanonicalMenuIntegration(AuroraLoreCatalog catalog)
    {
        Scene scene = SceneManager.GetActiveScene();
        Expect(scene.path == MainMenuScene, "teste executado na MainMenu oficial");
        GameObject panel = FindSceneObjects(scene, "LoreArchivePanel").SingleOrDefault();
        Expect(panel != null, "LoreArchivePanel existe uma única vez");
        AuroraLoreArchiveController controller = panel.GetComponent<AuroraLoreArchiveController>();
        Expect(controller != null, "painel possui controller oficial");
        SerializedObject serializedController = new SerializedObject(controller);
        Expect(serializedController.FindProperty("catalog").objectReferenceValue == catalog,
            "controller referencia catálogo oficial");

        Transform panelExtra = panel.transform.parent;
        AuroraMenuExtraController extra = panelExtra.GetComponent<AuroraMenuExtraController>();
        Expect(extra != null, "Panel_Extra possui controller");
        SerializedObject serializedExtra = new SerializedObject(extra);
        Expect(serializedExtra.FindProperty("lorePanel").objectReferenceValue == panel,
            "botão LORE aponta para LoreArchivePanel");
        Expect(serializedExtra.FindProperty("loreBackButton").objectReferenceValue != null,
            "Voltar do Lore está ligado ao Extra");

        Image overlay = panel.transform.Find("BackgroundOverlay").GetComponent<Image>();
        Expect(overlay.raycastTarget, "overlay bloqueia click-through");
        ScrollRect scroll = panel.transform.Find("LoreContentPanel/ScrollView").GetComponent<ScrollRect>();
        Expect(scroll != null && scroll.vertical && !scroll.horizontal, "ScrollRect vertical está configurado");
        Expect(scroll.viewport != null && scroll.content != null, "ScrollRect possui viewport e content");
        TMP_Text fullText = scroll.content.Find("FullLoreText").GetComponent<TMP_Text>();
        Expect(fullText != null && fullText.enableWordWrapping, "texto longo possui quebra de linha");
        Expect(panel.GetComponentsInChildren<AuroraLoreArchiveController>(true).Length == 1,
            "carrossel usa um único controller/card");
        Expect(panel.transform.Find("FileCarousel/FileCard") != null,
            "carrossel possui um único FileCard reutilizável");
        Expect(panel.transform.Find("Header/Button_Retornar_LoreArchive") != null,
            "botão Voltar existe");
        Expect(panel.transform.Find("FileCarousel/NavigationArea/PreviousFileButton") != null &&
               panel.transform.Find("FileCarousel/NavigationArea/NextFileButton") != null,
            "botões anterior/próximo existem");
        Expect(panel.transform.Find("ActionArea/PurchaseButton") != null,
            "ação de compra existe");

        RectTransform root = panel.transform as RectTransform;
        Rect left = GetLocalRect(panel.transform.Find("FileCarousel") as RectTransform, root);
        Rect right = GetLocalRect(panel.transform.Find("LoreContentPanel") as RectTransform, root);
        Rect action = GetLocalRect(panel.transform.Find("ActionArea") as RectTransform, root);
        Expect(left.xMax + 30f <= right.xMin, "carrossel e conteúdo não se sobrepõem");
        Expect(right.yMin >= action.yMax + 20f, "conteúdo e ActionArea não se sobrepõem");
        ValidateProjectedResolution(root, left, right, action, 1920, 1080);
        ValidateProjectedResolution(root, left, right, action, 1280, 720);

        EventSystem eventSystem = FindSceneComponents<EventSystem>(scene).SingleOrDefault();
        Expect(eventSystem != null, "EventSystem oficial foi preservado sem duplicação");
        CanvasScaler scaler = FindSceneComponents<CanvasScaler>(scene).FirstOrDefault();
        Expect(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
            "Canvas usa Scale With Screen Size");
        Expect(scaler.referenceResolution == new Vector2(1920f, 1080f),
            "referência responsiva é 1920x1080");
        bool gameplayEnabled = EditorBuildSettings.scenes.Any(buildScene =>
            buildScene.enabled && Path.GetFileNameWithoutExtension(buildScene.path) == "Beta03_Principal");
        Expect(gameplayEnabled, "Beta03_Principal continua habilitada para JOGAR");
    }

    private static void ValidateProjectedResolution(
        RectTransform root, Rect left, Rect right, Rect action, int width, int height)
    {
        float scale = Mathf.Min(width / 1920f, height / 1080f);
        Expect(left.width * scale >= 340f, "carrossel legível em " + width + "x" + height);
        Expect(right.width * scale >= 800f, "painel de texto legível em " + width + "x" + height);
        Expect(action.height * scale >= 90f, "área de ação legível em " + width + "x" + height);
        Expect(root.anchorMin == Vector2.zero && root.anchorMax == Vector2.one,
            "painel full-screen preservado em " + width + "x" + height);
    }

    private static bool ContainsMojibake(string text)
    {
        string[] markers = { "Ã§", "Ã£", "Ã©", "Ã¡", "Ã³", "Ãº", "Ãª", "Â", "�" };
        return markers.Any(text.Contains);
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
            Debug.LogWarning("[AuroraLoreTests] Não foi possível limpar temporários: " + exception.Message);
        }
    }
}
#endif
