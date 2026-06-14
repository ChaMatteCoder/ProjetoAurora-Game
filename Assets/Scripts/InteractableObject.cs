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
    public string prompt = "Pressione E";
    public string message = "CELESTIA: Acesso autorizado.";
    public GameObject targetObject;
    public LaserHazard targetLaser;
    public TutorialManager tutorial;
    public bool oneShot = true;

    public bool CanInteract { get; private set; } = true;
    public string Prompt => action == InteractableAction.FinalTerminal
        ? "Pressione E para iniciar restauração"
        : action == InteractableAction.TutorialPanel
            ? "Pressione E para abrir a porta"
            : prompt;

    public void Interact(PlayerInteraction player)
    {
        if (!CanInteract)
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
                GameManager.Instance.BeginFinalCutscene();
                break;
            case InteractableAction.Message:
                GameManager.Instance.celestIA.ShowTemporary(message, 3f);
                break;
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            GameManager.Instance.celestIA.ShowTemporary(message, 2.5f);
        }

        if (oneShot)
        {
            CanInteract = false;
        }
    }
}
