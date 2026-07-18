using System.Collections;
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int startingLives = 3;
    public float invulnerabilityDuration = 2f;
    public float slowDuration = 1.5f;
    [Range(0.1f, 1f)] public float slowMultiplier = 0.5f;
    public Renderer[] renderers;

    public int Lives { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public int MaxIntegrity => startingLives;
    public event Action<int, int> IntegrityChanged;
    public event Action OnDeath;

    private PlayerRunner runner;
    private bool deathRaised;

    private void Awake()
    {
        runner = GetComponent<PlayerRunner>();
        Lives = startingLives;
    }

    private void Start()
    {
        GameManager.Instance?.ui?.SetLives(Lives);
        IntegrityChanged?.Invoke(Lives, startingLives);
    }

    /// Invulnerabilidade externa para cutscenes curtas (ex.: perseguicao dos robos).
    /// Nao interfere na janela de invulnerabilidade pos-dano.
    public void SetExternalInvulnerability(bool value)
    {
        IsInvulnerable = value;
    }

    /// Restaura 1 segmento de integridade (usado pelo SuitIntegrityRecovery).
    /// Nao ressuscita (Lives <= 0) e nao ultrapassa o maximo.
    public bool TryRestoreSegment()
    {
        if (Lives <= 0 || Lives >= startingLives)
        {
            return false;
        }

        Lives++;
        GameManager.Instance?.ui?.SetLives(Lives);
        IntegrityChanged?.Invoke(Lives, startingLives);
        return true;
    }

    public void TakeDamage()
    {
        if (GameManager.Instance == null || !GameManager.Instance.AllowsDamage ||
            IsInvulnerable || Lives <= 0)
        {
            return;
        }

        Lives--;
        GameManager.Instance.ui.SetLives(Lives);
        IntegrityChanged?.Invoke(Lives, startingLives);
        // SFX de impacto em TODO dano (pedido do cliente) + VFX de faiscas/shake.
        // Ambos no-op se os servicos nao estiverem na cena.
        AuroraSfx.PlayHit();
        ProjectAurora.VFX.AuroraVFXController.PlayerDamage(transform.position + Vector3.up * 1f);
        if (!VoiceLinePlayer.TryPlayQueued("CEL_045", new VoicePlaybackOptions
        {
            group = VoiceGroup.Suit,
            priority = VoicePriority.Context,
            interruptCurrent = false,
            clearQueueOfSameGroup = true,
            cancelOnStateExit = true,
            blockGameplay = false,
            fadeOutTime = 0.08f,
            ownerStateId = "Suit_Damage"
        }))
        {
            GameManager.Instance.celestIA.ShowTemporary("CELESTIA: Impacto detectado. Estabilizando traje.", 2f);
        }

        if (Lives <= 0)
        {
            if (!deathRaised)
            {
                deathRaised = true;
                // A fala de morte do Dr. Elias (ELI_011/012/013 sorteada) e tocada pelo
                // GameOverManager, ANTES do CEL_056 — enfileirar aqui nao funciona porque
                // o fluxo de game over faz ClearQueue+InterruptWith e engoliria a linha.
                OnDeath?.Invoke();
            }
            return;
        }

        StartCoroutine(DamageFeedback());
    }

    private IEnumerator DamageFeedback()
    {
        IsInvulnerable = true;
        runner.SetSpeedMultiplier(slowMultiplier);
        float elapsed = 0f;

        while (elapsed < invulnerabilityDuration)
        {
            SetVisible(false);
            yield return new WaitForSeconds(0.12f);
            SetVisible(true);
            yield return new WaitForSeconds(0.12f);
            elapsed += 0.24f;

            if (elapsed >= slowDuration)
            {
                runner.SetSpeedMultiplier(1f);
            }
        }

        runner.SetSpeedMultiplier(1f);
        SetVisible(true);
        IsInvulnerable = false;
    }

    private void SetVisible(bool value)
    {
        foreach (Renderer item in renderers)
        {
            if (item != null)
            {
                item.enabled = value;
            }
        }
    }
}
