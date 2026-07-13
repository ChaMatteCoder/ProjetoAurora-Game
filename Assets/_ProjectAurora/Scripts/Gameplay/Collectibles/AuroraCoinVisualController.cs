using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lightweight collectible presentation: spin, bob, emission pulse and pickup animation.
/// Uses one cached MaterialPropertyBlock and is safe to reuse from a pool.
/// </summary>
[DisallowMultipleComponent]
public sealed class AuroraCoinVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer[] emissionRenderers = System.Array.Empty<Renderer>();

    [Header("Idle")]
    [SerializeField, Min(0.1f)] private float rotationDuration = 3.2f;
    [SerializeField, Range(0f, 0.12f)] private float bobAmplitude = 0.055f;
    [SerializeField, Min(0.1f)] private float bobCycle = 1.6f;
    [SerializeField, Range(0.1f, 4f)] private float pulseFrequency = 0.85f;
    [SerializeField] private Color emissionColor = new Color(0f, 0.82f, 1f, 1f);
    [SerializeField, Range(0f, 8f)] private float emissionMin = 1.35f;
    [SerializeField, Range(0f, 8f)] private float emissionMax = 3.1f;

    [Header("Collection")]
    [SerializeField, Range(0.2f, 0.8f)] private float collectDuration = 0.42f;
    [SerializeField, Range(0f, 0.5f)] private float collectRise = 0.22f;
    [SerializeField, Range(1f, 1.4f)] private float collectPeakScale = 1.15f;
    [SerializeField, Range(1f, 8f)] private float collectSpinMultiplier = 4.5f;
    [SerializeField, Range(1f, 8f)] private float collectFlashMultiplier = 2.2f;
    [SerializeField] private UnityEvent onCollectAnimationCompleted = new UnityEvent();

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private const float TwoPi = Mathf.PI * 2f;

    private MaterialPropertyBlock propertyBlock;
    private Vector3 idleLocalPosition;
    private Vector3 idleLocalScale;
    private Vector3 collectStartPosition;
    private float phase;
    private float collectTime;
    private bool collecting;

    public UnityEvent OnCollectAnimationCompleted => onCollectAnimationCompleted;
    public bool IsCollecting => collecting;

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        propertyBlock = new MaterialPropertyBlock();
        idleLocalPosition = visualRoot.localPosition;
        idleLocalScale = visualRoot.localScale;
        Vector3 worldPosition = transform.position;
        phase = Mathf.Repeat(Mathf.Abs(worldPosition.x * 12.9898f + worldPosition.z * 78.233f), TwoPi);
        ResolveEmissionRenderersIfNeeded();
    }

    private void OnEnable()
    {
        collecting = false;
        collectTime = 0f;

        if (visualRoot != null)
        {
            visualRoot.localPosition = idleLocalPosition;
            visualRoot.localScale = idleLocalScale;
        }

        ApplyEmission(emissionMin);
    }

    private void Update()
    {
        if (collecting)
        {
            UpdateCollection(Time.deltaTime);
        }
        else
        {
            UpdateIdle(Time.deltaTime);
        }
    }

    public void PlayCollectAnimation()
    {
        if (collecting || visualRoot == null)
        {
            return;
        }

        collecting = true;
        collectTime = 0f;
        collectStartPosition = visualRoot.localPosition;
    }

    public void Configure(Transform targetVisualRoot, Renderer[] targetEmissionRenderers)
    {
        visualRoot = targetVisualRoot;
        emissionRenderers = targetEmissionRenderers ?? System.Array.Empty<Renderer>();
    }

    private void UpdateIdle(float deltaTime)
    {
        float spinSpeed = 360f / Mathf.Max(0.1f, rotationDuration);
        visualRoot.Rotate(Vector3.up, spinSpeed * deltaTime, Space.World);

        float bobTime = (Time.time / Mathf.Max(0.1f, bobCycle)) * TwoPi;
        float bob = Mathf.Sin(bobTime + phase) * bobAmplitude;
        visualRoot.localPosition = idleLocalPosition + Vector3.up * bob;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseFrequency * TwoPi + phase);
        pulse = pulse * pulse * (3f - 2f * pulse);
        ApplyEmission(Mathf.Lerp(emissionMin, emissionMax, pulse));
    }

    private void UpdateCollection(float deltaTime)
    {
        collectTime += deltaTime;
        float t = Mathf.Clamp01(collectTime / Mathf.Max(0.01f, collectDuration));
        float eased = t * t * (3f - 2f * t);

        float spinSpeed = (360f / Mathf.Max(0.1f, rotationDuration)) * collectSpinMultiplier;
        visualRoot.Rotate(Vector3.up, spinSpeed * deltaTime, Space.World);
        visualRoot.localPosition = collectStartPosition + Vector3.up * (collectRise * eased);

        float scaleFactor;
        if (t < 0.28f)
        {
            float grow = t / 0.28f;
            scaleFactor = Mathf.Lerp(1f, collectPeakScale, grow * grow * (3f - 2f * grow));
        }
        else
        {
            float shrink = (t - 0.28f) / 0.72f;
            shrink = shrink * shrink * (3f - 2f * shrink);
            scaleFactor = Mathf.Lerp(collectPeakScale, 0f, shrink);
        }

        visualRoot.localScale = idleLocalScale * scaleFactor;
        float flash = Mathf.Lerp(emissionMax * collectFlashMultiplier, 0f, eased);
        ApplyEmission(flash);

        if (t < 1f)
        {
            return;
        }

        collecting = false;
        onCollectAnimationCompleted.Invoke();
        gameObject.SetActive(false);
    }

    private void ResolveEmissionRenderersIfNeeded()
    {
        if (emissionRenderers != null && emissionRenderers.Length > 0)
        {
            return;
        }

        Renderer[] candidates = visualRoot.GetComponentsInChildren<Renderer>(true);
        int matchCount = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            string candidateName = candidates[i].name;
            if (candidateName.Contains("Emission") || candidateName.Contains("Symbol") || candidateName.Contains("Hologram"))
            {
                matchCount++;
            }
        }

        emissionRenderers = new Renderer[matchCount];
        int targetIndex = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            string candidateName = candidates[i].name;
            if (candidateName.Contains("Emission") || candidateName.Contains("Symbol") || candidateName.Contains("Hologram"))
            {
                emissionRenderers[targetIndex++] = candidates[i];
            }
        }
    }

    private void ApplyEmission(float intensity)
    {
        if (propertyBlock == null || emissionRenderers == null)
        {
            return;
        }

        Color hdrEmission = emissionColor * Mathf.Max(0f, intensity);
        hdrEmission.a = 1f;
        Color baseColor = Color.Lerp(emissionColor, Color.white, Mathf.Clamp01((intensity - emissionMin) * 0.12f));
        baseColor.a = emissionColor.a;

        for (int i = 0; i < emissionRenderers.Length; i++)
        {
            Renderer targetRenderer = emissionRenderers[i];
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, hdrEmission);
            propertyBlock.SetColor(BaseColorId, baseColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
