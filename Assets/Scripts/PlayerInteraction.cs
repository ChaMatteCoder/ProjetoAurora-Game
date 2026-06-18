using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float exitGraceSeconds = 0.3f;

    private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();
    private IInteractable current;
    private IInteractable graceInteractable;
    private float graceExpiresAt;
    private TutorialActionGate tutorialGate;

    private TutorialActionGate Gate
    {
        get
        {
            if (tutorialGate == null)
            {
                tutorialGate = GetComponent<TutorialActionGate>();
                if (tutorialGate == null)
                {
                    tutorialGate = gameObject.AddComponent<TutorialActionGate>();
                }
            }

            return tutorialGate;
        }
    }

    private void Awake()
    {
        _ = Gate;
    }

    private void Update()
    {
        RefreshPrompt();

        if (GameManager.Instance == null || !GameManager.Instance.AllowsInteraction || !InteractPressedThisFrame())
        {
            return;
        }

        IInteractable target = ResolveInteractionTarget();
        if (target == null || !target.CanInteract(gameObject) || !Gate.CanInteract(target))
        {
            return;
        }

        current = target;
        target.Interact(gameObject);
        Gate.NotifyInteract();
        RemoveUnavailableInteractables();
        RefreshPrompt();
    }

    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable == null || !interactable.CanInteract(gameObject))
        {
            return;
        }

        if (!nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }

        current = GetClosestInteractable();
        InteractableObject configured = interactable as InteractableObject;
        if (configured != null && configured.action == InteractableAction.FinalTerminal)
        {
            GetComponent<PlayerRunner>()?.SetAutoRun(false);
        }

        RefreshPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable == null)
        {
            return;
        }

        nearbyInteractables.Remove(interactable);
        if (ReferenceEquals(interactable, current))
        {
            graceInteractable = interactable;
            graceExpiresAt = Time.time + Mathf.Max(0f, exitGraceSeconds);
            current = GetClosestInteractable();
        }

        RefreshPrompt();
    }

    public void RefreshPrompt()
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.ui == null)
        {
            return;
        }

        TutorialManager tutorial = game.tutorial;
        if (tutorial != null && tutorial.IsTutorialActive &&
            tutorial.CurrentAllowedAction != TutorialAction.Interact)
        {
            game.ui.SetInteractionPrompt(false, string.Empty);
            return;
        }

        IInteractable target = ResolveInteractionTarget();
        bool show = game.AllowsInteraction &&
            target != null && target.CanInteract(gameObject) && Gate.CanInteract(target);
        game.ui.SetInteractionPrompt(show, show ? target.GetInteractionPrompt() : string.Empty);
    }

    private bool InteractPressedThisFrame()
    {
        if (Keyboard.current != null)
        {
            return Keyboard.current.eKey.wasPressedThisFrame;
        }

        return Input.GetKeyDown(KeyCode.E);
    }

    private IInteractable ResolveInteractionTarget()
    {
        if (Gate.TryGetTutorialInteractable(out IInteractable tutorialInteractable))
        {
            return tutorialInteractable;
        }

        RemoveUnavailableInteractables();
        IInteractable closest = GetClosestInteractable();
        if (closest != null)
        {
            current = closest;
            return closest;
        }

        if (graceInteractable != null && Time.time <= graceExpiresAt &&
            graceInteractable.CanInteract(gameObject) && Gate.CanInteract(graceInteractable))
        {
            return graceInteractable;
        }

        graceInteractable = null;
        return null;
    }

    private IInteractable GetClosestInteractable()
    {
        IInteractable closest = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            IInteractable interactable = nearbyInteractables[i];
            if (interactable == null || !interactable.CanInteract(gameObject) || !Gate.CanInteract(interactable))
            {
                continue;
            }

            Component component = interactable as Component;
            if (component == null)
            {
                continue;
            }

            float sqrDistance = (component.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = interactable;
            }
        }

        return closest;
    }

    private void RemoveUnavailableInteractables()
    {
        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            IInteractable interactable = nearbyInteractables[i];
            if (interactable == null || !interactable.CanInteract(gameObject))
            {
                nearbyInteractables.RemoveAt(i);
            }
        }
    }
}
