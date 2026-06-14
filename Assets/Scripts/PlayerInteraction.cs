using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private IInteractable current;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.AllowsInteraction &&
            current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            current.Interact(this);
            RefreshPrompt();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null && interactable.CanInteract)
        {
            current = interactable;
            InteractableObject configured = interactable as InteractableObject;
            if (configured != null && configured.action == InteractableAction.FinalTerminal)
            {
                GetComponent<PlayerRunner>().SetAutoRun(false);
            }
            RefreshPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null && ReferenceEquals(interactable, current))
        {
            current = null;
            GameManager.Instance.ui.SetInteractionPrompt(false, string.Empty);
        }
    }

    private void RefreshPrompt()
    {
        bool show = GameManager.Instance != null && GameManager.Instance.AllowsInteraction &&
            current != null && current.CanInteract;
        GameManager.Instance.ui.SetInteractionPrompt(show, show ? current.Prompt : string.Empty);
    }
}
