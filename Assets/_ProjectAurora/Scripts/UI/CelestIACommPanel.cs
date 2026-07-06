using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CelestIACommPanel : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text statusText;
    public TMP_Text messageText;
    public Image portraitImage;
    public Image signalIcon;
    public Image[] waveformBars;
    public Color normalColor = new Color(0.05f, 0.88f, 1f);
    public Color transitionColor = new Color(1f, 0.62f, 0.08f);
    public Color corruptedColor = new Color(1f, 0.12f, 0.15f);
    public bool animateWaveform = true;

    [Header("Glitch corrompido (Round 13) — so status/nome, nunca a mensagem")]
    public bool glitchWhenCorrupted = true;
    [Tooltip("Deslocamento maximo em px durante um burst de glitch.")]
    public float glitchJitter = 1.7f;
    public Color glitchAltColor = new Color(0.05f, 0.88f, 1f);

    private Color accentColor;
    private CelestIAState currentState = CelestIAState.Normal;
    private Vector2 statusBasePos;
    private Vector2 nameBasePos;
    private bool glitchBasesCaptured;
    private bool glitchActive;
    private float glitchBurstUntil;
    private float nextGlitchBurstAt;

    private void Awake()
    {
        SetState(CelestIAState.Normal);
        SetMessage(string.Empty);
    }

    private void Update()
    {
        UpdateCorruptedGlitch();

        if (!animateWaveform || waveformBars == null)
        {
            return;
        }

        float time = Time.unscaledTime * 4.5f;
        for (int i = 0; i < waveformBars.Length; i++)
        {
            Image bar = waveformBars[i];
            if (bar == null)
            {
                continue;
            }

            float wave = Mathf.Sin(time + i * 0.73f) * 0.5f + 0.5f;
            float secondary = Mathf.Sin(time * 0.63f - i * 0.39f) * 0.5f + 0.5f;
            float height = Mathf.Lerp(5f, 25f, wave * secondary);
            bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            Color color = accentColor;
            color.a = Mathf.Lerp(0.45f, 1f, wave);
            bar.color = color;
        }
    }

    public void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message ?? string.Empty;
        }
    }

    public void SetStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = "STATUS: " + (string.IsNullOrWhiteSpace(status) ? "NORMAL" : status.ToUpperInvariant());
        }
    }

    /// Round 13: glitch leve APENAS no status/nome quando a CelestIA esta corrompida.
    /// Bursts curtos (offset + cor instavel + alpha), com retorno exato a posicao base.
    /// Suprimido quando o card mostra o Dr. Elias (status "BIOSINAL: ..." nao comeca com STATUS).
    private void UpdateCorruptedGlitch()
    {
        bool eligible = glitchWhenCorrupted && currentState == CelestIAState.Corrupted &&
            statusText != null && statusText.text.StartsWith("STATUS");

        if (!eligible)
        {
            if (glitchActive)
            {
                RestoreGlitchBases();
            }
            return;
        }

        if (!glitchBasesCaptured)
        {
            statusBasePos = statusText.rectTransform.anchoredPosition;
            if (nameText != null)
            {
                nameBasePos = nameText.rectTransform.anchoredPosition;
            }
            glitchBasesCaptured = true;
        }

        float now = Time.unscaledTime;
        if (!glitchActive && now >= nextGlitchBurstAt)
        {
            glitchActive = true;
            // burst curto; duracao/intervalo pseudo-aleatorios e deterministas por frame-time
            glitchBurstUntil = now + Mathf.Lerp(0.05f, 0.14f, Mathf.PerlinNoise(now * 3.1f, 0.7f));
        }

        if (glitchActive)
        {
            if (now >= glitchBurstUntil)
            {
                RestoreGlitchBases();
                nextGlitchBurstAt = now + Mathf.Lerp(0.3f, 0.95f, Mathf.PerlinNoise(now * 1.7f, 4.2f));
                return;
            }

            // offsets pequenos trocando a cada ~1/30s (aspecto de scanline instavel)
            float step = Mathf.Floor(now * 30f);
            float ox = (Mathf.PerlinNoise(step * 0.31f, 1.3f) - 0.5f) * 2f * glitchJitter;
            float oy = (Mathf.PerlinNoise(step * 0.47f, 8.9f) - 0.5f) * 2f * (glitchJitter * 0.6f);
            statusText.rectTransform.anchoredPosition = statusBasePos + new Vector2(ox, oy);
            if (nameText != null)
            {
                nameText.rectTransform.anchoredPosition = nameBasePos + new Vector2(-ox * 0.5f, oy * 0.4f);
            }

            bool swap = Mathf.Repeat(step, 3f) < 1f;
            Color c = swap ? glitchAltColor : accentColor;
            c.a = 0.65f + 0.35f * Mathf.PerlinNoise(step * 0.9f, 2.2f);
            statusText.color = c;
        }
    }

    private void RestoreGlitchBases()
    {
        glitchActive = false;
        if (!glitchBasesCaptured)
        {
            return;
        }

        statusText.rectTransform.anchoredPosition = statusBasePos;
        statusText.color = accentColor;
        if (nameText != null)
        {
            nameText.rectTransform.anchoredPosition = nameBasePos;
        }
    }

    public void SetState(CelestIAState state)
    {
        currentState = state;
        if (state != CelestIAState.Corrupted)
        {
            RestoreGlitchBases();
        }

        switch (state)
        {
            case CelestIAState.Transition:
                SetStatus("OSCILANDO");
                SetAccent(transitionColor);
                break;
            case CelestIAState.Corrupted:
                SetStatus("CORROMPIDA");
                SetAccent(corruptedColor);
                break;
            default:
                SetStatus("NORMAL");
                SetAccent(normalColor);
                break;
        }
    }

    public void SetAccent(Color color)
    {
        accentColor = color;
        if (nameText != null)
        {
            nameText.color = color;
        }
        if (statusText != null)
        {
            statusText.color = color;
        }
        if (signalIcon != null)
        {
            signalIcon.color = color;
        }
        if (waveformBars == null)
        {
            return;
        }

        foreach (Image bar in waveformBars)
        {
            if (bar != null)
            {
                bar.color = color;
            }
        }
    }
}
