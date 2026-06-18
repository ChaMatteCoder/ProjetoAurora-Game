using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuroraGameplayHudLayoutFixer
{
    private const string GameplayScenePath = "Assets/_ProjectAurora/Scenes/Beta03_Principal.unity";
    private static readonly Color Panel = new Color(0.015f, 0.075f, 0.12f, 0.78f);
    private static readonly Color PanelStrong = new Color(0.008f, 0.045f, 0.075f, 0.88f);
    private static readonly Color Cyan = new Color(0.04f, 0.86f, 1f, 1f);
    private static readonly Color CyanSoft = new Color(0.28f, 0.92f, 1f, 1f);
    private static readonly Color White = new Color(0.86f, 0.96f, 1f, 1f);

    [MenuItem("Tools/Projeto Aurora/UI/Apply Gameplay HUD Layout Fix")]
    public static void ApplyGameplayHudLayoutFix()
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        GameObject hudCanvas = GameObject.Find("HUD Canvas");
        if (hudCanvas == null)
        {
            Debug.LogError("PROJETO:AURORA - HUD Canvas nao encontrado em Beta03_Principal.");
            return;
        }

        ConfigureCanvas(hudCanvas);
        Transform root = hudCanvas.transform;

        SetPanel(root, "Sector Identification", Panel, 0f, 1f, 0f, 1f, 0f, 1f, 42f, -34f, 520f, 132f);
        SetText(root, "Sector Identification/Sector Name", "SETOR A: Laborat\u00f3rio Limpo", 24f, TextAlignmentOptions.Left, false);
        SetRect(root, "Sector Identification/Sector Name", 0f, 1f, 0f, 1f, 0f, 1f, 22f, -14f, 474f, 44f);
        SetText(root, "Sector Identification/Objective", "Escape do setor", 23f, TextAlignmentOptions.Left, false);
        SetRect(root, "Sector Identification/Objective", 0f, 1f, 0f, 1f, 0f, 1f, 66f, -76f, 410f, 38f);
        SetRect(root, "Sector Identification/Objective Diamond", 0f, 1f, 0f, 1f, 0.5f, 0.5f, 42f, -95f, 18f, 18f, 45f);
        SetRect(root, "Sector Identification/Objective Diamond/Objective Diamond Core", 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 10f, 10f);

        SetPanel(root, "Integrity System", PanelStrong, 0.5f, 1f, 0.5f, 1f, 0.5f, 1f, 0f, -30f, 430f, 78f);
        SetText(root, "Integrity System/Integrity Label", "INTEGRIDADE", 22f, TextAlignmentOptions.Left, false);
        SetRect(root, "Integrity System/Integrity Label", 0f, 1f, 0f, 1f, 0f, 1f, 24f, -20f, 194f, 36f);
        for (int i = 0; i < 3; i++)
        {
            SetRect(root, "Integrity System/Integrity Segment " + (i + 1), 0f, 1f, 0f, 1f, 0f, 1f, 254f + i * 48f, -18f, 36f, 42f);
        }

        SetPanel(root, "Distance System", Panel, 1f, 1f, 1f, 1f, 1f, 1f, -42f, -34f, 560f, 126f);
        SetText(root, "Distance System/Distance Label", "DIST\u00c2NCIA", 22f, TextAlignmentOptions.Left, false);
        SetRect(root, "Distance System/Distance Label", 0f, 1f, 0f, 1f, 0f, 1f, 25f, -14f, 190f, 34f);
        SetText(root, "Distance System/Distance Value", "0 m", 38f, TextAlignmentOptions.Left, false);
        SetRect(root, "Distance System/Distance Value", 0f, 1f, 0f, 1f, 0f, 1f, 25f, -52f, 190f, 56f);
        SetRect(root, "Distance System/Distance Track", 1f, 0f, 1f, 0f, 1f, 0f, -58f, 40f, 310f, 8f);
        SetRect(root, "Distance System/Distance Track/Start Marker", 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, -155f, 0f, 5f, 24f);
        SetRect(root, "Distance System/Distance Track/Progress Marker", 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, -155f, 0f, 16f, 16f, 45f);

        SetPanel(root, "CelestIA Communication", PanelStrong, 1f, 0f, 1f, 0f, 1f, 0f, -42f, 42f, 740f, 260f);
        SetRect(root, "CelestIA Communication/Portrait Ring", 0f, 0f, 0f, 0f, 0f, 0f, 28f, 28f, 204f, 204f);
        SetRect(root, "CelestIA Communication/Portrait Ring/Portrait Mask", 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 184f, 184f);
        SetText(root, "CelestIA Communication/CelestIA Name", "CELESTIA", 27f, TextAlignmentOptions.Left, false);
        SetRect(root, "CelestIA Communication/CelestIA Name", 0f, 1f, 0f, 1f, 0f, 1f, 270f, -24f, 190f, 42f);
        SetText(root, "CelestIA Communication/CelestIA Status", "STATUS: NORMAL", 17f, TextAlignmentOptions.Right, false);
        SetRect(root, "CelestIA Communication/CelestIA Status", 1f, 1f, 1f, 1f, 1f, 1f, -38f, -30f, 230f, 32f);
        SetRect(root, "CelestIA Communication/Header Divider", 0f, 1f, 0f, 1f, 0f, 1f, 262f, -70f, 440f, 2f);
        SetText(root, "CelestIA Communication/CelestIA Message", "Doutor Elias, mantenha a rota. Detectando obst\u00e1culos \u00e0 frente.", 24f, TextAlignmentOptions.TopLeft, true);
        SetRect(root, "CelestIA Communication/CelestIA Message", 0f, 1f, 0f, 1f, 0f, 1f, 272f, -90f, 430f, 104f);
        for (int i = 0; i < 26; i++)
        {
            SetRect(root, "CelestIA Communication/Transmission " + i, 0f, 0f, 0f, 0f, 0f, 0f, 282f + i * 13.5f, 24f, 5f, 12f);
        }

        DisableDecorativeRaycasts(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("PROJETO:AURORA - HUD de gameplay realinhada em Beta03_Principal.");
    }

    private static void ConfigureCanvas(GameObject hudCanvas)
    {
        Canvas canvas = hudCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
        }

        CanvasScaler scaler = hudCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private static void SetPanel(Transform root, string path, Color color, float minX, float minY, float maxX, float maxY, float pivotX, float pivotY, float posX, float posY, float width, float height)
    {
        RectTransform rect = SetRect(root, path, minX, minY, maxX, maxY, pivotX, pivotY, posX, posY, width, height);
        if (rect != null && rect.TryGetComponent(out Image image))
        {
            image.color = color;
            image.raycastTarget = false;
        }
    }

    private static RectTransform SetRect(Transform root, string path, float minX, float minY, float maxX, float maxY, float pivotX, float pivotY, float posX, float posY, float width, float height, float rotationZ = 0f)
    {
        Transform target = Find(root, path);
        if (target == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Elemento de HUD nao encontrado: " + path);
            return null;
        }

        RectTransform rect = target as RectTransform;
        if (rect == null)
        {
            return null;
        }

        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.pivot = new Vector2(pivotX, pivotY);
        rect.anchoredPosition = new Vector2(posX, posY);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        return rect;
    }

    private static void SetText(Transform root, string path, string value, float size, TextAlignmentOptions alignment, bool wrap)
    {
        Transform target = Find(root, path);
        if (target == null || !target.TryGetComponent(out TMP_Text text))
        {
            Debug.LogWarning("PROJETO:AURORA - Texto de HUD nao encontrado: " + path);
            return;
        }

        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = path.Contains("Objective") || path.Contains("Distance Value") || path.Contains("CelestIA Name") || path.Contains("Status")
            ? CyanSoft
            : White;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
        text.raycastTarget = false;
        text.rectTransform.localScale = Vector3.one;
        text.rectTransform.localRotation = Quaternion.identity;
    }

    private static Transform Find(Transform root, string path)
    {
        Transform current = root;
        foreach (string part in path.Split('/'))
        {
            current = current == null ? null : current.Find(part);
        }
        return current;
    }

    private static void DisableDecorativeRaycasts(Transform root)
    {
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = graphic.GetComponent<Button>() != null;
        }
    }
}
