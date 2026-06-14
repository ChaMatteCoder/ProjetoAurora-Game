using UnityEngine;

public class SectorManager : MonoBehaviour
{
    public UIManager ui;
    public CelestIAHudController celestIAHud;
    public float sectorLength = 450f;

    private readonly string[] names =
    {
        "SETOR A: Laboratório Limpo",
        "Corredor de contenção",
        "Sala de máquinas",
        "Corredor vermelho",
        "Ponte técnica",
        "Terminal central"
    };

    private int currentSector = -1;

    public int CurrentSector => currentSector;

    public void UpdateSector(float distance)
    {
        int index = Mathf.Clamp(Mathf.FloorToInt(distance / sectorLength), 0, names.Length - 1);
        if (index == currentSector)
        {
            return;
        }

        currentSector = index;
        ui.SetSector(names[index]);

        CelestIAState state;
        if (index <= 2)
        {
            state = CelestIAState.Normal;
        }
        else if (index == 3)
        {
            state = CelestIAState.Transition;
        }
        else
        {
            state = CelestIAState.Corrupted;
        }

        celestIAHud?.SetCelestIAState(state);
        ui.SetCelestIAState(state);
    }
}
