#if UNITY_EDITOR
using ProjectAurora.Lore;
using ProjectAurora.UI.Menu;
using ProjectAurora.UI.Menu.Lore;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectAurora.Editor.Lore
{
    public static class AuroraLoreMenuInstaller
    {
        private const string MainMenuScene = "Assets/_ProjectAurora/Scenes/MainMenu.unity";
        private static readonly Color Cyan = new Color(0.04f, 0.9f, 1f, 1f);
        private static readonly Color White = new Color(0.92f, 0.97f, 1f, 1f);
        private static readonly Color Muted = new Color(0.54f, 0.68f, 0.73f, 1f);
        private static readonly Color Dark = new Color(0.003f, 0.014f, 0.024f, 0.99f);
        private static readonly Color Panel = new Color(0.009f, 0.042f, 0.058f, 0.97f);
        private static readonly Color Red = new Color(0.95f, 0.16f, 0.22f, 1f);

        [MenuItem("Tools/Projeto Aurora/Lore/Install Or Update Lore Menu")]
        public static void InstallOrUpdateLoreMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MainMenuScene)
            {
                Debug.LogError("[AuroraLoreMenu] Abra a MainMenu antes de instalar: " + MainMenuScene);
                return;
            }

            AuroraLoreCatalog catalog = AuroraLoreCatalogBuilder.RebuildLoreCatalog();
            if (catalog == null || catalog.Count != AuroraLoreCatalog.OfficialLoreCount)
            {
                Debug.LogError("[AuroraLoreMenu] Catálogo oficial indisponível.");
                return;
            }

            Transform panelExtra = FindSceneTransform("Canvas_MainMenu/MenuRoot_16x9/Panel_Extra");
            Transform card = FindSceneTransform("Canvas_MainMenu/MenuRoot_16x9/Panel_Extra/Card");
            if (panelExtra == null || card == null)
            {
                Debug.LogError("[AuroraLoreMenu] Panel_Extra/Card não encontrado na MainMenu.");
                return;
            }

            Transform existing = panelExtra.Find("LoreArchivePanel");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            TMP_FontAsset font = ResolveMenuFont(panelExtra);
            int layer = panelExtra.gameObject.layer;
            RectTransform root = CreateUiRect("LoreArchivePanel", panelExtra, layer);
            Stretch(root, 0f, 0f, 0f, 0f);
            root.gameObject.AddComponent<CanvasGroup>();

            RectTransform overlay = CreateImage("BackgroundOverlay", root, Dark, true);
            Stretch(overlay, 0f, 0f, 0f, 0f);

            RectTransform header = CreateImage("Header", root, new Color(0.006f, 0.032f, 0.048f, 1f), false);
            AnchorTopStretch(header, 0f, 0f, 92f);
            AddLine(header, false, Cyan, 2f);

            ButtonParts back = CreateButton("Button_Retornar_LoreArchive", header, font, "<  VOLTAR", 18f);
            AnchorMiddleLeft(back.Rect, 54f, 0f, 180f, 52f);
            TMP_Text title = CreateText("Title", header, font, "ARQUIVO DE LORE", 32f,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, White);
            AnchorMiddleLeft(title.rectTransform, 270f, 2f, 440f, 52f);
            TMP_Text unlockedCounter = CreateText("UnlockedCounter", header, font,
                "ARQUIVOS DESBLOQUEADOS: 02 / 24", 17f,
                FontStyles.Bold, TextAlignmentOptions.MidlineRight, Cyan);
            AnchorMiddleRight(unlockedCounter.rectTransform, 338f, 1f, 560f, 42f);
            TMP_Text balance = CreateText("AuroraCoinBalance", header, font, "AURORACOINS: 0", 17f,
                FontStyles.Bold, TextAlignmentOptions.MidlineRight, White);
            AnchorMiddleRight(balance.rectTransform, 54f, 1f, 260f, 42f);

            RectTransform carousel = CreateFrame("FileCarousel", root, Panel);
            AnchorTopLeft(carousel, 64f, 124f, 520f, 850f);
            AddCornerAccents(carousel, Cyan);
            TMP_Text carouselCaption = CreateText("CarouselCaption", carousel, font,
                "BANCO DE DADOS  /  ÍNDICE GERAL", 14f, FontStyles.Bold,
                TextAlignmentOptions.TopLeft, Muted);
            AnchorTopLeft(carouselCaption.rectTransform, 22f, 17f, 430f, 28f);

            RectTransform fileCard = CreateFrame("FileCard", carousel,
                new Color(0.005f, 0.026f, 0.041f, 1f));
            AnchorTopLeft(fileCard, 22f, 56f, 476f, 660f);

            RectTransform stateAccentRect = CreateImage("StateAccent", fileCard, Cyan, false);
            stateAccentRect.anchorMin = new Vector2(0f, 1f);
            stateAccentRect.anchorMax = new Vector2(1f, 1f);
            stateAccentRect.pivot = new Vector2(0.5f, 1f);
            stateAccentRect.sizeDelta = new Vector2(0f, 5f);
            stateAccentRect.anchoredPosition = Vector2.zero;
            Image stateAccent = stateAccentRect.GetComponent<Image>();

            RectTransform iconPlate = CreateImage("FileIcon", fileCard,
                new Color(0.015f, 0.12f, 0.15f, 1f), false);
            CenterTop(iconPlate, 0f, 58f, 236f, 236f);
            Outline iconOutline = iconPlate.gameObject.AddComponent<Outline>();
            iconOutline.effectColor = Cyan;
            iconOutline.effectDistance = new Vector2(2f, -2f);
            RectTransform iconDiamond = CreateImage("DataCore", iconPlate, Cyan, false);
            Center(iconDiamond, 0f, 0f, 82f, 82f);
            iconDiamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
            RectTransform iconInner = CreateImage("CoreInner", iconDiamond,
                new Color(0.005f, 0.035f, 0.05f, 1f), false);
            Stretch(iconInner, 10f, 10f, 10f, 10f);
            TMP_Text iconLabel = CreateText("IconLabel", iconPlate, font, "DATA", 18f,
                FontStyles.Bold, TextAlignmentOptions.Center, White);
            Center(iconLabel.rectTransform, 0f, 0f, 130f, 42f);

            TMP_Text fileId = CreateText("FileId", fileCard, font, "LORE_001", 19f,
                FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            AnchorTopLeft(fileId.rectTransform, 26f, 322f, 424f, 34f);
            TMP_Text fileTitle = CreateText("FileTitle", fileCard, font, "ARQUIVO NÃO LOCALIZADO", 26f,
                FontStyles.Bold, TextAlignmentOptions.Center, White);
            AnchorTopLeft(fileTitle.rectTransform, 26f, 370f, 424f, 90f);
            fileTitle.enableWordWrapping = true;
            TMP_Text unlockType = CreateText("UnlockTypeLabel", fileCard, font, "DATAFILE DE CAMPO", 15f,
                FontStyles.Bold, TextAlignmentOptions.Center, Muted);
            AnchorTopLeft(unlockType.rectTransform, 26f, 476f, 424f, 30f);
            TMP_Text fileState = CreateText("FileState", fileCard, font, "NÃO LOCALIZADO", 18f,
                FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            AnchorTopLeft(fileState.rectTransform, 26f, 530f, 424f, 38f);

            RectTransform lockOverlayRect = CreateImage("LockOverlay", fileCard,
                new Color(Red.r, Red.g, Red.b, 0.055f), false);
            Stretch(lockOverlayRect, 5f, 5f, 5f, 5f);
            lockOverlayRect.SetAsFirstSibling();

            RectTransform navigation = CreateUiRect("NavigationArea", carousel, layer);
            AnchorTopLeft(navigation, 22f, 748f, 476f, 72f);
            ButtonParts previous = CreateButton("PreviousFileButton", navigation, font, "<", 32f);
            AnchorMiddleLeft(previous.Rect, 0f, 0f, 68f, 58f);
            ButtonParts next = CreateButton("NextFileButton", navigation, font, ">", 32f);
            AnchorMiddleRight(next.Rect, 0f, 0f, 68f, 58f);
            TMP_Text positionCounter = CreateText("PositionCounter", navigation, font, "01 / 24", 21f,
                FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            Center(positionCounter.rectTransform, 0f, 0f, 220f, 56f);

            RectTransform contentPanel = CreateFrame("LoreContentPanel", root, Panel);
            AnchorTopRight(contentPanel, 64f, 124f, 1236f, 690f);
            AddCornerAccents(contentPanel, Cyan);
            TMP_Text contentTitle = CreateText("ContentTitle", contentPanel, font,
                "ARQUIVO NÃO LOCALIZADO", 29f, FontStyles.Bold,
                TextAlignmentOptions.TopLeft, White);
            AnchorTopLeft(contentTitle.rectTransform, 28f, 22f, 940f, 46f);
            TMP_Text category = CreateText("CategoryLabel", contentPanel, font,
                "DATAFILE DE CAMPO", 15f, FontStyles.Bold,
                TextAlignmentOptions.TopRight, Cyan);
            AnchorTopRight(category.rectTransform, 28f, 28f, 300f, 32f);
            AddLine(contentPanel, true, new Color(Cyan.r, Cyan.g, Cyan.b, 0.28f), 1f, 84f);

            ScrollParts scroll = CreateScrollView("ScrollView", contentPanel, font);
            AnchorTopLeft(scroll.Rect, 24f, 104f, 1188f, 558f);

            RectTransform actionArea = CreateFrame("ActionArea", root,
                new Color(0.008f, 0.038f, 0.052f, 0.985f));
            AnchorTopRight(actionArea, 64f, 838f, 1236f, 136f);
            TMP_Text actionMessage = CreateText("ActionMessage", actionArea, font,
                "RECUPERAÇÃO DISPONÍVEL EM CAMPO", 18f, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft, Muted);
            AnchorMiddleLeft(actionMessage.rectTransform, 28f, 0f, 710f, 62f);
            ButtonParts purchase = CreateButton("PurchaseButton", actionArea, font,
                "DESBLOQUEAR — 15 AURORACOINS", 18f);
            AnchorMiddleRight(purchase.Rect, 28f, 0f, 430f, 62f);

            RectTransform footer = CreateUiRect("Footer", root, layer);
            AnchorBottomStretch(footer, 0f, 0f, 54f);
            TMP_Text hint = CreateText("NavigationHint", footer, font,
                "A / D  NAVEGAR     ENTER  DESBLOQUEAR     MOUSE  ROLAR TEXTO     ESC  VOLTAR",
                14f, FontStyles.Normal, TextAlignmentOptions.Center, Muted);
            Stretch(hint.rectTransform, 32f, 5f, 32f, 5f);

            AuroraLoreArchiveController controller = root.gameObject.AddComponent<AuroraLoreArchiveController>();
            controller.ConfigureForEditor(
                catalog, unlockedCounter, balance, fileId, fileTitle, unlockType, fileState,
                positionCounter, stateAccent, lockOverlayRect.gameObject, contentTitle, category,
                scroll.Text, scroll.ScrollRect, previous.Button, next.Button, purchase.Button,
                purchase.Label, actionMessage);

            Transform legacyLore = card.Find("Sub_Lore");
            if (legacyLore != null) legacyLore.gameObject.SetActive(false);
            WireExtraController(panelExtra, card, root.gameObject, back.Button);

            root.SetAsLastSibling();
            root.gameObject.SetActive(false);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[AuroraLoreMenu] Instalado: painel=1, entradas=" + catalog.Count +
                      ", ScrollRect=1, cardReutilizável=1.");
        }

        private static ScrollParts CreateScrollView(string name, Transform parent, TMP_FontAsset font)
        {
            RectTransform root = CreateImage(name, parent, new Color(0.002f, 0.018f, 0.028f, 0.9f), true);
            ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 42f;

            RectTransform viewport = CreateImage("Viewport", root, new Color(1f, 1f, 1f, 0.001f), true);
            Stretch(viewport, 0f, 0f, 22f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateUiRect("Content", viewport, viewport.gameObject.layer);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 30);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text fullText = CreateText("FullLoreText", content, font,
                "DATAFILE NÃO LOCALIZADO\n\nEncontre este arquivo durante a gameplay.",
                18f, FontStyles.Normal, TextAlignmentOptions.TopLeft, White);
            fullText.enableWordWrapping = true;
            fullText.overflowMode = TextOverflowModes.Overflow;
            fullText.lineSpacing = 10f;
            fullText.paragraphSpacing = 8f;
            fullText.margin = new Vector4(0f, 0f, 0f, 0f);
            LayoutElement textLayout = fullText.gameObject.AddComponent<LayoutElement>();
            textLayout.minHeight = 450f;

            RectTransform scrollbarRect = CreateUiRect("Scrollbar Vertical", root, root.gameObject.layer);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-16f, 8f);
            scrollbarRect.offsetMax = new Vector2(-4f, -8f);
            Image scrollbarBackground = scrollbarRect.gameObject.AddComponent<Image>();
            scrollbarBackground.color = new Color(0.12f, 0.22f, 0.25f, 0.55f);
            RectTransform slidingArea = CreateUiRect("Sliding Area", scrollbarRect, root.gameObject.layer);
            Stretch(slidingArea, 2f, 2f, 2f, 2f);
            RectTransform handle = CreateImage("Handle", slidingArea, Cyan, true);
            Stretch(handle, 0f, 0f, 0f, 0f);
            Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1f;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return new ScrollParts(root, scrollRect, fullText);
        }

        private static void WireExtraController(
            Transform panelExtra,
            Transform card,
            GameObject lorePanel,
            Button loreBackButton)
        {
            AuroraMenuExtraController extra = panelExtra.GetComponent<AuroraMenuExtraController>();
            if (extra == null)
            {
                Debug.LogError("[AuroraLoreMenu] AuroraMenuExtraController ausente.");
                return;
            }

            SerializedObject serialized = new SerializedObject(extra);
            serialized.FindProperty("mainCard").objectReferenceValue = card.gameObject;
            serialized.FindProperty("lorePanel").objectReferenceValue = lorePanel;
            serialized.FindProperty("loreBackButton").objectReferenceValue = loreBackButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(extra);
        }

        private static TMP_FontAsset ResolveMenuFont(Transform panelExtra)
        {
            TMP_Text text = panelExtra.GetComponentInChildren<TMP_Text>(true);
            return text != null && text.font != null ? text.font : TMP_Settings.defaultFontAsset;
        }

        private static Transform FindSceneTransform(string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                Transform current = root.transform;
                for (int i = 1; i < parts.Length && current != null; i++) current = current.Find(parts[i]);
                return current;
            }
            return null;
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
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return rect;
        }

        private static TMP_Text CreateText(
            string name, Transform parent, TMP_FontAsset font, string value, float size,
            FontStyles style, TextAlignmentOptions alignment, Color color)
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
            string name, Transform parent, TMP_FontAsset font, string label, float size)
        {
            RectTransform rect = CreateImage(name, parent,
                new Color(0.015f, 0.105f, 0.13f, 0.99f), true);
            Image image = rect.GetComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.68f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.38f, 0.78f, 0.84f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.22f, 0.3f, 0.32f, 0.72f);
            button.colors = colors;
            button.targetGraphic = image;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Cyan;
            outline.effectDistance = new Vector2(1f, -1f);
            TMP_Text text = CreateText("Label", rect, font, label, size,
                FontStyles.Bold, TextAlignmentOptions.Center, White);
            Stretch(text.rectTransform, 10f, 5f, 10f, 5f);
            return new ButtonParts(rect, button, text);
        }

        private static void AddCornerAccents(RectTransform parent, Color color)
        {
            CreateCorner(parent, "Corner_TL_H", true, true, 34f, 3f, color);
            CreateCorner(parent, "Corner_TL_V", true, true, 3f, 34f, color);
            CreateCorner(parent, "Corner_TR_H", false, true, 34f, 3f, color);
            CreateCorner(parent, "Corner_TR_V", false, true, 3f, 34f, color);
            CreateCorner(parent, "Corner_BL_H", true, false, 34f, 3f, color);
            CreateCorner(parent, "Corner_BL_V", true, false, 3f, 34f, color);
            CreateCorner(parent, "Corner_BR_H", false, false, 34f, 3f, color);
            CreateCorner(parent, "Corner_BR_V", false, false, 3f, 34f, color);
        }

        private static void CreateCorner(
            RectTransform parent, string name, bool left, bool top,
            float width, float height, Color color)
        {
            RectTransform corner = CreateImage(name, parent, color, false);
            corner.anchorMin = corner.anchorMax = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            corner.pivot = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            corner.anchoredPosition = Vector2.zero;
            corner.sizeDelta = new Vector2(width, height);
        }

        private static void AddLine(
            RectTransform parent, bool top, Color color, float thickness, float topOffset = 0f)
        {
            RectTransform line = CreateImage(top ? "Line_Top" : "Line_Bottom", parent, color, false);
            line.anchorMin = new Vector2(0f, top ? 1f : 0f);
            line.anchorMax = new Vector2(1f, top ? 1f : 0f);
            line.pivot = new Vector2(0.5f, top ? 1f : 0f);
            line.anchoredPosition = new Vector2(0f, top ? -topOffset : 0f);
            line.sizeDelta = new Vector2(0f, thickness);
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

        private readonly struct ScrollParts
        {
            public readonly RectTransform Rect;
            public readonly ScrollRect ScrollRect;
            public readonly TMP_Text Text;
            public ScrollParts(RectTransform rect, ScrollRect scrollRect, TMP_Text text)
            {
                Rect = rect;
                ScrollRect = scrollRect;
                Text = text;
            }
        }
    }
}
#endif
