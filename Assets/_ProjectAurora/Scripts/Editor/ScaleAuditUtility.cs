using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoAurora.EditorTools
{
    /// <summary>
    /// Measurement-first tooling for the canonical Beta03 scene. Audit output is
    /// intentionally written to Temp so reviewed documentation remains hand-curated.
    /// </summary>
    public static class ScaleAuditUtility
    {
        private const string CanonicalScene = "Assets/_ProjectAurora/Scenes/Beta03_Principal.unity";
        private const string AuditOutput = "Temp/ScaleAudit_Beta03.json";
        private const string PreviousPlayStartKey = "ProjetoAurora.ScaleAudit.PreviousPlayStart";
        private const string PendingPlayRestoreKey = "ProjetoAurora.ScaleAudit.PendingPlayRestore";

        [Serializable]
        private sealed class AuditData
        {
            public string scenePath;
            public string generatedUtc;
            public float groundY;
            public string groundBasis;
            public float playerHeight;
            public float playerVisualHeight;
            public float[] laneCenters;
            public float inferredLaneSpacing;
            public int totalSceneObjects;
            public List<AuditRecord> records = new List<AuditRecord>();
        }

        [Serializable]
        private sealed class AuditRecord
        {
            public string name;
            public string path;
            public string indexedPath;
            public bool activeInHierarchy;
            public Vector3 worldPosition;
            public Vector3 localPosition;
            public Vector3 localScale;
            public Vector3 lossyScale;
            public bool hasRendererBounds;
            public Vector3 rendererCenter;
            public Vector3 rendererSize;
            public float rendererMinY;
            public float rendererMaxY;
            public bool hasColliderBounds;
            public Vector3 colliderCenter;
            public Vector3 colliderSize;
            public float colliderMinY;
            public float colliderMaxY;
            public string components;
            public string colliders;
            public string scripts;
            public bool prefabInstance;
            public string prefabAssetPath;
            public string issues;
        }

        [Serializable]
        private sealed class CorrectionLog
        {
            public string scenePath;
            public string generatedUtc;
            public List<CorrectionEntry> entries = new List<CorrectionEntry>();
        }

        [Serializable]
        private sealed class CorrectionEntry
        {
            public string path;
            public string action;
            public Vector3 previousWorldPosition;
            public Vector3 newWorldPosition;
            public Vector3 previousLocalScale;
            public Vector3 newLocalScale;
            public float previousRendererMinY;
            public float newRendererMinY;
            public string colliderState;
        }

        [MenuItem("Tools/Project Aurora/Scale Audit/1 - Export Beta03 Audit")]
        public static void ExportBeta03Audit()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != CanonicalScene)
                throw new InvalidOperationException("Abra a cena canônica antes da auditoria: " + CanonicalScene);

            List<Transform> transforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToList();

            float groundY;
            float playerHeight;
            float playerVisualHeight;
            string groundBasis;
            InferPlayerAndGround(transforms, out groundY, out groundBasis, out playerHeight, out playerVisualHeight);

            float[] laneCenters = InferLaneCenters(transforms);
            var data = new AuditData
            {
                scenePath = scene.path,
                generatedUtc = DateTime.UtcNow.ToString("O"),
                groundY = groundY,
                groundBasis = groundBasis,
                playerHeight = playerHeight,
                playerVisualHeight = playerVisualHeight,
                laneCenters = laneCenters,
                inferredLaneSpacing = laneCenters.Length >= 2 ? laneCenters[1] - laneCenters[0] : 0f,
                totalSceneObjects = transforms.Count
            };

            foreach (Transform transform in transforms)
            {
                if (!ShouldAudit(transform))
                    continue;

                Bounds rendererBounds;
                bool hasRenderer = TryGetRendererBounds(transform.gameObject, true, out rendererBounds);
                Bounds colliderBounds;
                bool hasCollider = TryGetColliderBounds(transform.gameObject, true, out colliderBounds);

                Component[] components = transform.GetComponents<Component>();
                MonoBehaviour[] scripts = transform.GetComponents<MonoBehaviour>();
                Collider[] colliders = transform.GetComponents<Collider>();

                var record = new AuditRecord
                {
                    name = transform.name,
                    path = GetPath(transform, false),
                    indexedPath = GetPath(transform, true),
                    activeInHierarchy = transform.gameObject.activeInHierarchy,
                    worldPosition = transform.position,
                    localPosition = transform.localPosition,
                    localScale = transform.localScale,
                    lossyScale = transform.lossyScale,
                    hasRendererBounds = hasRenderer,
                    rendererCenter = hasRenderer ? rendererBounds.center : Vector3.zero,
                    rendererSize = hasRenderer ? rendererBounds.size : Vector3.zero,
                    rendererMinY = hasRenderer ? rendererBounds.min.y : 0f,
                    rendererMaxY = hasRenderer ? rendererBounds.max.y : 0f,
                    hasColliderBounds = hasCollider,
                    colliderCenter = hasCollider ? colliderBounds.center : Vector3.zero,
                    colliderSize = hasCollider ? colliderBounds.size : Vector3.zero,
                    colliderMinY = hasCollider ? colliderBounds.min.y : 0f,
                    colliderMaxY = hasCollider ? colliderBounds.max.y : 0f,
                    components = string.Join(", ", components.Select(c => c == null ? "MissingScript" : c.GetType().Name)),
                    colliders = string.Join("; ", colliders.Select(DescribeCollider)),
                    scripts = string.Join(", ", scripts.Select(s => s == null ? "MissingScript" : s.GetType().Name)),
                    prefabInstance = PrefabUtility.IsPartOfPrefabInstance(transform.gameObject),
                    prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject)
                };
                record.issues = string.Join("; ", DetectIssues(record, groundY));
                data.records.Add(record);
            }

            string absoluteOutput = Path.GetFullPath(AuditOutput);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutput));
            File.WriteAllText(absoluteOutput, JsonUtility.ToJson(data, true));
            Debug.Log("[ScaleAudit] Exportado: " + absoluteOutput + " (" + data.records.Count + " registros)");
        }

        [MenuItem("Tools/Project Aurora/Scale Audit/2 - Apply Reviewed Beta03 Corrections")]
        public static void ApplyReviewedBeta03Corrections()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != CanonicalScene)
                throw new InvalidOperationException("Abra a cena canônica antes de corrigir: " + CanonicalScene);

            var log = new CorrectionLog
            {
                scenePath = scene.path,
                generatedUtc = DateTime.UtcNow.ToString("O")
            };

            // Imported curated visuals use a centered pivot while their gameplay
            // collider is already correctly based on Y=0. Move only the visual child.
            Transform curated = FindByPath("Gameplay Objects/Fase01 - Curated Obstacle Pass");
            if (curated != null)
            {
                foreach (Transform obstacle in curated)
                {
                    if (obstacle.name == "Low Cargo Obstacle" || obstacle.name == "Tall Containment Obstacle")
                        SnapNamedChild(obstacle, "Obstacle Visual", 0f, log,
                            "Apoiar visual importado no piso; collider e X/Z preservados");
                    else if (obstacle.name == "Laser Obstacle")
                        SnapNamedChild(obstacle, "Laser Unit Visual", 0f, log,
                            "Apoiar unidade visual do laser; feixe/trigger preservados");
                }
            }

            // Detailed visual pass is already dimensionally coherent. Correct only
            // the small residual penetrations caused by imported mesh pivots.
            Transform detailed = FindByPath("Fase01 - Detailed Obstacles");
            if (detailed != null)
            {
                foreach (Transform visual in detailed)
                {
                    Bounds bounds;
                    if (!TryGetRendererBounds(visual.gameObject, true, out bounds) || bounds.min.y >= -0.005f)
                        continue;
                    RecordAndSnap(visual.gameObject, 0f, log,
                        "Remover penetração residual do visual detalhado no piso");
                }
            }

            // Initial/tutorial gate: target 7.8m wide x 3.4m high. This spans the
            // three canonical lanes without the previous oversized silhouette.
            Transform tutorialDoor = FindByPath("Gameplay Objects/Tutorial Door");
            if (tutorialDoor != null)
            {
                foreach (Transform child in tutorialDoor)
                {
                    if (child.name != "Aurora_Door_01 Visual") continue;
                    Bounds beforeBounds;
                    if (!TryGetRendererBounds(child.gameObject, true, out beforeBounds)) continue;
                    Vector3 previousPosition = child.position;
                    Vector3 previousScale = child.localScale;
                    Undo.RecordObject(child, "Normalize Beta03 tutorial door visual");
                    Vector3 scale = child.localScale;
                    // The imported GLB root is axis-converted: local Z controls
                    // world width and local X controls world depth.
                    scale.z *= 7.8f / beforeBounds.size.x;
                    scale.y *= 3.4f / beforeBounds.size.y;
                    scale.x *= 0.72f / beforeBounds.size.z;
                    child.localScale = scale;
                    AddEntry(log, child.gameObject,
                        "Normalizar porta inicial para 7.8u x 3.4u", previousPosition, previousScale,
                        beforeBounds.min.y);
                }
                RecordAndSnap(tutorialDoor.gameObject, 0f, log,
                    "Apoiar porta inicial redimensionada no piso");
            }

            // Final gate is proportionate to the terminal corridor, but its centered
            // imported pivot placed half the mesh below ground.
            Transform terminalGate = FindByPath(
                "Fase05 - Terminal Central/Approach Corridor - Three Lanes/Terminal Entry Gate");
            if (terminalGate != null)
                RecordAndSnap(terminalGate.gameObject, 0f, log,
                    "Elevar portão final pela base dos bounds; X/Z/escala preservados");

            Transform setDressing = FindByPath("Fase05 - Terminal Central/Terminal Set Dressing");
            if (setDressing != null)
            {
                string[] groundedProps =
                {
                    "Containment Cargo L", "Containment Cargo R",
                    "Corrupted Laser Bank L", "Corrupted Laser Bank R"
                };
                foreach (string propName in groundedProps)
                {
                    Transform prop = FindDirectChild(setDressing, propName);
                    if (prop != null)
                        RecordAndSnap(prop.gameObject, 0f, log,
                            "Apoiar prop importado do terminal no piso");
                }
            }

            // Interaction volumes intentionally exceed the small panel mesh. Keep
            // their reach and trigger state; only lift the lower half out of the floor.
            string[] panelPaths =
            {
                "Gameplay Objects/Painel de lasers",
                "Gameplay Objects/Painel de porta",
                "Gameplay Objects/Tutorial Panel"
            };
            foreach (string panelPath in panelPaths)
            {
                Transform panel = FindByPath(panelPath);
                if (panel == null) continue;
                BoxCollider trigger = panel.GetComponent<BoxCollider>();
                if (trigger == null || !trigger.isTrigger) continue;

                Vector3 previousPosition = panel.position;
                Vector3 previousScale = panel.localScale;
                Bounds previousBounds = trigger.bounds;
                Undo.RecordObject(trigger, "Ground Beta03 interaction trigger");
                Vector3 center = trigger.center;
                center.y = 0.5f;
                trigger.center = center;
                AddEntry(log, panel.gameObject,
                    "Elevar center.y do trigger para 0.5; Size e isTrigger preservados",
                    previousPosition, previousScale, previousBounds.min.y);
            }

            CreateScaleReference(log);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            string output = Path.GetFullPath("Temp/ScaleCorrections_Beta03.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(log, true));
            Debug.Log("[ScaleAudit] Correções revisadas aplicadas: " + log.entries.Count + ". Log: " + output);
        }

        [MenuItem("Tools/Project Aurora/Scale Audit/3 - Play Beta03 Direct (Temporary)")]
        public static void PlayBeta03DirectForValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != CanonicalScene)
                throw new InvalidOperationException("Abra a cena canônica antes do teste: " + CanonicalScene);

            SceneAsset previous = EditorSceneManager.playModeStartScene;
            SessionState.SetString(PreviousPlayStartKey,
                previous == null ? string.Empty : AssetDatabase.GetAssetPath(previous));
            SessionState.SetBool(PendingPlayRestoreKey, true);
            RegisterPlayModeRestore();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(CanonicalScene);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeRestore()
        {
            EditorApplication.playModeStateChanged -= RestorePlayModeStartScene;
            if (SessionState.GetBool(PendingPlayRestoreKey, false))
                EditorApplication.playModeStateChanged += RestorePlayModeStartScene;
        }

        private static void RestorePlayModeStartScene(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;
            string previousPath = SessionState.GetString(PreviousPlayStartKey, string.Empty);
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previousPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPath);
            SessionState.EraseString(PreviousPlayStartKey);
            SessionState.EraseBool(PendingPlayRestoreKey);
            EditorApplication.playModeStateChanged -= RestorePlayModeStartScene;
            Debug.Log("[ScaleAudit] Play Mode start scene restaurada após validação direta da Beta03.");
        }

        [MenuItem("Tools/Project Aurora/Scale Audit/Snap Selected To Ground By Bounds")]
        public static void SnapSelectedToGround()
        {
            foreach (GameObject gameObject in Selection.gameObjects)
                SnapObjectToGroundByBounds(gameObject, InferGroundY());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Project Aurora/Scale Audit/Fit Selected BoxColliders To Renderers")]
        public static void FitSelectedBoxColliders()
        {
            foreach (GameObject gameObject in Selection.gameObjects)
                FitColliderToRenderer(gameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static bool SnapObjectToGroundByBounds(GameObject gameObject, float groundY)
        {
            Bounds bounds;
            if (!TryGetRendererBounds(gameObject, true, out bounds) &&
                !TryGetColliderBounds(gameObject, true, out bounds))
                return false;

            float deltaY = groundY - bounds.min.y;
            if (Mathf.Abs(deltaY) < 0.002f)
                return false;

            Undo.RecordObject(gameObject.transform, "Snap object to Beta03 ground");
            gameObject.transform.position += Vector3.up * deltaY;
            return true;
        }

        public static bool FitColliderToRenderer(GameObject gameObject)
        {
            BoxCollider collider = gameObject.GetComponent<BoxCollider>();
            Bounds worldBounds;
            if (collider == null || !TryGetRendererBounds(gameObject, true, out worldBounds))
                return false;

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            Vector3 boundsMin = worldBounds.min;
            Vector3 boundsMax = worldBounds.max;
            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            {
                Vector3 corner = new Vector3(x == 0 ? boundsMin.x : boundsMax.x,
                    y == 0 ? boundsMin.y : boundsMax.y,
                    z == 0 ? boundsMin.z : boundsMax.z);
                Vector3 local = gameObject.transform.InverseTransformPoint(corner);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }

            Undo.RecordObject(collider, "Fit BoxCollider to renderer bounds");
            collider.center = (min + max) * 0.5f;
            collider.size = max - min;
            return true;
        }

        private static void SnapNamedChild(Transform parent, string childName, float groundY,
            CorrectionLog log, string action)
        {
            Transform child = FindDirectChild(parent, childName);
            if (child != null) RecordAndSnap(child.gameObject, groundY, log, action);
        }

        private static void RecordAndSnap(GameObject gameObject, float groundY,
            CorrectionLog log, string action)
        {
            Bounds beforeBounds;
            if (!TryGetRendererBounds(gameObject, true, out beforeBounds)) return;
            if (Mathf.Abs(groundY - beforeBounds.min.y) < 0.002f) return;
            Vector3 previousPosition = gameObject.transform.position;
            Vector3 previousScale = gameObject.transform.localScale;
            if (!SnapObjectToGroundByBounds(gameObject, groundY)) return;
            AddEntry(log, gameObject, action, previousPosition, previousScale, beforeBounds.min.y);
        }

        private static void AddEntry(CorrectionLog log, GameObject gameObject, string action,
            Vector3 previousPosition, Vector3 previousScale, float previousRendererMinY)
        {
            Bounds newBounds;
            bool hasNewBounds = TryGetRendererBounds(gameObject, true, out newBounds);
            Collider collider = gameObject.GetComponent<Collider>();
            log.entries.Add(new CorrectionEntry
            {
                path = GetPath(gameObject.transform, true),
                action = action,
                previousWorldPosition = previousPosition,
                newWorldPosition = gameObject.transform.position,
                previousLocalScale = previousScale,
                newLocalScale = gameObject.transform.localScale,
                previousRendererMinY = previousRendererMinY,
                newRendererMinY = hasNewBounds ? newBounds.min.y : 0f,
                colliderState = collider == null ? "sem collider direto" : DescribeCollider(collider)
            });
        }

        private static void CreateScaleReference(CorrectionLog log)
        {
            if (FindByPath("ScaleReference_Beta03") != null) return;

            GameObject root = new GameObject("ScaleReference_Beta03");
            Undo.RegisterCreatedObjectUndo(root, "Create Beta03 scale reference");
            root.tag = "EditorOnly";

            CreateReferenceCube(root.transform, "Reference_PlayerHeight", new Vector3(-4.5f, 1.025f, 0f),
                new Vector3(0.15f, 2.05f, 0.15f));
            CreateReferenceCube(root.transform, "Reference_GroundPlane", new Vector3(0f, -0.01f, 6f),
                new Vector3(10f, 0.02f, 12f));
            CreateReferenceCube(root.transform, "Reference_Lane_Left", new Vector3(-3f, 0.01f, 6f),
                new Vector3(0.04f, 0.02f, 12f));
            CreateReferenceCube(root.transform, "Reference_Lane_Center", new Vector3(0f, 0.01f, 6f),
                new Vector3(0.04f, 0.02f, 12f));
            CreateReferenceCube(root.transform, "Reference_Lane_Right", new Vector3(3f, 0.01f, 6f),
                new Vector3(0.04f, 0.02f, 12f));
            root.SetActive(false);

            log.entries.Add(new CorrectionEntry
            {
                path = "ScaleReference_Beta03[novo]",
                action = "Criar referência EditorOnly desativada: player 2.05u, piso Y=0 e faixas X=-3/0/3",
                previousWorldPosition = Vector3.zero,
                newWorldPosition = root.transform.position,
                previousLocalScale = Vector3.zero,
                newLocalScale = root.transform.localScale,
                colliderState = "sem colliders; root desativado"
            });
        }

        private static void CreateReferenceCube(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.tag = "EditorOnly";
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = scale;
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        private static Transform FindByPath(string path)
        {
            string[] parts = path.Split('/');
            Transform current = SceneManager.GetActiveScene().GetRootGameObjects()
                .Select(go => go.transform).FirstOrDefault(t => t.name == parts[0]);
            for (int i = 1; current != null && i < parts.Length; i++)
                current = FindDirectChild(current, parts[i]);
            return current;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
                if (child.name == name) return child;
            return null;
        }

        private static void InferPlayerAndGround(List<Transform> transforms, out float groundY,
            out string basis, out float playerHeight, out float playerVisualHeight)
        {
            CharacterController controller = transforms.Select(t => t.GetComponent<CharacterController>())
                .FirstOrDefault(c => c != null);
            if (controller != null)
            {
                float scaleY = Mathf.Abs(controller.transform.lossyScale.y);
                playerHeight = controller.height * scaleY;
                groundY = controller.transform.TransformPoint(controller.center).y - playerHeight * 0.5f;
                Bounds visualBounds;
                playerVisualHeight = TryGetRendererBounds(controller.gameObject, true, out visualBounds)
                    ? visualBounds.size.y : playerHeight;
                basis = "Base do CharacterController de " + GetPath(controller.transform, false);
                return;
            }

            groundY = 0f;
            basis = "Fallback Y=0 (CharacterController não encontrado)";
            playerHeight = 1.8f;
            playerVisualHeight = 1.8f;
        }

        private static float[] InferLaneCenters(List<Transform> transforms)
        {
            var values = transforms
                .Where(t => GetPath(t, false).StartsWith("Gameplay Objects/", StringComparison.Ordinal))
                .Where(t => t.GetComponent<Collider>() != null)
                .Select(t => Mathf.Round(t.position.x * 10f) / 10f)
                .Where(x => Mathf.Abs(x) <= 10f)
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .OrderBy(x => x)
                .ToArray();
            return values;
        }

        private static float InferGroundY()
        {
            List<Transform> transforms = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToList();
            float groundY, playerHeight, playerVisualHeight;
            string basis;
            InferPlayerAndGround(transforms, out groundY, out basis, out playerHeight, out playerVisualHeight);
            return groundY;
        }

        private static bool ShouldAudit(Transform transform)
        {
            string path = GetPath(transform, false);
            string lower = path.ToLowerInvariant();
            bool inScope = path.StartsWith("Gameplay Objects/", StringComparison.Ordinal) ||
                           path.StartsWith("Fase01 - Detailed Obstacles/", StringComparison.Ordinal) ||
                           path.StartsWith("Fase 01 - Aurora Research Corridor/", StringComparison.Ordinal) ||
                           path.StartsWith("Fase05 - Terminal Central/", StringComparison.Ordinal) ||
                           path.StartsWith("GameplayInteractions_Examples/", StringComparison.Ordinal) ||
                           path.StartsWith("Dr. Elias - Player", StringComparison.Ordinal);
            string[] keywords = { "door", "porta", "box", "caixa", "block", "bloco", "laser", "lazer",
                "cable", "cabo", "wire", "fio", "panel", "painel", "terminal", "obstacle", "barrier",
                "tutorial", "ground", "floor", "piso", "lane", "corridor", "corredor", "gate" };
            bool keyword = keywords.Any(lower.Contains);
            bool functional = transform.GetComponents<MonoBehaviour>().Any(script => script != null &&
                keywords.Any(k => script.GetType().Name.ToLowerInvariant().Contains(k)));
            bool measurable = transform.GetComponent<Renderer>() != null || transform.GetComponent<Collider>() != null;
            return (inScope && measurable) || keyword || functional;
        }

        private static List<string> DetectIssues(AuditRecord record, float groundY)
        {
            var issues = new List<string>();
            Vector3 scale = Abs(record.lossyScale);
            float minScale = Mathf.Max(0.0001f, Mathf.Min(scale.x, Mathf.Min(scale.y, scale.z)));
            float maxScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            string lower = record.path.ToLowerInvariant();
            bool expectedThin = lower.Contains("floor") || lower.Contains("ground") || lower.Contains("piso") ||
                                lower.Contains("laser") || lower.Contains("cable") || lower.Contains("cabo") ||
                                lower.Contains("wire") || lower.Contains("fio");

            if (maxScale / minScale > 6f)
                issues.Add("escala mundial muito não uniforme (razão " + (maxScale / minScale).ToString("F1") + ")");
            if (!expectedThin && scale.y < Mathf.Max(scale.x, scale.z) * 0.18f)
                issues.Add("escala Y potencialmente achatada");
            if (record.hasRendererBounds && record.rendererMinY < groundY - 0.05f)
                issues.Add("visual abaixo do piso em " + (groundY - record.rendererMinY).ToString("F3") + "u");
            if (record.hasColliderBounds && record.colliderMinY < groundY - 0.05f)
                issues.Add("collider abaixo do piso em " + (groundY - record.colliderMinY).ToString("F3") + "u");
            if (record.hasRendererBounds && record.hasColliderBounds)
            {
                float visualVolume = Mathf.Max(0.0001f, record.rendererSize.x * record.rendererSize.y * record.rendererSize.z);
                float colliderVolume = Mathf.Max(0.0001f, record.colliderSize.x * record.colliderSize.y * record.colliderSize.z);
                float ratio = colliderVolume / visualVolume;
                if (ratio > 2.5f || ratio < 0.4f)
                    issues.Add("volume collider/visual discrepante (razão " + ratio.ToString("F2") + ")");
            }
            if ((lower.Contains("door") || lower.Contains("porta") || lower.Contains("gate")) && record.hasRendererBounds)
            {
                if (record.rendererSize.y < 2.2f) issues.Add("porta visualmente baixa");
                if (record.rendererSize.x > record.rendererSize.y * 1.7f) issues.Add("porta larga em relação à altura");
            }
            if ((record.scripts.Contains("Interactable") || lower.Contains("panel") || lower.Contains("painel")) &&
                record.worldPosition.y < groundY - 0.05f)
                issues.Add("interativo com pivot abaixo do piso");
            return issues;
        }

        private static string DescribeCollider(Collider collider)
        {
            string result = collider.GetType().Name + " trigger=" + collider.isTrigger + " enabled=" + collider.enabled +
                            " boundsCenter=" + collider.bounds.center.ToString("F3") +
                            " boundsSize=" + collider.bounds.size.ToString("F3");
            BoxCollider box = collider as BoxCollider;
            if (box != null) result += " localCenter=" + box.center.ToString("F3") + " localSize=" + box.size.ToString("F3");
            CapsuleCollider capsule = collider as CapsuleCollider;
            if (capsule != null) result += " localCenter=" + capsule.center.ToString("F3") +
                                          " radius=" + capsule.radius.ToString("F3") + " height=" + capsule.height.ToString("F3");
            return result;
        }

        private static bool TryGetRendererBounds(GameObject gameObject, bool includeChildren, out Bounds bounds)
        {
            Renderer[] renderers = includeChildren ? gameObject.GetComponentsInChildren<Renderer>(true) : gameObject.GetComponents<Renderer>();
            renderers = renderers.Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            if (renderers.Length == 0) { bounds = default(Bounds); return false; }
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static bool TryGetColliderBounds(GameObject gameObject, bool includeChildren, out Bounds bounds)
        {
            Collider[] colliders = includeChildren ? gameObject.GetComponentsInChildren<Collider>(true) : gameObject.GetComponents<Collider>();
            colliders = colliders.Where(c => c.enabled).ToArray();
            if (colliders.Length == 0) { bounds = default(Bounds); return false; }
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
            return true;
        }

        private static string GetPath(Transform transform, bool indexed)
        {
            var parts = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                parts.Add(indexed ? current.name + "[" + current.GetSiblingIndex() + "]" : current.name);
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
