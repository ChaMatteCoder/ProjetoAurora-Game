using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AuroraPlayModeStartup
{
    private const string MainMenuScenePath =
        "Assets/_ProjectAurora/Scenes/MainMenu.unity";
    private const string GameplayScenePath =
        "Assets/_ProjectAurora/Scenes/FASE 01 - Laboratório Limpo A/" +
        "Fase01_SetorA_LaboratorioLimpo.unity";

    static AuroraPlayModeStartup()
    {
        EditorApplication.delayCall += EnsureMainMenuStartScene;
    }

    [MenuItem("Tools/Projeto Aurora/Definir Menu como Cena Inicial")]
    public static void EnsureMainMenuStartScene()
    {
        SceneAsset menuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (menuScene == null)
        {
            Debug.LogError("PROJETO:AURORA - Cena do menu principal nao encontrada.");
            return;
        }

        if (EditorSceneManager.playModeStartScene != menuScene)
        {
            EditorSceneManager.playModeStartScene = menuScene;
            Debug.Log("PROJETO:AURORA - Menu principal definido como inicio do Play Mode.");
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true)
        };
    }
}
