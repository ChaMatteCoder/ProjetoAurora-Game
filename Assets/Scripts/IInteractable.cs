public interface IInteractable
{
    bool CanInteract { get; }
    string Prompt { get; }
    void Interact(PlayerInteraction player);
}
