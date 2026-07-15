using ProjectAurora.Customization.Skins;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectAurora.UI.Menu.Skins
{
    [DisallowMultipleComponent]
    public sealed class AuroraSkinSelectionController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private AuroraSkinCatalog catalog;
        [SerializeField] private AuroraSkinPreviewController previewController;
        [SerializeField] private bool wrapNavigation = true;

        [Header("Content")]
        [SerializeField] private Image splashImage;
        [SerializeField] private AspectRatioFitter splashAspect;
        [SerializeField] private TMP_Text skinNameText;
        [SerializeField] private TMP_Text skinDescriptionText;
        [SerializeField] private TMP_Text skinCounterText;
        [SerializeField] private TMP_Text selectedSkinStatusText;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private TMP_Text previewLoadingText;
        [SerializeField] private TMP_Text previewUnavailableText;

        [Header("Actions")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text selectButtonText;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private GameObject lockedBadge;

        private AuroraSkinSelectionService selectionService;
        private int viewedIndex;
        private bool listenersBound;

        public string ViewedSkinId => CurrentSkin == null ? string.Empty : CurrentSkin.Id;
        public string SelectedSkinId => selectionService == null ? string.Empty : selectionService.SelectedSkinId;
        public int ViewedIndex => viewedIndex;
        public bool PreviewCameraEnabled => previewController != null && previewController.CameraEnabled;
        public bool HasPreviewModel => previewController != null && previewController.HasPreview;
        public bool IsCurrentUnlocked => selectionService != null && selectionService.IsUnlocked(CurrentSkin);
        public bool CanSelectCurrent => selectionService != null && CurrentSkin != null && selectionService.CanSelect(CurrentSkin.Id);
        public string CurrentSplashName => CurrentSkin == null || CurrentSkin.SplashArt == null
            ? string.Empty
            : CurrentSkin.SplashArt.name;

        private AuroraSkinDefinition CurrentSkin =>
            catalog != null && catalog.Count > 0 && viewedIndex >= 0 && viewedIndex < catalog.Count
                ? catalog.Skins[viewedIndex]
                : null;

        private void Awake()
        {
            BindListeners();
            InitializeService();
        }

        private void OnEnable()
        {
            BindListeners();
            InitializeService();
            selectionService?.LoadSelectedSkin();
            viewedIndex = FindSkinIndex(selectionService == null ? string.Empty : selectionService.SelectedSkinId);
            RefreshView();

            if (EventSystem.current != null && nextButton != null)
            {
                EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
            }
        }

        private void OnDisable()
        {
            if (previewController != null)
            {
                previewController.ClosePreview();
            }
        }

        private void OnDestroy()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(NavigatePrevious);
            if (nextButton != null) nextButton.onClick.RemoveListener(NavigateNext);
            if (selectButton != null) selectButton.onClick.RemoveListener(TrySelectViewed);
            if (selectionService != null) selectionService.OnSelectedSkinChanged -= HandleSelectedSkinChanged;
        }

        private void Update()
        {
            bool previous = false;
            bool next = false;
            bool select = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                previous = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
                next = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
                select = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            }
#else
            previous = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            next = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            select = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
            if (previous) NavigatePrevious();
            else if (next) NavigateNext();
            else if (select) TrySelectViewed();
        }

        public void NavigatePrevious()
        {
            Navigate(-1);
        }

        public void NavigateNext()
        {
            Navigate(1);
        }

        public void TrySelectViewed()
        {
            AuroraSkinDefinition skin = CurrentSkin;
            if (skin != null && selectionService != null)
            {
                selectionService.TrySelect(skin.Id);
            }
        }

        public void RefreshView()
        {
            AuroraSkinDefinition skin = CurrentSkin;
            if (skin == null)
            {
                ApplyEmptyState();
                return;
            }

            if (splashImage != null)
            {
                splashImage.sprite = skin.SplashArt;
                splashImage.enabled = skin.SplashArt != null;
                splashImage.preserveAspect = true;
            }
            if (splashAspect != null)
            {
                splashAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                splashAspect.aspectRatio = 16f / 9f;
            }

            if (skinNameText != null) skinNameText.text = skin.DisplayName;
            if (skinDescriptionText != null) skinDescriptionText.text = skin.Description;
            if (skinCounterText != null) skinCounterText.text = (viewedIndex + 1).ToString("00") + " / " + catalog.Count.ToString("00");

            AuroraSkinDefinition equipped = selectionService == null ? null : selectionService.GetSelectedSkin();
            if (selectedSkinStatusText != null)
            {
                selectedSkinStatusText.text = equipped == null ? "EQUIPADA: --" : "EQUIPADA: " + equipped.DisplayName.ToUpperInvariant();
            }

            bool isEquipped = selectionService != null && skin.Id == selectionService.SelectedSkinId;
            bool isUnlocked = selectionService != null && selectionService.IsUnlocked(skin);
            bool hasSelectableModel = skin.HasSelectableModel;
            if (equippedBadge != null) equippedBadge.SetActive(isEquipped);
            if (lockedBadge != null) lockedBadge.SetActive(!isUnlocked);

            if (previewLoadingText != null) previewLoadingText.gameObject.SetActive(skin.HasPreviewModel);
            bool previewAvailable = previewController != null && previewController.Show(skin);
            if (previewImage != null) previewImage.gameObject.SetActive(previewAvailable);
            if (previewLoadingText != null) previewLoadingText.gameObject.SetActive(false);
            if (previewUnavailableText != null)
            {
                previewUnavailableText.gameObject.SetActive(!previewAvailable);
                previewUnavailableText.text = "MODELO 3D\nINDISPONIVEL";
            }

            ApplyActionState(isEquipped, isUnlocked, hasSelectableModel);
        }

        private void Navigate(int direction)
        {
            if (catalog == null || catalog.Count <= 1)
            {
                return;
            }

            int nextIndex = viewedIndex + direction;
            if (wrapNavigation)
            {
                nextIndex = (nextIndex % catalog.Count + catalog.Count) % catalog.Count;
            }
            else
            {
                nextIndex = Mathf.Clamp(nextIndex, 0, catalog.Count - 1);
            }

            if (nextIndex == viewedIndex)
            {
                return;
            }

            viewedIndex = nextIndex;
            RefreshView();
        }

        private void ApplyActionState(bool isEquipped, bool isUnlocked, bool hasSelectableModel)
        {
            string label;
            bool interactable;
            if (isEquipped)
            {
                label = "EQUIPADA";
                interactable = false;
            }
            else if (!isUnlocked)
            {
                label = "BLOQUEADA";
                interactable = false;
            }
            else if (!hasSelectableModel)
            {
                label = "INDISPONIVEL";
                interactable = false;
            }
            else
            {
                label = "SELECIONAR";
                interactable = true;
            }

            if (selectButtonText != null) selectButtonText.text = label;
            if (selectButton != null) selectButton.interactable = interactable;
        }

        private void ApplyEmptyState()
        {
            if (skinNameText != null) skinNameText.text = "CATALOGO INDISPONIVEL";
            if (skinDescriptionText != null) skinDescriptionText.text = "Nenhuma skin foi cadastrada.";
            if (skinCounterText != null) skinCounterText.text = "00 / 00";
            if (splashImage != null) splashImage.enabled = false;
            if (previewImage != null) previewImage.gameObject.SetActive(false);
            if (previewUnavailableText != null) previewUnavailableText.gameObject.SetActive(true);
            if (previewController != null) previewController.ClosePreview();
            ApplyActionState(false, false, false);
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (previousButton != null) previousButton.onClick.AddListener(NavigatePrevious);
            if (nextButton != null) nextButton.onClick.AddListener(NavigateNext);
            if (selectButton != null) selectButton.onClick.AddListener(TrySelectViewed);
            listenersBound = true;
        }

        private void InitializeService()
        {
            if (selectionService != null || catalog == null)
            {
                return;
            }

            selectionService = new AuroraSkinSelectionService(catalog);
            selectionService.OnSelectedSkinChanged += HandleSelectedSkinChanged;
        }

        private void HandleSelectedSkinChanged(string skinId)
        {
            RefreshView();
        }

        private int FindSkinIndex(string skinId)
        {
            if (catalog == null || catalog.Count == 0)
            {
                return 0;
            }

            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog.Skins[i] != null && catalog.Skins[i].Id == skinId)
                {
                    return i;
                }
            }

            return 0;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            AuroraSkinCatalog skinCatalog,
            AuroraSkinPreviewController targetPreview,
            Image targetSplash,
            AspectRatioFitter targetAspect,
            TMP_Text targetName,
            TMP_Text targetDescription,
            TMP_Text targetCounter,
            TMP_Text targetSelectedStatus,
            RawImage targetPreviewImage,
            TMP_Text targetLoading,
            TMP_Text targetUnavailable,
            Button targetPrevious,
            Button targetNext,
            Button targetSelect,
            TMP_Text targetSelectLabel,
            GameObject targetEquippedBadge,
            GameObject targetLockedBadge)
        {
            catalog = skinCatalog;
            previewController = targetPreview;
            splashImage = targetSplash;
            splashAspect = targetAspect;
            skinNameText = targetName;
            skinDescriptionText = targetDescription;
            skinCounterText = targetCounter;
            selectedSkinStatusText = targetSelectedStatus;
            previewImage = targetPreviewImage;
            previewLoadingText = targetLoading;
            previewUnavailableText = targetUnavailable;
            previousButton = targetPrevious;
            nextButton = targetNext;
            selectButton = targetSelect;
            selectButtonText = targetSelectLabel;
            equippedBadge = targetEquippedBadge;
            lockedBadge = targetLockedBadge;
        }
#endif
    }
}
