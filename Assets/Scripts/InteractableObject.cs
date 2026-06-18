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
                if (targetObject != null)
                {
                    targetObject.SetActive(false);
                }
                break;

            case InteractableAction.DisableLaser:
                targetLaser?.Deactivate();
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

        if (!string.IsNullOrWhiteSpace(message))
        {
            GameManager.Instance?.celestIA?.ShowTemporary(message, 2.5f);
        }

        if (oneShot)
        {
            CanInteractLegacy = false;
        }
    }
}
