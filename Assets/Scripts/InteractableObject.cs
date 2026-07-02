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
                if (slideTarget != null)
                {
                    StartCoroutine(SlideOpenRoutine());
                }
                else if (targetObject != null)
                {
                    targetObject.SetActive(false);
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
                if (targetObject != null)
                {
                    targetObject.SetActive(false);
                }
                tutorial?.NotifyInteractionComplete();
                break;

            case InteractableAction.FinalTerminal:
                GameManager.Instance?.BeginFinalCutscene();
                break;

            case InteractableAction.Message:
                GameManager.Instance?.celestIA?.ShowTemporary(message, 3f);
                break;
        }

        ApplyStatusOff();
        if (interactSfx != null && interactSfx.clip != null)
        {
            interactSfx.Play();
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            GameManager.Instance?.celestIA?.ShowTemporary(message, 2.5f);
        }

        if (oneShot)
        {
            CanInteractLegacy = false;
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
