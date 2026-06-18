using System.Collections;
using UnityEngine;

public class MovingBlockInteractable : InteractableBase
{
    [Header("Moving Block")]
    [SerializeField] private Transform blockTransform;
    [SerializeField] private Vector3 moveOffset = new Vector3(0f, 0f, 4f);
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip moveSfx;
    [SerializeField] private string celestIAMessage = "CELESTIA: Caminho parcialmente liberado.";

    private Coroutine moveRoutine;

    private void Reset()
    {
        blockTransform = transform;
        audioSource = GetComponent<AudioSource>();
    }

    protected override void HandleInteraction(GameObject interactor)
    {
        if (blockTransform == null)
        {
            blockTransform = transform;
        }

        PlaySfx(audioSource, moveSfx);
        NotifyCelestIA(celestIAMessage);

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveBlockRoutine());
    }

    private IEnumerator MoveBlockRoutine()
    {
        Vector3 start = blockTransform.position;
        Vector3 end = start + moveOffset;
        float duration = Mathf.Max(0.05f, moveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float t = movementCurve == null ? normalized : movementCurve.Evaluate(normalized);
            blockTransform.position = Vector3.LerpUnclamped(start, end, t);
            yield return null;
        }

        blockTransform.position = end;
        moveRoutine = null;
    }
}
