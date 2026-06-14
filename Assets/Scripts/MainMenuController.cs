using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject controlsPanel;
    public GameObject settingsPanel;
    public GameObject extraPanel;
    public GameObject creditsPanel;
    public Button playButton;
    public Button controlsButton;
    public Button settingsButton;
    public Button extraButton;
    public Button creditsButton;
    public Button quitButton;
    public Button controlsCloseButton;
    public Button settingsCloseButton;
    public Button extraCloseButton;
    public Button creditsCloseButton;

    private void Start()
    {
        playButton.onClick.AddListener(Play);
        controlsButton.onClick.AddListener(() => ShowPanel(controlsPanel));
        settingsButton.onClick.AddListener(() => ShowPanel(settingsPanel));
        extraButton.onClick.AddListener(() => ShowPanel(extraPanel));
        creditsButton.onClick.AddListener(() => ShowPanel(creditsPanel));
        quitButton.onClick.AddListener(Quit);
        controlsCloseButton.onClick.AddListener(ClosePanels);
        settingsCloseButton.onClick.AddListener(ClosePanels);
        extraCloseButton.onClick.AddListener(ClosePanels);
        creditsCloseButton.onClick.AddListener(ClosePanels);
        ClosePanels();
    }

    public void Play() => SceneManager.LoadScene("Game");

    public void ClosePanels()
    {
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        extraPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    private void ShowPanel(GameObject panel)
    {
        ClosePanels();
        panel.SetActive(true);
    }

    public void Quit()
    {
        Debug.Log("PROJETO:AURORA - Solicitação para sair do jogo.");
        Application.Quit();
    }
}
