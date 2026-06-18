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

    public bool IsComplete { get; private set; }
    public bool IsTutorialActive { get; private set; }
    public TutorialAction CurrentAllowedAction { get; private set; } = TutorialAction.None;

    private TutorialStepTrigger activeStep;
    private TutorialStepTrigger[] orderedSteps;
    private Coroutine reminderRoutine;
    private int completedSteps;
    private int nextStepIndex;

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

        celestIA.SetTutorialMessage("CELESTIA: Controle assistido iniciado. Mantenha-se em movimento.");
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
        CurrentAllowedAction = step.requiredAction;
        player.SetAutoRun(false);
        player.SetSpeedMultiplier(tutorialActionSpeedMultiplier);

        if (!string.IsNullOrWhiteSpace(step.celestiaMessage))
        {
            celestIA.SetTutorialMessage("CELESTIA: " + step.celestiaMessage);
        }

        if (!string.IsNullOrWhiteSpace(step.hudMessage))
        {
            GameManager.Instance.ui.SetInteractionPrompt(true, step.hudMessage);
        }

        if (CurrentAllowedAction == TutorialAction.Interact)
        {
            RefreshInteractionPrompt();
        }
        StartReminder(step);
        return true;
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
            StartCoroutine(FinishTutorial());
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
        activeStep = null;
        CurrentAllowedAction = TutorialAction.None;
        IsTutorialActive = false;
        IsComplete = true;
        player.SetInputEnabled(true);
        player.SetAutoRun(true);
        player.SetSpeedMultiplier(1f);
        GameManager.Instance.ui.SetInteractionPrompt(false, string.Empty);
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

        activeStep.MarkCompleted();
        activeStep = null;
        completedSteps++;
        CurrentAllowedAction = TutorialAction.None;
        player.SetAutoRun(true);
        player.SetSpeedMultiplier(tutorialCruiseSpeedMultiplier);
        GameManager.Instance.ui.SetInteractionPrompt(false, string.Empty);
        RefreshInteractionPrompt();
        StopReminder();
    }

    private IEnumerator FinishTutorial()
    {
        celestIA.SetTutorialMessage("CELESTIA: Acesso liberado. Prossiga.");
        yield return new WaitForSeconds(2f);
        CompleteTutorial();
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
        yield return new WaitForSeconds(Mathf.Max(0.5f, step.reminderDelay));
        if (activeStep == step && CurrentAllowedAction != TutorialAction.None)
        {
            celestIA.SetTutorialMessage("CELESTIA: " + step.reminderMessage);
            if (!string.IsNullOrWhiteSpace(step.hudMessage))
            {
                GameManager.Instance.ui.SetInteractionPrompt(true, step.hudMessage);
            }
        }

        reminderRoutine = null;
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

public class TutorialActionGate : MonoBehaviour
{
    public TutorialManager tutorial;

    private TutorialManager Tutorial =>
        tutorial != null ? tutorial : GameManager.Instance == null ? null : GameManager.Instance.tutorial;

    public bool CanMoveLeft() => Tutorial == null || Tutorial.CanMoveLeft();
    public bool CanMoveRight() => Tutorial == null || Tutorial.CanMoveRight();
    public bool CanJump() => Tutorial == null || Tutorial.CanJump();
    public bool CanInteract(IInteractable interactable) =>
        Tutorial == null || Tutorial.CanInteract(interactable);
    public bool TryGetTutorialInteractable(out IInteractable interactable)
    {
        interactable = null;
        return Tutorial != null && Tutorial.TryGetActiveInteractable(out interactable);
    }

    public void NotifyMoveLeft() => Tutorial?.NotifyMoveLeft();
    public void NotifyMoveRight() => Tutorial?.NotifyMoveRight();
    public void NotifyJump() => Tutorial?.NotifyJump();
    public void NotifyInteract() => Tutorial?.NotifyInteract();
}

[RequireComponent(typeof(BoxCollider))]
public class TutorialStepTrigger : MonoBehaviour
{
    public TutorialManager tutorial;
    public TutorialAction requiredAction;
    public string celestiaMessage;
    public string hudMessage;
    public string reminderMessage;
    public float reminderDelay = 3f;
    public bool oneShot = true;

    public bool WasTriggered { get; private set; }
    public bool IsCompleted { get; private set; }

    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((oneShot && WasTriggered) || IsCompleted)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerRunner>() == null)
        {
            return;
        }

        TutorialManager targetTutorial = tutorial != null
            ? tutorial
            : GameManager.Instance == null ? null : GameManager.Instance.tutorial;
        if (targetTutorial != null && targetTutorial.ActivateStep(this))
        {
            WasTriggered = true;
        }
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }
}
