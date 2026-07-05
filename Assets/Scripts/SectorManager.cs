using UnityEngine;

public class SectorManager : MonoBehaviour
{
    public UIManager ui;
    public CelestIAHudController celestIAHud;
    public float sectorLength = 450f;

    [Header("Overlay de setor (Round 11)")]
    public SectorTitleOverlayController titleOverlay;

    private struct SectorInfo
    {
        public string hudLabel;     // rotulo compacto da HUD (topo)
        public string title;        // overlay: titulo grande
        public string subtitle;     // overlay: subtitulo
        public bool corrupted;      // overlay em vermelho

        public SectorInfo(string hudLabel, string title, string subtitle, bool corrupted)
        {
            this.hudLabel = hudLabel;
            this.title = title;
            this.subtitle = subtitle;
            this.corrupted = corrupted;
        }
    }

    private static readonly SectorInfo[] Sectors =
    {
        new SectorInfo("SETOR A: Laboratório Limpo", "SETOR A", "Laboratório Limpo", false),
        new SectorInfo("SETOR B: Corredor de Contenção", "SETOR B", "Setor de Contenção", false),
        new SectorInfo("SETOR C: Sala de Máquinas", "SETOR C", "Sala de Máquinas", false),
        new SectorInfo("SETOR D: Corredor Vermelho", "SETOR D", "Corredor Vermelho", true),
        new SectorInfo("SETOR E: Ponte Técnica", "SETOR E", "Ponte Técnica", true),
        new SectorInfo("NÚCLEO: Terminal Central", "NÚCLEO", "Terminal Central", true)
    };

    private int currentSector = -1;

    public int CurrentSector => currentSector;

    public void UpdateSector(float distance)
    {
        int index = Mathf.Clamp(Mathf.FloorToInt(distance / sectorLength), 0, Sectors.Length - 1);
        if (index == currentSector)
        {
            return;
        }

        currentSector = index;
        ui.SetSector(Sectors[index].hudLabel);

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

        // overlay so aparece na gameplay real (nunca na intro/cutscenes)
        GameManager game = GameManager.Instance;
        if (game != null && (game.State == GameState.Playing || game.State == GameState.Tutorial))
        {
            ShowSectorTitle(index);
        }
    }

    /// Exibe o titulo do setor atual (usado no inicio da corrida completa).
    public void ShowCurrentSectorTitle()
    {
        ShowSectorTitle(Mathf.Max(0, currentSector));
    }

    private void ShowSectorTitle(int index)
    {
        if (titleOverlay == null || index < 0 || index >= Sectors.Length)
        {
            return;
        }

        SectorInfo info = Sectors[index];
        titleOverlay.ShowSector(index, info.title, info.subtitle, info.corrupted);
    }
}
