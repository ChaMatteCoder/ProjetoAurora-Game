using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public PlayerRunner player;
    public PlayerHealth health;
    public UIManager ui;
    public SectorManager sectors;
    public CelestIAController celestIA;
    public TutorialManager tutorial;
    public DialogueManager dialogue;
    public IntroCutsceneController introCutscene;
    public NarrativeEventManager narrativeEvents;
    public FinalCutsceneController finalCutscene;
    public GameOverManager gameOverManager;

    [Header("Run")]
    public float finishDistance = 2700f;

    [Header("Scene Preview")]
    public bool terminalSequencePreview;
    public bool previewAutoRun = true;
    public string previewSectorName = "TERMINAL CENTRAL";
    [TextArea]
    public string previewObjective =
        "Terminal Central alcancado. Aproxime-se do painel principal.";

    public GameState State { get; private set; } = GameState.IntroCutscene;
    public bool CanRun => State == GameState.Playing;
    public bool IsFinished => State == GameState.GameOver || State == GameState.Finished;
    public bool IsPaused => State == GameState.Paused;
    public bool AllowsInteraction => State == GameState.Tutorial || State == GameState.Playing;
    public bool AllowsDamage => State == GameState.Playing;
    public float Distance => player == null ? 0f : Mathf.Max(0f, player.transform.position.z);

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        EnsureNarrativeControllers();
        gameOverManager = gameOverManager != null
            ? gameOverManager
            : FindFirstObjectByType<GameOverManager>();
        if (health != null)
        {
            health.OnDeath += OnPlayerDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= OnPlayerDied;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        ui.SetPause(false);
        ui.SetGameOver(false);
        ui.SetFinal(false);
        ui.ShowIntro(false, string.Empty);
        ui.SetInteractionPrompt(false, string.Empty);
        narrativeEvents.ResetEvents();

        if (terminalSequencePreview)
        {
            SetState(GameState.Playing);
            player.SetInputEnabled(true);
            player.SetAutoRun(previewAutoRun);
            ui.SetSector(previewSectorName);
            ui.SetDistance(Distance, finishDistance);
            ui.SetCelestIAState(CelestIAState.Corrupted);
            if (!VoiceLinePlayer.TryPlay("CEL_055"))
            {
                dialogue.ShowPersistent("CELESTIA", previewObjective);
            }
            AudioManager.Instance?.BeginGameplayMusic();
            return;
        }

        SetState(GameState.IntroCutscene);
        introCutscene.Begin();
    }

    private void Update()
    {
        if (PlayerRunner.PausePressedThisFrame())
        {
            TogglePause();
        }

        if (IsFinished || State == GameState.FinalCutscene)
        {
            return;
        }

        ui.SetDistance(Distance, finishDistance);
        if (terminalSequencePreview)
        {
            return;
        }

        sectors.UpdateSector(Distance);
        if (State == GameState.Playing)
        {
            narrativeEvents.UpdateDistance(Distance);
        }
    }

    public void EnterTutorial()
    {
        SetState(GameState.Tutorial);
    }

    public void StartFullRun()
    {
        SetState(GameState.Playing);
        player.SetAutoRun(true);
        celestIA.Begin();
        AudioManager.Instance?.BeginGameplayMusic();
        // Round 11: a HUD completa acabou de entrar — apresenta o setor atual
        sectors?.ShowCurrentSectorTitle();
    }

    public void DamagePlayer()
    {
        if (AllowsDamage)
        {
            health.TakeDamage();
        }
    }

    public void OnPlayerDied()
    {
        if (State == GameState.GameOver || State == GameState.Finished)
        {
            return;
        }

        SetState(GameState.GameOver);
        player.SetAutoRun(false);
        player.SetInputEnabled(false);
        ui.SetGameOver(false);
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
        }
        else
        {
            Debug.LogError("PROJETO:AURORA - GameOverManager nao encontrado.");
        }
    }

    // Round 18: o ESC agora tambem abre a pausa durante o Tutorial. Guardamos o estado
    // anterior para o "Continuar" retornar ao estado correto (Tutorial -> Tutorial,
    // Playing -> Playing), em vez de sempre cair em Playing.
    private GameState stateBeforePause = GameState.Playing;

    /// True quando a pausa foi aberta a partir do tutorial (o menu usa isso para exibir
    /// o botao "PULAR TUTORIAL").
    public bool IsPausedFromTutorial =>
        State == GameState.Paused && stateBeforePause == GameState.Tutorial;

    public void TogglePause()
    {
        // A pausa nao abre em cinematicas nem apos o fim de jogo.
        if (IsFinished || State == GameState.FinalCutscene || State == GameState.IntroCutscene)
        {
            return;
        }

        // Alem de Paused, so Tutorial e Playing podem alternar a pausa.
        if (State != GameState.Paused && State != GameState.Tutorial && State != GameState.Playing)
        {
            return;
        }

        bool pause = State != GameState.Paused;
        if (pause)
        {
            stateBeforePause = State; // Tutorial ou Playing
            SetState(GameState.Paused);
        }
        else
        {
            SetState(stateBeforePause);
        }

        Time.timeScale = pause ? 0f : 1f;
        ui.SetPause(pause);
        AudioManager.Instance?.SetPaused(pause);
        // Congela o input enquanto pausado: evita trocar de faixa/pular com o jogo parado
        // ou completar um passo do tutorial "por baixo" do menu.
        player?.SetInputEnabled(!pause);
    }

    /// Round 18: "PULAR TUTORIAL" a partir do menu de pausa. Encerra a pausa e conclui o
    /// tutorial de forma segura (o TutorialManager teleporta o Dr. Elias para depois da
    /// porta de contencao e libera a corrida completa).
    public void SkipTutorialFromPause()
    {
        if (!IsPausedFromTutorial || tutorial == null || !tutorial.IsTutorialActive)
        {
            return;
        }

        // Sai da pausa sem passar por TogglePause (que voltaria ao estado Tutorial).
        Time.timeScale = 1f;
        ui.SetPause(false);
        AudioManager.Instance?.SetPaused(false);
        player?.SetInputEnabled(true);
        stateBeforePause = GameState.Playing;

        tutorial.SkipToEnd(); // -> CompleteTutorial -> StartFullRun (SetState Playing)
    }

    public void Resume()
    {
        if (State == GameState.Paused)
        {
            TogglePause();
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.StopMusic();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        AudioManager.Instance?.StopMusic();
        SceneManager.LoadScene("MainMenu");
    }

    public void BeginFinalCutscene()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        SetState(GameState.FinalCutscene);
        player.SetAutoRun(false);
        player.SetInputEnabled(false);
        ui.SetInteractionPrompt(false, string.Empty);
        finalCutscene.Begin();
    }

    public void FinishGame()
    {
        SetState(GameState.Finished);
        ui.SetFinal(false);
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameCompleted();
        }
        else
        {
            Debug.LogError("PROJETO:AURORA - GameOverManager nao encontrado.");
        }
    }

    private void SetState(GameState state)
    {
        State = state;
        // Round 11: a HUD acompanha o estado do jogo (intro esconde HUD de gameplay etc.)
        if (ui != null)
        {
            switch (state)
            {
                case GameState.IntroCutscene:
                    ui.SetHudVisibilityState(GameplayHudVisibilityState.IntroCinematic);
                    break;
                case GameState.Tutorial:
                    ui.SetHudVisibilityState(GameplayHudVisibilityState.Tutorial);
                    break;
                case GameState.Playing:
                    ui.SetHudVisibilityState(GameplayHudVisibilityState.Gameplay);
                    break;
                case GameState.Paused:
                    ui.SetHudVisibilityState(GameplayHudVisibilityState.Paused);
                    break;
                case GameState.GameOver:
                    ui.SetHudVisibilityState(GameplayHudVisibilityState.GameOver);
                    break;
                case GameState.FinalCutscene:
                case GameState.Finished:
                    ui.SetHudVisibilityState(GameplayHudVisibilityState.Final);
                    break;
            }
        }
    }

    private void EnsureNarrativeControllers()
    {
        CelestIAHudController hud = sectors == null
            ? FindFirstObjectByType<CelestIAHudController>()
            : sectors.celestIAHud;

        dialogue = dialogue != null
            ? dialogue
            : GetComponent<DialogueManager>() ?? gameObject.AddComponent<DialogueManager>();
        dialogue.ui = ui;
        dialogue.celestIAHud = hud;

        introCutscene = introCutscene != null
            ? introCutscene
            : GetComponent<IntroCutsceneController>() ?? gameObject.AddComponent<IntroCutsceneController>();
        introCutscene.dialogue = dialogue;
        introCutscene.player = player;
        introCutscene.tutorial = tutorial;

        narrativeEvents = narrativeEvents != null
            ? narrativeEvents
            : GetComponent<NarrativeEventManager>() ?? gameObject.AddComponent<NarrativeEventManager>();
        narrativeEvents.dialogue = dialogue;
        narrativeEvents.celestIAHud = hud;

        finalCutscene = finalCutscene != null
            ? finalCutscene
            : GetComponent<FinalCutsceneController>() ?? gameObject.AddComponent<FinalCutsceneController>();
        finalCutscene.dialogue = dialogue;
        finalCutscene.player = player;
        finalCutscene.celestIAHud = hud;
    }
}
