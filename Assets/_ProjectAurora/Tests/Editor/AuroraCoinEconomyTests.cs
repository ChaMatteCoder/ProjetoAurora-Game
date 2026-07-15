#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AuroraCoinEconomyTests
{
    private const string ProbeOriginalBalanceKey = "AuroraCoinProbeOriginalBalance";
    private static int assertions;

    [MenuItem("Tools/Projeto Aurora/Economy/Runtime Probe/Collect First Scene Coin")]
    public static void CollectFirstSceneCoin()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[AuroraCoinRuntimeProbe] Entre em Play Mode primeiro.");
            return;
        }

        AuroraCoinWallet wallet = FindOne<AuroraCoinWallet>();
        PlayerHealth player = FindOne<PlayerHealth>();
        AuroraCoinCollectible coin = UnityEngine.Object
            .FindObjectsByType<AuroraCoinCollectible>(FindObjectsInactive.Include)
            .Where(candidate => candidate.gameObject.activeInHierarchy && !candidate.IsCollected)
            .OrderBy(candidate => candidate.transform.position.z)
            .FirstOrDefault();
        if (wallet == null || player == null || coin == null)
        {
            Debug.LogError("[AuroraCoinRuntimeProbe] Wallet, player ou moeda indisponivel.");
            return;
        }

        SessionState.SetInt(ProbeOriginalBalanceKey, wallet.Balance);
        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        Vector3 target = coin.transform.position;
        target.y = 0.05f;
        player.transform.position = target;
        if (controller != null) controller.enabled = controllerWasEnabled;
        Physics.SyncTransforms();
        Debug.Log("[AuroraCoinRuntimeProbe] Player movido para " + coin.name +
                  "; balanceBefore=" + wallet.Balance + ". Aguardando trigger.");
    }

    [MenuItem("Tools/Projeto Aurora/Economy/Runtime Probe/Report State")]
    public static void ReportRuntimeState()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[AuroraCoinRuntimeProbe] Entre em Play Mode primeiro.");
            return;
        }

        AuroraCoinWallet[] wallets = UnityEngine.Object.FindObjectsByType<AuroraCoinWallet>(FindObjectsInactive.Include);
        AuroraCoinCollectible[] coins = UnityEngine.Object.FindObjectsByType<AuroraCoinCollectible>(FindObjectsInactive.Include);
        AuroraCoinHudController hud = FindOne<AuroraCoinHudController>();
        AuroraGameplayHUDController gameplayHud = hud == null
            ? null
            : hud.GetComponentInParent<AuroraGameplayHUDController>();
        CanvasGroup coinGroup = hud == null ? null : hud.GetComponent<CanvasGroup>();
        int activeCoins = coins.Count(coin => coin.gameObject.activeInHierarchy);
        int collectedCoins = coins.Count(coin => coin.IsCollected);
        int balance = wallets.Length == 1 ? wallets[0].Balance : -1;
        string hudValue = hud == null ? "<missing>" : hud.DisplayedBalance;
        Debug.Log("[AuroraCoinRuntimeProbe] walletCount=" + wallets.Length +
                  ", balance=" + balance + ", hud=" + hudValue +
                  ", totalCoins=" + coins.Length + ", activeCoins=" + activeCoins +
                  ", collectedThisRun=" + collectedCoins +
                  ", hudState=" + (gameplayHud == null ? "<missing>" : gameplayHud.VisibilityState.ToString()) +
                  ", coinAlpha=" + (coinGroup == null ? "<missing>" : coinGroup.alpha.ToString("0.00")) + ".");
    }

    [MenuItem("Tools/Projeto Aurora/Economy/Runtime Probe/Set HUD Gameplay")]
    public static void SetHudGameplay() => SetHudState(GameplayHudVisibilityState.Gameplay);

    [MenuItem("Tools/Projeto Aurora/Economy/Runtime Probe/Set HUD Tutorial")]
    public static void SetHudTutorial() => SetHudState(GameplayHudVisibilityState.Tutorial);

    [MenuItem("Tools/Projeto Aurora/Economy/Runtime Probe/Set HUD Paused")]
    public static void SetHudPaused() => SetHudState(GameplayHudVisibilityState.Paused);

    [MenuItem("Tools/Projeto Aurora/Economy/Runtime Probe/Set HUD GameOver")]
    public static void SetHudGameOver() => SetHudState(GameplayHudVisibilityState.GameOver);

    [MenuItem("Tools/Projeto Aurora/Economy/Runtime Probe/Set HUD Final")]
    public static void SetHudFinal() => SetHudState(GameplayHudVisibilityState.Final);

    [MenuItem("Tools/Projeto Aurora/Economy/Runtime Probe/Restore Original Balance")]
    public static void RestoreProbeBalance()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[AuroraCoinRuntimeProbe] Entre em Play Mode primeiro.");
            return;
        }

        AuroraCoinWallet wallet = FindOne<AuroraCoinWallet>();
        if (wallet == null)
        {
            Debug.LogError("[AuroraCoinRuntimeProbe] Wallet indisponivel.");
            return;
        }

        int original = SessionState.GetInt(ProbeOriginalBalanceKey, wallet.Balance);
        wallet.SetBalanceForTests(original);
        Debug.Log("[AuroraCoinRuntimeProbe] Saldo original restaurado: " + original + ".");
    }

    [MenuItem("Tools/Projeto Aurora/Economy/Run AuroraCoin Economy Tests")]
    public static void RunAll()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "AuroraCoinTests-" + Guid.NewGuid().ToString("N"));
        string savePath = Path.Combine(tempDirectory, AuroraProgressSaveService.DefaultFileName);
        Directory.CreateDirectory(tempDirectory);

        GameObject walletObject = null;
        GameObject playerObject = null;
        GameObject coinObject = null;
        AuroraPurchasableItem skin = null;
        AuroraPurchasableItem dataFile = null;
        assertions = 0;

        try
        {
            walletObject = new GameObject("AuroraCoinWallet_Test");
            AuroraCoinWallet wallet = walletObject.AddComponent<AuroraCoinWallet>();
            var service = new AuroraProgressSaveService(savePath);
            wallet.ConfigureForTests(service);

            AuroraCoinWallet.ReleaseTestInstance(wallet);
            Expect(AuroraCoinWallet.Instance == wallet,
                "wallet carregada e recuperada apos perda da referencia estatica");

            Expect(wallet.Balance == 0, "novo save comeca em zero");
            Expect(!wallet.TryAddCoins(0), "adicao zero e rejeitada");
            Expect(!wallet.TryAddCoins(-1), "adicao negativa e rejeitada");
            Expect(wallet.TryAddCoins(1), "primeira moeda adicionada");
            Expect(wallet.Balance == 1, "uma moeda vale uma unidade");
            for (int i = 1; i < 10; i++) Expect(wallet.TryAddCoins(1), "adicao " + (i + 1));
            Expect(wallet.Balance == 10, "dez moedas resultam em saldo 10");

            AuroraProgressSaveData reloaded = new AuroraProgressSaveService(savePath).Load();
            Expect(reloaded.auroraCoins == 10, "saldo persiste ao recarregar o arquivo");

            wallet.SetBalanceForTests(998);
            Expect(wallet.TryAddCoins(1), "998 aceita uma moeda");
            Expect(!wallet.TryAddCoins(1), "999 recusa moeda excedente");
            Expect(wallet.Balance == AuroraCoinWallet.MaxBalance, "saldo limitado a 999");
            wallet.SetBalanceForTests(998);
            Expect(wallet.TryAddCoins(int.MaxValue), "adicao extrema e aceita ate o espaco restante");
            Expect(wallet.Balance == AuroraCoinWallet.MaxBalance, "adicao extrema nao causa overflow");
            Expect(!wallet.TrySpendCoins(-1), "custo negativo e rejeitado");
            Expect(!wallet.TrySpendCoins(1000), "gasto acima do saldo negado");
            Expect(wallet.Balance == 999, "gasto negado nao altera saldo");

            wallet.SetBalanceForTests(60);
            skin = CreateItem("Skin_Test_01", AuroraPurchaseCategory.Skin, 25);
            dataFile = CreateItem("DataFile_Test_01", AuroraPurchaseCategory.DataFile, 15);
            var purchases = new AuroraPurchaseService(wallet);
            Expect(purchases.CanPurchase(skin), "skin pode ser comprada com saldo suficiente");
            Expect(purchases.TryPurchase(skin), "compra de skin aprovada");
            Expect(wallet.Balance == 35, "compra subtrai custo exato");
            Expect(purchases.IsUnlocked(skin), "skin fica desbloqueada");
            Expect(!purchases.TryPurchase(skin), "skin nao pode ser comprada duas vezes");
            Expect(purchases.TryPurchase(dataFile), "DataFile de teste pode ser comprado");
            Expect(wallet.Balance == 20, "segunda compra subtrai custo exato");

            AuroraProgressSaveData purchaseReload = new AuroraProgressSaveService(savePath).Load();
            Expect(purchaseReload.auroraCoins == 20, "saldo de compra persiste");
            Expect(purchaseReload.unlockedSkins.Contains("Skin_Test_01"), "unlock de skin persiste");
            Expect(purchaseReload.unlockedDataFiles.Contains("DataFile_Test_01"), "unlock de DataFile persiste");

            wallet.SetBalanceForTests(0);
            playerObject = new GameObject("Player_Test");
            playerObject.AddComponent<PlayerHealth>();
            BoxCollider playerCollider = playerObject.AddComponent<BoxCollider>();
            coinObject = new GameObject("Coin_Test");
            coinObject.AddComponent<SphereCollider>().isTrigger = true;
            AuroraCoinCollectible collectible = coinObject.AddComponent<AuroraCoinCollectible>();
            Expect(collectible.TryCollect(playerCollider), "primeiro trigger coleta a instancia");
            Expect(!collectible.TryCollect(playerCollider), "mesma instancia nao recompensa duas vezes");
            Expect(wallet.Balance == 1, "instancia duplicada adicionou somente uma unidade");
            UnityEngine.Object.DestroyImmediate(coinObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
            coinObject = null;
            playerObject = null;

            TestCorruptedSaveFallback(tempDirectory);

            service.Load();
            service.Data.auroraCoins = 77;
            if (!service.Data.unlockedSkins.Contains("Skin_Test_01")) service.Data.unlockedSkins.Add("Skin_Test_01");
            if (!service.Data.unlockedDataFiles.Contains("DataFile_Test_01")) service.Data.unlockedDataFiles.Add("DataFile_Test_01");
            service.Save(service.Data);
            Expect(service.ResetTestEconomyData(), "reset de teste salva com sucesso");
            AuroraProgressSaveData reset = new AuroraProgressSaveService(savePath).Load();
            Expect(reset.auroraCoins == 0, "reset zera AuroraCoins");
            Expect(!reset.unlockedSkins.Contains("Skin_Test_01"), "reset remove skin de teste");
            Expect(!reset.unlockedDataFiles.Contains("DataFile_Test_01"), "reset remove DataFile de teste");

            TestCanonicalSceneIntegration();

            Debug.Log("[AuroraCoinTests] PASS: " + assertions + " assertions.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[AuroraCoinTests] FAIL apos " + assertions + " assertions. " + exception);
            throw;
        }
        finally
        {
            AuroraCoinWallet wallet = walletObject == null ? null : walletObject.GetComponent<AuroraCoinWallet>();
            AuroraCoinWallet.ReleaseTestInstance(wallet);
            if (coinObject != null) UnityEngine.Object.DestroyImmediate(coinObject);
            if (playerObject != null) UnityEngine.Object.DestroyImmediate(playerObject);
            if (walletObject != null) UnityEngine.Object.DestroyImmediate(walletObject);
            if (skin != null) UnityEngine.Object.DestroyImmediate(skin);
            if (dataFile != null) UnityEngine.Object.DestroyImmediate(dataFile);
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static void TestCorruptedSaveFallback(string parentDirectory)
    {
        string path = Path.Combine(parentDirectory, "corruption-test.json");
        var service = new AuroraProgressSaveService(path);
        AuroraProgressSaveData data = service.Load();
        data.auroraCoins = 7;
        Expect(service.Save(data), "save base para fallback");
        data.auroraCoins = 8;
        Expect(service.Save(data), "segundo save gera backup");
        File.WriteAllText(path, "{ invalid json");

        var recoveredService = new AuroraProgressSaveService(path);
        AuroraProgressSaveData recovered = recoveredService.Load();
        Expect(recoveredService.LastLoadRecoveredFromBackup, "save corrompido usa backup");
        Expect(recovered.auroraCoins == 7, "backup preserva ultima versao anterior");
        Expect(!string.IsNullOrEmpty(recoveredService.CorruptedBackupPath), "arquivo corrompido e preservado");
        Expect(File.Exists(recoveredService.CorruptedBackupPath), "backup do corrompido existe");
    }

    private static AuroraPurchasableItem CreateItem(string id, AuroraPurchaseCategory category, int price)
    {
        AuroraPurchasableItem item = ScriptableObject.CreateInstance<AuroraPurchasableItem>();
        item.ConfigureForEditor(id, id + " [TESTE]", category, price, false, "CONTEUDO DE TESTE");
        return item;
    }

    private static void TestCanonicalSceneIntegration()
    {
        AuroraCoinPlacementTools.ValidationResult placement = AuroraCoinPlacementTools.ValidatePlacement(false);
        Expect(placement.CoinCount >= 30, "cena canonica preserva ao menos a rodada inicial de moedas");
        Expect(placement.ErrorCount == 0, "posicionamento nao possui erros");
        Debug.Log("[AuroraCoinTests] Placement atual: " + placement.CoinCount +
                  " moedas, " + placement.WarningCount + " avisos de level design.");

        AuroraCoinHudController coinHud = FindOne<AuroraCoinHudController>();
        Expect(coinHud != null, "HUD de AuroraCoins existe");
        if (coinHud == null)
        {
            return;
        }

        RectTransform coinRect = coinHud.transform as RectTransform;
        AuroraGameplayHUDController gameplayHud = coinHud.GetComponentInParent<AuroraGameplayHUDController>();
        CanvasScaler scaler = coinHud.GetComponentInParent<CanvasScaler>();
        Expect(coinRect != null && gameplayHud != null, "HUD de moedas pertence ao controller existente");
        Expect(scaler != null && scaler.referenceResolution == new Vector2(1920f, 1080f),
            "Canvas usa referencia 1920x1080");
        Expect(coinRect.anchorMin == Vector2.one && coinRect.anchorMax == Vector2.one,
            "card usa ancora superior direita responsiva");
        Expect(-coinRect.anchoredPosition.x >= 24f, "card possui margem direita");

        RectTransform distanceRect = FindDirectChildRect(
            gameplayHud == null ? null : gameplayHud.transform,
            gameplayHud == null || gameplayHud.distanceValueText == null
                ? null
                : gameplayHud.distanceValueText.transform);
        Expect(distanceRect != null, "bloco de distancia foi localizado por referencia");
        if (distanceRect != null)
        {
            float distanceBottom = distanceRect.anchoredPosition.y - distanceRect.pivot.y * distanceRect.sizeDelta.y;
            float coinTop = coinRect.anchoredPosition.y + (1f - coinRect.pivot.y) * coinRect.sizeDelta.y;
            Expect(distanceBottom - coinTop >= 12f, "card nao sobrepoe o bloco de distancia");
        }

        TMP_Text[] labels = coinHud.GetComponentsInChildren<TMP_Text>(true);
        Expect(labels.Length >= 4, "icone, saldo, rotulo e status estao presentes");
        for (int i = 0; i < labels.Length; i++)
        {
            Expect(IsInside(labels[i].rectTransform, coinRect), labels[i].name + " respeita os bounds do card");
        }
    }

    private static RectTransform FindDirectChildRect(Transform root, Transform descendant)
    {
        if (root == null || descendant == null)
        {
            return null;
        }

        Transform current = descendant;
        while (current.parent != null && current.parent != root)
        {
            current = current.parent;
        }

        return current.parent == root ? current as RectTransform : null;
    }

    private static bool IsInside(RectTransform child, RectTransform parent)
    {
        var corners = new Vector3[4];
        child.GetWorldCorners(corners);
        Rect bounds = parent.rect;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = parent.InverseTransformPoint(corners[i]);
            if (local.x < bounds.xMin - 0.1f || local.x > bounds.xMax + 0.1f ||
                local.y < bounds.yMin - 0.1f || local.y > bounds.yMax + 0.1f)
            {
                return false;
            }
        }

        return true;
    }

    private static void SetHudState(GameplayHudVisibilityState state)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[AuroraCoinRuntimeProbe] Entre em Play Mode primeiro.");
            return;
        }

        AuroraGameplayHUDController hud = FindOne<AuroraGameplayHUDController>();
        if (hud == null)
        {
            Debug.LogError("[AuroraCoinRuntimeProbe] AuroraGameplayHUDController indisponivel.");
            return;
        }

        hud.SetHudVisibilityState(state);
        Debug.Log("[AuroraCoinRuntimeProbe] HUD alterada para " + state + ".");
    }

    private static T FindOne<T>() where T : UnityEngine.Object
    {
        T[] values = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        return values.Length == 0 ? null : values[0];
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
            Debug.LogWarning("[AuroraCoinTests] Nao foi possivel limpar temporarios: " + exception.Message);
        }
    }
}
#endif
