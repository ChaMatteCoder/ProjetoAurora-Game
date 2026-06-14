using System.Collections;
using UnityEngine;

public class FinalCutsceneController : MonoBehaviour
{
    public DialogueManager dialogue;
    public PlayerRunner player;
    public CelestIAHudController celestIAHud;

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
        RenderSettings.ambientLight = new Color(0.5f, 0.01f, 0.02f);

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

        GameManager.Instance.FinishGame();
    }

    private static DialogueLine C(string message) => new DialogueLine("CELESTIA", message, 1.7f);
    private static DialogueLine E(string message) => new DialogueLine("DR. ELIAS", message, 1.6f);
}
