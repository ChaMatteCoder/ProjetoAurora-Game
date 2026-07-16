using UnityEngine;

/// Recuperacao gradual da integridade do traje do Dr. Elias (Round 3).
/// Reutiliza PlayerHealth (nao cria sistema de vida paralelo): apos recoveryDelay
/// segundos sem dano, o proximo segmento carrega por recoveryDurationPerSegment
/// segundos (com feedback na HUD) e entao PlayerHealth.TryRestoreSegment() e chamado.
[RequireComponent(typeof(PlayerHealth))]
public class SuitIntegrityRecovery : MonoBehaviour
{
    public int maxIntegrity = 3;
    public float recoveryDelay = 60f;
    public float recoveryDurationPerSegment = 10f;
    public bool resetRecoveryOnDamage = true;
    public bool recoverOnlyDuringGameplay = true;

    [Header("SFX (opcional — sem clips no projeto ainda)")]
    public AudioSource recoveryStartSfx;
    public AudioSource recoveryCompleteSfx;

    private bool startSfxPlayed;
    private ParticleSystem recoveryVfx; // instancia viva do efeito de recuperacao

    private PlayerHealth health;
    private float lastDamageTime;
    private float chargeProgress;
    private int lastKnownLives;
    private bool hudShowingProgress;

    private AuroraGameplayHUDController Hud =>
        GameManager.Instance == null || GameManager.Instance.ui == null
            ? null
            : GameManager.Instance.ui.auroraHud;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.IntegrityChanged += OnIntegrityChanged;
            lastKnownLives = health.Lives;
        }

        lastDamageTime = Time.time;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.IntegrityChanged -= OnIntegrityChanged;
        }

        StopRecoveryVfx(); // nunca deixar particulas presas ao desligar/trocar de estado
    }

    /// Interrompe o efeito continuo de recuperacao e limpa as particulas ja emitidas.
    /// Chamado no cancelamento (dano), na conclusao e no OnDisable.
    private void StopRecoveryVfx()
    {
        if (recoveryVfx == null)
        {
            return;
        }

        recoveryVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        recoveryVfx = null; // o pool recolhe sozinho quando o sistema morre
    }

    private void OnIntegrityChanged(int current, int max)
    {
        if (current < lastKnownLives)
        {
            // dano: reinicia cooldown e cancela a carga em andamento
            lastDamageTime = Time.time;
            if (resetRecoveryOnDamage)
            {
                chargeProgress = 0f;
                startSfxPlayed = false;
                HideHudProgress();
                StopRecoveryVfx(); // CANCELAR: nao deixar particulas presas no personagem
            }
        }

        lastKnownLives = current;
    }

    private void Update()
    {
        if (health == null || health.Lives <= 0 || health.Lives >= Mathf.Min(maxIntegrity, health.MaxIntegrity))
        {
            if (chargeProgress > 0f)
            {
                chargeProgress = 0f;
            }
            HideHudProgress();
            return;
        }

        if (recoverOnlyDuringGameplay &&
            (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing))
        {
            // fora da gameplay livre: pausa a exibicao sem zerar o progresso
            HideHudProgress();
            return;
        }

        if (Time.time - lastDamageTime < recoveryDelay)
        {
            HideHudProgress();
            return;
        }

        int segmentIndex = health.Lives; // proximo segmento a restaurar (0-based)
        if (!startSfxPlayed)
        {
            startSfxPlayed = true;
            if (recoveryStartSfx != null && recoveryStartSfx.clip != null)
            {
                recoveryStartSfx.Play();
            }
            // START: energia ciano presa ao corpo (sobe junto com o Dr. Elias)
            recoveryVfx = ProjectAurora.VFX.AuroraVFXController.SuitRecoveryStart(transform);
        }
        chargeProgress += Time.deltaTime / Mathf.Max(1f, recoveryDurationPerSegment);

        if (chargeProgress >= 1f)
        {
            chargeProgress = 0f;
            startSfxPlayed = false;
            lastDamageTime = Time.time; // proximo segmento exige novo delay completo
            HideHudProgress();
            // COMPLETE: encerra o efeito continuo e solta um flash curto no lugar
            StopRecoveryVfx();
            ProjectAurora.VFX.AuroraVFXController.SuitRecoveryComplete(transform.position + Vector3.up * 1f);
            if (recoveryCompleteSfx != null && recoveryCompleteSfx.clip != null)
            {
                recoveryCompleteSfx.Play();
            }
            if (health.TryRestoreSegment())
            {
                Hud?.NotifyRecoveryComplete(segmentIndex);
                if (!VoiceLinePlayer.TryPlayQueued("CEL_046", new VoicePlaybackOptions
                {
                    group = VoiceGroup.Suit,
                    priority = VoicePriority.Context,
                    interruptCurrent = false,
                    clearQueueOfSameGroup = true,
                    cancelOnStateExit = true,
                    blockGameplay = false,
                    fadeOutTime = 0.08f,
                    ownerStateId = "Suit_Recovery"
                }))
                {
                    GameManager.Instance?.celestIA?.ShowTemporary(
                        "CELESTIA: Integridade do traje restaurada.", 2.5f, DialogueManager.PriorityLow);
                }
            }
        }
        else
        {
            Hud?.SetIntegrityRecoveryProgress(segmentIndex, chargeProgress);
            hudShowingProgress = true;
        }
    }

    private void HideHudProgress()
    {
        if (hudShowingProgress)
        {
            hudShowingProgress = false;
            Hud?.SetIntegrityRecoveryProgress(-1, 0f);
        }
    }
}
