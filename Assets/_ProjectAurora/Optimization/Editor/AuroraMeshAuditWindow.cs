#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjetoAurora.EditorTools
{
    public sealed class AuroraMeshAuditWindow : EditorWindow
    {
        private const string ReportsFolder = "Assets/_ProjectAurora/Optimization/00_Reports";
        private const string CsvReportPath = ReportsFolder + "/aurora_mesh_audit.csv";
        private const string JsonReportPath = ReportsFolder + "/aurora_mesh_audit.json";
        private const string MarkdownReportPath = ReportsFolder + "/aurora_mesh_audit_summary.md";
        private const string CandidatesCsvPath = ReportsFolder + "/aurora_optimization_candidates.csv";
        private const string HeavyCandidateLabel = "AuroraMeshAuditCandidate";
        private const int PreviewLimit = 100;

        private static readonly string[] ModelExtensions =
        {
            ".fbx",
            ".obj",
            ".glb",
            ".gltf",
            ".blend"
        };

        private static readonly string[] SuspiciousNameTokens =
        {
            "tripo",
            "meshy",
            "generated",
            "ai",
            "scan"
        };

        private static readonly string[] SuspiciousFolderTokens =
        {
            "/Generated/",
            "/Tripo/",
            "/Meshy/",
            "/AI/",
            "/Import/"
        };

        private readonly List<MeshAuditRecord> records = new List<MeshAuditRecord>();
        private Vector2 scrollPosition;
        private string lastStatus = "No scan has been run in this editor session.";

        [MenuItem("Tools/Projeto Aurora/Optimization/Mesh Audit")]
        public static void OpenWindow()
        {
            AuroraMeshAuditWindow window = GetWindow<AuroraMeshAuditWindow>("Aurora Mesh Audit");
            window.minSize = new Vector2(980f, 520f);
        }

        public static void RunFullAuditFromCommandLine()
        {
            List<MeshAuditRecord> scanRecords = ScanMeshAssetsInternal();
            ExportCsv(scanRecords, CsvReportPath, false);
            ExportJson(scanRecords);
            ExportMarkdown(scanRecords);
            ExportCsv(scanRecords.Where(IsOptimizationCandidate).ToList(), CandidatesCsvPath, true);
            Debug.Log("[AuroraMeshAudit] Audit finished. Assets: " + scanRecords.Count +
                      ", Heavy: " + scanRecords.Count(r => r.OptimizationCategory == "HEAVY") +
                      ", VeryHeavy: " + scanRecords.Count(r => r.OptimizationCategory == "VERY_HEAVY") +
                      ", Critical: " + scanRecords.Count(r => r.OptimizationCategory == "CRITICAL"));
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Projeto Aurora Mesh Audit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Measurement-only tool. Scan and report exports do not move, delete, optimize, replace references, or change model import settings.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Mesh Assets", GUILayout.Height(32f)))
                {
                    ScanMeshAssets();
                }

                if (GUILayout.Button("Export CSV Report", GUILayout.Height(32f)))
                {
                    EnsureScanned();
                    ExportCsv(records, CsvReportPath, false);
                    ExportCsv(records.Where(IsOptimizationCandidate).ToList(), CandidatesCsvPath, true);
                    SetStatus("CSV reports exported.");
                }

                if (GUILayout.Button("Export JSON Report", GUILayout.Height(32f)))
                {
                    EnsureScanned();
                    ExportJson(records);
                    SetStatus("JSON report exported.");
                }

                if (GUILayout.Button("Export Markdown Summary", GUILayout.Height(32f)))
                {
                    EnsureScanned();
                    ExportMarkdown(records);
                    SetStatus("Markdown summary exported.");
                }

                if (GUILayout.Button("Label Heavy Assets", GUILayout.Height(32f)))
                {
                    EnsureScanned();
                    LabelHeavyAssets(records);
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(lastStatus, EditorStyles.wordWrappedLabel);

            if (records.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Assets: " + records.Count, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("HEAVY: " + records.Count(r => r.OptimizationCategory == "HEAVY"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("VERY_HEAVY: " + records.Count(r => r.OptimizationCategory == "VERY_HEAVY"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("CRITICAL: " + records.Count(r => r.OptimizationCategory == "CRITICAL"), EditorStyles.boldLabel);
            }

            DrawPreviewTable();
        }

        private void DrawPreviewTable()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Preview - worst assets by triangles", EditorStyles.boldLabel);

            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scroll.scrollPosition;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Category", GUILayout.Width(95f));
                    GUILayout.Label("Triangles", GUILayout.Width(95f));
                    GUILayout.Label("MB", GUILayout.Width(70f));
                    GUILayout.Label("Meshes", GUILayout.Width(60f));
                    GUILayout.Label("Materials", GUILayout.Width(70f));
                    GUILayout.Label("Asset Path");
                }

                foreach (MeshAuditRecord record in records
                             .OrderByDescending(r => r.TotalTriangles)
                             .ThenByDescending(r => r.FileSizeMB)
                             .Take(PreviewLimit))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(record.OptimizationCategory, GUILayout.Width(95f));
                        GUILayout.Label(record.TotalTriangles.ToString(CultureInfo.InvariantCulture), GUILayout.Width(95f));
                        GUILayout.Label(record.FileSizeMB.ToString("0.00", CultureInfo.InvariantCulture), GUILayout.Width(70f));
                        GUILayout.Label(record.MeshCount.ToString(CultureInfo.InvariantCulture), GUILayout.Width(60f));
                        GUILayout.Label(record.MaterialCount.ToString(CultureInfo.InvariantCulture), GUILayout.Width(70f));
                        GUILayout.Label(record.AssetPath);
                    }
                }
            }
        }

        private void ScanMeshAssets()
        {
            records.Clear();
            records.AddRange(ScanMeshAssetsInternal());
            SetStatus("Scan complete. Assets found: " + records.Count);
        }

        private void EnsureScanned()
        {
            if (records.Count > 0)
            {
                return;
            }

            ScanMeshAssets();
        }

        private void SetStatus(string status)
        {
            lastStatus = status;
            Debug.Log("[AuroraMeshAudit] " + status);
            Repaint();
        }

        private static List<MeshAuditRecord> ScanMeshAssetsInternal()
        {
            EnsureReportFolders();

            string[] modelPaths = FindModelAssetPaths();
            Dictionary<string, DependencyUsage> dependencyUsage = BuildDependencyUsage(modelPaths);
            var scanRecords = new List<MeshAuditRecord>(modelPaths.Length);

            try
            {
                for (int i = 0; i < modelPaths.Length; i++)
                {
                    string path = modelPaths[i];
                    EditorUtility.DisplayProgressBar(
                        "Aurora Mesh Audit",
                        "Scanning " + path,
                        modelPaths.Length == 0 ? 1f : (float)i / modelPaths.Length);

                    scanRecords.Add(AnalyzeModelAsset(path, dependencyUsage));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return scanRecords
                .OrderByDescending(record => record.TotalTriangles)
                .ThenBy(record => record.AssetPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string[] FindModelAssetPaths()
        {
            string assetsRoot = Application.dataPath;
            return Directory.EnumerateFiles(assetsRoot, "*.*", SearchOption.AllDirectories)
                .Where(file => ModelExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .Select(AbsolutePathToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static MeshAuditRecord AnalyzeModelAsset(string assetPath, Dictionary<string, DependencyUsage> dependencyUsage)
        {
            var record = new MeshAuditRecord
            {
                AssetPath = assetPath,
                FileName = Path.GetFileName(assetPath),
                FileExtension = Path.GetExtension(assetPath).ToLowerInvariant(),
                IsReadable = "Unknown",
                ImporterScale = "Unknown",
                Notes = string.Empty
            };

            string absolutePath = AssetPathToAbsolutePath(assetPath);
            if (File.Exists(absolutePath))
            {
                var fileInfo = new FileInfo(absolutePath);
                record.FileSizeKB = Math.Round(fileInfo.Length / 1024d, 2);
                record.FileSizeMB = Math.Round(fileInfo.Length / (1024d * 1024d), 3);
            }

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer is ModelImporter modelImporter)
            {
                record.IsReadable = modelImporter.isReadable ? "True" : "False";
                record.ImporterScale = modelImporter.globalScale.ToString("0.###", CultureInfo.InvariantCulture);
            }

            var notes = new List<string>();
            Object[] loadedObjects = Array.Empty<Object>();

            try
            {
                loadedObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            }
            catch (Exception exception)
            {
                notes.Add("LoadAllAssetsAtPath failed: " + exception.GetType().Name + " - " + exception.Message);
            }

            AnalyzeLoadedObjects(loadedObjects, record, notes);
            ApplyDependencyUsage(assetPath, dependencyUsage, record, notes);

            record.OptimizationCategory = Classify(record.TotalTriangles);
            record.RecommendedTargetTris = RecommendTargetTriangles(record);
            AppendSuspicionNotes(record, notes);
            record.Notes = string.Join("; ", notes.Distinct());

            return record;
        }

        private static void AnalyzeLoadedObjects(Object[] loadedObjects, MeshAuditRecord record, List<string> notes)
        {
            var meshes = new List<Mesh>();
            var materialIds = new HashSet<int>();

            foreach (Object loadedObject in loadedObjects)
            {
                if (loadedObject == null)
                {
                    continue;
                }

                if (loadedObject is Mesh mesh && !meshes.Contains(mesh))
                {
                    meshes.Add(mesh);
                }

                if (loadedObject is Material material)
                {
                    materialIds.Add(material.GetInstanceID());
                }

                if (loadedObject is GameObject gameObject)
                {
                    foreach (MeshFilter meshFilter in gameObject.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (meshFilter.sharedMesh != null && !meshes.Contains(meshFilter.sharedMesh))
                        {
                            meshes.Add(meshFilter.sharedMesh);
                        }
                    }

                    foreach (SkinnedMeshRenderer skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        record.HasSkinnedMesh = true;
                        if (skinnedMeshRenderer.sharedMesh != null && !meshes.Contains(skinnedMeshRenderer.sharedMesh))
                        {
                            meshes.Add(skinnedMeshRenderer.sharedMesh);
                        }
                    }

                    foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
                    {
                        foreach (Material sharedMaterial in renderer.sharedMaterials)
                        {
                            if (sharedMaterial != null)
                            {
                                materialIds.Add(sharedMaterial.GetInstanceID());
                            }
                        }
                    }
                }
            }

            record.MeshCount = meshes.Count;
            record.MaterialCount = materialIds.Count;

            foreach (Mesh mesh in meshes)
            {
                try
                {
                    record.TotalVertices += mesh.vertexCount;
                    record.TotalSubMeshes += mesh.subMeshCount;

                    for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                    {
                        record.TotalTriangles += SafeIndexCount(mesh, subMesh, notes) / 3L;
                    }
                }
                catch (Exception exception)
                {
                    notes.Add("Could not read mesh '" + mesh.name + "': " + exception.GetType().Name + " - " + exception.Message);
                }
            }
        }

        private static long SafeIndexCount(Mesh mesh, int subMesh, List<string> notes)
        {
            try
            {
                return (long)mesh.GetIndexCount(subMesh);
            }
            catch (Exception exception)
            {
                notes.Add("Could not read indices for mesh '" + mesh.name + "' submesh " + subMesh + ": " +
                          exception.GetType().Name + " - " + exception.Message);
                return 0L;
            }
        }

        private static Dictionary<string, DependencyUsage> BuildDependencyUsage(IEnumerable<string> modelPaths)
        {
            var modelSet = new HashSet<string>(modelPaths, StringComparer.OrdinalIgnoreCase);
            var usage = modelSet.ToDictionary(path => path, _ => new DependencyUsage(), StringComparer.OrdinalIgnoreCase);

            string[] dependencyCarrierPaths = Directory.EnumerateFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    string extension = Path.GetExtension(file);
                    return extension.Equals(".prefab", StringComparison.OrdinalIgnoreCase) ||
                           extension.Equals(".unity", StringComparison.OrdinalIgnoreCase) ||
                           extension.Equals(".asset", StringComparison.OrdinalIgnoreCase);
                })
                .Select(AbsolutePathToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToArray();

            try
            {
                for (int i = 0; i < dependencyCarrierPaths.Length; i++)
                {
                    string carrierPath = dependencyCarrierPaths[i];
                    EditorUtility.DisplayProgressBar(
                        "Aurora Mesh Audit",
                        "Checking dependencies " + carrierPath,
                        dependencyCarrierPaths.Length == 0 ? 1f : (float)i / dependencyCarrierPaths.Length);

                    string[] dependencies;
                    try
                    {
                        dependencies = AssetDatabase.GetDependencies(carrierPath, true);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning("[AuroraMeshAudit] Could not read dependencies for " + carrierPath + ": " + exception.Message);
                        continue;
                    }

                    foreach (string dependency in dependencies)
                    {
                        if (!modelSet.Contains(dependency) || dependency.Equals(carrierPath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        DependencyUsage entry = usage[dependency];
                        string extension = Path.GetExtension(carrierPath).ToLowerInvariant();

                        if (extension == ".prefab")
                        {
                            entry.Prefabs.Add(carrierPath);
                        }
                        else if (extension == ".unity")
                        {
                            entry.Scenes.Add(carrierPath);
                        }
                        else if (extension == ".asset")
                        {
                            entry.Assets.Add(carrierPath);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return usage;
        }

        private static void ApplyDependencyUsage(
            string assetPath,
            Dictionary<string, DependencyUsage> dependencyUsage,
            MeshAuditRecord record,
            List<string> notes)
        {
            if (!dependencyUsage.TryGetValue(assetPath, out DependencyUsage usage))
            {
                return;
            }

            record.UsedInPrefabs = JoinPaths(usage.Prefabs);
            record.UsedInScenes = JoinPaths(usage.Scenes);
            record.DependencyCount = usage.Prefabs.Count + usage.Scenes.Count + usage.Assets.Count;

            if (usage.Assets.Count > 0)
            {
                notes.Add("Referenced by asset files: " + JoinPaths(usage.Assets));
            }
        }

        private static string JoinPaths(IEnumerable<string> paths)
        {
            return string.Join("; ", paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        }

        private static string Classify(long triangles)
        {
            if (triangles <= 10000L)
            {
                return "LOW";
            }

            if (triangles <= 30000L)
            {
                return "OK";
            }

            if (triangles <= 100000L)
            {
                return "HEAVY";
            }

            if (triangles <= 500000L)
            {
                return "VERY_HEAVY";
            }

            return "CRITICAL";
        }

        private static int RecommendTargetTriangles(MeshAuditRecord record)
        {
            string searchableName = (record.FileName + " " + record.AssetPath).ToLowerInvariant();

            if (ContainsAny(searchableName, "box", "crate", "caixa", "obstacle", "barreira"))
            {
                return 5000;
            }

            if (ContainsAny(searchableName, "panel", "terminal", "console", "computer", "painel"))
            {
                return 8000;
            }

            if (ContainsAny(searchableName, "door", "gate", "portal", "scanner", "porta"))
            {
                return 12000;
            }

            if (ContainsAny(searchableName, "machine", "core", "reactor", "maquina"))
            {
                return 20000;
            }

            switch (record.OptimizationCategory)
            {
                case "CRITICAL":
                    return 15000;
                case "VERY_HEAVY":
                    return 12000;
                case "HEAVY":
                    return 8000;
                default:
                    return 0;
            }
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            return tokens.Any(token => value.Contains(token));
        }

        private static void AppendSuspicionNotes(MeshAuditRecord record, List<string> notes)
        {
            string lowerFileName = record.FileName.ToLowerInvariant();
            string normalizedPath = record.AssetPath.Replace('\\', '/');

            if (record.FileSizeMB > 10d)
            {
                notes.Add("Suspicious: file size above 10 MB");
            }

            if (record.TotalTriangles > 100000L)
            {
                notes.Add("Suspicious: triangle count above 100000");
            }

            foreach (string token in SuspiciousNameTokens)
            {
                if (lowerFileName.Contains(token))
                {
                    notes.Add("Suspicious: file name contains '" + token + "'");
                }
            }

            foreach (string token in SuspiciousFolderTokens)
            {
                if (normalizedPath.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    notes.Add("Suspicious: folder contains '" + token.Trim('/') + "'");
                }
            }
        }

        private static bool IsOptimizationCandidate(MeshAuditRecord record)
        {
            return record.OptimizationCategory == "HEAVY" ||
                   record.OptimizationCategory == "VERY_HEAVY" ||
                   record.OptimizationCategory == "CRITICAL" ||
                   record.FileSizeMB > 10d ||
                   record.TotalTriangles > 30000L;
        }

        private static void ExportCsv(List<MeshAuditRecord> scanRecords, string outputPath, bool candidatesOnly)
        {
            EnsureReportFolders();

            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", CsvHeaders()));

            foreach (MeshAuditRecord record in scanRecords)
            {
                builder.AppendLine(string.Join(",", CsvValues(record).Select(EscapeCsv)));
            }

            WriteTextAsset(outputPath, builder.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[AuroraMeshAudit] Exported " + (candidatesOnly ? "candidate " : string.Empty) + "CSV: " + outputPath +
                      " (" + scanRecords.Count + " rows)");
        }

        private static IEnumerable<string> CsvHeaders()
        {
            yield return "AssetPath";
            yield return "FileName";
            yield return "FileExtension";
            yield return "FileSizeKB";
            yield return "FileSizeMB";
            yield return "MeshCount";
            yield return "TotalVertices";
            yield return "TotalTriangles";
            yield return "TotalSubMeshes";
            yield return "MaterialCount";
            yield return "HasSkinnedMesh";
            yield return "IsReadable";
            yield return "ImporterScale";
            yield return "UsedInPrefabs";
            yield return "UsedInScenes";
            yield return "DependencyCount";
            yield return "OptimizationCategory";
            yield return "RecommendedTargetTris";
            yield return "Notes";
        }

        private static IEnumerable<string> CsvValues(MeshAuditRecord record)
        {
            yield return record.AssetPath;
            yield return record.FileName;
            yield return record.FileExtension;
            yield return record.FileSizeKB.ToString("0.##", CultureInfo.InvariantCulture);
            yield return record.FileSizeMB.ToString("0.###", CultureInfo.InvariantCulture);
            yield return record.MeshCount.ToString(CultureInfo.InvariantCulture);
            yield return record.TotalVertices.ToString(CultureInfo.InvariantCulture);
            yield return record.TotalTriangles.ToString(CultureInfo.InvariantCulture);
            yield return record.TotalSubMeshes.ToString(CultureInfo.InvariantCulture);
            yield return record.MaterialCount.ToString(CultureInfo.InvariantCulture);
            yield return record.HasSkinnedMesh ? "True" : "False";
            yield return record.IsReadable;
            yield return record.ImporterScale;
            yield return record.UsedInPrefabs;
            yield return record.UsedInScenes;
            yield return record.DependencyCount.ToString(CultureInfo.InvariantCulture);
            yield return record.OptimizationCategory;
            yield return record.RecommendedTargetTris.ToString(CultureInfo.InvariantCulture);
            yield return record.Notes;
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static void ExportJson(List<MeshAuditRecord> scanRecords)
        {
            EnsureReportFolders();
            var report = new MeshAuditReport
            {
                GeneratedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                TotalAssets = scanRecords.Count,
                TotalTriangles = scanRecords.Sum(record => record.TotalTriangles),
                Records = scanRecords
            };

            WriteTextAsset(JsonReportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            Debug.Log("[AuroraMeshAudit] Exported JSON: " + JsonReportPath);
        }

        private static void ExportMarkdown(List<MeshAuditRecord> scanRecords)
        {
            EnsureReportFolders();

            var builder = new StringBuilder();
            long totalTriangles = scanRecords.Sum(record => record.TotalTriangles);

            builder.AppendLine("# Projeto Aurora Mesh Audit Summary");
            builder.AppendLine();
            builder.AppendLine("- Generated UTC: " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("- Total 3D assets analyzed: " + scanRecords.Count);
            builder.AppendLine("- Total triangles: " + totalTriangles.ToString("N0", CultureInfo.InvariantCulture));
            builder.AppendLine("- HEAVY assets: " + scanRecords.Count(record => record.OptimizationCategory == "HEAVY"));
            builder.AppendLine("- VERY_HEAVY assets: " + scanRecords.Count(record => record.OptimizationCategory == "VERY_HEAVY"));
            builder.AppendLine("- CRITICAL assets: " + scanRecords.Count(record => record.OptimizationCategory == "CRITICAL"));
            builder.AppendLine("- Optimization candidates: " + scanRecords.Count(IsOptimizationCandidate));
            builder.AppendLine();

            AppendAssetTable(builder, "Top 20 assets by triangles", scanRecords.OrderByDescending(record => record.TotalTriangles).Take(20));
            AppendAssetTable(builder, "Top 20 largest files by MB", scanRecords.OrderByDescending(record => record.FileSizeMB).Take(20));
            AppendAssetTable(builder, "CRITICAL assets", scanRecords.Where(record => record.OptimizationCategory == "CRITICAL"));
            AppendAssetTable(builder, "VERY_HEAVY assets", scanRecords.Where(record => record.OptimizationCategory == "VERY_HEAVY"));
            AppendAssetTable(builder, "Assets used in scenes", scanRecords.Where(record => !string.IsNullOrEmpty(record.UsedInScenes)));
            AppendAssetTable(builder, "Assets not used in any scene or prefab", scanRecords.Where(record => string.IsNullOrEmpty(record.UsedInScenes) && string.IsNullOrEmpty(record.UsedInPrefabs)));
            AppendPriorityRecommendations(builder, scanRecords);

            WriteTextAsset(MarkdownReportPath, builder.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[AuroraMeshAudit] Exported Markdown: " + MarkdownReportPath);
        }

        private static void AppendAssetTable(StringBuilder builder, string title, IEnumerable<MeshAuditRecord> source)
        {
            List<MeshAuditRecord> rows = source.ToList();

            builder.AppendLine("## " + title);
            builder.AppendLine();

            if (rows.Count == 0)
            {
                builder.AppendLine("_None._");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("| Category | Triangles | MB | Target Tris | Asset | Used in scenes |");
            builder.AppendLine("|---|---:|---:|---:|---|---|");

            foreach (MeshAuditRecord record in rows)
            {
                builder.Append("| ");
                builder.Append(EscapeMarkdown(record.OptimizationCategory));
                builder.Append(" | ");
                builder.Append(record.TotalTriangles.ToString(CultureInfo.InvariantCulture));
                builder.Append(" | ");
                builder.Append(record.FileSizeMB.ToString("0.###", CultureInfo.InvariantCulture));
                builder.Append(" | ");
                builder.Append(record.RecommendedTargetTris.ToString(CultureInfo.InvariantCulture));
                builder.Append(" | ");
                builder.Append(EscapeMarkdown(record.AssetPath));
                builder.Append(" | ");
                builder.Append(EscapeMarkdown(string.IsNullOrEmpty(record.UsedInScenes) ? "-" : record.UsedInScenes));
                builder.AppendLine(" |");
            }

            builder.AppendLine();
        }

        private static void AppendPriorityRecommendations(StringBuilder builder, List<MeshAuditRecord> scanRecords)
        {
            List<MeshAuditRecord> candidates = scanRecords
                .Where(IsOptimizationCandidate)
                .OrderBy(record => PriorityRank(record))
                .ThenByDescending(record => record.TotalTriangles)
                .ThenByDescending(record => record.FileSizeMB)
                .ToList();

            builder.AppendLine("## Priority recommendation");
            builder.AppendLine();

            if (candidates.Count == 0)
            {
                builder.AppendLine("No optimization candidates were found by the current thresholds.");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("| Priority | Category | Triangles | MB | Target Tris | Asset | Reason |");
            builder.AppendLine("|---|---|---:|---:|---:|---|---|");

            foreach (MeshAuditRecord record in candidates)
            {
                builder.Append("| ");
                builder.Append(PriorityLabel(record));
                builder.Append(" | ");
                builder.Append(EscapeMarkdown(record.OptimizationCategory));
                builder.Append(" | ");
                builder.Append(record.TotalTriangles.ToString(CultureInfo.InvariantCulture));
                builder.Append(" | ");
                builder.Append(record.FileSizeMB.ToString("0.###", CultureInfo.InvariantCulture));
                builder.Append(" | ");
                builder.Append(record.RecommendedTargetTris.ToString(CultureInfo.InvariantCulture));
                builder.Append(" | ");
                builder.Append(EscapeMarkdown(record.AssetPath));
                builder.Append(" | ");
                builder.Append(EscapeMarkdown(PriorityReason(record)));
                builder.AppendLine(" |");
            }

            builder.AppendLine();
        }

        private static int PriorityRank(MeshAuditRecord record)
        {
            switch (record.OptimizationCategory)
            {
                case "CRITICAL":
                    return 1;
                case "VERY_HEAVY":
                    return 2;
                case "HEAVY":
                    return 3;
                default:
                    return record.FileSizeMB > 10d ? 4 : 5;
            }
        }

        private static string PriorityLabel(MeshAuditRecord record)
        {
            switch (PriorityRank(record))
            {
                case 1:
                    return "P1";
                case 2:
                    return "P2";
                case 3:
                    return "P3";
                default:
                    return "P4";
            }
        }

        private static string PriorityReason(MeshAuditRecord record)
        {
            var reasons = new List<string>();

            if (record.OptimizationCategory == "CRITICAL" ||
                record.OptimizationCategory == "VERY_HEAVY" ||
                record.OptimizationCategory == "HEAVY")
            {
                reasons.Add(record.OptimizationCategory + " triangle count");
            }

            if (record.FileSizeMB > 10d)
            {
                reasons.Add("file size above 10 MB");
            }

            if (record.TotalTriangles > 30000L)
            {
                reasons.Add("triangles above 30000");
            }

            if (!string.IsNullOrEmpty(record.UsedInScenes))
            {
                reasons.Add("used in scene");
            }

            return string.Join("; ", reasons);
        }

        private static string EscapeMarkdown(string value)
        {
            return (value ?? string.Empty)
                .Replace("|", "\\|")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static void LabelHeavyAssets(List<MeshAuditRecord> scanRecords)
        {
            int changed = 0;

            foreach (MeshAuditRecord record in scanRecords.Where(IsOptimizationCandidate))
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(record.AssetPath);
                if (asset == null)
                {
                    continue;
                }

                var labels = new HashSet<string>(AssetDatabase.GetLabels(asset), StringComparer.OrdinalIgnoreCase)
                {
                    HeavyCandidateLabel,
                    "AuroraMeshAudit_" + record.OptimizationCategory
                };

                if (record.FileSizeMB > 10d)
                {
                    labels.Add("AuroraMeshAudit_SizeOver10MB");
                }

                if (record.Notes.IndexOf("Suspicious:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    labels.Add("AuroraMeshAudit_Suspicious");
                }

                AssetDatabase.SetLabels(asset, labels.OrderBy(label => label, StringComparer.OrdinalIgnoreCase).ToArray());
                changed++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[AuroraMeshAudit] Labels applied to " + changed + " optimization candidate assets.");
            EditorUtility.DisplayDialog("Aurora Mesh Audit", "Labels applied to " + changed + " candidate assets.", "OK");
        }

        private static void EnsureReportFolders()
        {
            Directory.CreateDirectory(AssetPathToAbsolutePath(ReportsFolder));
            Directory.CreateDirectory(AssetPathToAbsolutePath("Assets/_ProjectAurora/Optimization/01_HeavyAssetCandidates"));
        }

        private static void WriteTextAsset(string assetPath, string content)
        {
            string absolutePath = AssetPathToAbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? Application.dataPath);
            File.WriteAllText(absolutePath, content, new UTF8Encoding(false));
        }

        private static string AbsolutePathToAssetPath(string absolutePath)
        {
            string normalizedAbsolute = absolutePath.Replace('\\', '/');
            string normalizedDataPath = Application.dataPath.Replace('\\', '/');

            if (!normalizedAbsolute.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return "Assets" + normalizedAbsolute.Substring(normalizedDataPath.Length);
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        [Serializable]
        private sealed class MeshAuditReport
        {
            public string GeneratedUtc;
            public int TotalAssets;
            public long TotalTriangles;
            public List<MeshAuditRecord> Records = new List<MeshAuditRecord>();
        }

        [Serializable]
        private sealed class MeshAuditRecord
        {
            public string AssetPath;
            public string FileName;
            public string FileExtension;
            public double FileSizeKB;
            public double FileSizeMB;
            public int MeshCount;
            public long TotalVertices;
            public long TotalTriangles;
            public int TotalSubMeshes;
            public int MaterialCount;
            public bool HasSkinnedMesh;
            public string IsReadable;
            public string ImporterScale;
            public string UsedInPrefabs;
            public string UsedInScenes;
            public int DependencyCount;
            public string OptimizationCategory;
            public int RecommendedTargetTris;
            public string Notes;
        }

        private sealed class DependencyUsage
        {
            public readonly HashSet<string> Prefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Scenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
#endif
