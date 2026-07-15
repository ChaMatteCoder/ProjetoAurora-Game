using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectAurora.Lore
{
    public sealed class AuroraLoreService
    {
        private static AuroraLoreService instance;

        private readonly AuroraLoreCatalog catalog;
        private readonly AuroraCoinWallet wallet;
        private readonly AuroraPurchaseService purchaseService;

        public static AuroraLoreService Instance => instance;
        public AuroraLoreCatalog Catalog => catalog;
        public int UnlockedCount => GetUnlockedCount();

        public event Action<string> OnLoreUnlocked;
        public event Action<string> OnLorePurchased;

        public AuroraLoreService(AuroraLoreCatalog loreCatalog, AuroraCoinWallet targetWallet = null)
        {
            catalog = loreCatalog;
            wallet = targetWallet ?? AuroraCoinWallet.Instance;
            purchaseService = wallet == null ? null : new AuroraPurchaseService(wallet);
            SynchronizeProgress();
        }

        public static AuroraLoreService Initialize(
            AuroraLoreCatalog loreCatalog,
            AuroraCoinWallet targetWallet = null)
        {
            if (loreCatalog == null)
            {
                return instance;
            }

            AuroraCoinWallet resolvedWallet = targetWallet ?? AuroraCoinWallet.Instance;
            if (instance == null || instance.catalog != loreCatalog || instance.wallet != resolvedWallet)
            {
                instance = new AuroraLoreService(loreCatalog, resolvedWallet);
            }

            return instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        public bool IsUnlocked(string loreId)
        {
            AuroraLoreDefinition definition = GetById(loreId);
            return definition != null &&
                   (definition.UnlockedByDefault ||
                    (wallet != null && wallet.IsUnlocked(definition.Id, AuroraPurchaseCategory.DataFile)));
        }

        public AuroraLoreState GetState(string loreId)
        {
            if (IsUnlocked(loreId))
            {
                return AuroraLoreState.Unlocked;
            }

            return CanPurchase(loreId)
                ? AuroraLoreState.AvailableForPurchase
                : AuroraLoreState.Locked;
        }

        public bool CanPurchase(string loreId)
        {
            AuroraLoreDefinition definition = GetById(loreId);
            return definition != null &&
                   definition.UnlockType == AuroraLoreUnlockType.AuroraCoinPurchase &&
                   definition.AuroraCoinPrice > 0 &&
                   purchaseService != null &&
                   purchaseService.CanPurchase(
                       definition.Id,
                       AuroraPurchaseCategory.DataFile,
                       definition.AuroraCoinPrice,
                       definition.UnlockedByDefault);
        }

        public bool TryPurchase(string loreId)
        {
            AuroraLoreDefinition definition = GetById(loreId);
            if (definition == null ||
                definition.UnlockType != AuroraLoreUnlockType.AuroraCoinPurchase ||
                purchaseService == null)
            {
                return false;
            }

            if (!purchaseService.TryPurchase(
                    definition.Id,
                    AuroraPurchaseCategory.DataFile,
                    definition.AuroraCoinPrice,
                    definition.UnlockedByDefault))
            {
                return false;
            }

            OnLorePurchased?.Invoke(definition.Id);
            OnLoreUnlocked?.Invoke(definition.Id);
            return true;
        }

        public bool TryUnlockFromGameplay(string loreId)
        {
            AuroraLoreDefinition definition = GetById(loreId);
            if (definition == null ||
                definition.UnlockType != AuroraLoreUnlockType.GameplayCollectible ||
                wallet == null ||
                !wallet.TryUnlockItem(definition.Id, AuroraPurchaseCategory.DataFile))
            {
                return false;
            }

            OnLoreUnlocked?.Invoke(definition.Id);
            return true;
        }

        public bool TryUnlockSecret(string loreId, string missionId)
        {
            AuroraLoreDefinition definition = GetById(loreId);
            if (definition == null ||
                definition.UnlockType != AuroraLoreUnlockType.SecretMission ||
                string.IsNullOrWhiteSpace(definition.FutureMissionId) ||
                !string.Equals(definition.FutureMissionId, missionId, StringComparison.Ordinal) ||
                wallet == null ||
                !wallet.TryUnlockItem(definition.Id, AuroraPurchaseCategory.DataFile))
            {
                return false;
            }

            OnLoreUnlocked?.Invoke(definition.Id);
            return true;
        }

        public AuroraLoreDefinition GetById(string loreId)
        {
            return catalog == null ? null : catalog.GetById(loreId);
        }

        public IReadOnlyList<AuroraLoreDefinition> GetAll()
        {
            return catalog == null
                ? Array.Empty<AuroraLoreDefinition>()
                : catalog.Entries;
        }

        public int GetUnlockedCount()
        {
            if (catalog == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                AuroraLoreDefinition definition = catalog.Entries[i];
                if (definition != null && IsUnlocked(definition.Id))
                {
                    count++;
                }
            }

            return count;
        }

        public void SynchronizeProgress()
        {
            if (catalog == null || wallet == null)
            {
                return;
            }

            IEnumerable<string> validIds = catalog.Entries
                .Where(entry => entry != null)
                .Select(entry => entry.Id);
            IEnumerable<string> defaults = catalog.Entries
                .Where(entry => entry != null && entry.UnlockedByDefault)
                .Select(entry => entry.Id);
            wallet.SynchronizeUnlocks(
                AuroraPurchaseCategory.DataFile,
                validIds,
                defaults,
                "LORE_");
        }

#if UNITY_EDITOR
        public static void ReleaseTestInstance(AuroraLoreService service)
        {
            if (instance == service)
            {
                instance = null;
            }
        }
#endif
    }
}
