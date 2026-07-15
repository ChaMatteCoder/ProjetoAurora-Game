using System;

namespace ProjectAurora.Customization.Skins
{
    public sealed class AuroraSkinSelectionService
    {
        private readonly AuroraSkinCatalog catalog;
        private AuroraCoinWallet wallet;

        public string SelectedSkinId { get; private set; }
        public event Action<string> OnSelectedSkinChanged;

        public AuroraSkinSelectionService(AuroraSkinCatalog skinCatalog, AuroraCoinWallet progressWallet = null)
        {
            catalog = skinCatalog;
            wallet = progressWallet;
        }

        public bool CanSelect(string skinId)
        {
            AuroraSkinDefinition skin = catalog == null ? null : catalog.GetById(skinId);
            return skin != null && skin.HasSelectableModel && IsUnlocked(skin);
        }

        public bool IsUnlocked(AuroraSkinDefinition skin)
        {
            if (skin == null)
            {
                return false;
            }

            if (skin.IsDefaultSkin || skin.UnlockedByDefault)
            {
                return true;
            }

            AuroraCoinWallet targetWallet = ResolveWallet();
            return targetWallet != null &&
                   targetWallet.IsUnlocked(skin.FutureUnlockId, AuroraPurchaseCategory.Skin);
        }

        public bool TrySelect(string skinId)
        {
            if (!CanSelect(skinId))
            {
                return false;
            }

            if (string.Equals(SelectedSkinId, skinId, StringComparison.Ordinal))
            {
                return true;
            }

            AuroraCoinWallet targetWallet = ResolveWallet();
            if (targetWallet == null || !targetWallet.TrySetSelectedSkinId(skinId))
            {
                return false;
            }

            SelectedSkinId = skinId;
            OnSelectedSkinChanged?.Invoke(SelectedSkinId);
            return true;
        }

        public AuroraSkinDefinition GetSelectedSkin()
        {
            return catalog == null ? null : catalog.GetById(SelectedSkinId);
        }

        public void LoadSelectedSkin()
        {
            AuroraCoinWallet targetWallet = ResolveWallet();
            string savedId = targetWallet == null ? string.Empty : targetWallet.SelectedSkinId;
            AuroraSkinDefinition saved = catalog == null ? null : catalog.GetById(savedId);
            AuroraSkinDefinition fallback = ResolveFallback();
            AuroraSkinDefinition resolved = saved != null && CanSelect(saved.Id) ? saved : fallback;

            SelectedSkinId = resolved == null ? string.Empty : resolved.Id;
            if (targetWallet != null && !string.IsNullOrEmpty(SelectedSkinId) &&
                !string.Equals(savedId, SelectedSkinId, StringComparison.Ordinal))
            {
                targetWallet.TrySetSelectedSkinId(SelectedSkinId);
            }
        }

        public bool SaveSelectedSkin()
        {
            AuroraCoinWallet targetWallet = ResolveWallet();
            return targetWallet != null && targetWallet.TrySetSelectedSkinId(SelectedSkinId);
        }

        private AuroraSkinDefinition ResolveFallback()
        {
            if (catalog == null)
            {
                return null;
            }

            AuroraSkinDefinition defaultSkin = catalog.GetDefaultSkin();
            if (defaultSkin != null && CanSelect(defaultSkin.Id))
            {
                return defaultSkin;
            }

            for (int i = 0; i < catalog.Count; i++)
            {
                AuroraSkinDefinition candidate = catalog.Skins[i];
                if (candidate != null && CanSelect(candidate.Id))
                {
                    return candidate;
                }
            }

            return defaultSkin;
        }

        private AuroraCoinWallet ResolveWallet()
        {
            if (wallet == null)
            {
                wallet = AuroraCoinWallet.Instance;
            }

            return wallet;
        }
    }
}
