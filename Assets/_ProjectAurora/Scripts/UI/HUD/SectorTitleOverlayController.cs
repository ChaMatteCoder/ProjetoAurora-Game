using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Overlay de mudanca de setor (Round 11): titulo + subtitulo com fade in/hold/out.
/// Round 13: fundo sci-fi translucido com linhas de acento (ciano/vermelho por setor)
/// e leve animacao de entrada (desliza + varredura das linhas).
/// Nao bloqueia input, nao pausa o jogo e nunca repete o mesmo setor.
public class SectorTitleOverlayController : MonoBehaviour
{
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text subtitleText;

    [Header("Fundo tematico (Round 13)")]
    public Image backgroundImage;
    public Image[] accentImages;
    public Color backgroundColor = new Color(0.008f, 0.035f, 0.055f, 0.82f);
    public Color backgroundCorruptedColor = new Color(0.07f, 0.008f, 0.014f, 0.82f);
    public float entranceSlide = 22f;

    public Color normalColor = new Color(0.05f, 0.88f, 1f);
    public Color corruptedColor = new Color(1f, 0.16f, 0.16f);

    public float fadeInDuration = 0.35f;
    public float holdDuration = 2.0f;
    public float fadeOutDuration = 0.45f;

    private Coroutine routine;
    private int lastShownSector = int.MinValue;
    private RectTransform rect;
    private Vector2 basePosition;
    private bool baseCaptured;

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

        rect = GetComponent<RectTransform>();
        if (rect != null && !baseCaptured)
        {
            basePosition = rect.anchoredPosition;
            baseCaptured = true;
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

        // Round 13: tema do fundo/linhas por setor
        if (backgroundImage != null)
        {
            backgroundImage.color = corrupted ? backgroundCorruptedColor : backgroundColor;
        }
        if (accentImages != null)
        {
            Color accent = corrupted ? corruptedColor : normalColor;
            accent.a = 0.8f;
            foreach (Image img in accentImages)
            {
                if (img != null)
                {
                    img.color = accent;
                }
            }
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

        // entrada: desliza de cima + varredura horizontal das linhas de acento
        float elapsed = 0f;
        float inDuration = Mathf.Max(0.05f, fadeInDuration);
        float startAlpha = group.alpha;
        while (elapsed < inDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / inDuration));
            group.alpha = Mathf.Lerp(startAlpha, 1f, t);
            if (rect != null && baseCaptured)
            {
                rect.anchoredPosition = basePosition + new Vector2(0f, (1f - t) * entranceSlide);
            }
            if (accentImages != null)
            {
                foreach (Image img in accentImages)
                {
                    if (img != null)
                    {
                        Vector3 s = img.rectTransform.localScale;
                        s.x = t;
                        img.rectTransform.localScale = s;
                    }
                }
            }
            yield return null;
        }
        group.alpha = 1f;
        if (rect != null && baseCaptured)
        {
            rect.anchoredPosition = basePosition;
        }

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
