using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Decoupled pickup trigger. Reward integration is exposed through onCollected.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class AuroraCoinCollectible : MonoBehaviour
{
    [Header("Collectible")]
    [SerializeField, Min(1)] private int value = 1;
    [SerializeField] private bool collectOnce = true;
    [SerializeField] private AuroraCoinVisualController visualController;
    [SerializeField] private UnityEvent onCollected = new UnityEvent();

    [Header("Optional feedback")]
    [SerializeField] private AudioSource collectionAudioSource;
    [SerializeField] private AudioClip collectionClip;
    [SerializeField] private ParticleSystem collectionBurst;

    private Collider collectionTrigger;
    private bool collected;

    public int Value => value;
    public UnityEvent OnCollected => onCollected;

    private void Awake()
    {
        collectionTrigger = GetComponent<Collider>();
        if (visualController == null)
        {
            visualController = GetComponent<AuroraCoinVisualController>();
        }
    }

    private void OnEnable()
    {
        collected = false;
        if (collectionTrigger != null)
        {
            collectionTrigger.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected && collectOnce)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerHealth>() == null)
        {
            return;
        }

        collected = true;
        if (collectionTrigger != null)
        {
            collectionTrigger.enabled = false;
        }

        if (collectionAudioSource != null && collectionClip != null)
        {
            collectionAudioSource.PlayOneShot(collectionClip);
        }

        if (collectionBurst != null)
        {
            collectionBurst.Play(true);
        }

        onCollected.Invoke();

        if (visualController != null)
        {
            visualController.PlayCollectAnimation();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
