using UnityEngine;

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
