using UnityEngine;

public class CelestIAController : MonoBehaviour
{
    public UIManager ui;

    public void Begin()
    {
        var options = new VoicePlaybackOptions
        {
            group = VoiceGroup.Gameplay,
            priority = VoicePriority.Gameplay,
            interruptCurrent = false,
            clearQueueOfSameGroup = false,
            cancelOnStateExit = false,
            blockGameplay = false,
            fadeOutTime = 0.08f,
            ownerStateId = "FullRun_Start"
        };
        if (!VoiceLinePlayer.TryPlay("CEL_001", options))
        {
            ui.SetDialogue(
                "CELESTIA",
                "Doutor Elias, mantenha a rota. Detectando obstáculos à frente.");
        }
    }

    public void SetTutorialMessage(string message)
    {
        GameManager.Instance.dialogue.ShowPersistent("CELESTIA", StripSpeaker(message));
    }

    public void UpdateMessage(float distance)
    {
    }

    public void ShowTemporary(string message, float duration)
    {
        GameManager.Instance.dialogue.ShowTemporary("CELESTIA", StripSpeaker(message), duration);
    }

    // Overload com prioridade (Round 4): painel/recuperacao usam DialogueManager.PriorityLow
    // para nao cortar sequencias narrativas em andamento.
    public void ShowTemporary(string message, float duration, int priority)
    {
        GameManager.Instance.dialogue.ShowTemporary("CELESTIA", StripSpeaker(message), duration, priority);
    }

    public void ShowFinalSequence()
    {
        GameManager.Instance.BeginFinalCutscene();
    }

    private static string StripSpeaker(string message)
    {
        const string prefix = "CELESTIA:";
        return message != null && message.StartsWith(prefix)
            ? message.Substring(prefix.Length).Trim()
            : message;
    }
}
