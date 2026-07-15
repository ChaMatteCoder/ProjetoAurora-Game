#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class AuroraCoinPlacementTools : EditorWindow
{
    private const string CanonicalScene = "Assets/_ProjectAurora/Scenes/Beta03_Principal.unity";
    private const string CoinPrefabPath = "Assets/_ProjectAurora/Prefabs/Collectibles/PF_Aurora_HoloCoin.prefab";
    private const string CollectiblesRootPath = "Gameplay_Collectibles/AuroraCoins";
    private const float MinimumCoinSpacing = 4f;
    private const float MinimumDataFileDistance = 12f;
    private const float TrackHalfWidth = 4.5f;

    private float spacing = 7f;
    private float laneX;
    private float height = 1.1f;
    private int count = 5;
    private string parentSector = "SectorA_Coins";
    private float orientationDegrees;
    private float arcDegrees = 60f;

    [MenuItem("Tools/Projeto Aurora/Collectibles/Aurora Coin/Placement Tools")]
    public static void OpenWindow()
    {
        GetWindow<AuroraCoinPlacementTools>("Aurora Coin");
    }

    [MenuItem("Tools/Projeto Aurora/Collectibles/Aurora Coin/Install Round 1")]
    public static void InstallRound1()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != CanonicalScene)
        {
            Debug.LogError("[AuroraCoinPlacement] Abra a cena canonica antes da instalacao: " + CanonicalScene);
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[AuroraCoinPlacement] Prefab nao encontrado: " + CoinPrefabPath);
            return;
        }

        Transform root = FindInActiveScene(CollectiblesRootPath);
        if (root != null && root.GetComponentsInChildren<AuroraCoinCollectible>(true).Length > 0)
        {
            Debug.LogError("[AuroraCoinPlacement] Round 1 ja existe. A instalacao nao sobrescreve ajustes manuais.");
            return;
        }

        root = EnsureHierarchy(CollectiblesRootPath);
        PlacementGroup[] groups = BuildRound1Groups();
        int created = 0;
        for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
        {
            PlacementGroup group = groups[groupIndex];
            Transform parent = EnsureChild(root, group.ParentName);
            for (int i = 0; i < group.Positions.Length; i++)
            {
                GameObject coin = CreateCoin(prefab, parent, group.Positions[i]);
                coin.name = group.Prefix + (i + 1).ToString("000");
                created++;
            }
        }

        EnsureHudCounter();
        AuroraCoinEconomyEditorTools.CreateOrUpdateTestCatalog();
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        ValidationResult validation = ValidatePlacement(false);
        Debug.Log("[AuroraCoinPlacement] Round 1 instalado: " + created +
                  " moedas em 6 grupos. Validacao: " + validation.ErrorCount +
                  " erros, " + validation.WarningCount + " avisos.");
        Selection.activeTransform = root;
    }

    [MenuItem("Tools/Projeto Aurora/Collectibles/Aurora Coin/Validate Coin Placement")]
    public static void ValidateFromMenu()
    {
        ValidationResult result = ValidatePlacement(true);
        if (result.ErrorCount == 0 && result.WarningCount == 0)
        {
            Debug.Log("[AuroraCoinPlacement] PASS: " + result.CoinCount + " moedas validas.");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Aurora Coin Placement", EditorStyles.boldLabel);
        spacing = EditorGUILayout.FloatField("Spacing", Mathf.Max(0.5f, spacing));
        laneX = EditorGUILayout.FloatField("Lane X", laneX);
        height = EditorGUILayout.FloatField("Height", height);
        count = EditorGUILayout.IntField("Count", Mathf.Clamp(count, 1, 40));
        parentSector = EditorGUILayout.TextField("Parent sector", parentSector);
        orientationDegrees = EditorGUILayout.FloatField("Orientation", orientationDegrees);
        arcDegrees = EditorGUILayout.Slider("Arc angle", arcDegrees, 10f, 180f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create Coin At Scene View")) CreateAtSceneView();
        if (GUILayout.Button("Create Coin At Selected Position")) CreateAtSelectedPosition();
        if (GUILayout.Button("Create Coin Line")) CreateLine();
        if (GUILayout.Button("Align Selected Coins To Ground")) AlignSelectedToGround();
        if (GUILayout.Button("Distribute Selected Coins In Line")) DistributeSelectedInLine();
        if (GUILayout.Button("Distribute Selected Coins In Arc")) DistributeSelectedInArc();
        if (GUILayout.Button("Rename Selected Coins Sequentially")) RenameSelectedSequentially();
        if (GUILayout.Button("Validate Coin Placement")) ValidateFromMenu();
    }

    private void CreateAtSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null)
        {
            Debug.LogWarning("[AuroraCoinPlacement] Scene View indisponivel.");
            return;
        }

        Ray ray = sceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 point;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            point = hit.point + Vector3.up * height;
        }
        else
        {
            point = ray.GetPoint(10f);
            point.y = height;
        }

        SelectCreated(CreateManualCoin(point));
    }

    private void CreateAtSelectedPosition()
    {
        Vector3 point = Selection.activeTransform == null ? new Vector3(laneX, height, 0f) : Selection.activeTransform.position;
        point.y = height;
        SelectCreated(CreateManualCoin(point));
    }

    private void CreateLine()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        Transform parent = EnsureHierarchy(CollectiblesRootPath + "/" + parentSector);
        Vector3 start = Selection.activeTransform == null ? new Vector3(laneX, height, 0f) : Selection.activeTransform.position;
        start.x = laneX;
        start.y = height;
        Vector3 direction = Quaternion.Euler(0f, orientationDegrees, 0f) * Vector3.forward;
        var created = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject coin = CreateCoin(prefab, parent, start + direction * (spacing * i));
            created.Add(coin);
        }

        Selection.objects = created.ToArray();
        RenameSelectedSequentially();
        MarkSceneDirty();
    }

    private void AlignSelectedToGround()
    {
        foreach (AuroraCoinCollectible coin in GetSelectedCoins())
        {
            Vector3 origin = coin.transform.position + Vector3.up * 5f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            Undo.RecordObject(coin.transform, "Align Aurora Coin To Ground");
            Vector3 position = coin.transform.position;
            position.y = hit.point.y + height;
            coin.transform.position = position;
        }

        MarkSceneDirty();
    }

    private void DistributeSelectedInLine()
    {
        AuroraCoinCollectible[] coins = GetSelectedCoins();
        if (coins.Length < 2) return;
        Vector3 start = coins[0].transform.position;
        start.x = laneX;
        start.y = height;
        Vector3 direction = Quaternion.Euler(0f, orientationDegrees, 0f) * Vector3.forward;
        for (int i = 0; i < coins.Length; i++)
        {
            Undo.RecordObject(coins[i].transform, "Distribute Aurora Coins In Line");
            coins[i].transform.position = start + direction * (spacing * i);
        }

        MarkSceneDirty();
    }

    private void DistributeSelectedInArc()
    {
        AuroraCoinCollectible[] coins = GetSelectedCoins();
        if (coins.Length < 2) return;
        Vector3 center = coins[0].transform.position;
        float radius = Mathf.Max(spacing, spacing * (coins.Length - 1) / Mathf.Deg2Rad / Mathf.Max(arcDegrees, 1f));
        float startAngle = orientationDegrees - arcDegrees * 0.5f;
        for (int i = 0; i < coins.Length; i++)
        {
            float t = i / (float)(coins.Length - 1);
            float angle = Mathf.Lerp(startAngle, startAngle + arcDegrees, t) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            Undo.RecordObject(coins[i].transform, "Distribute Aurora Coins In Arc");
            coins[i].transform.position = new Vector3(center.x + offset.x, height, center.z + offset.z);
        }

        MarkSceneDirty();
    }

    private static void RenameSelectedSequentially()
    {
        AuroraCoinCollectible[] coins = GetSelectedCoins();
        if (coins.Length == 0) return;
        string prefix = PrefixForParent(coins[0].transform.parent == null ? string.Empty : coins[0].transform.parent.name);
        for (int i = 0; i < coins.Length; i++)
        {
            Undo.RecordObject(coins[i].gameObject, "Rename Aurora Coins");
            coins[i].name = prefix + (i + 1).ToString("000");
        }

        MarkSceneDirty();
    }

    public static ValidationResult ValidatePlacement(bool logIssues)
    {
        Physics.SyncTransforms();
        AuroraCoinCollectible[] coins = UnityEngine.Object.FindObjectsByType<AuroraCoinCollectible>(FindObjectsInactive.Include);
        DataFileCollectible[] dataFiles = UnityEngine.Object.FindObjectsByType<DataFileCollectible>(FindObjectsInactive.Include);
        var result = new ValidationResult(coins.Length);

        for (int i = 0; i < coins.Length; i++)
        {
            AuroraCoinCollectible coin = coins[i];
            ValidateComponents(coin, result);
            ValidateTransformAndTrack(coin, result);
            ValidateGroundAndSolids(coin, result);
            ValidateDataFileDistance(coin, dataFiles, result);

            for (int j = i + 1; j < coins.Length; j++)
            {
                if (Vector3.Distance(coin.transform.position, coins[j].transform.position) < MinimumCoinSpacing)
                {
                    result.Warn(coin.name + " esta muito proxima de " + coins[j].name + ".");
                }
            }
        }

        if (logIssues)
        {
            for (int i = 0; i < result.Issues.Count; i++)
            {
                ValidationIssue issue = result.Issues[i];
                if (issue.IsError) Debug.LogError("[AuroraCoinPlacement] " + issue.Message);
                else Debug.LogWarning("[AuroraCoinPlacement] " + issue.Message);
            }

            Debug.Log("[AuroraCoinPlacement] Resultado: coins=" + result.CoinCount +
                      ", errors=" + result.ErrorCount + ", warnings=" + result.WarningCount + ".");
        }

        return result;
    }

    private static void ValidateComponents(AuroraCoinCollectible coin, ValidationResult result)
    {
        Collider trigger = coin.GetComponent<Collider>();
        Rigidbody body = coin.GetComponent<Rigidbody>();
        AuroraCoinVisualController visual = coin.GetComponent<AuroraCoinVisualController>();
        if (trigger == null || !trigger.isTrigger) result.Error(coin.name + " sem trigger valido.");
        if (body == null || !body.isKinematic) result.Error(coin.name + " sem Rigidbody cinematico.");
        if (visual == null) result.Error(coin.name + " sem AuroraCoinVisualController.");
        if (visual != null)
        {
            SerializedObject serialized = new SerializedObject(visual);
            SerializedProperty visualRoot = serialized.FindProperty("visualRoot");
            if (visualRoot == null || visualRoot.objectReferenceValue == null)
            {
                result.Error(coin.name + " sem visualRoot configurado.");
            }
        }
    }

    private static void ValidateTransformAndTrack(AuroraCoinCollectible coin, ValidationResult result)
    {
        Vector3 scale = coin.transform.localScale;
        if ((scale - Vector3.one).sqrMagnitude > 0.0004f)
        {
            result.Warn(coin.name + " usa scale diferente de 1: " + scale + ".");
        }

        if (Mathf.Abs(coin.transform.position.x) > TrackHalfWidth)
        {
            result.Error(coin.name + " esta fora da pista em x=" + coin.transform.position.x.ToString("0.00") + ".");
        }

        if (!HasAncestor(coin.transform, "AuroraCoins"))
        {
            result.Error(coin.name + " nao esta sob Gameplay_Collectibles/AuroraCoins.");
        }
    }

    private static void ValidateGroundAndSolids(AuroraCoinCollectible coin, ValidationResult result)
    {
        Vector3 position = coin.transform.position;
        if (Physics.Raycast(position + Vector3.up * 0.25f, Vector3.down, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Ignore))
        {
            float clearance = position.y - hit.point.y;
            if (clearance < 0.2f) result.Error(coin.name + " esta abaixo ou dentro do chao.");
            else if (clearance > 2.5f) result.Warn(coin.name + " esta alta demais sobre a superficie.");
        }
        else
        {
            result.Warn(coin.name + " nao encontrou chao em ate 4 metros.");
        }

        Collider[] overlaps = Physics.OverlapSphere(position, 0.22f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Transform overlapTransform = overlaps[i].transform;
            if (overlapTransform.IsChildOf(coin.transform) || coin.transform.IsChildOf(overlapTransform))
            {
                continue;
            }

            result.Error(coin.name + " intersecta collider solido: " + overlaps[i].name + ".");
            break;
        }
    }

    private static void ValidateDataFileDistance(
        AuroraCoinCollectible coin,
        DataFileCollectible[] dataFiles,
        ValidationResult result)
    {
        for (int i = 0; i < dataFiles.Length; i++)
        {
            float distance = Vector3.Distance(coin.transform.position, dataFiles[i].transform.position);
            if (distance < MinimumDataFileDistance)
            {
                result.Warn(coin.name + " esta a " + distance.ToString("0.0") +
                            "m de " + dataFiles[i].name + " (rota de DataFile).");
            }
        }
    }

    private GameObject CreateManualCoin(Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        Transform parent = EnsureHierarchy(CollectiblesRootPath + "/" + parentSector);
        GameObject coin = CreateCoin(prefab, parent, position);
        MarkSceneDirty();
        return coin;
    }

    private static GameObject CreateCoin(GameObject prefab, Transform parent, Vector3 position)
    {
        GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(coin, "Create Aurora Coin");
        coin.transform.position = position;
        coin.transform.rotation = Quaternion.identity;
        coin.transform.localScale = Vector3.one;
        return coin;
    }

    private static void EnsureHudCounter()
    {
        Transform hudCanvas = FindInActiveScene("HUD Canvas");
        if (hudCanvas == null)
        {
            Debug.LogError("[AuroraCoinPlacement] HUD Canvas nao encontrada.");
            return;
        }

        Transform existing = hudCanvas.Find("HUD_AuroraCoinCounter");
        if (existing != null)
        {
            return;
        }

        TMP_FontAsset font = null;
        AuroraGameplayHUDController hud = hudCanvas.GetComponent<AuroraGameplayHUDController>();
        if (hud != null && hud.distanceValueText != null)
        {
            font = hud.distanceValueText.font;
        }
        if (font == null) font = TMP_Settings.defaultFontAsset;

        RectTransform card = CreateUiRect("HUD_AuroraCoinCounter", hudCanvas, hudCanvas.gameObject.layer);
        card.anchorMin = card.anchorMax = new Vector2(1f, 1f);
        card.pivot = new Vector2(1f, 1f);
        card.anchoredPosition = new Vector2(-42f, -176f);
        card.sizeDelta = new Vector2(300f, 72f);
        Image background = card.gameObject.AddComponent<Image>();
        background.color = new Color(0.004f, 0.032f, 0.052f, 0.94f);
        background.raycastTarget = false;
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.05f, 0.88f, 1f, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);
        card.gameObject.AddComponent<CanvasGroup>();

        RectTransform accent = CreateUiRect("AccentTop", card, card.gameObject.layer);
        SetRect(accent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f));
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = new Color(0.05f, 0.88f, 1f, 1f);
        accentImage.raycastTarget = false;

        RectTransform icon = CreateUiRect("AuroraSymbol", card, card.gameObject.layer);
        SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 7f), new Vector2(38f, 38f));
        icon.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image iconGlow = icon.gameObject.AddComponent<Image>();
        iconGlow.color = new Color(0.05f, 0.88f, 1f, 0.95f);
        iconGlow.raycastTarget = false;
        RectTransform iconInner = CreateUiRect("Inner", icon, card.gameObject.layer);
        iconInner.anchorMin = Vector2.zero;
        iconInner.anchorMax = Vector2.one;
        iconInner.offsetMin = new Vector2(5f, 5f);
        iconInner.offsetMax = new Vector2(-5f, -5f);
        Image iconInnerImage = iconInner.gameObject.AddComponent<Image>();
        iconInnerImage.color = new Color(0.004f, 0.032f, 0.052f, 1f);
        iconInnerImage.raycastTarget = false;

        TMP_Text symbol = CreateText("Symbol_A", card, font, 17f, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(symbol.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 7f), new Vector2(38f, 38f));
        symbol.text = "A";
        symbol.color = Color.white;

        TMP_Text balance = CreateText("Balance", card, font, 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        SetRect(balance.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 9f), new Vector2(92f, 38f));
        balance.text = "000";
        balance.color = Color.white;

        TMP_Text label = CreateText("Label", card, font, 12f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(154f, 15f), new Vector2(136f, 18f));
        label.text = "AURORACOINS";
        label.color = new Color(0.45f, 0.94f, 1f, 0.95f);

        TMP_Text status = CreateText("Status", card, font, 10f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        SetRect(status.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, -13f), new Vector2(232f, 17f));
        status.text = string.Empty;
        status.color = new Color(0.65f, 0.94f, 1f, 0.8f);
        status.enableAutoSizing = true;
        status.fontSizeMin = 8f;
        status.fontSizeMax = 10f;

        AuroraCoinHudController controller = card.gameObject.AddComponent<AuroraCoinHudController>();
        controller.Configure(balance, status, card, iconGlow);
        Undo.RegisterCreatedObjectUndo(card.gameObject, "Create Aurora Coin HUD");
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        float size,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUiRect(name, parent, parent.gameObject.layer);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static RectTransform CreateUiRect(string name, Transform parent, int layer)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static PlacementGroup[] BuildRound1Groups()
    {
        return new[]
        {
            new PlacementGroup("SectorA_Coins", "Coin_SectorA_", new[]
            {
                new Vector3(3f, 1.1f, 112f), new Vector3(3f, 1.1f, 118f),
                new Vector3(3f, 1.1f, 150f), new Vector3(3f, 1.1f, 156f), new Vector3(3f, 1.1f, 162f)
            }),
            new PlacementGroup("Containment_Coins", "Coin_Containment_", new[]
            {
                new Vector3(3f, 1.1f, 568f), new Vector3(3f, 1.1f, 576f), new Vector3(3f, 1.1f, 584f),
                new Vector3(3f, 1.1f, 592f), new Vector3(3f, 1.1f, 600f)
            }),
            new PlacementGroup("MachineRoom_Coins", "Coin_MachineRoom_", new[]
            {
                new Vector3(0f, 1.1f, 1008f), new Vector3(0f, 1.1f, 1016f), new Vector3(0f, 1.1f, 1024f),
                new Vector3(-1.5f, 1.1f, 1032f), new Vector3(-3f, 1.1f, 1040f)
            }),
            new PlacementGroup("RedCorridor_Coins", "Coin_RedCorridor_", new[]
            {
                new Vector3(0f, 1.1f, 1664f), new Vector3(1f, 1.1f, 1671f), new Vector3(2f, 1.1f, 1678f),
                new Vector3(3f, 1.1f, 1685f), new Vector3(3f, 1.1f, 1692f)
            }),
            new PlacementGroup("TechnicalBridge_Coins", "Coin_TechnicalBridge_", new[]
            {
                new Vector3(0f, 1.1f, 1888f), new Vector3(0f, 1.1f, 1896f), new Vector3(0f, 1.1f, 1904f),
                new Vector3(0f, 1.1f, 1912f), new Vector3(0f, 1.1f, 1920f)
            }),
            new PlacementGroup("FinalApproach_Coins", "Coin_FinalApproach_", new[]
            {
                new Vector3(0f, 1.1f, 2490f), new Vector3(0f, 1.1f, 2500f), new Vector3(0f, 1.1f, 2510f),
                new Vector3(0f, 1.1f, 2520f), new Vector3(0f, 1.1f, 2530f)
            })
        };
    }

    private static Transform EnsureHierarchy(string path)
    {
        string[] parts = path.Split('/');
        Transform current = null;
        string currentPath = string.Empty;
        for (int i = 0; i < parts.Length; i++)
        {
            currentPath = i == 0 ? parts[i] : currentPath + "/" + parts[i];
            Transform found = FindInActiveScene(currentPath);
            if (found != null)
            {
                current = found;
                continue;
            }

            var go = new GameObject(parts[i]);
            Undo.RegisterCreatedObjectUndo(go, "Create Aurora Coin Hierarchy");
            if (current != null) go.transform.SetParent(current, false);
            current = go.transform;
        }

        return current;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Aurora Coin Sector");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Transform FindInActiveScene(string path)
    {
        string[] parts = path.Split('/');
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (!string.Equals(roots[i].name, parts[0], StringComparison.Ordinal)) continue;
            if (parts.Length == 1) return roots[i].transform;
            string childPath = string.Join("/", parts.Skip(1));
            return roots[i].transform.Find(childPath);
        }

        return null;
    }

    private static AuroraCoinCollectible[] GetSelectedCoins()
    {
        return Selection.gameObjects
            .Select(go => go.GetComponent<AuroraCoinCollectible>())
            .Where(coin => coin != null)
            .OrderBy(coin => coin.name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasAncestor(Transform target, string ancestorName)
    {
        Transform current = target.parent;
        while (current != null)
        {
            if (string.Equals(current.name, ancestorName, StringComparison.Ordinal)) return true;
            current = current.parent;
        }
        return false;
    }

    private static string PrefixForParent(string parentName)
    {
        string clean = parentName.Replace("_Coins", string.Empty);
        return "Coin_" + clean + "_";
    }

    private static void SelectCreated(GameObject created)
    {
        if (created != null) Selection.activeGameObject = created;
    }

    private static void MarkSceneDirty()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private readonly struct PlacementGroup
    {
        public readonly string ParentName;
        public readonly string Prefix;
        public readonly Vector3[] Positions;

        public PlacementGroup(string parentName, string prefix, Vector3[] positions)
        {
            ParentName = parentName;
            Prefix = prefix;
            Positions = positions;
        }
    }

    public sealed class ValidationResult
    {
        public int CoinCount { get; }
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public List<ValidationIssue> Issues { get; } = new List<ValidationIssue>();

        public ValidationResult(int coinCount)
        {
            CoinCount = coinCount;
        }

        public void Error(string message)
        {
            ErrorCount++;
            Issues.Add(new ValidationIssue(true, message));
        }

        public void Warn(string message)
        {
            WarningCount++;
            Issues.Add(new ValidationIssue(false, message));
        }
    }

    public readonly struct ValidationIssue
    {
        public readonly bool IsError;
        public readonly string Message;

        public ValidationIssue(bool isError, string message)
        {
            IsError = isError;
            Message = message;
        }
    }
}
#endif
