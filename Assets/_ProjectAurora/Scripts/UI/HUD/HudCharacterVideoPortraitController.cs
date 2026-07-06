using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public enum HudPortraitSpeaker
{
    CelestIA,
    DrElias
}

public enum CelestIAVisualState
{
    Auto,
    Normal,
    Transitioning,
    Corrupted
}

public enum DrEliasMood
{
    Normal,
    Nervous
}

/// Retrato animado do comunicador da HUD (Round 5).
/// POOL de VideoPlayers PRE-PREPARADOS (um por clip), cada um com sua RenderTexture 16:9.
/// Trocar de personagem/estado e apenas um swap de textura no RawImage (crop central 1:1
/// via uvRect) + Play do player ja preparado -> troca INSTANTANEA, sem congelar.
/// CelestIA: 01 loop -> 02 uma unica vez -> blackout curto -> 03 loop permanente.
/// Dr. Elias: normal/nervoso enquanto fala, retorno automatico a CelestIA no estado atual.
public class HudCharacterVideoPortraitController : MonoBehaviour
{
    [Header("UI")]
    public RawImage portraitVideoRawImage;
    public Image fallbackPortraitImage;
    public CelestIACommPanel commPanel;

    [Header("Video (videoPlayer/portraitRenderTexture legados — o pool e criado em runtime)")]
    public VideoPlayer videoPlayer;
    public RenderTexture portraitRenderTexture;
    public VideoClip celestiaNormalClip;
    public VideoClip celestiaTransitionClip;
    public VideoClip celestiaCorruptedClip;
    public VideoClip drEliasNormalClip;
    public VideoClip drEliasNervousClip;

    [Header("Fallback Sprites")]
    public Sprite celestiaFallbackSprite;
    public Sprite drEliasNormalFallbackSprite;
    public Sprite drEliasNervousFallbackSprite;

    [Header("Crop 16:9 -> 1:1 central (configuravel se o conteudo sair do centro)")]
    public Rect centerCrop16x9To1x1 = new Rect(0.21875f, 0f, 0.5625f, 1f);

    [Header("RenderTexture do pool")]
    public int renderTextureWidth = 1024;
    public int renderTextureHeight = 576;

    [Header("Timing")]
    public float transitionBlackoutDuration = 0.35f;
    [Tooltip("Sem nova fala do Dr. Elias por este tempo, o retrato volta para a CelestIA.")]
    public float drEliasHoldSeconds = 3.5f;
    [Tooltip("Watchdog: retoma o video se ele pausar sozinho (perda de foco do app/editor).")]
    public float resumeCheckInterval = 0.5f;

    [Header("Identidade do falante")]
    public string celestiaDisplayName = "CELESTIA";
    public string drEliasDisplayName = "DR. ELIAS";
    public string drEliasNormalStatus = "BIOSINAL: ESTÁVEL";
    public string drEliasNervousStatus = "BIOSINAL: ELEVADO";
    public Color drEliasAccentColor = new Color(0.95f, 0.85f, 0.4f);

    // termos de tensao para inferir humor nervoso (fallback; nao ha metadado de humor)
    private static readonly string[] NervousHints =
    {
        "cedendo", "energia", "desligado", "chance", "painel", "isso ainda",
        "devia", "deveria", "não", "nao", "agora", "oscilação", "oscilacao", "?"
    };

    public HudPortraitSpeaker CurrentSpeaker { get; private set; } = HudPortraitSpeaker.CelestIA;
    public CelestIAVisualState CelestiaState { get; private set; } = CelestIAVisualState.Normal;
    public bool TransitionPlayed { get; private set; }

    private class Slot
    {
        public VideoClip clip;
        public VideoPlayer player;
        public RenderTexture texture;
        public bool loop;
    }

    private readonly Dictionary<VideoClip, Slot> slots = new Dictionary<VideoClip, Slot>();
    private Slot activeSlot;
    private Slot pendingSlot;              // aguardando prepare para exibir
    private System.Action pendingOnFinished;
    private System.Action oneShotOnFinished;
    private Coroutine eliasReturnRoutine;
    private Coroutine blackoutRoutine;
    private Sprite pendingFallback;
    private float resumeTimer;

    private void Awake()
    {
        // desliga o VideoPlayer legado autorado na cena (o pool o substitui)
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.enabled = false;
        }

        BuildSlot(celestiaNormalClip, true);
        BuildSlot(celestiaTransitionClip, false);
        BuildSlot(celestiaCorruptedClip, true);
        BuildSlot(drEliasNormalClip, true);
        BuildSlot(drEliasNervousClip, true);

        ApplyCrop();
    }

    private void Start()
    {
        // pre-prepara todos os clips ja no inicio: quando a gameplay pedir a troca,
        // o player ja tem o primeiro frame pronto -> exibicao instantanea
        foreach (Slot slot in slots.Values)
        {
            slot.player.Prepare();
        }

        // Round 11: a intro toca ELI_001 no MESMO frame (GameManager.Start) e a ordem de
        // Start() entre scripts e arbitraria. Se um falante ja foi pedido antes deste Start,
        // nao sobrescrever com a CelestIA — senao a primeira fala do Dr. Elias mostra o card errado.
        if (CurrentSpeaker == HudPortraitSpeaker.CelestIA && activeSlot == null && pendingSlot == null)
        {
            ShowCelestIANormal();
        }
    }

    private void OnDestroy()
    {
        foreach (Slot slot in slots.Values)
        {
            if (slot.player != null)
            {
                slot.player.errorReceived -= OnVideoError;
                slot.player.loopPointReached -= OnLoopPointReached;
                slot.player.prepareCompleted -= OnPrepareCompleted;
            }
            if (slot.texture != null)
            {
                slot.texture.Release();
            }
        }
    }

    private void BuildSlot(VideoClip clip, bool loop)
    {
        if (clip == null || slots.ContainsKey(clip))
        {
            return;
        }

        var rt = new RenderTexture(Mathf.Max(64, renderTextureWidth), Mathf.Max(64, renderTextureHeight), 0,
            RenderTextureFormat.ARGB32);
        rt.name = "RT_Portrait_" + clip.name;
        rt.Create();

        var go = new GameObject("VP_" + clip.name);
        go.transform.SetParent(transform, false);
        var vp = go.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.source = VideoSource.VideoClip;
        vp.clip = clip;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.isLooping = loop;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = true;
        vp.errorReceived += OnVideoError;
        vp.loopPointReached += OnLoopPointReached;
        vp.prepareCompleted += OnPrepareCompleted;

        slots[clip] = new Slot { clip = clip, player = vp, texture = rt, loop = loop };
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        // Assim que um clip do pool termina de preparar, o primeiro frame ja esta na sua RT.
        // Pausa quem nao e o slot ativo (segura o frame, pronto para resume instantaneo)
        // para nao desperdiçar decode com varios videos tocando ao mesmo tempo.
        if (activeSlot != null && source == activeSlot.player)
        {
            if (!source.isPlaying)
            {
                source.Play();
            }
        }
        else
        {
            source.Pause();
        }
    }

    private void Update()
    {
        // exibe o slot pendente assim que ele terminar de preparar (cobre o 1o clip no load)
        if (pendingSlot != null && pendingSlot.player.isPrepared)
        {
            Slot slot = pendingSlot;
            pendingSlot = null;
            DisplaySlotNow(slot, pendingOnFinished);
            pendingOnFinished = null;
        }

        // Watchdog: o VideoPlayer pausa sozinho quando o app/editor perde o foco.
        if (activeSlot == null)
        {
            return;
        }

        VideoPlayer active = activeSlot.player;
        if (active.isPlaying || !active.isPrepared)
        {
            resumeTimer = 0f;
            return;
        }

        // Round 13: slot nao-loop pausado por perda de foco tambem retoma — senao o clipe
        // de transicao (Celestia02) nunca chega ao fim e o retrato fica preso em
        // Transitioning (bloqueando inclusive a identidade do Dr. Elias no card).
        // So nao retoma se ja estiver no ultimo frame (fim natural: loopPointReached cuida).
        if (!activeSlot.loop && (long)active.frame >= (long)active.frameCount - 1)
        {
            return;
        }

        resumeTimer += Time.unscaledDeltaTime;
        if (resumeTimer >= Mathf.Max(0.2f, resumeCheckInterval))
        {
            resumeTimer = 0f;
            active.Play();
        }
    }

    private void OnLoopPointReached(VideoPlayer source)
    {
        if (activeSlot != null && source == activeSlot.player && !activeSlot.loop && oneShotOnFinished != null)
        {
            System.Action finished = oneShotOnFinished;
            oneShotOnFinished = null;
            finished.Invoke();
        }
    }

    public void SetCrop(Rect crop)
    {
        centerCrop16x9To1x1 = crop;
        ApplyCrop();
    }

    private void ApplyCrop()
    {
        if (portraitVideoRawImage != null)
        {
            portraitVideoRawImage.uvRect = centerCrop16x9To1x1;
            portraitVideoRawImage.raycastTarget = false;
        }
    }

    // ================= API publica =================

    public void ShowCelestIANormal()
    {
        if (TransitionPlayed)
        {
            ShowCelestIACorrupted();
            return;
        }
        if (CelestiaState == CelestIAVisualState.Transitioning)
        {
            return;
        }

        CurrentSpeaker = HudPortraitSpeaker.CelestIA;
        CelestiaState = CelestIAVisualState.Normal;
        ApplyCelestiaIdentity();
        SwitchTo(celestiaNormalClip, celestiaFallbackSprite, null);
    }

    public void PlayCelestIATransitionOnce()
    {
        if (TransitionPlayed)
        {
            ShowCelestIACorrupted();
            return;
        }
        if (CelestiaState == CelestIAVisualState.Transitioning)
        {
            return; // ja esta tocando; nunca reiniciar
        }

        CancelEliasReturn();
        CurrentSpeaker = HudPortraitSpeaker.CelestIA;
        CelestiaState = CelestIAVisualState.Transitioning;
        ApplyCelestiaIdentity();
        SwitchTo(celestiaTransitionClip, celestiaFallbackSprite, OnTransitionClipFinished);
    }

    public void ShowCelestIACorrupted()
    {
        CurrentSpeaker = HudPortraitSpeaker.CelestIA;
        CelestiaState = CelestIAVisualState.Corrupted;
        TransitionPlayed = true;
        ApplyCelestiaIdentity();
        SwitchTo(celestiaCorruptedClip, celestiaFallbackSprite, null);
    }

    public void ShowDrEliasNormal() => ShowDrElias(DrEliasMood.Normal, false);
    public void ShowDrEliasNervous() => ShowDrElias(DrEliasMood.Nervous, false);

    /// Caminho por ID (Round 11): a linha dublada informa quando termina (EndVoiceLine),
    /// entao o retrato segura o Dr. Elias ate la — sem timer que corta clipe longo no meio.
    public void ShowDrEliasForVoiceLine(DrEliasMood mood) => ShowDrElias(mood, true);

    public void ReturnToCurrentCelestIA()
    {
        CancelEliasReturn();
        if (CelestiaState == CelestIAVisualState.Transitioning)
        {
            return;
        }

        if (TransitionPlayed)
        {
            ShowCelestIACorrupted();
        }
        else
        {
            ShowCelestIANormal();
        }
    }

    public void SetSpeakerFromDialogue(string speaker, string message)
    {
        bool isElias = !string.IsNullOrWhiteSpace(speaker) &&
            speaker.ToUpperInvariant().Contains("ELIAS");

        if (isElias)
        {
            ShowDrElias(InferMood(message), false);
        }
        else if (CurrentSpeaker == HudPortraitSpeaker.DrElias)
        {
            ReturnToCurrentCelestIA();
        }
    }

    public void OnCelestIAStateChanged(CelestIAState state)
    {
        switch (state)
        {
            case CelestIAState.Transition:
            case CelestIAState.Corrupted:
                PlayCelestIATransitionOnce();
                break;
            default:
                if (!TransitionPlayed && CurrentSpeaker == HudPortraitSpeaker.CelestIA &&
                    CelestiaState != CelestIAVisualState.Transitioning)
                {
                    ShowCelestIANormal();
                }
                break;
        }
    }

    // ================= internals =================

    private void ShowDrElias(DrEliasMood mood, bool holdUntilCleared)
    {
        if (CelestiaState == CelestIAVisualState.Transitioning)
        {
            return; // prioridade: transformacao nunca e interrompida
        }

        CurrentSpeaker = HudPortraitSpeaker.DrElias;
        ApplyEliasIdentity(mood);

        VideoClip clip = mood == DrEliasMood.Nervous ? drEliasNervousClip : drEliasNormalClip;
        Sprite fb = mood == DrEliasMood.Nervous ? drEliasNervousFallbackSprite : drEliasNormalFallbackSprite;
        SwitchTo(clip, fb, null);

        CancelEliasReturn();
        if (!holdUntilCleared)
        {
            // caminho legado por texto: sem sinal de fim de fala, usa o timer de retorno
            eliasReturnRoutine = StartCoroutine(EliasReturnRoutine());
        }
    }

    private System.Collections.IEnumerator EliasReturnRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(1f, drEliasHoldSeconds));
        eliasReturnRoutine = null;
        if (CurrentSpeaker == HudPortraitSpeaker.DrElias)
        {
            ReturnToCurrentCelestIA();
        }
    }

    private void CancelEliasReturn()
    {
        if (eliasReturnRoutine != null)
        {
            StopCoroutine(eliasReturnRoutine);
            eliasReturnRoutine = null;
        }
    }

    private static DrEliasMood InferMood(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return DrEliasMood.Normal;
        }

        string lower = message.ToLowerInvariant();
        foreach (string hint in NervousHints)
        {
            if (lower.Contains(hint))
            {
                return DrEliasMood.Nervous;
            }
        }

        return DrEliasMood.Normal;
    }

    private void ApplyCelestiaIdentity()
    {
        if (commPanel == null)
        {
            return;
        }

        if (commPanel.nameText != null)
        {
            commPanel.nameText.text = celestiaDisplayName;
        }

        switch (CelestiaState)
        {
            case CelestIAVisualState.Transitioning:
                commPanel.SetState(CelestIAState.Transition);
                break;
            case CelestIAVisualState.Corrupted:
                commPanel.SetState(CelestIAState.Corrupted);
                break;
            default:
                commPanel.SetState(CelestIAState.Normal);
                break;
        }
    }

    private void ApplyEliasIdentity(DrEliasMood mood)
    {
        if (commPanel == null)
        {
            return;
        }

        if (commPanel.nameText != null)
        {
            commPanel.nameText.text = drEliasDisplayName;
        }

        if (commPanel.statusText != null)
        {
            commPanel.statusText.text = mood == DrEliasMood.Nervous ? drEliasNervousStatus : drEliasNormalStatus;
        }

        commPanel.SetAccent(drEliasAccentColor);
    }

    /// Troca instantanea para o clip (ja pre-preparado). Se por algum motivo ainda nao
    /// estiver pronto, marca como pendente e exibe assim que preparar (fallback enquanto isso).
    private void SwitchTo(VideoClip clip, Sprite fallback, System.Action onFinished)
    {
        // cancela blackout em andamento se uma nova troca chegar
        if (blackoutRoutine != null)
        {
            StopCoroutine(blackoutRoutine);
            blackoutRoutine = null;
        }

        Slot slot;
        if (clip == null || !slots.TryGetValue(clip, out slot))
        {
            pendingSlot = null;
            ShowFallback(fallback);
            onFinished?.Invoke();
            return;
        }

        if (slot.player.isPrepared)
        {
            pendingSlot = null;
            DisplaySlotNow(slot, onFinished);
        }
        else
        {
            // ainda preparando (raro, so no primeiro load): agenda e mostra fallback
            pendingSlot = slot;
            pendingOnFinished = onFinished;
            pendingFallback = fallback;
            ShowFallback(fallback);
        }
    }

    private void DisplaySlotNow(Slot slot, System.Action onFinished)
    {
        // pausa todos os outros slots (mantem o ultimo frame nas RTs; so o ativo decoda)
        foreach (Slot other in slots.Values)
        {
            if (other != slot && other.player.isPlaying)
            {
                other.player.Pause();
            }
        }

        activeSlot = slot;
        oneShotOnFinished = slot.loop ? null : onFinished;

        // aponta o RawImage para a RT deste slot: swap instantaneo, sem congelar
        if (portraitVideoRawImage != null)
        {
            portraitVideoRawImage.texture = slot.texture;
            portraitVideoRawImage.enabled = true;
            portraitVideoRawImage.uvRect = centerCrop16x9To1x1;
        }
        if (fallbackPortraitImage != null)
        {
            fallbackPortraitImage.enabled = false;
        }

        if (!slot.loop)
        {
            // clipe unico (Celestia02): reinicia do frame 0
            slot.player.frame = 0;
        }
        slot.player.Play();
    }

    private void OnTransitionClipFinished()
    {
        blackoutRoutine = StartCoroutine(TransitionBlackoutRoutine());
    }

    private System.Collections.IEnumerator TransitionBlackoutRoutine()
    {
        if (portraitVideoRawImage != null)
        {
            portraitVideoRawImage.enabled = false;
        }
        if (fallbackPortraitImage != null)
        {
            fallbackPortraitImage.enabled = false;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, transitionBlackoutDuration));
        blackoutRoutine = null;
        ShowCelestIACorrupted();
    }

    private void ShowFallback(Sprite sprite)
    {
        if (portraitVideoRawImage != null)
        {
            portraitVideoRawImage.enabled = false;
        }
        if (fallbackPortraitImage != null)
        {
            if (sprite != null)
            {
                fallbackPortraitImage.sprite = sprite;
            }
            fallbackPortraitImage.enabled = true;
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("[HudPortrait] VideoPlayer error: " + message);
        if (activeSlot != null && source == activeSlot.player)
        {
            ShowFallback(pendingFallback != null ? pendingFallback : celestiaFallbackSprite);
        }
    }
}
