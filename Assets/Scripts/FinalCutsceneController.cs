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
        }

        RenderSettings.ambientLight = new Color(0.5f, 0.01f, 0.02f);

        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        if (voice != null && voice.HasLines(FinalVoiceIds))
        {
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

    private static DialogueLine C(string message) => new DialogueLine("CELESTIA", message, 1.7f);
    private static DialogueLine E(string message) => new DialogueLine("DR. ELIAS", message, 1.6f);
}
