using System.Collections;
using UnityEngine;

public class DoorInteractable : InteractableBase
{
    [Header("Door")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 4f, 0f);
    [SerializeField] private float openDuration = 1f;
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorOpenTrigger = "Open";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSfx;
    [SerializeField] private string celestIAMessage = "CELESTIA: Acesso liberado.";

    private Coroutine openRoutine;

    private void Reset()
    {
        doorTransform = transform;
        blockingCollider = GetComponentInChildren<Collider>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void HandleInteraction(GameObject interactor)
    {
        PlaySfx(audioSource, openSfx);
        NotifyCelestIA(celestIAMessage);

        if (animator != null && !string.IsNullOrWhiteSpace(animatorOpenTrigger))
        {
            animator.SetTrigger(animatorOpenTrigger);
            if (blockingCollider != null)
            {
                blockingCollider.enabled = false;
            }
            return;
        }

        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(OpenDoorRoutine());
    }

    private IEnumerator OpenDoorRoutine()
    {
        Vector3 start = doorTransform.position;
        Vector3 end = start + openOffset;
        float duration = Mathf.Max(0.05f, openDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            doorTransform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        doorTransform.position = end;
        if (blockingCollider != null)
        {
            blockingCollider.enabled = false;
        }

        openRoutine = null;
    }
}
