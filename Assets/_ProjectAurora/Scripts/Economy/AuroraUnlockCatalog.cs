using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AuroraUnlockCatalog", menuName = "Projeto Aurora/Economy/Unlock Catalog")]
public sealed class AuroraUnlockCatalog : ScriptableObject
{
    [SerializeField] private List<AuroraPurchasableItem> items = new List<AuroraPurchasableItem>();

    public IReadOnlyList<AuroraPurchasableItem> Items => items;

    public bool TryGetItem(string itemId, out AuroraPurchasableItem item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            AuroraPurchasableItem candidate = items[i];
            if (candidate != null && string.Equals(candidate.Id, itemId, StringComparison.Ordinal))
            {
                item = candidate;
                return true;
            }
        }

        item = null;
        return false;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(IEnumerable<AuroraPurchasableItem> catalogItems)
    {
        items.Clear();
        if (catalogItems != null)
        {
            items.AddRange(catalogItems);
        }
    }
#endif
}
