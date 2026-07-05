using System.Collections;
using TMPro;
using UnityEngine;

/// Overlay de mudanca de setor (Round 11): titulo + subtitulo com fade in/hold/out.
/// Nao bloqueia input, nao pausa o jogo e nunca repete o mesmo setor.
public class SectorTitleOverlayController : MonoBehaviour
{
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text subtitleText;

    public Color normalColor = new Color(0.05f, 0.88f, 1f);
    public Color corruptedColor = new Color(1f, 0.16f, 0.16f);

    public float fadeInDuration = 0.35f;
    public float holdDuration = 2.0f;
    public float fadeOutDuration = 0.45f;

    private Coroutine routine;
    private int lastShownSector = int.MinValue;

    private void Awake()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
        }
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    /// Mostra o titulo do setor uma unica vez por indice.
    public void ShowSector(int sectorIndex, string title, string subtitle, bool corrupted)
    {
        if (sectorIndex == lastShownSector)
        {
            return;
        }

        lastShownSector = sectorIndex;
        if (titleText != null)
        {
            titleText.text = title ?? string.Empty;
            titleText.color = corrupted ? corruptedColor : normalColor;
        }
        if (subtitleText != null)
        {
            subtitleText.text = subtitle ?? string.Empty;
            Color sub = corrupted ? corruptedColor : new Color(0.88f, 0.96f, 1f, 0.92f);
            sub.a = 0.85f;
            subtitleText.color = sub;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (group == null)
        {
            yield break;
        }

        gameObject.SetActive(true);
        yield return Fade(group.alpha, 1f, fadeInDuration);

        float held = 0f;
        while (held < holdDuration)
        {
            held += Time.deltaTime;
            yield return null;
        }

        yield return Fade(1f, 0f, fadeOutDuration);
        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        duration = Mathf.Max(0.05f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }
}
