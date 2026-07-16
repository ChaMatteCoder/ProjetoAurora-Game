using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Pickup trigger integrated with the single persistent AuroraCoin wallet.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class AuroraCoinCollectible : MonoBehaviour
{
    public const int CoinValue = 1;

    [Header("Collectible")]
    [SerializeField] private AuroraCoinVisualController visualController;
    [SerializeField] private UnityEvent onCollected = new UnityEvent();

    [Header("Optional feedback")]
    [SerializeField] private AudioSource collectionAudioSource;
    [SerializeField] private AudioClip collectionClip;
    [SerializeField] private ParticleSystem collectionBurst;

    private Collider collectionTrigger;
    private bool collected;

    public int Value => CoinValue;
    public UnityEvent OnCollected => onCollected;
    public bool IsCollected => collected;

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
        TryCollect(other);
    }

    public bool TryCollect(Collider other)
    {
        if (collected || other == null)
        {
            return false;
        }

        if (other.GetComponentInParent<PlayerHealth>() == null)
        {
            return false;
        }

        AuroraCoinWallet wallet = AuroraCoinWallet.Instance;
        if (wallet == null)
        {
            Debug.LogError("[AuroraCoin] Wallet indisponivel; coleta mantida ativa.", this);
            return false;
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
        else
        {
            AuroraSfx.PlayCoin();
        }

        if (collectionBurst != null)
        {
            collectionBurst.Play(true);
        }
        else
        {
            // As 186 moedas da cena nao tem burst proprio: usa o pool compartilhado.
            // Sem pool na cena -> no-op (a coleta e o saldo seguem funcionando).
            ProjectAurora.VFX.AuroraVFXController.CoinCollect(transform.position);
        }

        // At the cap this returns false, but the pickup still resolves visually for this run.
        wallet.TryAddCoins(CoinValue);
        onCollected.Invoke();

        if (visualController != null)
        {
            visualController.PlayCollectAnimation();
        }
        else
        {
            gameObject.SetActive(false);
        }

        return true;
    }
}
