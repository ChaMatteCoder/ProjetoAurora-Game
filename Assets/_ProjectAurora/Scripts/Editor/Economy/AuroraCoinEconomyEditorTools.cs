#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AuroraCoinEconomyEditorTools
{
    private const string TestFolder = "Assets/_ProjectAurora/Economy/TestContent";
    private const string SkinPath = TestFolder + "/Skin_Test_01_TEST_ONLY.asset";
    private const string DataFilePath = TestFolder + "/DataFile_Test_01_TEST_ONLY.asset";
    private const string CatalogPath = TestFolder + "/AuroraUnlockCatalog_TEST_ONLY.asset";

    [MenuItem("Tools/Projeto Aurora/Economy/Create Or Update Test Catalog")]
    public static void CreateOrUpdateTestCatalog()
    {
        EnsureFolder("Assets/_ProjectAurora/Economy");
        EnsureFolder(TestFolder);

        AuroraPurchasableItem skin = LoadOrCreateItem(SkinPath);
        skin.ConfigureForEditor(
            "Skin_Test_01",
            "Skin Test 01 [TESTE]",
            AuroraPurchaseCategory.Skin,
            25,
            false,
            "CONTEUDO DE TESTE. Nao exibir como item final.");
        EditorUtility.SetDirty(skin);

        AuroraPurchasableItem dataFile = LoadOrCreateItem(DataFilePath);
        dataFile.ConfigureForEditor(
            "DataFile_Test_01",
            "DataFile Test 01 [TESTE]",
            AuroraPurchaseCategory.DataFile,
            15,
            false,
            "CONTEUDO DE TESTE. Validacao da fundacao de lore compravel.");
        EditorUtility.SetDirty(dataFile);

        AuroraUnlockCatalog catalog = AssetDatabase.LoadAssetAtPath<AuroraUnlockCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AuroraUnlockCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.ConfigureForEditor(new List<AuroraPurchasableItem> { skin, dataFile });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("[AuroraEconomy] Catalogo de teste criado/atualizado em " + CatalogPath + ".");
    }

    [MenuItem("Tools/Projeto Aurora/Economy/Reset AuroraCoin Save")]
    public static void ResetAuroraCoinSave()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Reset AuroraCoin Save",
            "Resetar o saldo e apenas os unlocks Skin_Test_01/DataFile_Test_01? Configuracoes e outros saves nao serao alterados.",
            "Resetar",
            "Cancelar");
        if (!confirmed)
        {
            return;
        }

        var service = new AuroraProgressSaveService();
        bool saved = service.ResetTestEconomyData();
        if (Application.isPlaying && AuroraCoinWallet.Instance != null)
        {
            AuroraCoinWallet.Instance.Load();
        }

        if (saved) Debug.Log("[AuroraEconomy] Saldo e unlocks de teste resetados.");
        else Debug.LogError("[AuroraEconomy] Falha ao resetar o save de AuroraCoins.");
    }

    private static AuroraPurchasableItem LoadOrCreateItem(string path)
    {
        AuroraPurchasableItem item = AssetDatabase.LoadAssetAtPath<AuroraPurchasableItem>(path);
        if (item != null) return item;
        item = ScriptableObject.CreateInstance<AuroraPurchasableItem>();
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        string name = path.Substring(separator + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
