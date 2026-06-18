using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string prompt = "PRESSIONE E";
    [SerializeField] private bool useOnce = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField] private UnityEvent onInteracted;

    public bool HasBeenUsed { get; private set; }

    public string GetInteractionPrompt()
    {
        return string.IsNullOrWhiteSpace(prompt) ? "PRESSIONE E" : prompt;
    }

    public bool CanInteract(GameObject interactor)
    {
        return canInteract && (!useOnce || !HasBeenUsed) && CanInteractInternal(interactor);
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        if (useOnce)
        {
            HasBeenUsed = true;
        }

        HandleInteraction(interactor);
        onInteracted?.Invoke();
        Debug.Log($"{name}: interacao executada.", this);
    }

    public void SetInteractable(bool value)
    {
        canInteract = value;
    }

    protected virtual bool CanInteractInternal(GameObject interactor)
    {
        return true;
    }

    protected void PlaySfx(AudioSource audioSource, AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    protected void NotifyCelestIA(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            GameManager.Instance?.celestIA?.ShowTemporary(message, 2.5f);
        }
    }

    protected abstract void HandleInteraction(GameObject interactor);
}
