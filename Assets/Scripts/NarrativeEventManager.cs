using UnityEngine;

public class NarrativeEventManager : MonoBehaviour
{
    public DialogueManager dialogue;
    public CelestIAHudController celestIAHud;
    public AudioSource sirenSource;

    private readonly float[] triggerDistances = { 100f, 450f, 900f, 1350f, 1800f, 2250f };
    private int nextEvent;

    public void ResetEvents()
    {
        nextEvent = 0;
    }

    public void UpdateDistance(float distance)
    {
        if (nextEvent < triggerDistances.Length && distance >= triggerDistances[nextEvent])
        {
            Trigger(nextEvent++);
        }
    }

    private void Trigger(int index)
    {
        switch (index)
        {
            case 0:
                Queue(CelestIAState.Normal, new[] { "CEL_020", "CEL_021" },
                    C("Setor A comprometido. Rotas secundárias indisponíveis."),
                    C("Mantenha-se no corredor principal."));
                break;
            case 1:
                Queue(CelestIAState.Normal, new[] { "CEL_022", "CEL_023" },
                    C("Portas de contenção instáveis à frente."),
                    C("Alguns sistemas de laser ainda podem ser desativados manualmente."));
                break;
            case 2:
                ActivateRobots();
                Queue(CelestIAState.Normal, new[] { "CEL_024", "CEL_025", "ELI_004", "CEL_026" },
                    C("Unidades autônomas detectadas na Sala de Máquinas."),
                    C("Elas não reconhecem mais sua credencial."),
                    E("Isso não deveria ser possível."),
                    C("Concordo. Isso não deveria ser possível."));
                break;
            case 3:
                SetRedLighting();
                if (sirenSource != null && sirenSource.clip != null)
                {
                    sirenSource.Play();
                }
                Queue(CelestIAState.Transition, new[] { "CEL_027", "CEL_028", "ELI_005", "CEL_029" },
                    C("Integridade dos protocolos em queda."),
                    C("Tentando isolar núcleo corrompido."),
                    E("CelestIA, mantenha o foco na contenção."),
                    C("Foco... redefinido."));
                break;
            case 4:
                Queue(CelestIAState.Corrupted,
                    new[] { "CEL_030", "CEL_031", "ELI_006", "CEL_032", "CEL_033" },
                    C("Estrutura instável."),
                    C("Probabilidade de sobrevivência reduzida."),
                    E("CelestIA?"),
                    C("Continue correndo, Dr. Elias."),
                    C("O Terminal precisa de você."));
                break;
            case 5:
                Queue(CelestIAState.Corrupted, new[] { "CEL_034", "CEL_035" },
                    C("Terminal Central alcançado."),
                    C("Aproxime-se do painel principal."));
                break;
        }
    }

    private void Queue(CelestIAState state, string[] voiceIds, params DialogueLine[] fallbackLines)
    {
        celestIAHud?.SetCelestIAState(state);
        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        if (voice != null && voice.HasLines(voiceIds))
        {
            voice.PlaySequence(voiceIds, false, null, new VoicePlaybackOptions
            {
                group = VoiceGroup.SectorNarrative,
                priority = VoicePriority.Narrative,
                interruptCurrent = true,
                clearQueueOfSameGroup = true,
                cancelOnStateExit = false,
                blockGameplay = false,
                fadeOutTime = 0.1f,
                ownerStateId = voiceIds[0]
            });
        }
        else
        {
            dialogue.Queue(fallbackLines);
        }
    }

    private static DialogueLine C(string message) => new DialogueLine("CELESTIA", message, 2.2f);
    private static DialogueLine E(string message) => new DialogueLine("DR. ELIAS", message, 2f);

    private static void ActivateRobots()
    {
        foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.name.Contains("Security Robot"))
            {
                item.gameObject.SetActive(true);
            }
        }
    }

    private static void SetRedLighting()
    {
        RenderSettings.ambientLight = new Color(0.45f, 0.035f, 0.045f);
    }
}
