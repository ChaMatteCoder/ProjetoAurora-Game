using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ProjectAurora.UI.Menu
{
    public enum AuroraMenuCardAction
    {
        StartGame,
        OpenSettings,
        OpenExtras,
        OpenCredits,
        QuitGame
    }

    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    public sealed class AuroraMenuCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Content")]
        [SerializeField] private string label;
        [SerializeField] private AuroraMenuCardAction action;

        [Header("Visual")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text labelText;
        [SerializeField] private TMP_Text labelTmpText;
        [SerializeField] private Sprite inactiveSprite;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private bool activeByDefault;
        [SerializeField] private Vector2 cardSize = new Vector2(500f, 76f);

        [Header("Click")]
        [SerializeField] private UnityEvent onClick = new UnityEvent();

        private Button button;
        private bool isSelected;
        private bool isPointerInside;

        public string Label => label;
        public AuroraMenuCardAction Action => action;
        public Button Button => button != null ? button : button = GetComponent<Button>();
        public UnityEvent OnClick => onClick;

        private void Awake()
        {
            ResolveReferences();
            Button.onClick.AddListener(HandleClick);
            ApplyContent();
            if (activeByDefault)
            {
                activeByDefault = false;
            }
            SetVisualActive(false);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Configure(
            string newLabel,
            AuroraMenuCardAction newAction,
            Sprite newInactiveSprite,
            Sprite newActiveSprite,
            bool startActive)
        {
            label = newLabel;
            action = newAction;
            inactiveSprite = newInactiveSprite;
            activeSprite = newActiveSprite;
            activeByDefault = false;

            ResolveReferences();
            ApplyContent();
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            SetVisualActive(isPointerInside);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            SetVisualActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            SetVisualActive(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            SetVisualActive(isPointerInside);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            SetVisualActive(isPointerInside);
        }

        private void ResolveReferences()
        {
            button = GetComponent<Button>();
            if (cardImage == null)
            {
                cardImage = GetComponent<Image>();
            }
            if (iconImage == null)
            {
                Transform icon = transform.Find("Image_Icon");
                if (icon != null)
                {
                    iconImage = icon.GetComponent<Image>();
                }
            }
            if (labelTmpText == null)
            {
                labelTmpText = GetComponentInChildren<TMP_Text>(true);
            }
            if (labelText == null)
            {
                labelText = GetComponentInChildren<Text>(true);
            }
        }

        private void ApplyContent()
        {
            if (labelTmpText != null)
            {
                labelTmpText.text = label;
                labelTmpText.fontSize = 24;
                labelTmpText.alignment = TextAlignmentOptions.Left;
                labelTmpText.raycastTarget = false;

                RectTransform tmpRect = labelTmpText.transform as RectTransform;
                if (tmpRect != null)
                {
                    tmpRect.anchorMin = new Vector2(0f, 0f);
                    tmpRect.anchorMax = new Vector2(1f, 1f);
                    tmpRect.offsetMin = new Vector2(116f, 0f);
                    tmpRect.offsetMax = new Vector2(-34f, 0f);
                }
            }

            if (labelText != null)
            {
                labelText.text = label;
                labelText.fontSize = 23;
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.raycastTarget = false;

                RectTransform labelRect = labelText.transform as RectTransform;
                if (labelRect != null)
                {
                    labelRect.anchorMin = new Vector2(0f, 0f);
                    labelRect.anchorMax = new Vector2(1f, 1f);
                    labelRect.offsetMin = new Vector2(116f, 0f);
                    labelRect.offsetMax = new Vector2(-34f, 0f);
                }
            }

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = cardSize;
            }

            if (cardImage != null)
            {
                cardImage.raycastTarget = true;
                cardImage.type = Image.Type.Simple;
                cardImage.preserveAspect = false;
            }

            if (iconImage != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.enabled = iconSprite != null;
                iconImage.raycastTarget = false;
                iconImage.preserveAspect = true;

                RectTransform iconRect = iconImage.transform as RectTransform;
                if (iconRect != null)
                {
                    iconRect.anchorMin = new Vector2(0f, 0.5f);
                    iconRect.anchorMax = new Vector2(0f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(58f, 0f);
                    iconRect.sizeDelta = new Vector2(42f, 42f);
                }
            }

            ConfigureButtonVisualState();
        }

        private void ConfigureButtonVisualState()
        {
            Button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = Button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.35f, 0.95f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.35f, 0.45f, 0.55f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            Button.colors = colors;
        }

        private void SetVisualActive(bool active)
        {
            if (cardImage == null)
            {
                return;
            }

            Sprite target = active ? activeSprite : inactiveSprite;
            if (target != null)
            {
                cardImage.sprite = target;
            }

            Color textColor = active
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(0.86f, 0.93f, 1f, 0.92f);
            if (labelText != null)
            {
                labelText.color = textColor;
            }
            if (labelTmpText != null)
            {
                labelTmpText.color = textColor;
            }
            if (iconImage != null)
            {
                iconImage.color = active ? Color.white : new Color(0.82f, 0.96f, 1f, 0.92f);
            }

            transform.localScale = active ? Vector3.one * 1.015f : Vector3.one;
        }

        private void HandleClick()
        {
            onClick?.Invoke();
        }
    }
}
