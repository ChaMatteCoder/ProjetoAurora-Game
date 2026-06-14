using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public PlayerRunner player;
    public CelestIAController celestIA;
    public GameObject tutorialPanel;

    public bool IsComplete { get; private set; }

    private int stage;

    public void BeginTutorial()
    {
        stage = 0;
        player.SetInputEnabled(true);
        player.SetAutoRun(false);
        player.LaneChanged += OnLaneChanged;
        player.Jumped += OnJumped;
        celestIA.SetTutorialMessage("CELESTIA: Obstáculo à frente. Mova-se para a direita.");
    }

    private void OnLaneChanged(int direction)
    {
        if (stage == 0 && direction > 0)
        {
            stage = 1;
            celestIA.SetTutorialMessage("CELESTIA: Porta de contenção instável. Corrija sua rota para a esquerda.");
        }
        else if (stage == 1 && direction < 0)
        {
            stage = 2;
            celestIA.SetTutorialMessage("CELESTIA: Cabo energizado detectado. Salte.");
        }
    }

    private void OnJumped()
    {
        if (stage != 2)
        {
            return;
        }

        stage = 3;
        tutorialPanel.SetActive(true);
        celestIA.SetTutorialMessage("CELESTIA: Painel de acesso bloqueado. Aproxime-se e pressione E.");
    }

    public void NotifyInteractionComplete()
    {
        if (stage == 3)
        {
            stage = 4;
            StartCoroutine(FinishTutorial());
        }
    }

    private IEnumerator FinishTutorial()
    {
        celestIA.SetTutorialMessage("CELESTIA: Acesso liberado. Siga para o Terminal Central.");
        yield return new WaitForSeconds(2f);
        IsComplete = true;
        player.LaneChanged -= OnLaneChanged;
        player.Jumped -= OnJumped;
        GameManager.Instance.StartFullRun();
    }
}
