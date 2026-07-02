using UnityEngine;

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
