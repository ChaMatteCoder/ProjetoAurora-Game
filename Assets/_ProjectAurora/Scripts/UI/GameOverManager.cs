using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas gameOverCanvas;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Image fadeBackground;
    [SerializeField] private Image impactFlash;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text celestIAMessageText;
    [SerializeField] private TMP_Text diagnosticText;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    [Header("Audio")]
    [SerializeField] private AudioSource gameOverMusicSource;
    [SerializeField] private AudioClip gameOverMusic;

    [Header("Gameplay")]
    [SerializeField] private PlayerRunner player;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private Behaviour[] systemsToDisable;
    [SerializeField] private Collider[] hazardColliders;

    [Header("Timing")]
    [SerializeField, Min(1f)] private float totalSequenceDuration = 14f;
    [SerializeField, Min(0f)] private float gameplayMusicFadeDuration = 0.75f;

    private bool isGameOverSequenceRunning;
    private Coroutine sequenceRoutine;
    private Vector2 titleBasePosition;

    public bool IsGameOverActive => isGameOverSequenceRunning;

    private void Awake()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerRunner>();
        }

        if (playerInteraction == null && player != null)
        {
            playerInteraction = player.GetComponent<PlayerInteraction>();
        }

        retryButton?.onClick.AddListener(RetryCurrentLevel);
        menuButton?.onClick.AddListener(ReturnToMainMenu);

        if (titleText != null)
        {
            titleBasePosition = titleText.rectTransform.anchoredPosition;
        }

        ResetPresentation();
    }

    private void OnDestroy()
    {
        retryButton?.onClick.RemoveListener(RetryCurrentLevel);
        menuButton?.onClick.RemoveListener(ReturnToMainMenu);
    }

    public void TriggerGameOver()
    {
        StartSequence(
            "SINAL VITAL PERDIDO",
            "GAME OVER",
            "CELESTIA: Dr. Elias não responde. Encerrando protocolo de evacuação.",
            new Color(1f, 0.12f, 0.12f));
    }

    public void TriggerGameCompleted()
    {
        StartSequence(
            "PROTOCOLO AURORA CONCLUÍDO",
            "FIM DA CONTENÇÃO",
            "CELESTIA: Dr. Elias, sua autorização foi revogada. O Protocolo Aurora continuará ativo.",
            new Color(0.08f, 0.88f, 1f));
    }

    public void RetryCurrentLevel()
    {
        if (!ButtonsAreAvailable())
        {
            return;
        }

        PrepareSceneChange();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    public void ReturnToMainMenu()
    {
        if (!ButtonsAreAvailable())
        {
            return;
        }

        const string menuScene = "MainMenu";
        if (!SceneExistsInBuildSettings(menuScene))
        {
            Debug.LogWarning("Cena MainMenu não encontrada nos Build Settings.");
            return;
        }

        PrepareSceneChange();
        SceneManager.LoadScene(menuScene);
    }

    private void StartSequence(
        string status,
        string title,
        string message,
        Color accent)
    {
        if (isGameOverSequenceRunning)
        {
            return;
        }

        if (!ValidateRuntimeReferences())
        {
            return;
        }

        isGameOverSequenceRunning = true;
        StopGameplay();
        ConfigurePresentation(status, title, message, accent);
        AudioManager.Instance?.FadeOutMusic(gameplayMusicFadeDuration);
        PlayGameOverMusic();
        sequenceRoutine = StartCoroutine(SequenceRoutine(accent));
    }

    private IEnumerator SequenceRoutine(Color accent)
    {
        float sequenceStartedAt = Time.realtimeSinceStartup;
        float elapsed = 0f;

        while (elapsed < totalSequenceDuration)
        {
            elapsed = Time.realtimeSinceStartup - sequenceStartedAt;
            UpdateSequenceVisuals(elapsed, accent);
            KeepButtonsLocked();

            yield return null;
        }

        UpdateSequenceVisuals(totalSequenceDuration, accent);
        EnableButtons();
        sequenceRoutine = StartCoroutine(PostSequenceFlicker(accent));
    }

    private IEnumerator PostSequenceFlicker(Color accent)
    {
        float phase = 0f;
        while (isGameOverSequenceRunning)
        {
            phase += Time.unscaledDeltaTime;
            if (diagnosticText != null)
            {
                Color color = diagnosticText.color;
                color.a = 0.58f + Mathf.Sin(phase * 3.2f) * 0.12f;
                diagnosticText.color = color;
            }

            if (titleText != null)
            {
                Color color = titleText.color;
                Color tint = Color.Lerp(
                    Color.white,
                    accent,
                    0.42f + Mathf.Sin(phase * 2.1f) * 0.08f);
                color.r = tint.r;
                color.g = tint.g;
                color.b = tint.b;
                titleText.color = color;
            }

            yield return null;
        }
    }

    private void UpdateSequenceVisuals(float elapsed, Color accent)
    {
        SetImageAlpha(fadeBackground, Mathf.Lerp(0f, 0.92f, Smooth01(elapsed / 1.15f)));

        float flashAlpha = elapsed < 0.4f
            ? Mathf.Lerp(0f, 0.18f, elapsed / 0.4f)
            : Mathf.Lerp(0.18f, 0f, Mathf.Clamp01((elapsed - 0.4f) / 0.7f));
        SetImageAlpha(impactFlash, flashAlpha);

        SetTextAlpha(statusText, Smooth01((elapsed - 1.2f) / 0.55f));
        SetTextAlpha(titleText, Smooth01((elapsed - 2.5f) / 0.75f));
        SetTextAlpha(celestIAMessageText, Smooth01((elapsed - 4f) / 0.85f));

        float diagnosticAlpha = elapsed < 7f ? 0f : Mathf.Clamp01((elapsed - 7f) / 0.6f);
        if (diagnosticText != null)
        {
            diagnosticText.text = DiagnosticMessage(elapsed);
            Color color = diagnosticText.color;
            color.a = diagnosticAlpha * (0.68f + Mathf.Sin(elapsed * 7f) * 0.13f);
            diagnosticText.color = color;
        }

        if (titleText != null)
        {
            bool glitchWindow = elapsed >= 2.5f && elapsed <= 10.5f;
            float jitter = glitchWindow && Mathf.Sin(elapsed * 19f) > 0.91f ? 3f : 0f;
            titleText.rectTransform.anchoredPosition =
                titleBasePosition + new Vector2(jitter, -jitter * 0.35f);

            Color color = titleText.color;
            Color tint = Color.Lerp(Color.white, accent, 0.45f);
            color.r = tint.r;
            color.g = tint.g;
            color.b = tint.b;
            if (glitchWindow && Mathf.Sin(elapsed * 23f) > 0.94f)
            {
                color.a *= 0.72f;
            }
            titleText.color = color;
        }

        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = Smooth01((elapsed - 11f) / 2.5f);
        }
    }

    private void StopGameplay()
    {
        Time.timeScale = 1f;

        if (player != null)
        {
            player.SetAutoRun(false);
            player.SetInputEnabled(false);
            player.SetSpeedMultiplier(0f);
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled = false;
        }

        if (systemsToDisable != null)
        {
            foreach (Behaviour system in systemsToDisable)
            {
                if (system != null && system != this)
                {
                    system.enabled = false;
                }
            }
        }

        if (hazardColliders != null)
        {
            foreach (Collider hazard in hazardColliders)
            {
                if (hazard != null)
                {
                    hazard.enabled = false;
                }
            }
        }
    }

    private void ConfigurePresentation(
        string status,
        string title,
        string message,
        Color accent)
    {
        panelRoot.SetActive(true);
        if (gameOverCanvas != null)
        {
            gameOverCanvas.enabled = true;
        }

        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;

        statusText.text = status;
        titleText.text = title;
        celestIAMessageText.text = message;
        statusText.color = new Color(accent.r, accent.g, accent.b, 0f);
        titleText.color = new Color(1f, 1f, 1f, 0f);
        celestIAMessageText.color = new Color(0.86f, 0.93f, 0.96f, 0f);
        titleText.rectTransform.anchoredPosition = titleBasePosition;

        if (diagnosticText != null)
        {
            diagnosticText.color = new Color(accent.r, accent.g, accent.b, 0f);
            diagnosticText.text = string.Empty;
        }

        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        retryButton.interactable = false;
        menuButton.interactable = false;
        SetImageAlpha(fadeBackground, 0f);
        SetImageAlpha(impactFlash, 0f);
    }

    private void ResetPresentation()
    {
        isGameOverSequenceRunning = false;
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        if (retryButton != null)
        {
            retryButton.interactable = false;
        }
        if (menuButton != null)
        {
            menuButton.interactable = false;
        }
    }

    private void EnableButtons()
    {
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 1f;
            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;
        }

        retryButton.interactable = true;
        menuButton.interactable = true;
    }

    private void KeepButtonsLocked()
    {
        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        if (retryButton != null)
        {
            retryButton.interactable = false;
        }
        if (menuButton != null)
        {
            menuButton.interactable = false;
        }
    }

    private void PlayGameOverMusic()
    {
        if (gameOverMusicSource == null || gameOverMusic == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Música de Game Over não configurada.");
            return;
        }

        gameOverMusicSource.Stop();
        gameOverMusicSource.clip = gameOverMusic;
        gameOverMusicSource.loop = false;
        gameOverMusicSource.spatialBlend = 0f;
        gameOverMusicSource.volume = 1f;
        gameOverMusicSource.Play();
    }

    private bool ValidateRuntimeReferences()
    {
        bool valid = panelRoot != null &&
            rootGroup != null &&
            fadeBackground != null &&
            statusText != null &&
            titleText != null &&
            celestIAMessageText != null &&
            buttonsGroup != null &&
            retryButton != null &&
            menuButton != null;
        if (!valid)
        {
            Debug.LogError(
                "PROJETO:AURORA - GameOverManager possui referências obrigatórias ausentes.");
        }
        return valid;
    }

    private bool ButtonsAreAvailable()
    {
        return isGameOverSequenceRunning &&
            buttonsGroup != null &&
            buttonsGroup.interactable;
    }

    private void PrepareSceneChange()
    {
        isGameOverSequenceRunning = false;
        Time.timeScale = 1f;
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }
        gameOverMusicSource?.Stop();
        AudioManager.Instance?.StopMusic();
    }

    private static bool SceneExistsInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    private static string DiagnosticMessage(float elapsed)
    {
        if (elapsed < 8.2f)
        {
            return "// verificando sinais vitais...";
        }
        if (elapsed < 9.4f)
        {
            return "// reconectando canal neural...";
        }
        if (elapsed < 10.6f)
        {
            return "// acesso negado // protocolo bloqueado";
        }
        return "// aguardando comando do operador";
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color = text.color;
        color.a = Mathf.Clamp01(alpha);
        text.color = color;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }
}
