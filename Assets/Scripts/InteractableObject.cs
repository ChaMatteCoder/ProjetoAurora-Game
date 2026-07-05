using System.Collections;
using UnityEngine;

public enum InteractableAction
{
    OpenDoor,
    DisableLaser,
    TutorialPanel,
    FinalTerminal,
    Message
}

public class InteractableObject : MonoBehaviour, IInteractable
{
    public InteractableAction action;
    public string prompt = "PRESSIONE E";
    public string message = "CELESTIA: Acesso autorizado.";
    public GameObject targetObject;
    public LaserHazard targetLaser;
    public TutorialManager tutorial;
    public bool oneShot = true;

    [Header("Round 3 — Interaction Polish")]
    [Tooltip("Lasers adicionais desativados junto com targetLaser (gates multi-feixe).")]
    public LaserHazard[] targetLasers;
    [Tooltip("Se definido, OpenDoor DESLIZA este transform em vez de desativar targetObject.")]
    public Transform slideTarget;
    public Vector3 slideOffset = new Vector3(0f, 4.2f, 0f);
    public float slideDuration = 1.2f;
    [Tooltip("Luzes/indicadores que trocam para 'desativado' ao interagir (renderers recebem cor apagada).")]
    public Renderer[] statusIndicators;
    public Color statusOffColor = new Color(0.12f, 0.12f, 0.12f);
    public AudioSource interactSfx;

    public bool CanInteractLegacy { get; private set; } = true;

    public string GetInteractionPrompt()
    {
        if (action == InteractableAction.FinalTerminal)
        {
            return "PRESSIONE E - INICIAR RESTAURACAO";
        }

        if (action == InteractableAction.TutorialPanel)
        {
            return string.IsNullOrWhiteSpace(prompt) ? "PRESSIONE E - ABRIR PORTA" : prompt;
        }

        return string.IsNullOrWhiteSpace(prompt) ? "PRESSIONE E" : prompt;
    }

    public bool CanInteract(GameObject interactor)
    {
        if (!CanInteractLegacy)
        {
            return false;
        }

        return action != InteractableAction.TutorialPanel || tutorial == null || tutorial.CanInteract(this);
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        switch (action)
        {
            case InteractableAction.OpenDoor:
                // Round 11: porta padrao Aurora tem prioridade (paineis deslizam na moldura)
                if (!TryOpenAuroraDoor())
                {
                    if (slideTarget != null)
                    {
                        StartCoroutine(SlideOpenRoutine());
                    }
                    else if (targetObject != null)
                    {
                        targetObject.SetActive(false);
                    }
                }
                break;

            case InteractableAction.DisableLaser:
                targetLaser?.Deactivate();
                if (targetLasers != null)
                {
                    foreach (LaserHazard laser in targetLasers)
                    {
                        laser?.Deactivate();
                    }
                }
                break;

            case InteractableAction.TutorialPanel:
                // Round 11: porta Aurora abre de verdade em vez de sumir (SetActive)
                if (!TryOpenAuroraDoor() && targetObject != null)
                {
                    targetObject.SetActive(false);
                }
                tutorial?.NotifyInteractionComplete();
                break;

            case InteractableAction.FinalTerminal:
                GameManager.Instance?.BeginFinalCutscene();
                break;

            case InteractableAction.Message:
                break;
        }

        ApplyStatusOff();
        if (interactSfx != null && interactSfx.clip != null)
        {
            interactSfx.Play();
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            string voiceLineId = ResolveVoiceLineId();
            if (!VoiceLinePlayer.TryPlayQueued(voiceLineId, new VoicePlaybackOptions
            {
                group = VoiceGroup.Interaction,
                priority = VoicePriority.Context,
                interruptCurrent = false,
                clearQueueOfSameGroup = true,
                cancelOnStateExit = true,
                blockGameplay = false,
                fadeOutTime = 0.08f,
                ownerStateId = "LegacyInteraction"
            }))
            {
                // mensagens de painel sao de baixa prioridade: nao cortam narrativa em andamento
                GameManager.Instance?.celestIA?.ShowTemporary(
                    message,
                    action == InteractableAction.Message ? 3f : 2.5f,
                    DialogueManager.PriorityLow);
            }
        }

        if (oneShot)
        {
            CanInteractLegacy = false;
        }
    }

    /// Se o alvo (targetObject/slideTarget) tiver AuroraDoorController, abre por ele.
    private bool TryOpenAuroraDoor()
    {
        AuroraDoorController door = null;
        if (targetObject != null)
        {
            door = targetObject.GetComponentInParent<AuroraDoorController>() ??
                   targetObject.GetComponentInChildren<AuroraDoorController>();
        }
        if (door == null && slideTarget != null)
        {
            door = slideTarget.GetComponentInParent<AuroraDoorController>() ??
                   slideTarget.GetComponentInChildren<AuroraDoorController>();
        }

        if (door == null)
        {
            return false;
        }

        door.SetLocked(false);
        door.Open();
        return true;
    }

    private string ResolveVoiceLineId()
    {
        string byMessage = VoiceLinePlayer.ResolveContextId(message);
        if (!string.IsNullOrEmpty(byMessage))
        {
            return byMessage;
        }

        switch (action)
        {
            case InteractableAction.OpenDoor: return "CEL_048";
            case InteractableAction.DisableLaser: return "CEL_049";
            case InteractableAction.Message: return "CEL_047";
            default: return null;
        }
    }

    private void ApplyStatusOff()
    {
        if (statusIndicators == null)
        {
            return;
        }

        foreach (Renderer indicator in statusIndicators)
        {
            if (indicator == null)
            {
                continue;
            }

            indicator.material.color = statusOffColor;
            if (indicator.material.HasProperty("_BaseColor"))
            {
                indicator.material.SetColor("_BaseColor", statusOffColor);
            }
            if (indicator.material.IsKeywordEnabled("_EMISSION"))
            {
                indicator.material.SetColor("_EmissionColor", statusOffColor * 0.2f);
            }
        }
    }

    private IEnumerator SlideOpenRoutine()
    {
        Vector3 closed = slideTarget.localPosition;
        Vector3 open = closed + slideOffset;
        float elapsed = 0f;
        float duration = Mathf.Max(0.2f, slideDuration);
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            slideTarget.localPosition = Vector3.Lerp(closed, open, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        slideTarget.localPosition = open;
    }
}
