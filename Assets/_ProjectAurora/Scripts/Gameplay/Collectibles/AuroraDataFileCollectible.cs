using ProjectAurora.Lore;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class AuroraDataFileCollectible : MonoBehaviour
{
    [SerializeField] private AuroraLoreCatalog loreCatalog;
    [SerializeField] private string loreId = "LORE_001";
    [SerializeField] private bool collectOncePerSave = true;
    [SerializeField] private UnityEvent onCollected;
    [SerializeField] private AudioClip collectSfx;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Collider triggerCollider;

    public string LoreId => loreId;
    public bool CollectOncePerSave => collectOncePerSave;
    public AuroraLoreCatalog LoreCatalog => loreCatalog;

    private void Awake()
    {
        if (triggerCollider == null) triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }

    private void Start()
    {
        RefreshAvailability();
    }

    public void RefreshAvailability()
    {
        AuroraLoreService service = ResolveService();
        if (collectOncePerSave && service != null && service.IsUnlocked(loreId))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.GetComponentInParent<PlayerHealth>() == null)
        {
            return;
        }

        TryCollect();
    }

    public bool TryCollect()
    {
        AuroraLoreService service = ResolveService();
        AuroraLoreDefinition definition = service == null ? null : service.GetById(loreId);
        if (definition == null || definition.UnlockType != AuroraLoreUnlockType.GameplayCollectible)
        {
            Debug.LogWarning("[AuroraDataFile] ID inválido ou não coletável: " + loreId + ".", this);
            return false;
        }

        if (!service.TryUnlockFromGameplay(loreId))
        {
            if (collectOncePerSave && service.IsUnlocked(loreId)) gameObject.SetActive(false);
            return false;
        }

        if (collectSfx != null) AudioSource.PlayClipAtPoint(collectSfx, transform.position);
        DataFileManager.Instance?.ShowCollectedFeedback(loreId);
        onCollected?.Invoke();
        gameObject.SetActive(false);
        return true;
    }

    private AuroraLoreService ResolveService()
    {
        return AuroraLoreService.Instance ?? AuroraLoreService.Initialize(loreCatalog);
    }

    private void OnValidate()
    {
        loreId = loreId == null ? string.Empty : loreId.Trim();
        if (triggerCollider == null) triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        AuroraLoreCatalog catalog,
        string targetLoreId,
        GameObject targetVisualRoot,
        Collider targetTrigger)
    {
        loreCatalog = catalog;
        loreId = targetLoreId == null ? string.Empty : targetLoreId.Trim();
        collectOncePerSave = true;
        visualRoot = targetVisualRoot;
        triggerCollider = targetTrigger;
        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }
#endif
}
