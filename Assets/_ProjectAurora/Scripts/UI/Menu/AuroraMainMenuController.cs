using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectAurora.UI.Menu
{
    public sealed class AuroraMainMenuController : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] private RectTransform cardsContainer;
        [SerializeField] private AuroraMenuCard cardPrefab;
        [SerializeField] private List<AuroraMenuCard> cards = new List<AuroraMenuCard>();

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject extraPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Gameplay")]
        [SerializeField] private string[] gameplaySceneCandidates =
        {
            "Beta03_Principal",
            "Gameplay",
            "RunnerScene",
            "GameScene",
            "SampleScene",
            "Fase01_SetorA_LaboratorioLimpo"
        };

        private AuroraMenuCard activeCard;
        private bool listenersBound;

        private void Awake()
        {
            DiscoverCards();
            CloseAllPanels();
        }

        private void Start()
        {
            BindCards();
            BindBackButtons();
            SelectInitialCard();
        }

        private void Update()
        {
            if (EscapePressedThisFrame() && AnyPanelOpen())
            {
                CloseAllPanels();
            }
        }

        public void SetCardsContainer(RectTransform container)
        {
            cardsContainer = container;
            DiscoverCards();
        }

        public void SetPanels(GameObject settings, GameObject extras, GameObject credits)
        {
            settingsPanel = settings;
            extraPanel = extras;
            creditsPanel = credits;
        }

        public void RegisterCard(AuroraMenuCard card)
        {
            if (card != null && !cards.Contains(card))
            {
                cards.Add(card);
            }
        }

        public void StartGame()
        {
            CloseAllPanels();
            foreach (string candidate in gameplaySceneCandidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (IsSceneInBuildSettings(candidate))
                {
                    SceneManager.LoadScene(candidate);
                    return;
                }
            }

            Debug.LogWarning("PROJETO:AURORA - Nenhuma cena de gameplay encontrada nos Build Settings.");
        }

        public void OpenSettings()
        {
            OpenPanel(settingsPanel, "CONFIGURACOES");
        }

        public void OpenExtras()
        {
            OpenPanel(extraPanel, "EXTRA");
        }

        public void OpenCredits()
        {
            OpenPanel(creditsPanel, "CREDITOS");
        }

        public void QuitGame()
        {
            Debug.Log("PROJETO:AURORA - Saindo do jogo pelo menu principal.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BindCards()
        {
            if (listenersBound)
            {
                return;
            }

            DiscoverCards();
            foreach (AuroraMenuCard card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                AuroraMenuCard captured = card;
                captured.OnClick.AddListener(() => HandleCardClick(captured));
            }

            listenersBound = true;
        }

        private void BindBackButtons()
        {
            BindBackButtons(settingsPanel);
            BindBackButtons(extraPanel);
            BindBackButtons(creditsPanel);
        }

        private void BindBackButtons(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                string buttonName = button.name.ToUpperInvariant();
                if (buttonName.Contains("VOLTAR") || buttonName.Contains("BACK"))
                {
                    button.onClick.AddListener(CloseAllPanels);
                }
            }
        }

        private void HandleCardClick(AuroraMenuCard card)
        {
            SetActiveCard(card);
            switch (card.Action)
            {
                case AuroraMenuCardAction.StartGame:
                    StartGame();
                    break;
                case AuroraMenuCardAction.OpenSettings:
                    OpenSettings();
                    break;
                case AuroraMenuCardAction.OpenExtras:
                    OpenExtras();
                    break;
                case AuroraMenuCardAction.OpenCredits:
                    OpenCredits();
                    break;
                case AuroraMenuCardAction.QuitGame:
                    QuitGame();
                    break;
                default:
                    Debug.LogWarning("PROJETO:AURORA - Acao de menu nao configurada: " + card.Action);
                    break;
            }
        }

        private void SelectInitialCard()
        {
            DiscoverCards();
            if (cards.Count == 0)
            {
                return;
            }

            SetActiveCard(cards[0]);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
            }
        }

        private void SetActiveCard(AuroraMenuCard card)
        {
            activeCard = card;
            foreach (AuroraMenuCard item in cards)
            {
                if (item != null)
                {
                    item.SetSelected(item == activeCard);
                }
            }
        }

        private void DiscoverCards()
        {
            cards.RemoveAll(item => item == null);
            if (cardsContainer == null)
            {
                return;
            }

            AuroraMenuCard[] found = cardsContainer.GetComponentsInChildren<AuroraMenuCard>(true);
            foreach (AuroraMenuCard card in found)
            {
                if (!cards.Contains(card))
                {
                    cards.Add(card);
                }
            }
        }

        private void OpenPanel(GameObject panel, string fallbackName)
        {
            CloseAllPanels();
            if (panel == null)
            {
                Debug.Log("PROJETO:AURORA - Painel placeholder acionado: " + fallbackName);
                return;
            }

            panel.SetActive(true);
            Selectable first = panel.GetComponentInChildren<Selectable>(true);
            if (first != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        private void CloseAllPanels()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            if (extraPanel != null)
            {
                extraPanel.SetActive(false);
            }
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }
        }

        private bool AnyPanelOpen()
        {
            return settingsPanel != null && settingsPanel.activeSelf ||
                   extraPanel != null && extraPanel.activeSelf ||
                   creditsPanel != null && creditsPanel.activeSelf;
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EscapePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
