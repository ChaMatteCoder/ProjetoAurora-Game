using UnityEngine;

public class CelestIAController : MonoBehaviour
{
    public UIManager ui;

    public void Begin()
    {
        // CEL_001 ("Doutor Elias, mantenha a rota...") saiu daqui: colada no fim do
        // tutorial (CEL_019 "Acesso liberado"), encavalava com CEL_020/021. Agora e o
        // NarrativeEventManager que a dispara por distancia (~150m), espacada das demais.
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
