using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AuroraProgressSaveData
{
    public const int CurrentVersion = 2;

    public int version = CurrentVersion;
    public int auroraCoins;
    public string selectedSkinId = "default";
    public List<string> unlockedSkins = new List<string>();
    public List<string> unlockedDataFiles = new List<string>();

    public void Sanitize()
    {
        version = CurrentVersion;
        auroraCoins = Mathf.Clamp(auroraCoins, 0, AuroraCoinWallet.MaxBalance);
        selectedSkinId = string.IsNullOrWhiteSpace(selectedSkinId) ? "default" : selectedSkinId.Trim();
        NormalizeIds(ref unlockedSkins);
        NormalizeIds(ref unlockedDataFiles);
    }

    private static void NormalizeIds(ref List<string> ids)
    {
        if (ids == null)
        {
            ids = new List<string>();
            return;
        }

        var unique = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i] == null ? string.Empty : ids[i].Trim();
            if (id.Length > 0)
            {
                unique.Add(id);
            }
        }

        ids.Clear();
        ids.AddRange(unique);
        ids.Sort(StringComparer.Ordinal);
    }
}
