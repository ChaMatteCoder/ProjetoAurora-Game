using UnityEngine;

public class CelestIAController : MonoBehaviour
{
    public UIManager ui;

    public void Begin()
    {
        ui.SetDialogue(
            "CELESTIA",
            "Doutor Elias, mantenha a rota. Detectando obstáculos à frente.");
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
