using System.Collections.Generic;
using UnityEngine;

/// Ponte de compatibilidade do prefab antigo DF_01..DF_12. Novos colecionáveis devem
/// usar AuroraDataFileCollectible com um ID LORE explícito.
[RequireComponent(typeof(Collider))]
public class DataFileCollectible : MonoBehaviour
{
    /// Registro dos DataFiles ativos (para o som de proximidade do DataFileManager).
    public static readonly List<DataFileCollectible> Active = new List<DataFileCollectible>();

    [Tooltip("ID legado sequencial da corrida (DF_01 a DF_12). O manager converte para o LORE coletável oficial.")]
    public string fileId = "DF_01";

    private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }

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
        if (other == null || other.GetComponentInParent<PlayerHealth>() == null) return;
        if (DataFileManager.Instance != null && DataFileManager.Instance.Collect(fileId))
            gameObject.SetActive(false);
    }
}
