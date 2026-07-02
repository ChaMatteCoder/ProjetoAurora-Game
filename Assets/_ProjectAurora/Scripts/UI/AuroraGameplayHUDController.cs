using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuroraGameplayHUDController : MonoBehaviour
{
    public TMP_Text sectorText;
    public TMP_Text objectiveText;
    public TMP_Text integrityLabel;
    public Image[] integritySegments;
    public Color integrityActiveColor = new Color(0.08f, 0.9f, 1f);
    public Color integrityEmptyColor = new Color(0.05f, 0.15f, 0.2f, 0.7f);

    [Header("Suit Recovery (Round 3)")]
    public TMP_Text recoveryLabel;
    public Color integrityRecoveringColor = new Color(0.08f, 0.9f, 1f);

    private int recoveringIndex = -1;
    private float recoveringProgress;
    private int flashIndex = -1;
    private float flashTimer;
    public TMP_Text distanceValueText;
    public Image distanceProgressFill;
    public RectTransform distanceMarker;
    public RectTransform distanceTrack;
    public CelestIACommPanel commPanel;
    public GameObject interactionPrompt;
    public TMP_Text interactionText;
    public GameObject sectorCard;
    public TMP_Text sectorCardText;
    public GameObject pausePanel;
    public GameObject failurePanel;
    public GameObject finalPanel;
    public GameObject introPanel;
    public TMP_Text introText;

    private static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-BR");

    private void Awake()
    {
        SetSector("SETOR A: Laboratório Limpo");
        SetIntegrity(3, 3);
        SetDistance(0f, 2700f);
    }

    public void SetSector(string value)
    {
        if (sectorText == null)
        {
            return;
        }

        string sector = string.IsNullOrWhiteSpace(value) ? "SETOR A: Laboratório Limpo" : value;
        if (sector.StartsWith("Setor ", System.StringComparison.OrdinalIgnoreCase))
        {
            sector = "SETOR " + sector.Substring(6);
        }
        sectorText.text = sector;
    }

    public void SetObjective(string value)
    {
        if (objectiveText != null)
        {
            objectiveText.text = value;
        }
    }

    public void SetIntegrity(int current, int maximum)
    {
        if (integritySegments == null)
        {
            return;
        }

        int visibleMaximum = Mathf.Min(maximum, integritySegments.Length);
        for (int i = 0; i < integritySegments.Length; i++)
        {
            Image segment = integritySegments[i];
            if (segment == null)
            {
                continue;
            }

            segment.gameObject.SetActive(i < visibleMaximum);
            segment.color = i < current ? integrityActiveColor : integrityEmptyColor;
        }
    }

    /// Mostra o segmento em recarga (progress 0..1). progress <= 0 ou index invalido limpa o estado.
    public void SetIntegrityRecoveryProgress(int segmentIndex, float progress)
    {
        if (integritySegments == null || segmentIndex < 0 || segmentIndex >= integritySegments.Length || progress <= 0f)
        {
            ClearRecovery();
            return;
        }

        recoveringIndex = segmentIndex;
        recoveringProgress = Mathf.Clamp01(progress);
        if (recoveryLabel != null && !recoveryLabel.gameObject.activeSelf)
        {
            recoveryLabel.gameObject.SetActive(true);
        }
    }

    /// Flash curto de conclusao no segmento restaurado.
    public void NotifyRecoveryComplete(int segmentIndex)
    {
        ClearRecovery();
        flashIndex = segmentIndex;
        flashTimer = 0.7f;
    }

    private void ClearRecovery()
    {
        if (recoveringIndex >= 0 && recoveringIndex < (integritySegments != null ? integritySegments.Length : 0))
        {
            Image segment = integritySegments[recoveringIndex];
            if (segment != null)
            {
                segment.color = integrityEmptyColor;
            }
        }

        recoveringIndex = -1;
        recoveringProgress = 0f;
        if (recoveryLabel != null && recoveryLabel.gameObject.activeSelf)
        {
            recoveryLabel.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (recoveringIndex >= 0 && integritySegments != null && recoveringIndex < integritySegments.Length)
        {
            Image segment = integritySegments[recoveringIndex];
            if (segment != null)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f);
                Color target = Color.Lerp(integrityEmptyColor, integrityRecoveringColor, recoveringProgress);
                target.a = Mathf.Lerp(integrityEmptyColor.a, 1f, recoveringProgress) * pulse;
                segment.color = target;
            }

            if (recoveryLabel != null)
            {
                Color labelColor = recoveryLabel.color;
                labelColor.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
                recoveryLabel.color = labelColor;
            }
        }

        if (flashTimer > 0f && integritySegments != null && flashIndex >= 0 && flashIndex < integritySegments.Length)
        {
            flashTimer -= Time.deltaTime;
            Image segment = integritySegments[flashIndex];
            if (segment != null)
            {
                float t = Mathf.Clamp01(flashTimer / 0.7f);
                segment.color = Color.Lerp(integrityActiveColor, Color.white, t);
            }

            if (flashTimer <= 0f)
            {
                flashIndex = -1;
            }
        }
    }

    public void SetDistance(float value, float total)
    {
        float distance = Mathf.Max(0f, value);
        float progress = total <= 0f ? 0f : Mathf.Clamp01(distance / total);
        if (distanceValueText != null)
        {
            distanceValueText.text = Mathf.FloorToInt(distance).ToString("N0", Portuguese) + " m";
        }
        if (distanceProgressFill != null)
        {
            distanceProgressFill.fillAmount = progress;
        }
        if (distanceMarker != null && distanceTrack != null)
        {
            float width = distanceTrack.rect.width;
            Vector2 anchored = distanceMarker.anchoredPosition;
            anchored.x = Mathf.Lerp(-width * 0.5f, width * 0.5f, progress);
            distanceMarker.anchoredPosition = anchored;
        }
    }

    public void SetDialogue(string speaker, string message)
    {
        if (commPanel == null)
        {
            return;
        }

        string content = message ?? string.Empty;
        if (!string.Equals(speaker, "CELESTIA", System.StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(speaker))
        {
            content = speaker.ToUpperInvariant() + ": " + content;
        }
        commPanel.SetMessage(content);
    }

    public void SetCelestIAState(CelestIAState state) => commPanel?.SetState(state);
    public void SetCelestIAColor(Color color) => commPanel?.SetAccent(color);

    public void SetInteractionPrompt(bool visible, string message)
    {
        interactionPrompt?.SetActive(visible);
        if (interactionText != null)
        {
            interactionText.text = message;
        }
    }

    public void SetPause(bool value) => pausePanel?.SetActive(value);
    public void SetFailure(bool value) => failurePanel?.SetActive(value);
    public void SetFinal(bool value) => finalPanel?.SetActive(value);

    public void ShowIntro(bool value, string message)
    {
        introPanel?.SetActive(value);
        SetIntroText(message);
    }

    public void SetIntroText(string message)
    {
        if (introText != null)
        {
            introText.text = message;
        }
    }
}
