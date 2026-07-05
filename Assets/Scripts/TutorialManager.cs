using System.Collections;
using UnityEngine;

public enum TutorialAction
{
    None,
    MoveRight,
    MoveLeft,
    Jump,
    Interact
}

/// Estado da etapa atual do tutorial (Round 11): a acao so libera quando a
/// instrucao principal da CelestIA termina.
public enum TutorialStepState
{
    WaitingForInstructionVoice,
    ActionEnabled,
    Completed
}

public class TutorialManager : MonoBehaviour
{
    public PlayerRunner player;
    public CelestIAController celestIA;
    public GameObject tutorialPanel;

    [Header("Guided Run")]
    public bool createRuntimeSequenceIfMissing = true;
    public float tutorialCruiseSpeedMultiplier = 0.8f;
    public float tutorialActionSpeedMultiplier = 1f;
    public float jumpObstacleForwardOffset = 2.8f;

    [Header("Gating por fala (Round 11)")]
    [Tooltip("Sem dublagem para a etapa: a acao libera apos este tempo (fallback de texto).")]
    public float fallbackInstructionSeconds = 2.6f;
    [Tooltip("Watchdog: liberacao forcada se a fala nao sinalizar termino (nunca prender o player).")]
    public float maxInstructionWaitSeconds = 12f;

    public bool IsComplete { get; private set; }
    public bool IsTutorialActive { get; private set; }
    public TutorialAction CurrentAllowedAction { get; private set; } = TutorialAction.None;
    public TutorialStepState CurrentStepState { get; private set; } = TutorialStepState.Completed;

    private TutorialStepTrigger activeStep;
    private TutorialStepTrigger[] orderedSteps;
    private Coroutine reminderRoutine;
    private Coroutine gateRoutine;
    private TutorialArrowIndicator arrows;
    private int completedSteps;
    private int nextStepIndex;
    private int tutorialVoiceVersion;

    private void Update()
    {
        if (!IsTutorialActive || IsComplete || activeStep != null || player == null || orderedSteps == null)
        {
            return;
        }

        while (nextStepIndex < orderedSteps.Length && orderedSteps[nextStepIndex] == null)
        {
            nextStepIndex++;
        }

        if (nextStepIndex < orderedSteps.Length &&
            player.transform.position.z >= orderedSteps[nextStepIndex].transform.position.z)
        {
            ActivateStep(orderedSteps[nextStepIndex]);
        }
    }

    public void BeginTutorial()
    {
        EnsureRuntimeSequence();
        CacheOrderedSteps();
        completedSteps = 0;
        nextStepIndex = 0;
        IsComplete = false;
        IsTutorialActive = true;
        CurrentAllowedAction = TutorialAction.None;

        player.SetInputEnabled(true);
        player.SetAutoRun(true);
        player.SetSpeedMultiplier(tutorialCruiseSpeedMultiplier);
        AudioManager.Instance?.BeginGameplayMusic();

        StopTutorialVoice();
        PlayTutorialVoice("CEL_008", "Tutorial_Intro",
            "CELESTIA: Controle assistido iniciado. Mantenha-se em movimento.");
        GameManager.Instance.ui.SetInteractionPrompt(false, string.Empty);
    }

    public bool ActivateStep(TutorialStepTrigger step)
    {
        if (!IsTutorialActive || IsComplete || step == null || activeStep != null)
        {
            return false;
        }

        activeStep = step;
        MarkStepAsNext(step);

        // Round 11: durante a instrucao a acao fica BLOQUEADA (CurrentAllowedAction = None).
        // Seguranca (Opcao C — safe hold ja existente): SetAutoRun(false) abaixo PARA o runner
        // no trigger, entao esperar a fala nunca faz o player passar do obstaculo nem bater.
        CurrentStepState = TutorialStepState.WaitingForInstructionVoice;
        CurrentAllowedAction = TutorialAction.None;
        player.SetAutoRun(false);
        player.SetSpeedMultiplier(tutorialActionSpeedMultiplier);
        GameManager.Instance.ui.SetInteractionPrompt(false, string.Empty);
        arrows?.Hide();

        StopTutorialVoice();
        int version = tutorialVoiceVersion;
        string primaryVoiceId = GetStepVoiceId(step, false);
        bool voiceStarted = PlayStepInstruction(primaryVoiceId, GetStepStateId(step), version);
        if (!voiceStarted && !string.IsNullOrWhiteSpace(step.celestiaMessage))
        {
            celestIA.SetTutorialMessage("CELESTIA: " + step.celestiaMessage);
        }

        if (gateRoutine != null)
        {
            StopCoroutine(gateRoutine);
        }
        gateRoutine = StartCoroutine(StepGateRoutine(step, version, voiceStarted));
        return true;
    }

    /// Toca a instrucao principal da etapa; o onComplete natural libera a acao.
    private bool PlayStepInstruction(string id, string ownerStateId, int version)
    {
        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        if (voice == null || string.IsNullOrWhiteSpace(id) || !voice.HasLine(id))
        {
            return false;
        }

        voice.PlaySequence(new[] { id }, false,
            () => EnableCurrentStepAction(version),
            TutorialVoiceOptions(ownerStateId, true, true));
        return true;
    }

    /// Watchdog do gating: cobre fallback de texto, fala cancelada e qualquer caso em que o
    /// onComplete nao dispare — o player NUNCA fica preso esperando audio.
    private IEnumerator StepGateRoutine(TutorialStepTrigger step, int version, bool voiceStarted)
    {
        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        float elapsed = 0f;
        float minWait = voiceStarted ? 0.5f : Mathf.Max(0.8f, fallbackInstructionSeconds);
        float maxWait = Mathf.Max(minWait + 0.5f, maxInstructionWaitSeconds);

        while (activeStep == step && version == tutorialVoiceVersion &&
            CurrentStepState == TutorialStepState.WaitingForInstructionVoice && elapsed < maxWait)
        {
            elapsed += Time.deltaTime;
            // fala terminou/cancelada sem callback (ex.: interrupcao critica): libera
            if (voiceStarted && elapsed > minWait && (voice == null || !voice.IsPlayingGroup(VoiceGroup.Tutorial)))
            {
                break;
            }
            if (!voiceStarted && elapsed >= minWait)
            {
                break;
            }
            yield return null;
        }

        gateRoutine = null;
        EnableCurrentStepAction(version);
    }

    /// Fim da instrucao: libera a acao da etapa, mostra prompt/lembrete e a seta animada.
    private void EnableCurrentStepAction(int version)
    {
        if (activeStep == null || IsComplete || version != tutorialVoiceVersion ||
            CurrentStepState != TutorialStepState.WaitingForInstructionVoice)
        {
            return;
        }

        TutorialStepTrigger step = activeStep;
        CurrentStepState = TutorialStepState.ActionEnabled;
        CurrentAllowedAction = step.requiredAction;

        if (!string.IsNullOrWhiteSpace(step.hudMessage))
        {
            GameManager.Instance.ui.SetInteractionPrompt(true, step.hudMessage);
        }
        if (CurrentAllowedAction == TutorialAction.Interact)
        {
            RefreshInteractionPrompt();
        }

        ShowStepArrow(step);
        StartReminder(step);
    }

    private void ShowStepArrow(TutorialStepTrigger step)
    {
        if (arrows == null)
        {
            arrows = TutorialArrowIndicator.GetOrCreate();
        }

        Vector3 playerPos = player != null ? player.transform.position : step.transform.position;
        int playerLane = Mathf.Clamp(Mathf.RoundToInt(playerPos.x / 3f) + 1, 0, 2);
        float stepZ = step.transform.position.z;

        switch (step.requiredAction)
        {
            case TutorialAction.MoveRight:
            {
                int targetLane = Mathf.Clamp(playerLane + 1, 0, 2);
                arrows.ShowLane(new Vector3((targetLane - 1) * 3f, 1.35f, stepZ + 5.5f), 1);
                break;
            }
            case TutorialAction.MoveLeft:
            {
                int targetLane = Mathf.Clamp(playerLane - 1, 0, 2);
                arrows.ShowLane(new Vector3((targetLane - 1) * 3f, 1.35f, stepZ + 5.5f), -1);
                break;
            }
            case TutorialAction.Jump:
                arrows.ShowJump(new Vector3((playerLane - 1) * 3f, 1.7f,
                    stepZ + GetJumpObstacleForwardOffset() + 0.6f));
                break;
            case TutorialAction.Interact:
            {
                // "E" sobre o console do painel (filho com "Console" no nome), senao sobre o trigger
                Transform console = FindChildContaining(step.transform, "Console");
                Vector3 anchor = console != null
                    ? console.position + Vector3.up * 1.6f
                    : step.transform.position + new Vector3(0f, 1.9f, 0f);
                arrows.ShowInteract(anchor);
                break;
            }
        }
    }

    private static Transform FindChildContaining(Transform root, string token)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name.ToUpperInvariant().Contains(token.ToUpperInvariant()))
            {
                return child;
            }
        }
        return null;
    }

    public bool CanMoveLeft() => IsCommandAllowed(TutorialAction.MoveLeft);
    public bool CanMoveRight() => IsCommandAllowed(TutorialAction.MoveRight);
    public bool CanJump() => IsCommandAllowed(TutorialAction.Jump);

    public bool CanInteract(IInteractable interactable)
    {
        if (!IsTutorialActive || IsComplete)
        {
            return true;
        }

        if (CurrentAllowedAction != TutorialAction.Interact)
        {
            return false;
        }

        InteractableObject configured = interactable as InteractableObject;
        return configured != null && configured.action == InteractableAction.TutorialPanel;
    }

    public bool TryGetActiveInteractable(out IInteractable interactable)
    {
        interactable = null;
        if (!IsTutorialActive || IsComplete || CurrentAllowedAction != TutorialAction.Interact || activeStep == null)
        {
            return false;
        }

        InteractableObject configured = activeStep.GetComponent<InteractableObject>();
        if (configured == null || !configured.CanInteract(player == null ? null : player.gameObject))
        {
            return false;
        }

        interactable = configured;
        return true;
    }

    public void NotifyMoveLeft()
    {
        if (CurrentAllowedAction == TutorialAction.MoveLeft)
        {
            CompleteCurrentStep();
        }
    }

    public void NotifyMoveRight()
    {
        if (CurrentAllowedAction == TutorialAction.MoveRight)
        {
            CompleteCurrentStep();
        }
    }

    public void NotifyJump()
    {
        if (CurrentAllowedAction == TutorialAction.Jump)
        {
            CompleteCurrentStep();
        }
    }

    public void NotifyInteract()
    {
        if (CurrentAllowedAction == TutorialAction.Interact)
        {
            CompleteCurrentStep();
            CompleteTutorial();
        }
    }

    public void NotifyInteractionComplete() => NotifyInteract();

    public void CompleteTutorial()
    {
        if (IsComplete)
        {
            return;
        }

        StopReminder();
        StopTutorialVoice();
        if (gateRoutine != null)
        {
            StopCoroutine(gateRoutine);
            gateRoutine = null;
        }
        arrows?.Hide();
        CurrentStepState = TutorialStepState.Completed;
        activeStep = null;
        CurrentAllowedAction = TutorialAction.None;
        IsTutorialActive = false;
        IsComplete = true;
        player.SetInputEnabled(true);
        player.SetAutoRun(true);
        player.SetSpeedMultiplier(1f);
        GameManager.Instance.ui.SetInteractionPrompt(false, string.Empty);
        PlayTutorialCompletion();
        GameManager.Instance.StartFullRun();
    }

    private bool IsCommandAllowed(TutorialAction action)
    {
        if (!IsTutorialActive || IsComplete)
        {
            return true;
        }

        return CurrentAllowedAction == action;
    }

    private void CompleteCurrentStep()
    {
        if (activeStep == null)
        {
            return;
        }

        StopReminder();
        StopTutorialVoice();
        if (gateRoutine != null)
        {
            StopCoroutine(gateRoutine);
            gateRoutine = null;
        }
        arrows?.Hide();
        CurrentStepState = TutorialStepState.Completed;
        activeStep.MarkCompleted();
        activeStep = null;
        completedSteps++;
        CurrentAllowedAction = TutorialAction.None;
        player.SetAutoRun(true);
        player.SetSpeedMultiplier(tutorialCruiseSpeedMultiplier);
        GameManager.Instance.ui.SetInteractionPrompt(false, string.Empty);
        RefreshInteractionPrompt();
    }

    private void StartReminder(TutorialStepTrigger step)
    {
        StopReminder();
        if (!string.IsNullOrWhiteSpace(step.reminderMessage))
        {
            reminderRoutine = StartCoroutine(ReminderRoutine(step));
        }
    }

    private IEnumerator ReminderRoutine(TutorialStepTrigger step)
    {
        int voiceVersion = tutorialVoiceVersion;
        yield return new WaitForSeconds(Mathf.Max(0.5f, step.reminderDelay));
        if (activeStep == step && CurrentAllowedAction != TutorialAction.None &&
            voiceVersion == tutorialVoiceVersion)
        {
            VoiceLinePlayer voice = VoiceLinePlayer.Instance;
            while (voice != null && voice.IsPlayingGroup(VoiceGroup.Tutorial) && activeStep == step &&
                voiceVersion == tutorialVoiceVersion)
            {
                yield return null;
            }

            if (activeStep != step || CurrentAllowedAction == TutorialAction.None ||
                voiceVersion != tutorialVoiceVersion)
            {
                reminderRoutine = null;
                yield break;
            }

            string reminderVoiceId = GetStepVoiceId(step, true);
            VoicePlaybackOptions options = TutorialVoiceOptions(GetStepStateId(step) + "_Reminder", false, false);
            if (!VoiceLinePlayer.TryPlay(reminderVoiceId, options))
            {
                celestIA.SetTutorialMessage("CELESTIA: " + step.reminderMessage);
            }
            if (!string.IsNullOrWhiteSpace(step.hudMessage))
            {
                GameManager.Instance.ui.SetInteractionPrompt(true, step.hudMessage);
            }
        }

        reminderRoutine = null;
    }

    private void PlayTutorialVoice(string id, string ownerStateId, string fallbackMessage)
    {
        VoicePlaybackOptions options = TutorialVoiceOptions(ownerStateId, true, true);
        if (!VoiceLinePlayer.TryPlay(id, options) && !string.IsNullOrWhiteSpace(fallbackMessage))
        {
            celestIA.SetTutorialMessage(fallbackMessage);
        }
    }

    private void PlayTutorialCompletion()
    {
        var options = new VoicePlaybackOptions
        {
            group = VoiceGroup.Gameplay,
            priority = VoicePriority.Tutorial,
            interruptCurrent = false,
            clearQueueOfSameGroup = false,
            cancelOnStateExit = false,
            blockGameplay = false,
            fadeOutTime = 0.1f,
            ownerStateId = "Tutorial_Complete"
        };
        if (!VoiceLinePlayer.TryPlay("CEL_019", options))
        {
            celestIA.SetTutorialMessage("CELESTIA: Acesso liberado. Prossiga.");
        }
    }

    private static VoicePlaybackOptions TutorialVoiceOptions(string ownerStateId, bool interrupt, bool clearGroup)
    {
        return new VoicePlaybackOptions
        {
            group = VoiceGroup.Tutorial,
            priority = VoicePriority.Tutorial,
            interruptCurrent = interrupt,
            clearQueueOfSameGroup = clearGroup,
            cancelOnStateExit = true,
            blockGameplay = false,
            fadeOutTime = 0.1f,
            ownerStateId = ownerStateId
        };
    }

    private string GetStepStateId(TutorialStepTrigger step)
    {
        int index = orderedSteps == null || step == null ? -1 : System.Array.IndexOf(orderedSteps, step);
        return index < 0 ? "Tutorial_Unknown" : "Tutorial_Step_" + (index + 1).ToString("00");
    }

    private void StopTutorialVoice()
    {
        tutorialVoiceVersion++;
        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        if (voice == null)
        {
            return;
        }
        voice.ClearQueueByGroup(VoiceGroup.Tutorial);
        voice.StopGroup(VoiceGroup.Tutorial, 0.1f);
    }

    private string GetStepVoiceId(TutorialStepTrigger step, bool reminder)
    {
        if (orderedSteps == null || step == null)
        {
            return null;
        }

        int index = System.Array.IndexOf(orderedSteps, step);
        if (index < 0 || index > 4)
        {
            return null;
        }

        int number = 9 + index * 2 + (reminder ? 1 : 0);
        return "CEL_" + number.ToString("000");
    }

    private void StopReminder()
    {
        if (reminderRoutine != null)
        {
            StopCoroutine(reminderRoutine);
            reminderRoutine = null;
        }
    }

    private void RefreshInteractionPrompt()
    {
        PlayerInteraction interaction = player == null ? null : player.GetComponent<PlayerInteraction>();
        interaction?.RefreshPrompt();
    }

    private void EnsureRuntimeSequence()
    {
        if (!createRuntimeSequenceIfMissing ||
            FindObjectsByType<TutorialStepTrigger>(FindObjectsInactive.Include).Length > 0)
        {
            return;
        }

        GameObject root = new GameObject("TutorialSequence_Fase01");
        float jumpOffset = GetJumpObstacleForwardOffset();
        CreateStep(root.transform, "Step01_MoveRight", 14f, TutorialAction.MoveRight,
            "Obstaculo no centro da pista. Vamos com calma: desvie para a direita.",
            "DESVIE PARA A DIREITA",
            "Doutor Elias, use D ou seta para a direita.",
            new Vector3(0f, 1f, 22f), new Vector3(2.4f, 2f, 1.4f), Color.red);

        CreateStep(root.transform, "Step02_MoveLeft", 38f, TutorialAction.MoveLeft,
            "Boa. Agora ha uma barreira na faixa da direita. Desvie para a esquerda.",
            "DESVIE PARA A ESQUERDA",
            "Agora use A ou seta para a esquerda.",
            new Vector3(3f, 1f, 46f), new Vector3(2.4f, 2f, 1.4f), new Color(1f, 0.35f, 0.1f));

        CreateStep(root.transform, "Step03_Jump", 62f, TutorialAction.Jump,
            "Fios energizados bloqueando o chao. Pule quando estiver pronto.",
            "PULE",
            "Pressione Espaco para pular.",
            new Vector3(0f, 0.22f, 62f + jumpOffset), new Vector3(8f, 0.28f, 1.4f), Color.yellow);

        CreateStep(root.transform, "Step04_Jump", 78f, TutorialAction.Jump,
            "Mais um obstaculo baixo. Mantenha o ritmo e pule de novo.",
            "PULE NOVAMENTE",
            "Espaco, doutor. Mais um salto.",
            new Vector3(0f, 0.45f, 78f + jumpOffset), new Vector3(7f, 0.65f, 1.6f), new Color(1f, 0.5f, 0.05f));

        GameObject door = CreateVisual(root.transform, "Tutorial_Door", new Vector3(0f, 2f, 96f),
            new Vector3(8f, 4f, 0.5f), new Color(0.25f, 0.55f, 0.65f));
        CreateTutorialPanel(root.transform, door);
    }

    private float GetJumpObstacleForwardOffset()
    {
        return Mathf.Max(1.8f, jumpObstacleForwardOffset);
    }

    private void CacheOrderedSteps()
    {
        orderedSteps = FindObjectsByType<TutorialStepTrigger>(FindObjectsInactive.Include);
        System.Array.Sort(orderedSteps, (a, b) =>
            a.transform.position.z.CompareTo(b.transform.position.z));
    }

    private void MarkStepAsNext(TutorialStepTrigger step)
    {
        if (orderedSteps == null)
        {
            return;
        }

        for (int i = 0; i < orderedSteps.Length; i++)
        {
            if (orderedSteps[i] == step)
            {
                nextStepIndex = Mathf.Max(nextStepIndex, i + 1);
                return;
            }
        }
    }

    private TutorialStepTrigger CreateStep(Transform parent, string name, float triggerZ, TutorialAction action,
        string celestiaMessage, string hudMessage, string reminderMessage, Vector3 visualPosition,
        Vector3 visualScale, Color visualColor)
    {
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(parent);
        holder.transform.position = new Vector3(0f, 1.5f, triggerZ);

        BoxCollider trigger = holder.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(9f, 3f, 3f);

        TutorialStepTrigger step = holder.AddComponent<TutorialStepTrigger>();
        step.tutorial = this;
        step.requiredAction = action;
        step.celestiaMessage = celestiaMessage;
        step.hudMessage = hudMessage;
        step.reminderMessage = reminderMessage;

        CreateVisual(holder.transform, "Visual", visualPosition - holder.transform.position, visualScale, visualColor);
        return step;
    }

    private GameObject CreateTutorialPanel(Transform parent, GameObject door)
    {
        GameObject panelRoot = new GameObject("Tutorial_PanelDoor");
        panelRoot.transform.SetParent(parent);
        panelRoot.transform.position = new Vector3(0f, 1f, 88f);

        BoxCollider trigger = panelRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(9f, 4f, 6f);
        trigger.center = new Vector3(0f, 0.5f, 0f);

        InteractableObject interactable = panelRoot.AddComponent<InteractableObject>();
        interactable.action = InteractableAction.TutorialPanel;
        interactable.prompt = "Pressione E - acionar painel";
        interactable.message = string.Empty;
        interactable.targetObject = door;
        interactable.tutorial = this;

        TutorialStepTrigger step = panelRoot.AddComponent<TutorialStepTrigger>();
        step.tutorial = this;
        step.requiredAction = TutorialAction.Interact;
        step.celestiaMessage = "Porta de contencao travada. Acione o painel manual.";
        step.hudMessage = "PRESSIONE E - ACIONAR PAINEL";
        step.reminderMessage = "Pressione E para acionar o painel.";

        CreateVisual(panelRoot.transform, "Console", new Vector3(-3.4f, 0f, 0f), new Vector3(0.8f, 1.4f, 0.25f),
            new Color(0.05f, 0.85f, 1f));
        tutorialPanel = panelRoot;
        return panelRoot;
    }

    private static GameObject CreateVisual(Transform parent, string name, Vector3 localPosition,
        Vector3 scale, Color color)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = name;
        visual.transform.SetParent(parent);
        visual.transform.localPosition = localPosition;
        visual.transform.localScale = scale;

        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
            if (renderer.material.HasProperty("_BaseColor"))
            {
                renderer.material.SetColor("_BaseColor", color);
            }
        }

        return visual;
    }
}

// TutorialActionGate e TutorialStepTrigger foram extraidos para arquivos proprios
// (TutorialActionGate.cs / TutorialStepTrigger.cs) para que os componentes possam
// ser serializados em cena (classes sem arquivo homonimo nao persistem no .unity).
