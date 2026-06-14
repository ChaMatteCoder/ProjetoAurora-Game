using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public AuroraGameplayHUDController auroraHud;
    public Text sectorText;
    public Text distanceText;
    public Text livesText;
    public Text celestiaText;
    public Text interactionText;
    public GameObject interactionPrompt;
    public GameObject sectorCard;
    public Text sectorCardText;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject finalPanel;
    public GameObject introPanel;
    public Text introText;
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button pauseMenuButton;
    public Button gameOverRestartButton;
    public Button gameOverMenuButton;
    public Button finalRestartButton;
    public Button finalMenuButton;

    private Coroutine sectorRoutine;

    private void Start()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("PROJETO:AURORA - GameManager nao encontrado para configurar a UI.");
            return;
        }

        resumeButton?.onClick.AddListener(game.Resume);
        pauseRestartButton?.onClick.AddListener(game.Restart);
        pauseMenuButton?.onClick.AddListener(game.ReturnToMenu);
        gameOverRestartButton?.onClick.AddListener(game.Restart);
        gameOverMenuButton?.onClick.AddListener(game.ReturnToMenu);
        finalRestartButton?.onClick.AddListener(game.Restart);
        finalMenuButton?.onClick.AddListener(game.ReturnToMenu);
    }

    public void SetSector(string value)
    {
        auroraHud?.SetSector(value);
        if (sectorText != null)
        {
            sectorText.text = "SETOR: " + value;
        }

        if (sectorRoutine != null)
        {
            StopCoroutine(sectorRoutine);
        }
        if (sectorCard != null && sectorCardText != null)
        {
            sectorRoutine = StartCoroutine(ShowSectorCard(value));
        }
    }

    public void SetDistance(float value, float total)
    {
        auroraHud?.SetDistance(value, total);
        if (distanceText != null)
        {
            distanceText.text = $"DISTÂNCIA: {Mathf.FloorToInt(value)}m / {Mathf.FloorToInt(total)}m";
        }
    }

    public void SetLives(int value)
    {
        auroraHud?.SetIntegrity(value, 3);
        if (livesText != null)
        {
            livesText.text = "VIDAS: " + value;
        }
    }

    public void SetCelestIA(string value)
    {
        auroraHud?.SetDialogue("CELESTIA", value);
        if (celestiaText != null)
        {
            celestiaText.text = value;
        }
    }

    public void SetDialogue(string speaker, string message)
    {
        auroraHud?.SetDialogue(speaker, message);
        if (celestiaText != null)
        {
            celestiaText.text = $"<b>{speaker}</b>\n{message}";
        }
    }

    public void SetCelestIAColor(Color color)
    {
        auroraHud?.SetCelestIAColor(color);
        if (celestiaText == null)
        {
            return;
        }

        celestiaText.color = color;
        Image panel = celestiaText.GetComponentInParent<Image>();
        if (panel != null)
        {
            Color panelColor = color;
            panelColor.a = 0.24f;
            panel.color = panelColor;
        }
    }

    public void SetCelestIAState(CelestIAState state) => auroraHud?.SetCelestIAState(state);

    public void SetPause(bool value)
    {
        auroraHud?.SetPause(value);
        if (pausePanel != null)
        {
            pausePanel.SetActive(value);
        }
    }

    public void SetGameOver(bool value)
    {
        auroraHud?.SetFailure(value);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(value);
        }
    }

    public void SetFinal(bool value)
    {
        auroraHud?.SetFinal(value);
        if (finalPanel != null)
        {
            finalPanel.SetActive(value);
        }
    }

    public void SetInteractionPrompt(bool visible, string message)
    {
        auroraHud?.SetInteractionPrompt(visible, message);
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(visible);
        }
        if (interactionText != null)
        {
            interactionText.text = message;
        }
    }

    public void ShowIntro(bool value, string message)
    {
        auroraHud?.ShowIntro(value, message);
        if (introPanel != null)
        {
            introPanel.SetActive(value);
        }
        if (introText != null)
        {
            introText.text = message;
        }
    }

    public void SetIntroText(string message)
    {
        auroraHud?.SetIntroText(message);
        if (introText != null)
        {
            introText.text = message;
        }
    }

    private IEnumerator ShowSectorCard(string sector)
    {
        sectorCardText.text = sector.ToUpperInvariant();
        sectorCard.SetActive(true);
        CanvasGroup group = sectorCard.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        while (group.alpha < 1f)
        {
            group.alpha += Time.deltaTime * 3f;
            yield return null;
        }

        yield return new WaitForSeconds(2f);

        while (group.alpha > 0f)
        {
            group.alpha -= Time.deltaTime * 2f;
            yield return null;
        }

        sectorCard.SetActive(false);
    }
}
