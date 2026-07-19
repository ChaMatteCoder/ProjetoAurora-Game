using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProjectAurora.EditorTools.Build
{
    /// Build de release "PROJETO:AURORA — Demo Alpha v0.1" para Windows e Linux (x64).
    /// Variante HEADLESS do AuroraBuildDemoAlphaV01: NAO abre dialogos nem RevealInFinder,
    /// para poder rodar via automacao (MCP/batch) sem travar a thread principal num modal.
    ///
    /// Progresso zerado por design: a build nunca embute save. O progresso do jogador vive
    /// em Application.persistentDataPath (fora da pasta Builds), entao cada maquina comeca
    /// "jogo novo" — o default de AuroraProgressSaveData e 0 moedas / 0 DataFiles.
    ///
    /// Menu: Tools/Projeto Aurora/Build/Release ...
    public static class AuroraBuildRelease
    {
        private const string OutputRoot = "Builds";
        private const string WindowsFolder = "ProjetoAurora_DemoAlpha_v0.1_Windows";
        private const string LinuxFolder = "ProjetoAurora_DemoAlpha_v0.1_Linux";
        private const string WindowsExe = "ProjetoAurora_DemoAlpha_v0.1.exe";
        private const string LinuxBinary = "ProjetoAurora_DemoAlpha_v0.1.x86_64";

        private static readonly string[] Scenes =
        {
            "Assets/_ProjectAurora/Scenes/MainMenu.unity",
            "Assets/_ProjectAurora/Scenes/Beta03_Principal.unity"
        };

        [MenuItem("Tools/Projeto Aurora/Build/Release - Windows x64", priority = 20)]
        public static bool BuildWindows()
        {
            return BuildFor(BuildTarget.StandaloneWindows64, WindowsFolder, WindowsExe);
        }

        [MenuItem("Tools/Projeto Aurora/Build/Release - Linux x64", priority = 21)]
        public static bool BuildLinux()
        {
            return BuildFor(BuildTarget.StandaloneLinux64, LinuxFolder, LinuxBinary);
        }

        [MenuItem("Tools/Projeto Aurora/Build/Release - Windows + Linux", priority = 22)]
        public static void BuildAll()
        {
            bool win = BuildWindows();
            bool lin = BuildLinux();
            Debug.Log("[AuroraRelease] === BuildAll concluido — Windows=" +
                      (win ? "OK" : "FALHA") + " Linux=" + (lin ? "OK" : "FALHA") + " ===");
        }

        private static bool BuildFor(BuildTarget target, string folder, string exeName)
        {
            Debug.Log("[AuroraRelease] === Iniciando build " + target + " ===");

            foreach (string scene in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scene) == null)
                {
                    Debug.LogError("[AuroraRelease] FALHA: cena ausente -> " + scene);
                    return false;
                }
            }

            var buildScenes = new EditorBuildSettingsScene[Scenes.Length];
            for (int i = 0; i < Scenes.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(Scenes[i], true);
            }
            EditorBuildSettings.scenes = buildScenes;

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outFolder = Path.Combine(projectRoot, OutputRoot, folder);
            Directory.CreateDirectory(outFolder);
            string exePath = Path.Combine(outFolder, exeName);
            Debug.Log("[AuroraRelease] Saida: " + exePath);

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = exePath,
                target = target,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;

            if (ok)
            {
                Debug.Log("[AuroraRelease] SUCESSO " + target + " — " +
                          summary.totalSize / (1024 * 1024) + " MB em " +
                          summary.totalTime.TotalSeconds.ToString("0") + "s");
            }
            else
            {
                Debug.LogError("[AuroraRelease] FALHOU " + target + " — result=" +
                               summary.result + " errors=" + summary.totalErrors);
            }

            WriteResultLog(outFolder, target, ok, summary);
            return ok;
        }

        private static void WriteResultLog(string outFolder, BuildTarget target, bool ok, BuildSummary summary)
        {
            try
            {
                string log = Path.Combine(outFolder, "build_result.txt");
                File.WriteAllText(log,
                    "PROJETO:AURORA — Demo Alpha v0.1 (" + target + ")\n" +
                    "Resultado: " + (ok ? "SUCESSO" : "FALHA (" + summary.result + ")") + "\n" +
                    "Concluido: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
                    "Plataforma: " + summary.platform + "\n" +
                    "Tamanho: " + (summary.totalSize / (1024 * 1024)) + " MB\n" +
                    "Duracao: " + summary.totalTime.TotalSeconds.ToString("0") + " s\n" +
                    "Warnings: " + summary.totalWarnings + " | Errors: " + summary.totalErrors + "\n" +
                    "Saida: " + summary.outputPath + "\n" +
                    "Progresso: jogo novo (save vive em persistentDataPath, nao embutido)\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AuroraRelease] Nao foi possivel escrever build_result.txt: " + e.Message);
            }
        }
    }
}
