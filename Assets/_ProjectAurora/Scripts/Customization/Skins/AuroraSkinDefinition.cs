using UnityEngine;

namespace ProjectAurora.Customization.Skins
{
    [CreateAssetMenu(fileName = "AuroraSkinDefinition", menuName = "Projeto Aurora/Skins/Skin Definition")]
    public sealed class AuroraSkinDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private Sprite splashArt;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private GameObject gameplayPrefab;
        [SerializeField] private bool unlockedByDefault;
        [SerializeField, Min(0)] private int futurePrice;
        [SerializeField] private string futureUnlockId;
        [SerializeField] private bool isDefaultSkin;

        [Header("Preview tuning")]
        [SerializeField] private Vector3 previewPositionOffset;
        [SerializeField] private Vector3 previewRotationOffset;
        [SerializeField, Min(0.01f)] private float previewScaleMultiplier = 1f;
        [SerializeField, Min(0f)] private float previewCameraDistance;
        [SerializeField] private Color previewBackgroundTint = new Color(0.004f, 0.018f, 0.03f, 1f);

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite SplashArt => splashArt;
        public GameObject PreviewPrefab => previewPrefab;
        public GameObject GameplayPrefab => gameplayPrefab;
        public bool UnlockedByDefault => unlockedByDefault;
        public int FuturePrice => futurePrice;
        public string FutureUnlockId => string.IsNullOrWhiteSpace(futureUnlockId) ? id : futureUnlockId;
        public bool IsDefaultSkin => isDefaultSkin;
        public Vector3 PreviewPositionOffset => previewPositionOffset;
        public Vector3 PreviewRotationOffset => previewRotationOffset;
        public float PreviewScaleMultiplier => Mathf.Max(0.01f, previewScaleMultiplier);
        public float PreviewCameraDistance => Mathf.Max(0f, previewCameraDistance);
        public Color PreviewBackgroundTint => previewBackgroundTint;
        public bool HasPreviewModel => previewPrefab != null;
        public bool HasSelectableModel => gameplayPrefab != null;

        private void OnValidate()
        {
            id = id == null ? string.Empty : id.Trim();
            displayName = displayName == null ? string.Empty : displayName.Trim();
            futureUnlockId = futureUnlockId == null ? string.Empty : futureUnlockId.Trim();
            futurePrice = Mathf.Max(0, futurePrice);
            previewScaleMultiplier = Mathf.Max(0.01f, previewScaleMultiplier);
            previewCameraDistance = Mathf.Max(0f, previewCameraDistance);
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string skinId,
            string skinDisplayName,
            string skinDescription,
            Sprite skinSplashArt,
            GameObject skinPreviewPrefab,
            GameObject skinGameplayPrefab,
            bool skinUnlockedByDefault,
            int skinFuturePrice,
            string skinFutureUnlockId,
            bool skinIsDefault,
            Vector3 positionOffset,
            Vector3 rotationOffset,
            float scaleMultiplier,
            float cameraDistance,
            Color backgroundTint)
        {
            id = skinId == null ? string.Empty : skinId.Trim();
            displayName = skinDisplayName == null ? string.Empty : skinDisplayName.Trim();
            description = skinDescription ?? string.Empty;
            splashArt = skinSplashArt;
            previewPrefab = skinPreviewPrefab;
            gameplayPrefab = skinGameplayPrefab;
            unlockedByDefault = skinUnlockedByDefault;
            futurePrice = Mathf.Max(0, skinFuturePrice);
            futureUnlockId = skinFutureUnlockId == null ? string.Empty : skinFutureUnlockId.Trim();
            isDefaultSkin = skinIsDefault;
            previewPositionOffset = positionOffset;
            previewRotationOffset = rotationOffset;
            previewScaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            previewCameraDistance = Mathf.Max(0f, cameraDistance);
            previewBackgroundTint = backgroundTint;
        }
#endif
    }
}
