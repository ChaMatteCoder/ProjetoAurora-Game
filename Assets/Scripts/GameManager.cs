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
        celestIA.Begin();
        narrativeEvents.ResetEvents();

        if (terminalSequencePreview)
        {
            SetState(GameState.Playing);
            player.SetInputEnabled(true);
            player.SetAutoRun(previewAutoRun);
            ui.SetSector(previewSectorName);
            ui.SetDistance(Distance, finishDistance);
            ui.SetCelestIAState(CelestIAState.Corrupted);
            dialogue.ShowPersistent("CELESTIA", previewObjective);
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
        AudioManager.Instance?.BeginGameplayMusic();
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

    public void TogglePause()
    {
        if (IsFinished || State == GameState.FinalCutscene || State == GameState.IntroCutscene ||
            State == GameState.Tutorial)
        {
            return;
        }

        bool pause = State != GameState.Paused;
        SetState(pause ? GameState.Paused : GameState.Playing);
        Time.timeScale = pause ? 0f : 1f;
        ui.SetPause(pause);
        AudioManager.Instance?.SetPaused(pause);
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
