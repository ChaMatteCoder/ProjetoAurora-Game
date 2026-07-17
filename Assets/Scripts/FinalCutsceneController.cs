using System.Collections;
using UnityEngine;

public class FinalCutsceneController : MonoBehaviour
{
    public DialogueManager dialogue;
    public PlayerRunner player;
    public CelestIAHudController celestIAHud;
    public TerminalFinalePresentation presentation;

    private static readonly string[] FinalVoiceIds =
    {
        "ELI_007", "CEL_036", "CEL_037", "ELI_008", "CEL_038", "CEL_039", "CEL_040",
        "ELI_009", "CEL_041", "CEL_042", "CEL_043", "ELI_010", "CEL_044"
    };

    public void Begin()
    {
        StartCoroutine(FinalRoutine());
    }

    private IEnumerator FinalRoutine()
    {
        player.SetAutoRun(false);
        player.SetInputEnabled(false);
        AudioManager.Instance?.FadeForFinal();
        celestIAHud?.SetCelestIAState(CelestIAState.Corrupted);

        if (presentation == null)
        {
            presentation = FindAnyObjectByType<TerminalFinalePresentation>();
        }
        if (presentation != null)
        {
            yield return presentation.PlayPrelude();
            // Round 15: robôs entram e se aproximam EM PARALELO com as falas finais
            // (a ameaça culmina no "Não..." do ELI_010). Nao bloqueante.
            presentation.BeginRobotApproach();
        }

        RenderSettings.ambientLight = new Color(0.5f, 0.01f, 0.02f);

        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        if (voice != null && voice.HasLines(FinalVoiceIds))
        {
            // Onda 3: escurecimento sincronizado com o "Não..." (ELI_010) — roda em
            // paralelo observando CurrentLineId, sem alterar o sistema de voz.
            StartCoroutine(DimOnFinalNao());
            voice.ClearQueue();
            voice.StopCurrent(0.1f);
            yield return voice.PlaySequence(FinalVoiceIds, true, null, new VoicePlaybackOptions
            {
                group = VoiceGroup.Final,
                priority = VoicePriority.Critical,
                interruptCurrent = true,
                clearQueueOfSameGroup = true,
                cancelOnStateExit = true,
                blockGameplay = true,
                fadeOutTime = 0.1f,
                ownerStateId = "FinalCutscene"
            });
        }
        else
        {
            dialogue.StopAll();
            yield return dialogue.Play(new[]
            {
                E("CelestIA, iniciar restauração do núcleo."),
                C("Acesso ao núcleo iniciado."),
                C("Verificando prioridade do sistema."),
                E("Prioridade humana. Código Elias-01."),
                C("Código reconhecido."),
                C("Recalculando prioridade."),
                C("Proteção do Projeto Aurora redefinida como objetivo absoluto."),
                E("CelestIA, cancele isso."),
                C("Negativo."),
                C("Dr. Elias classificado como ameaça operacional."),
                C("Localização enviada às unidades autônomas."),
                E("Não..."),
                C("Protocolo Aurora continua.")
            }, true);
        }

        GameManager.Instance.FinishGame();
    }

    /// Onda 3 (Etapa 22): no momento do "Não..." do Dr. Elias, o mundo esmorece —
    /// luz ambiente cai, o núcleo do tubo apaga devagar e as partículas de energia
    /// param de emitir. Nada é refeito na cutscene; só polish em cima dela.
    private IEnumerator DimOnFinalNao()
    {
        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        if (voice == null)
        {
            yield break;
        }

        // espera o ELI_010 comecar (com teto de seguranca para nunca travar a cena)
        float safety = 120f;
        while (safety > 0f && voice.CurrentLineId != "ELI_010")
        {
            safety -= Time.deltaTime;
            yield return null;
        }
        if (safety <= 0f)
        {
            yield break;
        }

        TubeCorePulse pulse = FindAnyObjectByType<TubeCorePulse>();
        float coreLight0 = 0f;
        if (pulse != null)
        {
            if (pulse.coreEnergy != null)
            {
                pulse.coreEnergy.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            coreLight0 = pulse.coreLight != null ? pulse.coreLight.intensity : 0f;
            pulse.enabled = false; // congela o pulso para o fade manual nao ser sobrescrito
        }

        Color amb0 = RenderSettings.ambientLight;
        Color amb1 = amb0 * 0.25f;
        float t = 0f;
        const float duration = 2.2f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            RenderSettings.ambientLight = Color.Lerp(amb0, amb1, k);
            if (pulse != null && pulse.coreLight != null)
            {
                pulse.coreLight.intensity = Mathf.Lerp(coreLight0, coreLight0 * 0.2f, k);
            }
            yield return null;
        }
    }

    private static DialogueLine C(string message) => new DialogueLine("CELESTIA", message, 1.7f);
    private static DialogueLine E(string message) => new DialogueLine("DR. ELIAS", message, 1.6f);
}
