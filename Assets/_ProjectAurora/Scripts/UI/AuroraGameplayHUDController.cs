using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Estados de visibilidade da HUD de gameplay (Round 11).
public enum GameplayHudVisibilityState
{
    IntroCinematic,
    Tutorial,
    Gameplay,
    Paused,
    GameOver,
    Final
}

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

    [Header("Character Video Portrait (Round 5)")]
    public HudCharacterVideoPortraitController characterPortrait;

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

    [Header("Visibilidade por estado (Round 11)")]
    [Tooltip("Nomes dos blocos de HUD de gameplay (filhos diretos) ocultos na intro/tutorial.")]
    public string[] gameplayBlockNames = { "Sector Identification", "Integrity System", "Distance System" };
    public float hudFadeDuration = 0.45f;
    [Tooltip("Card de comunicacao: atraso e fade ao ficar sem fala ativa.")]
    public float communicationCardFadeOutDelay = 0.35f;
    public float communicationCardFadeDuration = 0.25f;
    public string skipHintText = "ESC — Pular abertura";

    public GameplayHudVisibilityState VisibilityState { get; private set; } = GameplayHudVisibilityState.IntroCinematic;

    private readonly System.Collections.Generic.List<CanvasGroup> gameplayGroups =
        new System.Collections.Generic.List<CanvasGroup>();
    private CanvasGroup commGroup;
    private TMP_Text skipHintLabel;
    private Coroutine commFadeRoutine;
    private Coroutine groupsFadeRoutine;
    private bool commCardVisible;

    private void Awake()
    {
        SetSector("SETOR A: Laboratório Limpo");
        SetIntegrity(3, 3);
        SetDistance(0f, 2700f);

        BuildVisibilityGroups();
        BuildSkipHint();
        // frame 0 ja começa em modo cinematico: HUD de gameplay invisivel, card oculto
        ApplyVisibilityStateImmediate(GameplayHudVisibilityState.IntroCinematic);
    }

    // ===== Round 11: visibilidade por estado =====

    private void BuildVisibilityGroups()
    {
        gameplayGroups.Clear();
        foreach (string blockName in gameplayBlockNames)
        {
            Transform block = transform.Find(blockName);
            if (block == null)
            {
                continue;
            }

            CanvasGroup group = block.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = block.gameObject.AddComponent<CanvasGroup>();
            }
            gameplayGroups.Add(group);
        }

        if (commPanel != null)
        {
            commGroup = commPanel.GetComponent<CanvasGroup>();
            if (commGroup == null)
            {
                commGroup = commPanel.gameObject.AddComponent<CanvasGroup>();
            }
            // IMPORTANTE: o pool de VideoPlayers vive sob o painel — nunca desativar o
            // GameObject; o card some apenas por alpha.
            commGroup.alpha = 0f;
            commGroup.blocksRaycasts = false;
            commGroup.interactable = false;
            commCardVisible = false;
        }
    }

    private void BuildSkipHint()
    {
        if (skipHintLabel != null)
        {
            return;
        }

        var go = new GameObject("SkipIntro_Hint");
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(28f, 22f);
        rt.sizeDelta = new Vector2(420f, 30f);
        skipHintLabel = go.AddComponent<TextMeshProUGUI>();
        skipHintLabel.text = skipHintText;
        skipHintLabel.fontSize = 17f;
        skipHintLabel.alignment = TextAlignmentOptions.BottomLeft;
        skipHintLabel.color = new Color(0.75f, 0.95f, 1f, 0.62f);
        skipHintLabel.characterSpacing = 3f;
        skipHintLabel.raycastTarget = false;
        go.SetActive(false);
    }

    public void SetHudVisibilityState(GameplayHudVisibilityState state)
    {
        if (VisibilityState == state)
        {
            return;
        }

        VisibilityState = state;
        bool showGameplay = ShowsGameplayBlocks(state);
        if (groupsFadeRoutine != null)
        {
            StopCoroutine(groupsFadeRoutine);
        }
        groupsFadeRoutine = StartCoroutine(FadeGameplayGroups(showGameplay ? 1f : 0f));

        if (skipHintLabel != null)
        {
            skipHintLabel.gameObject.SetActive(state == GameplayHudVisibilityState.IntroCinematic);
        }
    }

    private void ApplyVisibilityStateImmediate(GameplayHudVisibilityState state)
    {
        VisibilityState = state;
        float alpha = ShowsGameplayBlocks(state) ? 1f : 0f;
        foreach (CanvasGroup group in gameplayGroups)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }
        if (skipHintLabel != null)
        {
            skipHintLabel.gameObject.SetActive(state == GameplayHudVisibilityState.IntroCinematic);
        }
    }

    private static bool ShowsGameplayBlocks(GameplayHudVisibilityState state)
    {
        // Tutorial fica diegetico (setor/integridade/distancia ocultos — nao ha dano no
        // tutorial); a HUD completa entra quando o runner e liberado (StartFullRun -> Gameplay).
        // GameOver/Final mantem o ultimo estado visivel: as telas existentes cobrem a HUD.
        switch (state)
        {
            case GameplayHudVisibilityState.Gameplay:
            case GameplayHudVisibilityState.Paused:
            case GameplayHudVisibilityState.GameOver:
                return true;
            // Round 15: a cutscene final oculta a HUD de gameplay (setor/integridade/
            // distancia) e mantem apenas o card de dialogo, como a intro.
            case GameplayHudVisibilityState.Final:
                return false;
            default:
                return false;
        }
    }

    private System.Collections.IEnumerator FadeGameplayGroups(float target)
    {
        float duration = Mathf.Max(0.05f, hudFadeDuration);
        float start = gameplayGroups.Count > 0 && gameplayGroups[0] != null ? gameplayGroups[0].alpha : target;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            foreach (CanvasGroup group in gameplayGroups)
            {
                if (group != null)
                {
                    group.alpha = alpha;
                }
            }
            yield return null;
        }
        groupsFadeRoutine = null;
    }

    // ===== Round 11: card de comunicacao so durante fala ativa =====

    private void ShowCommunicationCard()
    {
        if (commGroup == null)
        {
            return;
        }

        if (commFadeRoutine != null)
        {
            StopCoroutine(commFadeRoutine);
            commFadeRoutine = null;
        }
        commCardVisible = true;
        commFadeRoutine = StartCoroutine(FadeCommCard(1f, 0f));
    }

    /// Agenda o fade-out do card (cancelado se uma nova fala chegar dentro do delay).
    public void HideCommunicationCardSoon()
    {
        if (commGroup == null || !commCardVisible)
        {
            return;
        }

        if (commFadeRoutine != null)
        {
            StopCoroutine(commFadeRoutine);
        }
        commCardVisible = false;
        commFadeRoutine = StartCoroutine(FadeCommCard(0f, Mathf.Max(0f, communicationCardFadeOutDelay)));
    }

    private System.Collections.IEnumerator FadeCommCard(float target, float delay)
    {
        if (delay > 0f)
        {
            float wait = 0f;
            while (wait < delay)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        float duration = Mathf.Max(0.05f, communicationCardFadeDuration);
        float start = commGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            commGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        commGroup.alpha = target;
        commFadeRoutine = null;
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

        if (string.IsNullOrWhiteSpace(message))
        {
            // mensagem vazia = sem fala ativa -> card sai de cena
            commPanel.SetMessage(string.Empty);
            HideCommunicationCardSoon();
            return;
        }

        ShowCommunicationCard();
        characterPortrait?.SetSpeakerFromDialogue(speaker, message);

        string content = message ?? string.Empty;
        bool isElias = characterPortrait != null && !string.IsNullOrWhiteSpace(speaker) &&
            speaker.ToUpperInvariant().Contains("ELIAS");
        if (!string.Equals(speaker, "CELESTIA", System.StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(speaker) && !isElias)
        {
            // com retrato de personagem ativo, o nome do Dr. Elias ja aparece no card;
            // manter prefixo apenas para falantes sem retrato proprio
            content = speaker.ToUpperInvariant() + ": " + content;
        }
        commPanel.SetMessage(content);
    }

    public void SetVoiceLine(VoiceLineEntry line)
    {
        if (line == null)
        {
            return;
        }

        ShowCommunicationCard();
        if (line.speaker == VoiceSpeaker.DrElias)
        {
            // caminho por ID: o retrato segura o Dr. Elias ate EndVoiceLine (sem timer)
            characterPortrait?.ShowDrEliasForVoiceLine(line.drEliasMood);

            if (characterPortrait == null && commPanel != null)
            {
                if (commPanel.nameText != null)
                {
                    commPanel.nameText.text = line.SpeakerDisplayName;
                }
                commPanel.SetStatus(line.drEliasMood == DrEliasMood.Nervous
                    ? "BIOSINAL: ELEVADO"
                    : "BIOSINAL: ESTÁVEL");
            }
        }
        else if (line.speaker == VoiceSpeaker.CelestIA)
        {
            ApplyVoiceStateHint(line.celestIAStateHint);
            if (characterPortrait != null && characterPortrait.CurrentSpeaker == HudPortraitSpeaker.DrElias)
            {
                characterPortrait.ReturnToCurrentCelestIA();
            }
            else if (characterPortrait == null && commPanel != null && commPanel.nameText != null)
            {
                commPanel.nameText.text = line.SpeakerDisplayName;
            }
        }

        commPanel?.SetMessage(line.subtitleText);
    }

    public void EndVoiceLine(VoiceLineEntry line)
    {
        if (line != null && line.speaker == VoiceSpeaker.DrElias)
        {
            characterPortrait?.ReturnToCurrentCelestIA();
        }
        // fim natural da fala: agenda o fade do card (nova fala dentro do delay cancela)
        HideCommunicationCardSoon();
    }

    public void ClearVoiceLine(VoiceLineEntry line)
    {
        if (line != null && line.speaker == VoiceSpeaker.DrElias)
        {
            characterPortrait?.ReturnToCurrentCelestIA();
        }
        commPanel?.SetMessage(string.Empty);
        HideCommunicationCardSoon();
    }

    private void ApplyVoiceStateHint(CelestIAVisualState state)
    {
        switch (state)
        {
            case CelestIAVisualState.Transitioning:
                SetCelestIAState(CelestIAState.Transition);
                break;
            case CelestIAVisualState.Corrupted:
                SetCelestIAState(CelestIAState.Corrupted);
                break;
            case CelestIAVisualState.Normal:
                SetCelestIAState(CelestIAState.Normal);
                break;
        }
    }

    public void SetCelestIAState(CelestIAState state)
    {
        characterPortrait?.OnCelestIAStateChanged(state);
        // Round 11: enquanto o Dr. Elias fala, o estado da CelestIA nao pode sobrescrever
        // a identidade do card (nome/BIOSINAL/accent). O retrato reaplica o estado correto
        // da CelestIA quando ela volta (ApplyCelestiaIdentity).
        if (characterPortrait == null || characterPortrait.CurrentSpeaker != HudPortraitSpeaker.DrElias)
        {
            commPanel?.SetState(state);
        }
    }
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
