using ProjectAurora.Customization.Skins;
using UnityEngine;

namespace ProjectAurora.UI.Menu.Skins
{
    [DisallowMultipleComponent]
    public sealed class AuroraSkinPreviewController : MonoBehaviour
    {
        [SerializeField] private Transform previewCharacterAnchor;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private RenderTexture previewTexture;
        [SerializeField] private int previewLayer = 0;
        [SerializeField, Range(1.05f, 1.5f)] private float framingMargin = 1.18f;

        private GameObject currentPreview;
        private int currentRendererCount;

        public bool CameraEnabled => previewCamera != null && previewCamera.enabled;
        public bool HasPreview => currentPreview != null;
        public int CurrentRendererCount => currentRendererCount;
        public RenderTexture PreviewTexture => previewTexture;

        private void Awake()
        {
            if (previewCamera != null)
            {
                previewCamera.targetTexture = previewTexture;
                previewCamera.enabled = false;
            }
        }

        public bool Show(AuroraSkinDefinition skin)
        {
            ClosePreview();
            if (skin == null || skin.PreviewPrefab == null || previewCharacterAnchor == null || previewCamera == null)
            {
                return false;
            }

            currentPreview = Instantiate(skin.PreviewPrefab, previewCharacterAnchor, false);
            currentPreview.name = "Preview_" + skin.Id;
            currentPreview.transform.localPosition = Vector3.zero;
            currentPreview.transform.localRotation = Quaternion.Euler(skin.PreviewRotationOffset);
            currentPreview.transform.localScale *= skin.PreviewScaleMultiplier;
            SanitizePreview(currentPreview);

            if (!TryCalculateBounds(currentPreview, out Bounds bounds))
            {
                ClosePreview();
                return false;
            }

            Vector3 centeredFeet = previewCharacterAnchor.position -
                                   new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            currentPreview.transform.position += centeredFeet + skin.PreviewPositionOffset;
            if (!TryCalculateBounds(currentPreview, out bounds))
            {
                ClosePreview();
                return false;
            }

            FrameCamera(bounds, skin.PreviewCameraDistance);
            previewCamera.backgroundColor = skin.PreviewBackgroundTint;
            previewCamera.targetTexture = previewTexture;
            previewCamera.enabled = true;
            return true;
        }

        public void ClosePreview()
        {
            if (previewCamera != null)
            {
                previewCamera.enabled = false;
            }

            if (currentPreview != null)
            {
                currentPreview.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(currentPreview);
                }
                else
                {
                    DestroyImmediate(currentPreview);
                }
            }

            currentPreview = null;
            currentRendererCount = 0;
        }

        private void OnDestroy()
        {
            ClosePreview();
        }

        private void SanitizePreview(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = previewLayer;
                transforms[i].gameObject.tag = "Untagged";
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].detectCollisions = false;
            }

            AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++) audioSources[i].enabled = false;

            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++) animators[i].enabled = false;

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++) behaviours[i].enabled = false;
        }

        private bool TryCalculateBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            currentRendererCount = 0;
            bounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (currentRendererCount == 0) bounds = renderer.bounds;
                else bounds.Encapsulate(renderer.bounds);
                currentRendererCount++;
            }

            return currentRendererCount > 0 && bounds.size.sqrMagnitude > 0.0001f;
        }

        private void FrameCamera(Bounds bounds, float requestedDistance)
        {
            float verticalFov = Mathf.Max(1f, previewCamera.fieldOfView) * Mathf.Deg2Rad;
            float aspect = previewTexture == null || previewTexture.height == 0
                ? Mathf.Max(0.1f, previewCamera.aspect)
                : previewTexture.width / (float)previewTexture.height;
            float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * aspect);
            float verticalDistance = bounds.extents.y / Mathf.Tan(verticalFov * 0.5f);
            float horizontalDistance = bounds.extents.x / Mathf.Tan(horizontalFov * 0.5f);
            float automaticDistance = (Mathf.Max(verticalDistance, horizontalDistance) + bounds.extents.z) * framingMargin;
            float distance = requestedDistance > 0f ? Mathf.Max(automaticDistance, requestedDistance) : automaticDistance;
            Vector3 focus = bounds.center;
            previewCamera.transform.position = focus + Vector3.forward * Mathf.Max(0.25f, distance);
            previewCamera.transform.LookAt(focus, Vector3.up);
            previewCamera.nearClipPlane = Mathf.Max(0.01f, distance - bounds.extents.z * 2f - 0.5f);
            previewCamera.farClipPlane = distance + bounds.extents.z * 2f + 5f;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Transform characterAnchor,
            Camera targetCamera,
            RenderTexture targetTexture,
            int layer)
        {
            previewCharacterAnchor = characterAnchor;
            previewCamera = targetCamera;
            previewTexture = targetTexture;
            previewLayer = layer;
            if (previewCamera != null)
            {
                previewCamera.targetTexture = previewTexture;
                previewCamera.enabled = false;
            }
        }
#endif
    }
}
