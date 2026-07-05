#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class VoiceLineDatabaseBuilder
{
    private const string VoiceFolder = "Assets/_ProjectAurora/Audio/Voice/Dublagem";
    private const string DatabasePath = "Assets/_ProjectAurora/Audio/Voice/Database/VoiceLineDatabase.asset";
    private const string DocsFolder = "Assets/_ProjectAurora/Docs";
    private const string AuditPath = DocsFolder + "/VoiceIntegration_Audit.md";
    private const string FinalReportPath = DocsFolder + "/VoiceIntegration_FinalReport.md";

    private static readonly Regex IdRegex = new Regex(@"^(CEL|ELI)_\d{3}$", RegexOptions.IgnoreCase);
    private static readonly Regex DirectionTagRegex = new Regex(@"^\s*(\[[^\]]*\]\s*)+", RegexOptions.Compiled);

    private sealed class ParsedLine
    {
        public string id;
        public string speaker;
        public string sceneUse;
        public string text;
        public string direction;
        public bool optional;
    }

    [InitializeOnLoadMethod]
    private static void ScheduleInitialBuild()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying && AssetDatabase.LoadAssetAtPath<VoiceLineDatabase>(DatabasePath) == null &&
                Directory.Exists(VoiceFolder))
            {
                RebuildVoiceDatabase();
            }
        };
    }

    [MenuItem("Tools/Projeto Aurora/Voice/Rebuild Voice Database")]
    public static void RebuildVoiceDatabase()
    {
        try
        {
            Directory.CreateDirectory(VoiceFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath) ?? string.Empty);
            Directory.CreateDirectory(DocsFolder);

            string markdownPath = FindDirectionMarkdown();
            if (string.IsNullOrEmpty(markdownPath))
            {
                throw new FileNotFoundException(
                    "AURORA_Direcao_ElevenLabs.md ou FALAS_PROJETO_AURORA.md não foi encontrado.");
            }

            List<ParsedLine> parsed = ParseMarkdown(File.ReadAllLines(markdownPath, Encoding.UTF8));
            ConfigureVoiceImporters();

            VoiceLineDatabase database = AssetDatabase.LoadAssetAtPath<VoiceLineDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<VoiceLineDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            var entries = parsed
                .Select(CreateEntry)
                .OrderBy(entry => entry.id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            database.ReplaceEntries(entries);
            EditorUtility.SetDirty(database);
            AddToPreloadedAssets(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WriteReports(markdownPath, parsed, entries);
            Debug.Log($"[Voice] Banco reconstruído: {entries.Count} entradas em {DatabasePath}.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode)
            {
                throw;
            }
        }
    }

    public static void RebuildFromCommandLine()
    {
        RebuildVoiceDatabase();
    }

    private static string FindDirectionMarkdown()
    {
        string[] candidates =
        {
            DocsFolder + "/AURORA_Direcao_ElevenLabs.md",
            "AURORA_Direcao_ElevenLabs.md",
            "FALAS_PROJETO_AURORA.md"
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static List<ParsedLine> ParseMarkdown(IEnumerable<string> lines)
    {
        var result = new List<ParsedLine>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string section = string.Empty;
        string[] headers = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                section = line.TrimStart('#', ' ').Trim();
                headers = null;
                continue;
            }

            if (!line.StartsWith("|", StringComparison.Ordinal) || !line.EndsWith("|", StringComparison.Ordinal))
            {
                continue;
            }

            string[] cells = line.Split('|')
                .Skip(1)
                .Take(line.Split('|').Length - 2)
                .Select(cell => cell.Trim())
                .ToArray();
            if (cells.Length == 0 || cells.All(IsSeparatorCell))
            {
                continue;
            }

            if (cells.Any(IsIdHeader))
            {
                headers = cells;
                continue;
            }

            int idIndex = Array.FindIndex(cells, cell => IdRegex.IsMatch(StripMarkdown(cell)));
            if (idIndex < 0)
            {
                continue;
            }

            string id = StripMarkdown(cells[idIndex]).ToUpperInvariant();
            if (!seen.Add(id))
            {
                continue;
            }

            string speaker = GetColumn(cells, headers, "personagem");
            if (string.IsNullOrWhiteSpace(speaker))
            {
                speaker = id.StartsWith("ELI_", StringComparison.Ordinal) ? "Dr. Elias" : "CelestIA";
            }

            string text = GetColumn(cells, headers, "texto sugerido");
            if (string.IsNullOrWhiteSpace(text))
            {
                text = GetColumn(cells, headers, "fala");
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                text = cells
                    .Where((cell, index) => index != idIndex && !LooksLikeSpeaker(cell))
                    .OrderByDescending(cell => cell.Length)
                    .FirstOrDefault() ?? string.Empty;
            }

            string sceneUse = FirstNonEmpty(
                GetColumn(cells, headers, "cena/uso"),
                GetColumn(cells, headers, "momento"),
                GetColumn(cells, headers, "gatilho"),
                section);
            string direction = FirstNonEmpty(
                GetColumn(cells, headers, "direção"),
                GetColumn(cells, headers, "direcao"),
                ExtractDirectionTags(text));

            result.Add(new ParsedLine
            {
                id = id,
                speaker = StripMarkdown(speaker),
                sceneUse = StripMarkdown(sceneUse),
                text = CleanSubtitle(text),
                direction = StripMarkdown(direction),
                optional = id == "CEL_054" || id == "CEL_055" ||
                    section.IndexOf("opcional", StringComparison.OrdinalIgnoreCase) >= 0
            });
        }

        return result;
    }

    private static VoiceLineEntry CreateEntry(ParsedLine parsed)
    {
        var entry = new VoiceLineEntry
        {
            id = parsed.id,
            speaker = parsed.id.StartsWith("ELI_", StringComparison.Ordinal)
                ? VoiceSpeaker.DrElias
                : VoiceSpeaker.CelestIA,
            sceneUse = parsed.sceneUse,
            subtitleText = parsed.text,
            originalDirection = parsed.direction,
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoiceFolder}/{parsed.id}.mp3"),
            priority = InferPriority(parsed.id),
            minDisplayTime = 1.5f,
            postDelay = 0.15f,
            cooldownSeconds = InferCooldown(parsed.id),
            optional = parsed.optional,
            interruptCurrent = parsed.id == "CEL_056" || parsed.id == "CEL_057",
            canBeSkipped = true,
            drEliasMood = InferEliasMood(parsed.id),
            celestIAStateHint = InferCelestIAState(parsed.id)
        };
        return entry;
    }

    private static VoicePriority InferPriority(string id)
    {
        int number = ParseNumber(id);
        if (id == "CEL_056" || id == "CEL_057") return VoicePriority.Critical;
        if (id.StartsWith("ELI_", StringComparison.Ordinal))
        {
            if (number <= 3 || number >= 7) return VoicePriority.Cutscene;
            return VoicePriority.Narrative;
        }
        if (number <= 7) return VoicePriority.Cutscene;
        if (number <= 19) return VoicePriority.Tutorial;
        if (number <= 44) return VoicePriority.Narrative;
        if (number <= 55) return VoicePriority.Context;
        return VoicePriority.Gameplay;
    }

    private static float InferCooldown(string id)
    {
        if (id == "CEL_045") return 8f;
        if (id == "CEL_046") return 2f;
        VoicePriority priority = InferPriority(id);
        return priority == VoicePriority.Context ? 1.5f : 0f;
    }

    private static DrEliasMood InferEliasMood(string id)
    {
        switch (id)
        {
            case "ELI_002":
            case "ELI_004":
            case "ELI_005":
            case "ELI_006":
            case "ELI_009":
            case "ELI_010":
                return DrEliasMood.Nervous;
            default:
                return DrEliasMood.Normal;
        }
    }

    private static CelestIAVisualState InferCelestIAState(string id)
    {
        if (!id.StartsWith("CEL_", StringComparison.Ordinal)) return CelestIAVisualState.Auto;
        int number = ParseNumber(id);
        if (number >= 27 && number <= 29) return CelestIAVisualState.Transitioning;
        if (number >= 30 && number <= 44) return CelestIAVisualState.Corrupted;
        if (number <= 26) return CelestIAVisualState.Normal;
        return CelestIAVisualState.Auto;
    }

    private static void ConfigureVoiceImporters()
    {
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { VoiceFolder });
        foreach (string guid in audioGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                continue;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.75f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.ambisonic = false;
            AssetDatabase.WriteImportSettingsIfDirty(path);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void AddToPreloadedAssets(VoiceLineDatabase database)
    {
        UnityEngine.Object[] preloaded = PlayerSettings.GetPreloadedAssets() ?? Array.Empty<UnityEngine.Object>();
        if (preloaded.Contains(database))
        {
            return;
        }

        PlayerSettings.SetPreloadedAssets(preloaded.Concat(new UnityEngine.Object[] { database }).ToArray());
    }

    private static void WriteReports(string markdownPath, List<ParsedLine> parsed, List<VoiceLineEntry> entries)
    {
        string[] mp3Files = Directory.GetFiles(VoiceFolder, "*.mp3", SearchOption.TopDirectoryOnly);
        var audioIds = new HashSet<string>(
            mp3Files.Select(Path.GetFileNameWithoutExtension),
            StringComparer.OrdinalIgnoreCase);
        var markdownIds = new HashSet<string>(parsed.Select(line => line.id), StringComparer.OrdinalIgnoreCase);
        List<string> missingRequired = entries
            .Where(entry => entry.clip == null && !entry.optional)
            .Select(entry => entry.id)
            .OrderBy(id => id)
            .ToList();
        List<string> missingOptional = entries
            .Where(entry => entry.clip == null && entry.optional)
            .Select(entry => entry.id)
            .OrderBy(id => id)
            .ToList();
        List<string> orphanAudio = audioIds.Except(markdownIds, StringComparer.OrdinalIgnoreCase).OrderBy(id => id).ToList();

        var audit = new StringBuilder();
        audit.AppendLine("# Voice Integration — Audit");
        audit.AppendLine();
        audit.AppendLine($"- Gerado em: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
        audit.AppendLine($"- Documento analisado: `{markdownPath.Replace('\\', '/')}`");
        audit.AppendLine($"- MP3 encontrados: **{mp3Files.Length}**");
        audit.AppendLine($"- Entradas de roteiro: **{entries.Count}**");
        audit.AppendLine($"- Clipes associados: **{entries.Count(entry => entry.clip != null)}**");
        audit.AppendLine($"- IDs duplicados no banco: **{entries.GroupBy(entry => entry.id, StringComparer.OrdinalIgnoreCase).Count(group => group.Count() > 1)}**");
        audit.AppendLine($"- Faltantes obrigatórios: {FormatList(missingRequired)}");
        audit.AppendLine($"- Faltantes opcionais: {FormatList(missingOptional)}");
        audit.AppendLine($"- MP3 sem entrada no roteiro: {FormatList(orphanAudio)}");
        audit.AppendLine();
        audit.AppendLine("## Sistemas existentes preservados");
        audit.AppendLine();
        audit.AppendLine("- `DialogueManager` permanece como fallback visual para conteúdo legado.");
        audit.AppendLine("- `AuroraGameplayHUDController` e `HudCharacterVideoPortraitController` recebem speaker/metadados sem duplicar a HUD.");
        audit.AppendLine("- Intro, tutorial, narrativa por distância, interações, dano, recuperação, Game Over e final usam IDs oficiais.");
        File.WriteAllText(AuditPath, audit.ToString(), new UTF8Encoding(false));

        var final = new StringBuilder();
        final.AppendLine("# Voice Integration — Final Report");
        final.AppendLine();
        final.AppendLine($"- Banco: `{DatabasePath}`");
        final.AppendLine($"- Entradas: **{entries.Count}**");
        final.AppendLine($"- Áudios integrados: **{entries.Count(entry => entry.clip != null)}**");
        final.AppendLine($"- Áudios ausentes obrigatórios: {FormatList(missingRequired)}");
        final.AppendLine($"- Áudios ausentes opcionais: {FormatList(missingOptional)}");
        final.AppendLine("- Duração: `AudioClip.length + postDelay`, respeitando `minDisplayTime`.");
        final.AppendLine("- Fallback sem áudio: duração por caracteres, sem interromper o fluxo.");
        final.AppendLine("- Reprodução: `AudioSource` 2D dedicado, fila por prioridade e cooldown por ID/prioridade.");
        final.AppendLine("- HUD: speaker, legenda limpa, estado da CelestIA e humor do Dr. Elias integrados ao retrato existente.");
        final.AppendLine("- `CEL_054` e `CEL_055`: opcionais.");
        File.WriteAllText(FinalReportPath, final.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(AuditPath);
        AssetDatabase.ImportAsset(FinalReportPath);
    }

    private static string GetColumn(string[] cells, string[] headers, string headerFragment)
    {
        if (headers == null)
        {
            return null;
        }

        int index = Array.FindIndex(headers, header => HeaderMatches(header, headerFragment));
        return index >= 0 && index < cells.Length ? cells[index] : null;
    }

    private static bool HeaderMatches(string value, string fragment)
    {
        string normalized = RemoveDiacritics(StripMarkdown(value)).ToLowerInvariant();
        string expected = RemoveDiacritics(fragment).ToLowerInvariant();
        return normalized.Contains(expected);
    }

    private static bool IsIdHeader(string value)
    {
        string normalized = RemoveDiacritics(StripMarkdown(value)).ToLowerInvariant().Trim();
        return normalized == "take" || normalized == "id" || normalized == "take/id";
    }

    private static bool IsSeparatorCell(string value)
    {
        string cleaned = value.Replace(":", string.Empty).Replace("-", string.Empty).Trim();
        return cleaned.Length == 0;
    }

    private static bool LooksLikeSpeaker(string value)
    {
        string normalized = RemoveDiacritics(value).ToUpperInvariant();
        return normalized.Contains("CELESTIA") || normalized.Contains("ELIAS");
    }

    private static string CleanSubtitle(string value)
    {
        string cleaned = StripMarkdown(value);
        cleaned = DirectionTagRegex.Replace(cleaned, string.Empty);
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    private static string ExtractDirectionTags(string value)
    {
        Match match = DirectionTagRegex.Match(StripMarkdown(value));
        return match.Success ? match.Value.Trim() : string.Empty;
    }

    private static string StripMarkdown(string value)
    {
        return (value ?? string.Empty)
            .Replace("**", string.Empty)
            .Replace("`", string.Empty)
            .Replace("*(legado)*", "legado")
            .Trim();
    }

    private static string RemoveDiacritics(string value)
    {
        string normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static int ParseNumber(string id)
    {
        return int.TryParse(id.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
            ? number
            : 0;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static string FormatList(IEnumerable<string> values)
    {
        string[] array = values.ToArray();
        return array.Length == 0 ? "nenhum" : string.Join(", ", array.Select(value => $"`{value}`"));
    }
}
#endif
