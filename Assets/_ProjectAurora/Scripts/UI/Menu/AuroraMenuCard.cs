using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        [SerializeField, TextArea(2, 8)] private string iconSvgContent;
        [SerializeField] private string svgAssetPath;
        [SerializeField] private AuroraMenuCardAction action;

        [Header("Visual")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Sprite inactiveSprite;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private bool activeByDefault;

        [Header("Click")]
        [SerializeField] private UnityEvent onClick = new UnityEvent();

        private Button button;
        private bool isSelected;
        private bool isPointerInside;

        public string Label => label;
        public string IconSvgContent => iconSvgContent;
        public string SvgAssetPath => svgAssetPath;
        public AuroraMenuCardAction Action => action;
        public Button Button => button != null ? button : button = GetComponent<Button>();
        public UnityEvent OnClick => onClick;

        private void Awake()
        {
            ResolveReferences();
            Button.onClick.AddListener(HandleClick);
            ApplyContent();
            SetVisualActive(activeByDefault);
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
            string newIconSvgContent,
            string newSvgAssetPath,
            AuroraMenuCardAction newAction,
            Sprite newInactiveSprite,
            Sprite newActiveSprite,
            bool startActive)
        {
            label = newLabel;
            iconSvgContent = newIconSvgContent;
            svgAssetPath = newSvgAssetPath;
            action = newAction;
            inactiveSprite = newInactiveSprite;
            activeSprite = newActiveSprite;
            activeByDefault = startActive;

            ResolveReferences();
            ApplyContent();
            SetSelected(startActive);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            SetVisualActive(isSelected || isPointerInside);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            SetVisualActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            SetVisualActive(isSelected);
        }

        public void OnSelect(BaseEventData eventData)
        {
            isPointerInside = true;
            SetVisualActive(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isPointerInside = false;
            SetVisualActive(isSelected);
        }

        private void ResolveReferences()
        {
            button = GetComponent<Button>();
            if (cardImage == null)
            {
                cardImage = GetComponent<Image>();
            }
            if (labelText == null)
            {
                labelText = GetComponentInChildren<Text>(true);
            }
        }

        private void ApplyContent()
        {
            if (labelText != null)
            {
                labelText.text = label;
                labelText.raycastTarget = false;
            }

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(320f, 52f);
            }

            if (cardImage != null)
            {
                cardImage.raycastTarget = true;
                cardImage.type = Image.Type.Simple;
                cardImage.preserveAspect = true;
            }
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
        }

        private void HandleClick()
        {
            onClick?.Invoke();
        }
    }
}
