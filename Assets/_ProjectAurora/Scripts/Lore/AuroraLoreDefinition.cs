using UnityEngine;

namespace ProjectAurora.Lore
{
    public enum AuroraLoreUnlockType
    {
        Default,
        GameplayCollectible,
        AuroraCoinPurchase,
        SecretMission
    }

    public enum AuroraLoreState
    {
        Locked,
        AvailableForPurchase,
        Unlocked
    }

    [CreateAssetMenu(fileName = "AuroraLoreDefinition", menuName = "Projeto Aurora/Lore/Lore Definition")]
    public sealed class AuroraLoreDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string categoryName;
        [SerializeField, TextArea(2, 5)] private string shortDescription;
        [SerializeField] private TextAsset fullText;
        [SerializeField] private AuroraLoreUnlockType unlockType;
        [SerializeField, Min(0)] private int auroraCoinPrice;
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private bool isSecret;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int displayOrder = 1;
        [SerializeField] private string sourceFileName;
        [SerializeField] private string relatedSector;
        [SerializeField] private string relatedCharacter;
        [SerializeField] private string futureMissionId;
        [SerializeField] private string futureCollectibleId;

        public string Id => id;
        public string DisplayName => displayName;
        public string CategoryName => categoryName;
        public string ShortDescription => shortDescription;
        public TextAsset FullText => fullText;
        public AuroraLoreUnlockType UnlockType => unlockType;
        public int AuroraCoinPrice => auroraCoinPrice;
        public bool UnlockedByDefault => unlockedByDefault;
        public bool IsSecret => isSecret;
        public Sprite Icon => icon;
        public int DisplayOrder => displayOrder;
        public string SourceFileName => sourceFileName;
        public string RelatedSector => relatedSector;
        public string RelatedCharacter => relatedCharacter;
        public string FutureMissionId => futureMissionId;
        public string FutureCollectibleId => futureCollectibleId;

        private void OnValidate()
        {
            id = Normalize(id);
            displayName = Normalize(displayName);
            categoryName = Normalize(categoryName);
            sourceFileName = Normalize(sourceFileName);
            relatedSector = Normalize(relatedSector);
            relatedCharacter = Normalize(relatedCharacter);
            futureMissionId = Normalize(futureMissionId);
            futureCollectibleId = Normalize(futureCollectibleId);
            auroraCoinPrice = Mathf.Max(0, auroraCoinPrice);
            displayOrder = Mathf.Max(1, displayOrder);

            if (unlockType == AuroraLoreUnlockType.SecretMission)
            {
                isSecret = true;
                unlockedByDefault = false;
                auroraCoinPrice = 0;
            }
        }

        private static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string loreId,
            string loreDisplayName,
            string loreCategoryName,
            string loreShortDescription,
            TextAsset loreFullText,
            AuroraLoreUnlockType loreUnlockType,
            int loreAuroraCoinPrice,
            bool loreUnlockedByDefault,
            bool loreIsSecret,
            Sprite loreIcon,
            int loreDisplayOrder,
            string loreSourceFileName,
            string loreRelatedSector,
            string loreRelatedCharacter,
            string loreFutureMissionId,
            string loreFutureCollectibleId)
        {
            id = Normalize(loreId);
            displayName = Normalize(loreDisplayName);
            categoryName = Normalize(loreCategoryName);
            shortDescription = loreShortDescription == null ? string.Empty : loreShortDescription.Trim();
            fullText = loreFullText;
            unlockType = loreUnlockType;
            auroraCoinPrice = Mathf.Max(0, loreAuroraCoinPrice);
            unlockedByDefault = loreUnlockedByDefault;
            isSecret = loreIsSecret;
            icon = loreIcon;
            displayOrder = Mathf.Max(1, loreDisplayOrder);
            sourceFileName = Normalize(loreSourceFileName);
            relatedSector = Normalize(loreRelatedSector);
            relatedCharacter = Normalize(loreRelatedCharacter);
            futureMissionId = Normalize(loreFutureMissionId);
            futureCollectibleId = Normalize(loreFutureCollectibleId);
            OnValidate();
        }
#endif
    }
}
