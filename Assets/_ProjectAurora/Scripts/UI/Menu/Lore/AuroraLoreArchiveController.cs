using ProjectAurora.Lore;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectAurora.UI.Menu.Lore
{
    [DisallowMultipleComponent]
    public sealed class AuroraLoreArchiveController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private AuroraLoreCatalog catalog;
        [SerializeField] private bool wrapNavigation = true;

        [Header("Header")]
        [SerializeField] private TMP_Text unlockedCounterText;
        [SerializeField] private TMP_Text auroraCoinBalanceText;

        [Header("File card")]
        [SerializeField] private TMP_Text fileIdText;
        [SerializeField] private TMP_Text fileTitleText;
        [SerializeField] private TMP_Text unlockTypeText;
        [SerializeField] private TMP_Text fileStateText;
        [SerializeField] private TMP_Text positionCounterText;
        [SerializeField] private Image stateAccent;
        [SerializeField] private GameObject lockOverlay;

        [Header("Content")]
        [SerializeField] private TMP_Text contentTitleText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text fullLoreText;
        [SerializeField] private ScrollRect contentScrollRect;

        [Header("Actions")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text purchaseButtonText;
        [SerializeField] private TMP_Text actionMessageText;

        private AuroraLoreService loreService;
        private AuroraCoinWallet wallet;
        private int viewedIndex;
        private bool listenersBound;
        private bool eventsBound;

        private static readonly Color Cyan = new Color(0.04f, 0.9f, 1f, 1f);
        private static readonly Color Amber = new Color(1f, 0.62f, 0.18f, 1f);
        private static readonly Color Red = new Color(0.95f, 0.16f, 0.22f, 1f);
        private static readonly Color Muted = new Color(0.46f, 0.62f, 0.67f, 1f);

        public int ViewedIndex => viewedIndex;
        public string ViewedLoreId => Current == null ? string.Empty : Current.Id;
        public int EntryCount => catalog == null ? 0 : catalog.Count;
        public bool IsCurrentUnlocked => Current != null && loreService != null && loreService.IsUnlocked(Current.Id);
        public bool IsPurchaseVisible => purchaseButton != null && purchaseButton.gameObject.activeSelf;

        private AuroraLoreDefinition Current =>
            catalog != null && catalog.Count > 0 && viewedIndex >= 0 && viewedIndex < catalog.Count
                ? catalog.Entries[viewedIndex]
                : null;

        private void Awake()
        {
            BindButtonListeners();
        }

        private void OnEnable()
        {
            BindButtonListeners();
            InitializeService();
            BindEvents();
            viewedIndex = Mathf.Clamp(viewedIndex, 0, Mathf.Max(0, EntryCount - 1));
            RefreshView();
            if (EventSystem.current != null && nextButton != null)
                EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
            if (previousButton != null) previousButton.onClick.RemoveListener(NavigatePrevious);
            if (nextButton != null) nextButton.onClick.RemoveListener(NavigateNext);
            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(TryPurchaseCurrent);
        }

        private void Update()
        {
            bool previous = false;
            bool next = false;
            bool purchase = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                previous = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
                next = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
                purchase = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            }
#else
            previous = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            next = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            purchase = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
            if (previous) NavigatePrevious();
            else if (next) NavigateNext();
            else if (purchase && IsPurchaseVisible) TryPurchaseCurrent();
        }

        public void NavigatePrevious()
        {
            Navigate(-1);
        }

        public void NavigateNext()
        {
            Navigate(1);
        }

        public void TryPurchaseCurrent()
        {
            AuroraLoreDefinition definition = Current;
            if (definition == null || loreService == null ||
                definition.UnlockType != AuroraLoreUnlockType.AuroraCoinPurchase)
            {
                return;
            }

            if (loreService.IsUnlocked(definition.Id))
            {
                SetActionMessage("ARQUIVO JÁ DESBLOQUEADO", Cyan);
                RefreshView(false);
                return;
            }

            if (wallet == null || wallet.Balance < definition.AuroraCoinPrice)
            {
                SetActionMessage("AURORACOINS INSUFICIENTES", Red);
                return;
            }

            if (!loreService.TryPurchase(definition.Id))
            {
                SetActionMessage("NÃO FOI POSSÍVEL DESBLOQUEAR O ARQUIVO", Red);
                return;
            }

            SetActionMessage("ARQUIVO DESBLOQUEADO", Cyan);
            RefreshView();
        }

        public void RefreshView()
        {
            RefreshView(true);
        }

        private void RefreshView(bool resetScroll)
        {
            AuroraLoreDefinition definition = Current;
            RefreshHeader();
            if (definition == null)
            {
                ApplyEmptyState();
                return;
            }

            bool unlocked = loreService != null && loreService.IsUnlocked(definition.Id);
            bool secretLocked = definition.UnlockType == AuroraLoreUnlockType.SecretMission && !unlocked;
            bool collectibleLocked = definition.UnlockType == AuroraLoreUnlockType.GameplayCollectible && !unlocked;
            bool purchasableLocked = definition.UnlockType == AuroraLoreUnlockType.AuroraCoinPurchase && !unlocked;
            string visibleTitle = secretLocked
                ? "ARQUIVO SECRETO"
                : collectibleLocked ? "ARQUIVO NÃO LOCALIZADO" : definition.DisplayName;

            if (fileIdText != null) fileIdText.text = definition.Id;
            if (fileTitleText != null) fileTitleText.text = visibleTitle.ToUpperInvariant();
            if (unlockTypeText != null) unlockTypeText.text = definition.CategoryName;
            if (positionCounterText != null)
                positionCounterText.text = (viewedIndex + 1).ToString("00") + " / " + EntryCount.ToString("00");
            if (contentTitleText != null) contentTitleText.text = visibleTitle.ToUpperInvariant();
            if (categoryText != null) categoryText.text = definition.CategoryName;
            if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
            if (!unlocked && lockOverlay != null)
            {
                Image overlayImage = lockOverlay.GetComponent<Image>();
                if (overlayImage != null)
                {
                    Color overlayColor = secretLocked ? Red : purchasableLocked ? Amber : Muted;
                    overlayColor.a = secretLocked ? 0.16f : 0.08f;
                    overlayImage.color = overlayColor;
                }
            }

            if (unlocked)
            {
                ApplyState("DESBLOQUEADO", Cyan);
                if (fullLoreText != null)
                    fullLoreText.text = AuroraLoreTextFormatter.FormatForDisplay(
                        definition.FullText == null ? string.Empty : definition.FullText.text);
                SetPurchaseVisible(false, string.Empty);
                SetActionMessage("ARQUIVO DISPONÍVEL PARA LEITURA", Cyan);
            }
            else if (collectibleLocked)
            {
                ApplyState("NÃO LOCALIZADO", Muted);
                if (fullLoreText != null)
                    fullLoreText.text = "DATAFILE NÃO LOCALIZADO\n\nEncontre este arquivo durante a gameplay.";
                SetPurchaseVisible(false, string.Empty);
                SetActionMessage("RECUPERAÇÃO DISPONÍVEL EM CAMPO", Muted);
            }
            else if (purchasableLocked)
            {
                ApplyState("CRIPTOGRAFADO", Amber);
                if (fullLoreText != null)
                    fullLoreText.text = definition.ShortDescription +
                                        "\n\nEste registro pode ser descriptografado com AuroraCoins.";
                SetPurchaseVisible(true,
                    "DESBLOQUEAR — " + definition.AuroraCoinPrice + " AURORACOINS");
                SetActionMessage("SALDO ATUAL: " + (wallet == null ? 0 : wallet.Balance) + " AURORACOINS", Amber);
            }
            else if (secretLocked)
            {
                ApplyState("CLASSIFICADO", Red);
                if (fullLoreText != null)
                    fullLoreText.text = "CONTEÚDO CLASSIFICADO\n\nConclua uma missão secreta para desbloquear este arquivo.";
                SetPurchaseVisible(false, string.Empty);
                SetActionMessage("ACESSO RESTRITO", Red);
            }
            else
            {
                ApplyState("BLOQUEADO", Muted);
                if (fullLoreText != null) fullLoreText.text = "ARQUIVO INDISPONÍVEL";
                SetPurchaseVisible(false, string.Empty);
            }

            if (resetScroll && contentScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                contentScrollRect.StopMovement();
                contentScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void Navigate(int direction)
        {
            if (EntryCount <= 1) return;
            int nextIndex = viewedIndex + direction;
            nextIndex = wrapNavigation
                ? (nextIndex % EntryCount + EntryCount) % EntryCount
                : Mathf.Clamp(nextIndex, 0, EntryCount - 1);
            if (nextIndex == viewedIndex) return;
            viewedIndex = nextIndex;
            RefreshView();
        }

        private void InitializeService()
        {
            wallet = AuroraCoinWallet.Instance;
            loreService = AuroraLoreService.Initialize(catalog, wallet);
        }

        private void BindButtonListeners()
        {
            if (listenersBound) return;
            if (previousButton != null) previousButton.onClick.AddListener(NavigatePrevious);
            if (nextButton != null) nextButton.onClick.AddListener(NavigateNext);
            if (purchaseButton != null) purchaseButton.onClick.AddListener(TryPurchaseCurrent);
            listenersBound = true;
        }

        private void BindEvents()
        {
            if (eventsBound) return;
            if (wallet != null) wallet.OnBalanceChanged += HandleBalanceChanged;
            if (loreService != null) loreService.OnLoreUnlocked += HandleLoreChanged;
            eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!eventsBound) return;
            if (wallet != null) wallet.OnBalanceChanged -= HandleBalanceChanged;
            if (loreService != null) loreService.OnLoreUnlocked -= HandleLoreChanged;
            eventsBound = false;
        }

        private void HandleBalanceChanged(int balance)
        {
            RefreshHeader();
            if (Current != null && Current.UnlockType == AuroraLoreUnlockType.AuroraCoinPurchase &&
                loreService != null && !loreService.IsUnlocked(Current.Id))
                SetActionMessage("SALDO ATUAL: " + balance + " AURORACOINS", Amber);
        }

        private void HandleLoreChanged(string loreId)
        {
            RefreshView();
        }

        private void RefreshHeader()
        {
            int unlocked = loreService == null ? 0 : loreService.UnlockedCount;
            if (unlockedCounterText != null)
                unlockedCounterText.text = "ARQUIVOS DESBLOQUEADOS: " + unlocked.ToString("00") +
                                           " / " + EntryCount.ToString("00");
            if (auroraCoinBalanceText != null)
                auroraCoinBalanceText.text = "AURORACOINS: " + (wallet == null ? 0 : wallet.Balance);
        }

        private void ApplyState(string label, Color color)
        {
            if (fileStateText != null)
            {
                fileStateText.text = label;
                fileStateText.color = color;
            }
            if (stateAccent != null) stateAccent.color = color;
        }

        private void SetPurchaseVisible(bool visible, string label)
        {
            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(visible);
                purchaseButton.interactable = visible;
            }
            if (purchaseButtonText != null) purchaseButtonText.text = label;
        }

        private void SetActionMessage(string message, Color color)
        {
            if (actionMessageText == null) return;
            actionMessageText.text = message;
            actionMessageText.color = color;
        }

        private void ApplyEmptyState()
        {
            if (fileIdText != null) fileIdText.text = "LORE_---";
            if (fileTitleText != null) fileTitleText.text = "CATÁLOGO INDISPONÍVEL";
            if (unlockTypeText != null) unlockTypeText.text = "ARQUIVO";
            if (fileStateText != null) fileStateText.text = "SEM DADOS";
            if (positionCounterText != null) positionCounterText.text = "00 / 00";
            if (contentTitleText != null) contentTitleText.text = "CATÁLOGO INDISPONÍVEL";
            if (categoryText != null) categoryText.text = string.Empty;
            if (fullLoreText != null) fullLoreText.text = "Nenhum arquivo foi cadastrado.";
            SetPurchaseVisible(false, string.Empty);
            SetActionMessage("RECONSTRUA O CATÁLOGO DE LORE", Red);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            AuroraLoreCatalog loreCatalog,
            TMP_Text targetUnlockedCounter,
            TMP_Text targetBalance,
            TMP_Text targetFileId,
            TMP_Text targetFileTitle,
            TMP_Text targetUnlockType,
            TMP_Text targetFileState,
            TMP_Text targetPositionCounter,
            Image targetStateAccent,
            GameObject targetLockOverlay,
            TMP_Text targetContentTitle,
            TMP_Text targetCategory,
            TMP_Text targetFullText,
            ScrollRect targetScrollRect,
            Button targetPrevious,
            Button targetNext,
            Button targetPurchase,
            TMP_Text targetPurchaseLabel,
            TMP_Text targetActionMessage)
        {
            catalog = loreCatalog;
            unlockedCounterText = targetUnlockedCounter;
            auroraCoinBalanceText = targetBalance;
            fileIdText = targetFileId;
            fileTitleText = targetFileTitle;
            unlockTypeText = targetUnlockType;
            fileStateText = targetFileState;
            positionCounterText = targetPositionCounter;
            stateAccent = targetStateAccent;
            lockOverlay = targetLockOverlay;
            contentTitleText = targetContentTitle;
            categoryText = targetCategory;
            fullLoreText = targetFullText;
            contentScrollRect = targetScrollRect;
            previousButton = targetPrevious;
            nextButton = targetNext;
            purchaseButton = targetPurchase;
            purchaseButtonText = targetPurchaseLabel;
            actionMessageText = targetActionMessage;
        }
#endif
    }
}
