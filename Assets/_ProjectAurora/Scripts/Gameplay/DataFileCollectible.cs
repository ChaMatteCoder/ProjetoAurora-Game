using UnityEngine;

/// Coletável de Lore (DataFile): trigger que soma no DataFileManager e some.
/// Em dev aparece toda corrida; com persistProgress ligado no manager, já
/// coletados são desativados no spawn.
[RequireComponent(typeof(Collider))]
public class DataFileCollectible : MonoBehaviour
{
    [Tooltip("ID único do arquivo (referencia a Lore, ex.: DF_03 -> LORE_003).")]
    public string fileId = "DF_01";

    private void Start()
    {
        var mgr = DataFileManager.Instance;
        if (mgr != null && mgr.WasCollectedBefore(fileId))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerHealth>() == null) return;
        if (DataFileManager.Instance != null)
        {
            DataFileManager.Instance.Collect(fileId);
        }
        gameObject.SetActive(false);
    }
}
