using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectAurora.UI
{
    /// <summary>
    /// Controlador do Menu Principal de PROJETO:AURORA - Falha de Contenção.
    /// Versão organizada do menu. Mantida em namespace próprio para conviver
    /// com o MainMenuController legado (Assets/Scripts) sem colisão de tipo.
    /// Todas as referências são serializadas no Inspector (sem caminhos absolutos).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Botões principais")]
        public Button playButton;
        public Button settingsButton;
        public Button extraButton;
        public Button creditsButton;
        public Button quitButton;

        [Header("Painéis (começam desativados)")]
        public GameObject settingsPanel;
        public GameObject extraPanel;
        public GameObject creditsPanel;

        [Header("Botões Voltar dos painéis")]
        public Button settingsBackButton;
        public Button extraBackButton;
        public Button creditsBackButton;

        [Header("Carregamento de gameplay")]
        [Tooltip("Cenas de gameplay tentadas em ordem. A primeira encontrada nos Build Settings é carregada.")]
        public string[] gameplaySceneCandidates =
        {
            "Fase01_SetorA_LaboratorioLimpo"
        };

        private void Start()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(() => OpenPanel(settingsPanel));
            if (extraButton != null) extraButton.onClick.AddListener(() => OpenPanel(extraPanel));
            if (creditsButton != null) creditsButton.onClick.AddListener(() => OpenPanel(creditsPanel));
            if (quitButton != null) quitButton.onClick.AddListener(Quit);

            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseAllPanels);
            if (extraBackButton != null) extraBackButton.onClick.AddListener(CloseAllPanels);
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(CloseAllPanels);

            CloseAllPanels();
            // Não selecionar nenhum botão por padrão: evita o JOGAR parecer "em hover" ao iniciar.
            ClearSelection();
        }

        private void Update()
        {
            // Esc fecha qualquer painel aberto. Sem painel aberto, não faz nada (não sai do jogo).
            if (EscapePressedThisFrame() && AnyPanelOpen())
            {
                CloseAllPanels();
            }
        }

        // ---------- JOGAR ----------
        public void Play()
        {
            foreach (string candidate in gameplaySceneCandidates)
            {
                if (string.IsNullOrEmpty(candidate)) continue;
                if (IsSceneInBuildSettings(candidate))
                {
                    SceneManager.LoadScene(candidate);
                    return;
                }
            }
            Debug.LogWarning("Nenhuma cena de gameplay encontrada nos Build Settings.");
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // ---------- PAINÉIS ----------
        public void OpenPanel(GameObject panel)
        {
            if (panel == null) return;
            CloseAllPanels();
            panel.SetActive(true);

            // Seleciona o primeiro Selectable do painel (ex.: botão Voltar) para teclado/gamepad.
            Selectable first = panel.GetComponentInChildren<Selectable>(true);
            if (first != null) SetSelected(first.gameObject);
        }

        public void CloseAllPanels()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (extraPanel != null) extraPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            // Ao fechar painéis, limpa a seleção (nenhum botão fica destacado).
            ClearSelection();
        }

        private bool AnyPanelOpen()
        {
            return (settingsPanel != null && settingsPanel.activeSelf)
                || (extraPanel != null && extraPanel.activeSelf)
                || (creditsPanel != null && creditsPanel.activeSelf);
        }

        // ---------- SAIR ----------
        public void Quit()
        {
            Debug.Log("PROJETO:AURORA - Solicitação para sair do jogo.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Navegação / utilidades ----------
        private static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private static void SetSelected(GameObject target)
        {
            if (EventSystem.current == null || target == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
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
