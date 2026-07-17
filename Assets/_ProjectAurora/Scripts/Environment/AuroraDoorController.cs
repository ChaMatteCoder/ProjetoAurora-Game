using System.Collections;
using UnityEngine;

/// Porta padrao Aurora (Round 11): dois paineis que deslizam para dentro da moldura
/// lateral (nada de bloco subindo por cima do cenario), luzes de status e ease real.
/// Usada nas portas de transicao de setor e no retrofit das portas interativas.
public class AuroraDoorController : MonoBehaviour
{
    [Header("Paineis (deslizam em X local, para fora do vao)")]
    public Transform panelLeft;
    public Transform panelRight;
    public float panelTravel = 3.4f;
    public float openDuration = 1.1f;
    public AnimationCurve openCurve = null;

    [Header("Bloqueio")]
    public Collider blockingCollider;
    [Range(0f, 1f)]
    [Tooltip("Fracao da abertura em que o collider deixa de bloquear.")]
    public float unblockAtProgress = 0.45f;

    [Header("Status")]
    public Renderer[] statusLights;
    public Color lockedColor = new Color(1f, 0.15f, 0.1f);
    public Color openColor = new Color(0.1f, 1f, 0.5f);

    [Header("SFX (opcional)")]
    public AudioSource sfxSource;
    public AudioClip openClip;

    public bool IsOpen { get; private set; }
    public bool IsLocked { get; private set; }

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private Coroutine moveRoutine;
    private bool initialized;

    private void Awake()
    {
        CacheClosedPositions();
        SetStatusColor(lockedColor);
    }

    private void CacheClosedPositions()
    {
        if (initialized)
        {
            return;
        }

        if (panelLeft != null)
        {
            leftClosed = panelLeft.localPosition;
        }
        if (panelRight != null)
        {
            rightClosed = panelRight.localPosition;
        }
        initialized = true;
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        if (!IsOpen)
        {
            SetStatusColor(locked ? lockedColor : openColor);
        }
    }

    public void OnApproachOpen()
    {
        Open();
    }

    public void Open()
    {
        if (IsOpen || IsLocked)
        {
            return;
        }

        IsOpen = true;
        PlayStatusLight();
        // poeira leve no vao central; no-op sem AuroraVFXController na cena
        ProjectAurora.VFX.AuroraVFXController.DoorOpen(transform.position + Vector3.up * 1.4f);
        if (sfxSource != null && openClip != null)
        {
            sfxSource.PlayOneShot(openClip);
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }
        moveRoutine = StartCoroutine(MovePanels(true));
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        SetStatusColor(lockedColor);
        if (blockingCollider != null)
        {
            blockingCollider.enabled = true;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }
        moveRoutine = StartCoroutine(MovePanels(false));
    }

    public void PlayStatusLight()
    {
        SetStatusColor(openColor);
    }

    private IEnumerator MovePanels(bool opening)
    {
        CacheClosedPositions();
        float duration = Mathf.Max(0.2f, openDuration);
        float elapsed = 0f;
        bool unblocked = !opening;

        Vector3 leftOpen = leftClosed + Vector3.left * panelTravel;
        Vector3 rightOpen = rightClosed + Vector3.right * panelTravel;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float linear = Mathf.Clamp01(elapsed / duration);
            float t = openCurve != null && openCurve.keys.Length > 1
                ? openCurve.Evaluate(linear)
                : Mathf.SmoothStep(0f, 1f, linear);
            float k = opening ? t : 1f - t;

            if (panelLeft != null)
            {
                panelLeft.localPosition = Vector3.LerpUnclamped(leftClosed, leftOpen, k);
            }
            if (panelRight != null)
            {
                panelRight.localPosition = Vector3.LerpUnclamped(rightClosed, rightOpen, k);
            }

            if (opening && !unblocked && k >= unblockAtProgress && blockingCollider != null)
            {
                blockingCollider.enabled = false;
                unblocked = true;
            }
            yield return null;
        }

        if (panelLeft != null)
        {
            panelLeft.localPosition = opening ? leftOpen : leftClosed;
        }
        if (panelRight != null)
        {
            panelRight.localPosition = opening ? rightOpen : rightClosed;
        }
        if (opening && blockingCollider != null)
        {
            blockingCollider.enabled = false;
        }

        moveRoutine = null;
    }

    private void SetStatusColor(Color color)
    {
        if (statusLights == null)
        {
            return;
        }

        foreach (Renderer light in statusLights)
        {
            if (light == null)
            {
                continue;
            }

            light.material.color = color;
            if (light.material.HasProperty("_BaseColor"))
            {
                light.material.SetColor("_BaseColor", color);
            }
            if (light.material.IsKeywordEnabled("_EMISSION"))
            {
                light.material.SetColor("_EmissionColor", color * 2.4f);
            }
        }
    }
}
