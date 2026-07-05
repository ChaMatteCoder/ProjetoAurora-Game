using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VoiceLineDatabase", menuName = "Projeto Aurora/Voice/Voice Line Database")]
public class VoiceLineDatabase : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] private List<VoiceLineEntry> entries = new List<VoiceLineEntry>();

    private readonly Dictionary<string, VoiceLineEntry> byId =
        new Dictionary<string, VoiceLineEntry>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<VoiceLineEntry> Entries => entries;

    public VoiceLineEntry GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        EnsureCache();
        byId.TryGetValue(id.Trim(), out VoiceLineEntry entry);
        return entry;
    }

    public bool Contains(string id) => GetById(id) != null;

    public void ReplaceEntries(IEnumerable<VoiceLineEntry> newEntries)
    {
        entries = newEntries == null
            ? new List<VoiceLineEntry>()
            : new List<VoiceLineEntry>(newEntries);
        RebuildCache(true);
    }

    public List<string> FindDuplicateIds()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<string>();
        foreach (VoiceLineEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            if (!seen.Add(entry.id.Trim()) && !duplicates.Contains(entry.id.Trim()))
            {
                duplicates.Add(entry.id.Trim());
            }
        }

        return duplicates;
    }

    private void OnEnable() => RebuildCache(false);

    private void OnValidate() => RebuildCache(true);

    private void EnsureCache()
    {
        if (byId.Count == 0 && entries.Count > 0)
        {
            RebuildCache(false);
        }
    }

    private void RebuildCache(bool logDuplicates)
    {
        byId.Clear();
        foreach (VoiceLineEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            string normalized = entry.id.Trim().ToUpperInvariant();
            entry.id = normalized;
            if (byId.ContainsKey(normalized))
            {
                if (logDuplicates)
                {
                    Debug.LogWarning($"[Voice] ID duplicado ignorado: {normalized}", this);
                }
                continue;
            }

            byId.Add(normalized, entry);
        }
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        byId.Clear();
    }
}
