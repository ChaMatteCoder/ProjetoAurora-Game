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

    private Color accentColor;

    private void Awake()
    {
        SetState(CelestIAState.Normal);
        SetMessage("Doutor Elias, mantenha a rota. Detectando obstáculos à frente.");
    }

    private void Update()
    {
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

    public void SetState(CelestIAState state)
    {
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
