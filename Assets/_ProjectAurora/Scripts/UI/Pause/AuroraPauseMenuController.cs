using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAurora.UI.Pause
{
    /// Menu de pausa da gameplay (Round 10).
    /// NAO cria sistema de pausa novo: o GameManager continua dono do ESC/Time.timeScale/
    /// audio (TogglePause). Este painel e ativado/desativado pelo fluxo EXISTENTE
    /// (UIManager.SetPause -> hud.pausePanel) e apenas oferece os botoes.
    public class AuroraPauseMenuController : MonoBehaviour
    {
        [Header("Paineis")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text confirmText;

        [Header("Botoes principais")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button quitButton;

        [Header("Botoes auxiliares")]
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("Tutorial (Round 18)")]
        [Tooltip("Botao 'PULAR TUTORIAL' — visivel apenas quando a pausa e aberta no tutorial.")]
        [SerializeField] private Button skipTutorialButton;
        [Tooltip("Hint 'ESC retoma a corrida' — ocultado quando o botao Pular ocupa a area.")]
        [SerializeField] private GameObject hintObject;

        private System.Action pendingConfirmAction;

        private void Awake()
        {
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (restartButton != null) restartButton.onClick.AddListener(() =>
                AskConfirm("Reiniciar a corrida?", RestartRun));
            if (menuButton != null) menuButton.onClick.AddListener(() =>
                AskConfirm("Voltar ao menu principal?", ReturnToMenu));
            if (quitButton != null) quitButton.onClick.AddListener(() =>
                AskConfirm("Sair do jogo?", QuitGame));
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(ShowMain);
            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(RunConfirm);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(ShowMain);
            if (skipTutorialButton != null) skipTutorialButton.onClick.AddListener(() =>
                AskConfirm("Pular o tutorial e ir direto para a gameplay?", SkipTutorial));
        }

        private void OnEnable()
        {
            ShowMain();
        }

        private void OnDisable()
        {
            // pause fechou (ESC/Continuar): garante que subpaineis nao ficam abertos
            pendingConfirmAction = null;
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        private void ShowMain()
        {
            pendingConfirmAction = null;
            // O botao "PULAR TUTORIAL" so aparece quando a pausa foi aberta no tutorial;
            // nesse caso o hint padrao ("ESC retoma a corrida") cede o espaco.
            bool tutorialPause = GameManager.Instance != null && GameManager.Instance.IsPausedFromTutorial;
            if (skipTutorialButton != null) skipTutorialButton.gameObject.SetActive(tutorialPause);
            if (hintObject != null) hintObject.SetActive(!tutorialPause);
            if (mainPanel != null) mainPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        private void SkipTutorial()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SkipTutorialFromPause();
            }
        }

        private void OpenSettings()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        private void AskConfirm(string message, System.Action action)
        {
            pendingConfirmAction = action;
            if (confirmText != null) confirmText.text = message;
            if (mainPanel != null) mainPanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(true);
        }

        private void RunConfirm()
        {
            System.Action action = pendingConfirmAction;
            pendingConfirmAction = null;
            action?.Invoke();
        }

        private void Continue()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Resume(); // restaura timeScale/audio e fecha este painel
            }
        }

        private void RestartRun()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Restart(); // ja restaura Time.timeScale = 1
            }
        }

        private void ReturnToMenu()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMenu(); // ja restaura Time.timeScale = 1
            }
        }

        private void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
