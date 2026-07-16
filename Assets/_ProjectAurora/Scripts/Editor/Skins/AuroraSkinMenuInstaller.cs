#if UNITY_EDITOR
using ProjectAurora.Customization.Skins;
using ProjectAurora.UI.Menu;
using ProjectAurora.UI.Menu.Skins;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectAurora.Editor.Skins
{
    public static class AuroraSkinMenuInstaller
    {
        private const string MainMenuScene = "Assets/_ProjectAurora/Scenes/MainMenu.unity";
        private const string RenderTextureFolder = "Assets/_ProjectAurora/Art/Skin/RenderTextures";
        private const string RenderTexturePath = RenderTextureFolder + "/RT_SkinPreview.renderTexture";
        private const string MaterialFolder = "Assets/_ProjectAurora/Art/Skin/Materials";
        private const string BackdropMaterialPath = MaterialFolder + "/MAT_SkinPreviewBackdrop.mat";

        private static readonly Color Cyan = new Color(0.04f, 0.9f, 1f, 1f);
        private static readonly Color CyanMuted = new Color(0.22f, 0.72f, 0.78f, 1f);
        private static readonly Color White = new Color(0.92f, 0.97f, 1f, 1f);
        private static readonly Color Muted = new Color(0.56f, 0.69f, 0.74f, 1f);
        private static readonly Color Dark = new Color(0.004f, 0.018f, 0.03f, 0.985f);
        private static readonly Color Panel = new Color(0.012f, 0.055f, 0.075f, 0.96f);
        private static readonly Color Amber = new Color(1f, 0.62f, 0.18f, 1f);

        [MenuItem("Tools/Projeto Aurora/Skins/Install Or Update Skin Menu")]
        public static void InstallOrUpdateSkinMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainMenuScene)
            {
                Debug.LogError("[AuroraSkinMenu] Abra a MainMenu antes de instalar: " + MainMenuScene);
                return;
            }

            AuroraSkinCatalog catalog = AuroraSkinCatalogBuilder.RebuildSkinCatalog();
            if (catalog == null)
            {
                Debug.LogError("[AuroraSkinMenu] Catalogo indisponivel.");
                return;
            }

            int previewLayer = AuroraSkinCatalogBuilder.EnsureSkinPreviewLayer();
            RenderTexture renderTexture = EnsureRenderTexture();
            AuroraSkinPreviewController previewController =
                EnsurePreviewSystem(previewLayer, renderTexture);

            Transform panelExtra = FindSceneTransform("Canvas_MainMenu/MenuRoot_16x9/Panel_Extra");
            Transform card = FindSceneTransform("Canvas_MainMenu/MenuRoot_16x9/Panel_Extra/Card");
            if (panelExtra == null || card == null)
            {
                Debug.LogError("[AuroraSkinMenu] Panel_Extra/Card nao encontrado na MainMenu.");
                return;
            }

            Transform existing = panelExtra.Find("SkinSelectionPanel");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                Button existingBackButton = existing.Find("Header/Button_Retornar_SkinMenu")
                    ?.GetComponent<Button>();
                AuroraSkinSelectionController existingController =
                    existing.GetComponent<AuroraSkinSelectionController>();
                RawImage existingPreviewImage = existing.Find("Preview3DArea/PreviewFrame/PreviewRawImage")
                    ?.GetComponent<RawImage>();
                if (existingController != null)
                {
                    SerializedObject serializedController = new SerializedObject(existingController);
                    serializedController.FindProperty("previewImage").objectReferenceValue = existingPreviewImage;
                    serializedController.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(existingController);
                }
                WireExtraController(panelExtra, card, existing.gameObject,
                    existingBackButton);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[AuroraSkinMenu] Painel existente preservado; catalogo e preview atualizados.");
                return;
            }

            TMP_FontAsset font = ResolveMenuFont(panelExtra);
            RectTransform root = CreateUiRect("SkinSelectionPanel", panelExtra, panelExtra.gameObject.layer);
            Stretch(root, 0f, 0f, 0f, 0f);
            root.gameObject.AddComponent<CanvasGroup>();

            RectTransform overlay = CreateImage("BackgroundOverlay", root, Dark, true);
            Stretch(overlay, 0f, 0f, 0f, 0f);

            RectTransform header = CreateImage("Header", root, new Color(0.008f, 0.04f, 0.058f, 0.98f), false);
            AnchorTopStretch(header, 0f, 0f, 92f);
            AddLine(header, false, Cyan, 2f);

            ButtonParts back = CreateButton("Button_Retornar_SkinMenu", header, font, "<  VOLTAR", 19f);
            AnchorMiddleLeft(back.Rect, 58f, 0f, 158f, 52f);

            TMP_Text title = CreateText("Title", header, font, "SELEÇÃO DE SKINS", 34f,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, White);
            AnchorMiddleLeft(title.rectTransform, 250f, 4f, 560f, 52f);

            TMP_Text selectedStatus = CreateText("SelectedSkinStatus", header, font, "EQUIPADA: --", 17f,
                FontStyles.Bold, TextAlignmentOptions.MidlineRight, CyanMuted);
            AnchorMiddleRight(selectedStatus.rectTransform, 58f, 2f, 580f, 42f);

            RectTransform splashArea = CreateUiRect("SplashArtArea", root, root.gameObject.layer);
            AnchorTopLeft(splashArea, 64f, 122f, 1050f, 850f);

            RectTransform splashFrame = CreateFrame("SplashFrame", splashArea, new Color(0.008f, 0.038f, 0.052f, 1f));
            AnchorTopLeft(splashFrame, 0f, 0f, 1050f, 590f);
            AddCornerAccents(splashFrame);

            RectTransform splashRect = CreateUiRect("SplashImage", splashFrame, splashFrame.gameObject.layer);
            Stretch(splashRect, 8f, 8f, 8f, 8f);
            Image splashImage = splashRect.gameObject.AddComponent<Image>();
            splashImage.preserveAspect = true;
            splashImage.raycastTarget = false;
            AspectRatioFitter splashAspect = splashRect.gameObject.AddComponent<AspectRatioFitter>();
            splashAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            splashAspect.aspectRatio = 16f / 9f;

            TMP_Text skinName = CreateText("SkinName", splashArea, font, "DR. ELIAS", 32f,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, White);
            AnchorTopLeft(skinName.rectTransform, 0f, 610f, 1050f, 48f);

            TMP_Text description = CreateText("SkinDescription", splashArea, font, string.Empty, 18f,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, Muted);
            AnchorTopLeft(description.rectTransform, 0f, 666f, 1050f, 78f);
            description.enableWordWrapping = true;
            description.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform navigation = CreateUiRect("NavigationArea", splashArea, splashArea.gameObject.layer);
            AnchorTopLeft(navigation, 0f, 766f, 1050f, 66f);
            ButtonParts previous = CreateButton("PreviousSkinButton", navigation, font, "<", 34f);
            AnchorMiddleLeft(previous.Rect, 0f, 0f, 66f, 56f);
            ButtonParts next = CreateButton("NextSkinButton", navigation, font, ">", 34f);
            AnchorMiddleRight(next.Rect, 0f, 0f, 66f, 56f);
            TMP_Text counter = CreateText("SkinCounter", navigation, font, "01 / 06", 22f,
                FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            Center(counter.rectTransform, 0f, 0f, 220f, 56f);

            RectTransform previewArea = CreateUiRect("Preview3DArea", root, root.gameObject.layer);
            AnchorTopRight(previewArea, 64f, 122f, 650f, 850f);
            RectTransform previewFrame = CreateFrame("PreviewFrame", previewArea, Panel);
            AnchorTopLeft(previewFrame, 0f, 0f, 650f, 650f);
            AddCornerAccents(previewFrame);

            TMP_Text previewCaption = CreateText("PreviewCaption", previewFrame, font, "PREVIEW 3D  /  T-POSE", 14f,
                FontStyles.Bold, TextAlignmentOptions.TopLeft, CyanMuted);
            AnchorTopLeft(previewCaption.rectTransform, 18f, 14f, 360f, 28f);

            RectTransform rawRect = CreateUiRect("PreviewRawImage", previewFrame, previewFrame.gameObject.layer);
            Stretch(rawRect, 16f, 16f, 16f, 16f);
            RawImage rawImage = rawRect.gameObject.AddComponent<RawImage>();
            rawImage.texture = renderTexture;
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
            AspectRatioFitter rawAspect = rawRect.gameObject.AddComponent<AspectRatioFitter>();
            rawAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            rawAspect.aspectRatio = 1f;

            TMP_Text loading = CreateText("PreviewLoadingText", previewFrame, font, "CARREGANDO PREVIEW...", 16f,
                FontStyles.Bold, TextAlignmentOptions.Center, CyanMuted);
            Center(loading.rectTransform, 0f, 0f, 420f, 56f);

            TMP_Text unavailable = CreateText("PreviewUnavailableText", previewFrame, font,
                "MODELO 3D\nINDISPONÍVEL", 20f, FontStyles.Bold, TextAlignmentOptions.Center, Muted);
            Center(unavailable.rectTransform, 0f, 0f, 440f, 100f);

            RectTransform actionArea = CreateUiRect("ActionArea", previewArea, previewArea.gameObject.layer);
            AnchorTopLeft(actionArea, 0f, 672f, 650f, 164f);
            ButtonParts select = CreateButton("SelectSkinButton", actionArea, font, "SELECIONAR", 22f);
            CenterTop(select.Rect, 0f, 0f, 360f, 62f);

            GameObject equippedBadge = CreateBadge("EquippedBadge", actionArea, font, "EQUIPADA", Cyan,
                new Color(0.01f, 0.12f, 0.14f, 0.96f));
            CenterTop((RectTransform)equippedBadge.transform, -105f, 82f, 190f, 42f);
            GameObject lockedBadge = CreateBadge("LockedBadge", actionArea, font, "BLOQUEADA", Amber,
                new Color(0.15f, 0.075f, 0.01f, 0.96f));
            CenterTop((RectTransform)lockedBadge.transform, 105f, 82f, 190f, 42f);

            RectTransform footer = CreateUiRect("Footer", root, root.gameObject.layer);
            AnchorBottomStretch(footer, 0f, 0f, 58f);
            TMP_Text navigationHint = CreateText("NavigationHint", footer, font,
                "A / D  NAVEGAR     ENTER  SELECIONAR     ESC  VOLTAR", 15f,
                FontStyles.Normal, TextAlignmentOptions.Center, Muted);
            Stretch(navigationHint.rectTransform, 40f, 6f, 40f, 6f);

            AuroraSkinSelectionController selectionController =
                root.gameObject.AddComponent<AuroraSkinSelectionController>();
            selectionController.ConfigureForEditor(
                catalog,
                previewController,
                splashImage,
                splashAspect,
                skinName,
                description,
                counter,
                selectedStatus,
                rawImage,
                loading,
                unavailable,
                previous.Button,
                next.Button,
                select.Button,
                select.Label,
                equippedBadge,
                lockedBadge);

            Transform legacySkin = card.Find("Sub_Skin");
            if (legacySkin != null) legacySkin.gameObject.SetActive(false);
            WireExtraController(panelExtra, card, root.gameObject, back.Button);

            root.SetAsLastSibling();
            root.gameObject.SetActive(false);
            EditorUtility.SetDirty(selectionController);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[AuroraSkinMenu] Instalado: painel=1, catalogo=" + catalog.Count +
                      ", previewLayer=" + previewLayer + ", RT=1024x1024.");
        }

        private static AuroraSkinPreviewController EnsurePreviewSystem(int layer, RenderTexture renderTexture)
        {
            GameObject root = GameObject.Find("SkinPreviewSystem");
            if (root == null)
            {
                root = new GameObject("SkinPreviewSystem");
                Undo.RegisterCreatedObjectUndo(root, "Create Skin Preview System");
            }

            root.transform.position = new Vector3(1000f, -1000f, 1000f);
            root.layer = layer;
            Transform previewRoot = EnsureChild(root.transform, "PreviewRoot", layer);
            Transform anchor = EnsureChild(previewRoot, "PreviewCharacterAnchor", layer);

            Transform cameraTransform = EnsureChild(previewRoot, "PreviewCamera", layer);
            Camera camera = cameraTransform.GetComponent<Camera>();
            if (camera == null) camera = cameraTransform.gameObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Dark;
            camera.cullingMask = 1 << layer;
            camera.targetTexture = renderTexture;
            camera.fieldOfView = 30f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.enabled = false;

            ConfigureDirectionalLight(EnsureChild(previewRoot, "PreviewKeyLight", layer), layer,
                new Vector3(35f, 210f, 0f), new Color(0.88f, 0.97f, 1f), 1.15f);
            ConfigureDirectionalLight(EnsureChild(previewRoot, "PreviewFillLight", layer), layer,
                new Vector3(20f, 35f, 0f), new Color(0.28f, 0.72f, 0.82f), 0.5f);
            ConfigureDirectionalLight(EnsureChild(previewRoot, "PreviewBackLight", layer), layer,
                new Vector3(315f, 150f, 0f), Cyan, 0.75f);

            Material backdropMaterial = EnsureBackdropMaterial();
            Transform backdrop = EnsurePrimitive(previewRoot, "PreviewBackdrop", PrimitiveType.Cube, layer);
            backdrop.localPosition = new Vector3(0f, 2.4f, -2.1f);
            backdrop.localScale = new Vector3(8f, 6f, 0.1f);
            backdrop.GetComponent<Renderer>().sharedMaterial = backdropMaterial;

            Transform floor = EnsurePrimitive(previewRoot, "PreviewFloor", PrimitiveType.Cube, layer);
            floor.localPosition = new Vector3(0f, -0.06f, 0f);
            floor.localScale = new Vector3(8f, 0.08f, 8f);
            floor.GetComponent<Renderer>().sharedMaterial = backdropMaterial;

            Camera mainCamera = Camera.main;
            if (mainCamera != null) mainCamera.cullingMask &= ~(1 << layer);

            AuroraSkinPreviewController controller = root.GetComponent<AuroraSkinPreviewController>();
            if (controller == null) controller = root.AddComponent<AuroraSkinPreviewController>();
            controller.ConfigureForEditor(anchor, camera, renderTexture, layer);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void WireExtraController(
            Transform panelExtra,
            Transform card,
            GameObject skinPanel,
            Button skinBackButton)
        {
            AuroraMenuExtraController extra = panelExtra.GetComponent<AuroraMenuExtraController>();
            if (extra == null)
            {
                Debug.LogError("[AuroraSkinMenu] AuroraMenuExtraController ausente.");
                return;
            }

            SerializedObject serialized = new SerializedObject(extra);
            serialized.FindProperty("mainCard").objectReferenceValue = card.gameObject;
            serialized.FindProperty("skinPanel").objectReferenceValue = skinPanel;
            serialized.FindProperty("skinBackButton").objectReferenceValue = skinBackButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(extra);
        }

        private static RenderTexture EnsureRenderTexture()
        {
            EnsureFolder(RenderTextureFolder);
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (texture == null)
            {
                texture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
                {
                    name = "RT_SkinPreview",
                    antiAliasing = 2,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                AssetDatabase.CreateAsset(texture, RenderTexturePath);
            }
            return texture;
        }

        private static Material EnsureBackdropMaterial()
        {
            EnsureFolder(MaterialFolder);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BackdropMaterialPath);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            material = new Material(shader) { name = "MAT_SkinPreviewBackdrop", color = new Color(0.004f, 0.022f, 0.03f, 1f) };
            AssetDatabase.CreateAsset(material, BackdropMaterialPath);
            return material;
        }

        private static TMP_FontAsset ResolveMenuFont(Transform panelExtra)
        {
            TMP_Text text = panelExtra.GetComponentInChildren<TMP_Text>(true);
            return text != null && text.font != null ? text.font : TMP_Settings.defaultFontAsset;
        }

        private static Transform EnsureChild(Transform parent, string name, int layer)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                child = go.transform;
            }
            child.gameObject.layer = layer;
            return child;
        }

        private static Transform EnsurePrimitive(Transform parent, string name, PrimitiveType type, int layer)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = GameObject.CreatePrimitive(type);
                go.name = name;
                go.transform.SetParent(parent, false);
                child = go.transform;
            }
            child.gameObject.layer = layer;
            Collider collider = child.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return child;
        }

        private static void ConfigureDirectionalLight(
            Transform target,
            int layer,
            Vector3 rotation,
            Color color,
            float intensity)
        {
            Light light = target.GetComponent<Light>();
            if (light == null) light = target.gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << layer;
            target.localRotation = Quaternion.Euler(rotation);
        }

        private static RectTransform CreateUiRect(string name, Transform parent, int layer)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreateImage(string name, Transform parent, Color color, bool raycast)
        {
            RectTransform rect = CreateUiRect(name, parent, parent.gameObject.layer);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return rect;
        }

        private static RectTransform CreateFrame(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateImage(name, parent, color, false);
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return rect;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float size,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color)
        {
            RectTransform rect = CreateUiRect(name, parent, parent.gameObject.layer);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static ButtonParts CreateButton(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string label,
            float fontSize)
        {
            RectTransform rect = CreateImage(name, parent, new Color(0.018f, 0.12f, 0.145f, 0.98f), true);
            Image image = rect.GetComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.72f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.42f, 0.82f, 0.86f, 1f);
            colors.selectedColor = new Color(0.72f, 1f, 1f, 1f);
            colors.disabledColor = new Color(0.24f, 0.31f, 0.34f, 0.75f);
            button.colors = colors;
            button.targetGraphic = image;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
            TMP_Text text = CreateText("Label", rect, font, label, fontSize,
                FontStyles.Bold, TextAlignmentOptions.Center, White);
            Stretch(text.rectTransform, 10f, 6f, 10f, 6f);
            return new ButtonParts(rect, button, text);
        }

        private static GameObject CreateBadge(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string label,
            Color accent,
            Color background)
        {
            RectTransform rect = CreateImage(name, parent, background, false);
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = accent;
            outline.effectDistance = new Vector2(1f, -1f);
            TMP_Text text = CreateText("Label", rect, font, label, 15f,
                FontStyles.Bold, TextAlignmentOptions.Center, accent);
            Stretch(text.rectTransform, 6f, 4f, 6f, 4f);
            return rect.gameObject;
        }

        private static void AddCornerAccents(RectTransform parent)
        {
            CreateCorner(parent, "Corner_TL_H", true, true, 36f, 3f);
            CreateCorner(parent, "Corner_TL_V", true, true, 3f, 36f);
            CreateCorner(parent, "Corner_TR_H", false, true, 36f, 3f);
            CreateCorner(parent, "Corner_TR_V", false, true, 3f, 36f);
            CreateCorner(parent, "Corner_BL_H", true, false, 36f, 3f);
            CreateCorner(parent, "Corner_BL_V", true, false, 3f, 36f);
            CreateCorner(parent, "Corner_BR_H", false, false, 36f, 3f);
            CreateCorner(parent, "Corner_BR_V", false, false, 3f, 36f);
        }

        private static void CreateCorner(
            RectTransform parent,
            string name,
            bool left,
            bool top,
            float width,
            float height)
        {
            RectTransform corner = CreateImage(name, parent, Cyan, false);
            corner.anchorMin = corner.anchorMax = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            corner.pivot = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            corner.anchoredPosition = Vector2.zero;
            corner.sizeDelta = new Vector2(width, height);
        }

        private static void AddLine(RectTransform parent, bool top, Color color, float thickness)
        {
            RectTransform line = CreateImage(top ? "Line_Top" : "Line_Bottom", parent, color, false);
            line.anchorMin = new Vector2(0f, top ? 1f : 0f);
            line.anchorMax = new Vector2(1f, top ? 1f : 0f);
            line.pivot = new Vector2(0.5f, top ? 1f : 0f);
            line.anchoredPosition = Vector2.zero;
            line.sizeDelta = new Vector2(0f, thickness);
        }

        private static Transform FindSceneTransform(string path)
        {
            string[] parts = path.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                if (root.name != parts[0])
                {
                    continue;
                }

                Transform current = root.transform;
                for (int i = 1; i < parts.Length && current != null; i++)
                {
                    current = current.Find(parts[i]);
                }

                return current;
            }

            return null;
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

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void AnchorTopStretch(RectTransform rect, float left, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void AnchorBottomStretch(RectTransform rect, float left, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-(left + right), height);
        }

        private static void AnchorTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void AnchorTopRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void AnchorMiddleLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void AnchorMiddleRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Center(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void CenterTop(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private readonly struct ButtonParts
        {
            public readonly RectTransform Rect;
            public readonly Button Button;
            public readonly TMP_Text Label;

            public ButtonParts(RectTransform rect, Button button, TMP_Text label)
            {
                Rect = rect;
                Button = button;
                Label = label;
            }
        }
    }
}
#endif
