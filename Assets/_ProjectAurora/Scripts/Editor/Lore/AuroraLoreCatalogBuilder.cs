#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ProjectAurora.Lore;
using UnityEditor;
using UnityEngine;

namespace ProjectAurora.Editor.Lore
{
    public static class AuroraLoreCatalogBuilder
    {
        public const string TextFolder = "Assets/_ProjectAurora/Data/Lore/Text";
        public const string DataFolder = "Assets/_ProjectAurora/Data/Lore";
        public const string DefinitionsFolder = DataFolder + "/Definitions";
        public const string CatalogPath = DataFolder + "/AuroraLoreCatalog.asset";

        private static readonly HashSet<int> DefaultIds = new HashSet<int> { 8, 9 };
        private static readonly HashSet<int> CollectibleIds = new HashSet<int>
        {
            1, 3, 5, 6, 11, 13, 14, 17, 18, 19, 22, 23
        };
        private static readonly HashSet<int> PurchasableIds = new HashSet<int>
        {
            2, 4, 7, 10, 12, 15, 16, 21
        };
        private static readonly HashSet<int> SecretIds = new HashSet<int> { 20, 24 };
        private static readonly Dictionary<int, int> Prices = new Dictionary<int, int>
        {
            { 2, 10 }, { 4, 10 }, { 7, 15 }, { 10, 15 },
            { 12, 15 }, { 15, 20 }, { 16, 20 }, { 21, 20 }
        };
        private static readonly string[] MojibakeMarkers =
        {
            "Ã§", "Ã£", "Ã©", "Ã¡", "Ã³", "Ãº", "Ãª", "Â", "�"
        };

        [MenuItem("Tools/Projeto Aurora/Lore/Rebuild Lore Catalog")]
        public static void RebuildLoreCatalogFromMenu()
        {
            AuroraLoreCatalog catalog = RebuildLoreCatalog();
            if (catalog != null)
            {
                Selection.activeObject = catalog;
            }
        }

        public static AuroraLoreCatalog RebuildLoreCatalog()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(TextFolder);
            EnsureFolder(DefinitionsFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var definitions = new List<AuroraLoreDefinition>();
            for (int number = 1; number <= AuroraLoreCatalog.OfficialLoreCount; number++)
            {
                string id = ToId(number);
                string textPath = TextFolder + "/" + id + ".txt";
                if (!File.Exists(Path.GetFullPath(textPath)))
                {
                    Debug.LogError("[AuroraLoreCatalog] Arquivo ausente: " + textPath);
                    continue;
                }

                AssetDatabase.ImportAsset(textPath, ImportAssetOptions.ForceSynchronousImport);
                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textPath);
                if (textAsset == null)
                {
                    Debug.LogError("[AuroraLoreCatalog] Falha ao importar TextAsset: " + textPath);
                    continue;
                }

                string definitionPath = DefinitionsFolder + "/" + id + ".asset";
                AuroraLoreDefinition definition =
                    AssetDatabase.LoadAssetAtPath<AuroraLoreDefinition>(definitionPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<AuroraLoreDefinition>();
                    definition.name = id;
                    AssetDatabase.CreateAsset(definition, definitionPath);
                }

                AuroraLoreUnlockType unlockType = GetUnlockType(number);
                bool isSecret = unlockType == AuroraLoreUnlockType.SecretMission;
                definition.ConfigureForEditor(
                    id,
                    ExtractTitle(textAsset.text, number),
                    GetCategoryName(unlockType),
                    isSecret ? "Registro classificado. Conteúdo indisponível até autorização de missão."
                             : ExtractShortDescription(textAsset.text),
                    textAsset,
                    unlockType,
                    Prices.TryGetValue(number, out int price) ? price : 0,
                    unlockType == AuroraLoreUnlockType.Default,
                    isSecret,
                    definition.Icon,
                    number,
                    id + ".txt",
                    definition.RelatedSector,
                    definition.RelatedCharacter,
                    isSecret ? "SECRET_MISSION_" + id : string.Empty,
                    unlockType == AuroraLoreUnlockType.GameplayCollectible
                        ? "DATAFILE_" + id
                        : string.Empty);
                EditorUtility.SetDirty(definition);
                definitions.Add(definition);
            }

            definitions.Sort((left, right) => left.DisplayOrder.CompareTo(right.DisplayOrder));
            AuroraLoreCatalog catalog = AssetDatabase.LoadAssetAtPath<AuroraLoreCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AuroraLoreCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.ConfigureForEditor(definitions);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            List<string> issues = CollectValidationIssues(catalog, true);
            LogValidation(issues);
            Debug.Log("[AuroraLoreCatalog] Rebuild concluído: arquivos=" + definitions.Count +
                      ", default=2, coletáveis=12, compráveis=8, secretos=2, issues=" + issues.Count + ".");
            return catalog;
        }

        [MenuItem("Tools/Projeto Aurora/Lore/Validate Lore Files")]
        public static void ValidateLoreFiles()
        {
            AuroraLoreCatalog catalog = AssetDatabase.LoadAssetAtPath<AuroraLoreCatalog>(CatalogPath);
            List<string> issues = CollectValidationIssues(catalog, true);
            LogValidation(issues);
            if (issues.Count == 0)
            {
                Debug.Log("[AuroraLoreValidation] PASS: 24 arquivos UTF-8 e catálogo oficial válidos.");
            }
        }

        [MenuItem("Tools/Projeto Aurora/Lore/Reset Lore Unlocks")]
        public static void ResetLoreUnlocks()
        {
            if (!EditorUtility.DisplayDialog(
                    "Resetar Lore",
                    "Remover desbloqueios comprados/coletados e manter LORE_008 e LORE_009? O saldo de AuroraCoins e as configurações serão preservados.",
                    "Resetar Lore",
                    "Cancelar"))
            {
                return;
            }

            var saveService = new AuroraProgressSaveService();
            AuroraProgressSaveData data = saveService.Load();
            data.unlockedDataFiles.RemoveAll(id =>
                id != null && id.StartsWith("LORE_", StringComparison.Ordinal));
            data.unlockedDataFiles.Add("LORE_008");
            data.unlockedDataFiles.Add("LORE_009");
            bool saved = saveService.Save(data);
            if (saved && Application.isPlaying && AuroraCoinWallet.Instance != null)
            {
                AuroraCoinWallet.Instance.Load();
            }

            Debug.Log(saved
                ? "[AuroraLore] Desbloqueios resetados; defaults preservados e AuroraCoins mantidas."
                : "[AuroraLore] Falha ao resetar desbloqueios.");
        }

        public static List<string> CollectValidationIssues(
            AuroraLoreCatalog catalog,
            bool includeSaveValidation)
        {
            var issues = catalog == null
                ? new List<string> { "AuroraLoreCatalog.asset ausente." }
                : catalog.CollectValidationIssues();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(Path.GetFullPath(TextFolder)))
            {
                issues.Add("Pasta de textos ausente: " + TextFolder + ".");
                return issues;
            }

            string[] files = Directory.GetFiles(Path.GetFullPath(TextFolder), "LORE_*.txt", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                if (!names.Add(name)) issues.Add("Nome de arquivo duplicado: " + name + ".");
            }

            for (int number = 1; number <= AuroraLoreCatalog.OfficialLoreCount; number++)
            {
                string id = ToId(number);
                string path = Path.GetFullPath(TextFolder + "/" + id + ".txt");
                if (!File.Exists(path))
                {
                    issues.Add("Arquivo ausente: " + id + ".txt.");
                    continue;
                }

                string decoded;
                try
                {
                    decoded = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
                }
                catch (DecoderFallbackException)
                {
                    issues.Add(id + " não está em UTF-8 válido.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(decoded)) issues.Add(id + " está vazio.");
                if (MojibakeMarkers.Any(decoded.Contains)) issues.Add(id + " contém possível mojibake.");

                AuroraLoreDefinition definition = catalog == null ? null : catalog.GetById(id);
                if (definition == null) continue;
                AuroraLoreUnlockType expectedType = GetUnlockType(number);
                if (definition.UnlockType != expectedType)
                    issues.Add(id + " possui categoria incorreta.");
                int expectedPrice = Prices.TryGetValue(number, out int price) ? price : 0;
                if (definition.AuroraCoinPrice != expectedPrice)
                    issues.Add(id + " possui preço incorreto; esperado=" + expectedPrice + ".");
            }

            if (files.Length != AuroraLoreCatalog.OfficialLoreCount)
            {
                issues.Add("A pasta oficial deve conter exatamente 24 arquivos; atual=" + files.Length + ".");
            }

            if (includeSaveValidation)
            {
                AuroraProgressSaveData save = new AuroraProgressSaveService().Load();
                var valid = new HashSet<string>(Enumerable.Range(1, AuroraLoreCatalog.OfficialLoreCount)
                    .Select(ToId), StringComparer.Ordinal);
                foreach (string savedId in save.unlockedDataFiles)
                {
                    if (savedId != null && savedId.StartsWith("LORE_", StringComparison.Ordinal) &&
                        !valid.Contains(savedId))
                    {
                        issues.Add("ID de Lore inválido no save: " + savedId + ".");
                    }
                }
            }

            return issues.Distinct(StringComparer.Ordinal).ToList();
        }

        private static void LogValidation(IReadOnlyList<string> issues)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                Debug.LogWarning("[AuroraLoreValidation] " + issues[i]);
            }
        }

        private static string ExtractTitle(string source, int number)
        {
            string first = source.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n')
                .Select(line => line.Trim().TrimStart('\uFEFF'))
                .FirstOrDefault(line => line.Length > 0);
            if (string.IsNullOrWhiteSpace(first))
                return "ARQUIVO LORE " + number.ToString("000");

            string candidate = first.TrimStart('#').Trim();
            if (candidate.StartsWith("TÍTULO:", StringComparison.OrdinalIgnoreCase))
                candidate = candidate.Substring("TÍTULO:".Length).Trim();
            if (candidate.StartsWith("[") && candidate.EndsWith("]"))
                candidate = candidate.Substring(1, candidate.Length - 2).Trim();
            candidate = Regex.Replace(candidate, @"^LORE_\d{3}\s*[—–:\-]\s*", string.Empty);
            return candidate.Length == 0 ? "ARQUIVO LORE " + number.ToString("000") : candidate;
        }

        private static string ExtractShortDescription(string source)
        {
            string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int summary = Array.FindIndex(lines, line =>
                line.Trim().Equals("## Resumo do arquivo", StringComparison.OrdinalIgnoreCase));
            int start = summary >= 0 ? summary + 1 : 1;
            string paragraph = lines.Skip(start)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.Length > 0 && !line.StartsWith("#") && line != "---");
            if (string.IsNullOrWhiteSpace(paragraph))
                return "Registro do banco de dados do Projeto Aurora.";

            paragraph = Regex.Replace(paragraph.Replace("**", string.Empty), @"\s+", " ").Trim();
            const int maxLength = 110;
            if (paragraph.Length <= maxLength) return paragraph;
            int boundary = paragraph.LastIndexOf(' ', maxLength);
            if (boundary < 80) boundary = maxLength;
            return paragraph.Substring(0, boundary).TrimEnd(' ', '.', ',', ';', ':') + "...";
        }

        private static AuroraLoreUnlockType GetUnlockType(int number)
        {
            if (DefaultIds.Contains(number)) return AuroraLoreUnlockType.Default;
            if (CollectibleIds.Contains(number)) return AuroraLoreUnlockType.GameplayCollectible;
            if (PurchasableIds.Contains(number)) return AuroraLoreUnlockType.AuroraCoinPurchase;
            if (SecretIds.Contains(number)) return AuroraLoreUnlockType.SecretMission;
            throw new InvalidOperationException("LORE sem categoria oficial: " + number + ".");
        }

        private static string GetCategoryName(AuroraLoreUnlockType type)
        {
            switch (type)
            {
                case AuroraLoreUnlockType.Default: return "ARQUIVO DISPONÍVEL";
                case AuroraLoreUnlockType.GameplayCollectible: return "DATAFILE DE CAMPO";
                case AuroraLoreUnlockType.AuroraCoinPurchase: return "ARQUIVO CRIPTOGRAFADO";
                case AuroraLoreUnlockType.SecretMission: return "CONTEÚDO CLASSIFICADO";
                default: return "ARQUIVO";
            }
        }

        private static string ToId(int number)
        {
            return "LORE_" + number.ToString("000");
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
}
#endif
