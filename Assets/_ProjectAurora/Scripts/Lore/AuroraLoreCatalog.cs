using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAurora.Lore
{
    [CreateAssetMenu(fileName = "AuroraLoreCatalog", menuName = "Projeto Aurora/Lore/Lore Catalog")]
    public sealed class AuroraLoreCatalog : ScriptableObject
    {
        public const int OfficialLoreCount = 24;

        [SerializeField] private List<AuroraLoreDefinition> entries = new List<AuroraLoreDefinition>();

        public IReadOnlyList<AuroraLoreDefinition> Entries => entries;
        public int Count => entries.Count;

        public AuroraLoreDefinition GetById(string loreId)
        {
            if (string.IsNullOrWhiteSpace(loreId))
            {
                return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AuroraLoreDefinition entry = entries[i];
                if (entry != null && string.Equals(entry.Id, loreId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        public List<string> CollectValidationIssues()
        {
            var issues = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();

            if (entries.Count != OfficialLoreCount)
            {
                issues.Add("O catálogo deve conter 24 arquivos; atual=" + entries.Count + ".");
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AuroraLoreDefinition entry = entries[i];
                if (entry == null)
                {
                    issues.Add("Entrada nula no índice " + i + ".");
                    continue;
                }

                if (!ids.Add(entry.Id)) issues.Add("ID duplicado: " + entry.Id + ".");
                if (!orders.Add(entry.DisplayOrder)) issues.Add("Ordem duplicada: " + entry.DisplayOrder + ".");
                if (entry.FullText == null) issues.Add(entry.Id + " sem TextAsset.");
                if (entry.FullText != null && string.IsNullOrWhiteSpace(entry.FullText.text))
                    issues.Add(entry.Id + " possui texto vazio.");
                if (entry.SourceFileName != entry.Id + ".txt")
                    issues.Add(entry.Id + " possui sourceFileName incorreto.");

                bool shouldBeDefault = entry.UnlockType == AuroraLoreUnlockType.Default;
                if (entry.UnlockedByDefault != shouldBeDefault)
                    issues.Add(entry.Id + " possui regra de default inconsistente.");
                if (entry.UnlockType == AuroraLoreUnlockType.AuroraCoinPurchase && entry.AuroraCoinPrice <= 0)
                    issues.Add(entry.Id + " é comprável e está sem preço.");
                if (entry.UnlockType != AuroraLoreUnlockType.AuroraCoinPurchase && entry.AuroraCoinPrice != 0)
                    issues.Add(entry.Id + " não é comprável e possui preço.");
                if (entry.UnlockType == AuroraLoreUnlockType.SecretMission &&
                    (!entry.IsSecret || string.IsNullOrWhiteSpace(entry.FutureMissionId)))
                    issues.Add(entry.Id + " possui configuração secreta incompleta.");
            }

            for (int number = 1; number <= OfficialLoreCount; number++)
            {
                string expectedId = "LORE_" + number.ToString("000");
                if (!ids.Contains(expectedId)) issues.Add("ID ausente: " + expectedId + ".");
            }

            return issues;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(IEnumerable<AuroraLoreDefinition> orderedEntries)
        {
            entries.Clear();
            if (orderedEntries != null)
            {
                entries.AddRange(orderedEntries);
            }
        }
#endif
    }
}
