using UnityEngine;

public class PrototypeSliceEndTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
        {
            return;
        }

        PlayerRunner runner = other.GetComponent<PlayerRunner>();
        if (runner == null)
        {
            return;
        }

        triggered = true;
        runner.SetAutoRun(false);
        runner.SetInputEnabled(false);

        GameManager game = GameManager.Instance;
        if (game != null)
        {
            game.ui.SetInteractionPrompt(false, string.Empty);
            game.dialogue.ShowPersistent(
                "CELESTIA",
                "Setor A estabilizado. Primeira passagem concluida.");
        }
    }
}
