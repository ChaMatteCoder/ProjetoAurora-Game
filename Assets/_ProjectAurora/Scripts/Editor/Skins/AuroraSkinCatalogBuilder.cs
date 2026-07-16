#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ProjectAurora.Customization.Skins;
using UnityEditor;
using UnityEngine;

namespace ProjectAurora.Editor.Skins
{
    public static class AuroraSkinCatalogBuilder
    {
        public const string SkinArtFolder = "Assets/_ProjectAurora/Art/Skin";
        public const string DataFolder = "Assets/_ProjectAurora/Data/Skins";
        public const string DefinitionsFolder = DataFolder + "/Definitions";
        public const string CatalogPath = DataFolder + "/AuroraSkinCatalog.asset";
        public const string DefaultGameplayPrefabPath =
            "Assets/_ProjectAurora/Characters/DrElias/Prefabs/DrElias_AnimatedVisual.prefab";
        public const string DefaultPreviewPrefabPath =
            "Assets/_ProjectAurora/Prefabs/UI/Menu/PF_DrElias_Default_SkinPreview.prefab";
        public const string PreviewLayerName = "SkinPreview";

        private static readonly string[] ModelSearchFolders =
        {
            "Assets/_ProjectAurora/Characters",
            "Assets/_ProjectAurora/Prefabs"
        };

        [MenuItem("Tools/Projeto Aurora/Skins/Rebuild Skin Catalog")]
        public static void RebuildSkinCatalogFromMenu()
        {
            AuroraSkinCatalog catalog = RebuildSkinCatalog();
            if (catalog != null)
            {
                Selection.activeObject = catalog;
            }
        }

        public static AuroraSkinCatalog RebuildSkinCatalog()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(DefinitionsFolder);
            EnsureFolder("Assets/_ProjectAurora/Prefabs/UI/Menu");

            int previewLayer = EnsureSkinPreviewLayer();
            GameObject defaultPreviewPrefab = BuildDefaultPreviewPrefab(previewLayer);
            GameObject defaultGameplayPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DefaultGameplayPrefabPath);

            string[] splashPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { SkinArtFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith("Splash_", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => SortOrder(DeriveId(path)))
                .ThenBy(path => path, StringComparer.Ordinal)
                .ToArray();

            var definitions = new List<AuroraSkinDefinition>();
            var modelGaps = new List<string>();
            for (int i = 0; i < splashPaths.Length; i++)
            {
                string splashPath = splashPaths[i];
                ConfigureSplashImporter(splashPath);
                Sprite splash = AssetDatabase.LoadAssetAtPath<Sprite>(splashPath);
                string id = DeriveId(splashPath);
                string definitionPath = DefinitionsFolder + "/Skin_" + SafeAssetName(id) + ".asset";
                AuroraSkinDefinition definition = AssetDatabase.LoadAssetAtPath<AuroraSkinDefinition>(definitionPath);
                bool created = definition == null;
                if (created)
                {
                    definition = ScriptableObject.CreateInstance<AuroraSkinDefinition>();
                    AssetDatabase.CreateAsset(definition, definitionPath);
                }

                bool isDefault = id == "default";
                GameObject exactMatch = isDefault ? defaultPreviewPrefab : FindExactModel(id);
                GameObject previewPrefab = exactMatch != null ? exactMatch : definition.PreviewPrefab;
                GameObject gameplayPrefab = isDefault ? defaultGameplayPrefab : definition.GameplayPrefab;
                if (!isDefault && exactMatch != null &&
                    string.Equals(Path.GetExtension(AssetDatabase.GetAssetPath(exactMatch)), ".prefab", StringComparison.OrdinalIgnoreCase) &&
                    gameplayPrefab == null)
                {
                    gameplayPrefab = exactMatch;
                }

                if (previewPrefab == null)
                {
                    modelGaps.Add(id);
                }

                Vector3 positionOffset = created ? Vector3.zero : definition.PreviewPositionOffset;
                Vector3 rotationOffset = created
                    ? (id == "default" ? new Vector3(0f, 90f, 0f) : Vector3.zero)
                    : definition.PreviewRotationOffset;
                float scaleMultiplier = created ? 1f : definition.PreviewScaleMultiplier;
                float cameraDistance = created ? 0f : definition.PreviewCameraDistance;
                Color backgroundTint = created
                    ? GetBackgroundTint(id)
                    : definition.PreviewBackgroundTint;

                definition.ConfigureForEditor(
                    id,
                    GetDisplayName(id),
                    GetDescription(id),
                    splash,
                    previewPrefab,
                    gameplayPrefab,
                    id == "default" || id == "brazil",
                    GetFuturePrice(id),
                    id,
                    isDefault,
                    positionOffset,
                    rotationOffset,
                    scaleMultiplier,
                    cameraDistance,
                    backgroundTint);
                EditorUtility.SetDirty(definition);
                definitions.Add(definition);
            }

            AuroraSkinCatalog catalog = AssetDatabase.LoadAssetAtPath<AuroraSkinCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AuroraSkinCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.ConfigureForEditor(definitions);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            List<string> issues = catalog.CollectValidationIssues();
            for (int i = 0; i < issues.Count; i++)
            {
                Debug.LogWarning("[AuroraSkinCatalog] " + issues[i]);
            }

            Debug.Log("[AuroraSkinCatalog] Rebuild concluido: splashArts=" + splashPaths.Length +
                      ", skins=" + definitions.Count + ", modelosAusentes=" + modelGaps.Count +
                      ", issues=" + issues.Count + ". Sem modelo: " +
                      (modelGaps.Count == 0 ? "nenhuma" : string.Join(", ", modelGaps)) + ".");
            return catalog;
        }

        public static int EnsureSkinPreviewLayer()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == PreviewLayerName)
                {
                    return i;
                }
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = PreviewLayerName;
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return i;
                }
            }

            throw new InvalidOperationException("Nao ha layer livre para " + PreviewLayerName + ".");
        }

        private static GameObject BuildDefaultPreviewPrefab(int layer)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultGameplayPrefabPath);
            if (source == null)
            {
                Debug.LogError("[AuroraSkinCatalog] Modelo default nao encontrado: " + DefaultGameplayPrefabPath);
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[AuroraSkinCatalog] Falha ao instanciar o modelo default para preview.");
                return null;
            }

            try
            {
                instance.name = "PF_DrElias_Default_SkinPreview";
                SanitizePreviewPrefab(instance, layer);
                return PrefabUtility.SaveAsPrefabAsset(instance, DefaultPreviewPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void SanitizePreviewPrefab(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
                child.gameObject.tag = "Untagged";
            }

            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.enabled = false;
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
            foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }
        }

        private static void ConfigureSplashImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool alpha = importer.DoesSourceTextureHaveAlpha();
            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           importer.mipmapEnabled ||
                           importer.filterMode != FilterMode.Bilinear ||
                           importer.maxTextureSize != 2048 ||
                           importer.textureCompression != TextureImporterCompression.CompressedHQ ||
                           importer.alphaIsTransparency != alpha;
            if (!changed)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        private static GameObject FindExactModel(string id)
        {
            string pascal = string.Concat(id.Split('-').Select(part =>
                part.Length == 0 ? string.Empty : char.ToUpperInvariant(part[0]) + part.Substring(1)));
            var expected = new HashSet<string>(StringComparer.Ordinal)
            {
                NormalizeAssetStem(id),
                NormalizeAssetStem("Skin_" + pascal),
                NormalizeAssetStem("PF_DrElias_" + pascal),
                NormalizeAssetStem("Preview_" + pascal),
                NormalizeAssetStem("DrElias_" + pascal)
            };

            string[] guids = AssetDatabase.FindAssets("t:GameObject", ModelSearchFolders);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string stem = NormalizeAssetStem(Path.GetFileNameWithoutExtension(path));
                if (expected.Contains(stem))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            return null;
        }

        private static string DeriveId(string splashPath)
        {
            string stem = Path.GetFileNameWithoutExtension(splashPath).Substring("Splash_".Length).Trim();
            stem = Regex.Replace(stem, @"(?i)\s*Dr[.\s_-]*Elias\s*$", string.Empty).Trim();
            if (stem.Length == 0)
            {
                return "default";
            }

            string normalized = stem.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            bool separatorPending = false;
            for (int i = 0; i < normalized.Length; i++)
            {
                char character = normalized[i];
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    if (separatorPending && builder.Length > 0) builder.Append('-');
                    builder.Append(char.ToLowerInvariant(character));
                    separatorPending = false;
                }
                else
                {
                    separatorPending = true;
                }
            }

            return builder.Length == 0 ? "default" : builder.ToString();
        }

        private static string NormalizeAssetStem(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]", string.Empty);
        }

        private static string SafeAssetName(string id)
        {
            return string.Join("_", id.Split('-').Select(part =>
                part.Length == 0 ? string.Empty : char.ToUpperInvariant(part[0]) + part.Substring(1)));
        }

        private static int SortOrder(string id)
        {
            switch (id)
            {
                case "default": return 0;
                case "brazil": return 1;
                case "aurora-ceremonial": return 2;
                case "celestia-theme": return 3;
                case "corrupted": return 4;
                case "post-collapse-survivor": return 5;
                default: return 100;
            }
        }

        private static string GetDisplayName(string id)
        {
            switch (id)
            {
                case "default": return "Dr. Elias";
                case "brazil": return "Brasil";
                case "aurora-ceremonial": return "Cerimonial Aurora";
                case "celestia-theme": return "Tema CelestIA";
                case "corrupted": return "Corrompido";
                case "post-collapse-survivor": return "Sobrevivente Pós-Colapso";
                default: return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('-', ' '));
            }
        }

        private static string GetDescription(string id)
        {
            switch (id)
            {
                case "default": return "Traje científico original usado pelo Dr. Elias durante a falha de contenção.";
                case "brazil": return "Variação visual inspirada nas cores brasileiras. Modelo 3D ainda não disponível.";
                case "aurora-ceremonial": return "Uniforme cerimonial do Projeto Aurora. Conteúdo visual em preparação.";
                case "celestia-theme": return "Traje conceitual vinculado à identidade da CelestIA. Modelo pendente.";
                case "corrupted": return "Versão afetada pela instabilidade da contenção. Modelo pendente.";
                case "post-collapse-survivor": return "Equipamento improvisado para o período após o colapso. Modelo pendente.";
                default: return "Skin do Dr. Elias em preparação.";
            }
        }

        private static int GetFuturePrice(string id)
        {
            switch (id)
            {
                case "default": return 0;
                case "brazil": return 75;
                case "aurora-ceremonial": return 120;
                case "celestia-theme": return 150;
                case "corrupted": return 180;
                case "post-collapse-survivor": return 200;
                default: return 100;
            }
        }

        private static Color GetBackgroundTint(string id)
        {
            switch (id)
            {
                case "brazil": return new Color(0.01f, 0.055f, 0.035f, 1f);
                case "corrupted": return new Color(0.055f, 0.008f, 0.018f, 1f);
                case "aurora-ceremonial": return new Color(0.035f, 0.028f, 0.008f, 1f);
                default: return new Color(0.004f, 0.018f, 0.03f, 1f);
            }
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
