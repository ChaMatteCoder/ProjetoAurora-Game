using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProjectAurora.EditorTools.Build
{
    /// Gera a build de apresentação "PROJETO:AURORA — Demo Alpha v0.1" de forma repetível.
    /// Menu: Tools/Projeto Aurora/Build/Demo Alpha v0.1 - Windows.
    /// Seguro por design: só configura Build Settings e chama BuildPipeline; não altera
    /// gameplay, menu, backend de scripting nem apaga cenas.
    public static class AuroraBuildDemoAlphaV01
    {
        private const string ProductName = "PROJETO:AURORA — Demo Alpha v0.1";
        private const string ExeName = "ProjetoAurora_DemoAlpha_v0.1.exe";
        private const string OutputDir = @"Builds\ProjetoAurora_DemoAlpha_v0.1_Windows";

        private static readonly string[] Scenes =
        {
            "Assets/_ProjectAurora/Scenes/MainMenu.unity",
            "Assets/_ProjectAurora/Scenes/Beta03_Principal.unity"
        };

        [MenuItem("Tools/Projeto Aurora/Build/Demo Alpha v0.1 - Windows", priority = 1)]
        public static void BuildWindows()
        {
            Debug.Log("[AuroraBuild] === Iniciando " + ProductName + " (Windows x64) ===");

            // 1) valida cenas
            foreach (string scene in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scene) == null)
                {
                    Debug.LogError("[AuroraBuild] FALHA: cena ausente -> " + scene);
                    EditorUtility.DisplayDialog("Aurora Build", "Cena ausente:\n" + scene, "OK");
                    return;
                }
            }

            // 2) garante Build Settings com exatamente estas cenas (MainMenu=0, Beta03=1)
            var buildScenes = new EditorBuildSettingsScene[Scenes.Length];
            for (int i = 0; i < Scenes.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(Scenes[i], true);
            }
            EditorBuildSettings.scenes = buildScenes;

            // 3) build de apresentação (não development)
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;

            // 4) caminho de saída (raiz do projeto)
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outFolder = Path.Combine(projectRoot, OutputDir);
            Directory.CreateDirectory(outFolder);
            string exePath = Path.Combine(outFolder, ExeName);
            Debug.Log("[AuroraBuild] Saída: " + exePath);

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            // 5) executa e reporta
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[AuroraBuild] SUCESSO — " + summary.totalSize / (1024 * 1024) + " MB em " +
                          summary.totalTime.TotalSeconds.ToString("0") + "s");
                Debug.Log("[AuroraBuild] Executável: " + exePath);
                WriteResultLog(outFolder, true, summary);
                EditorUtility.RevealInFinder(exePath);
                EditorUtility.DisplayDialog("Aurora Build",
                    "Build gerada com sucesso!\n\n" + exePath, "OK");
            }
            else
            {
                Debug.LogError("[AuroraBuild] FALHOU — result=" + summary.result +
                               " errors=" + summary.totalErrors);
                WriteResultLog(outFolder, false, summary);
                EditorUtility.DisplayDialog("Aurora Build",
                    "Build FALHOU: " + summary.result + "\nVeja o Console.", "OK");
            }
        }

        private static void WriteResultLog(string outFolder, bool ok, BuildSummary summary)
        {
            try
            {
                string log = Path.Combine(outFolder, "build_result.txt");
                File.WriteAllText(log,
                    ProductName + "\n" +
                    "Resultado: " + (ok ? "SUCESSO" : "FALHA (" + summary.result + ")") + "\n" +
                    "Plataforma: " + summary.platform + "\n" +
                    "Tamanho: " + (summary.totalSize / (1024 * 1024)) + " MB\n" +
                    "Duração: " + summary.totalTime.TotalSeconds.ToString("0") + " s\n" +
                    "Warnings: " + summary.totalWarnings + " | Errors: " + summary.totalErrors + "\n" +
                    "Saída: " + summary.outputPath + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AuroraBuild] Não foi possível escrever build_result.txt: " + e.Message);
            }
        }
    }
}
