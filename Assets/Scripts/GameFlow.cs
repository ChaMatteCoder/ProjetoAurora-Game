using System;

public enum GameState
{
    MainMenu,
    IntroCutscene,
    Tutorial,
    Playing,
    Paused,
    FinalCutscene,
    GameOver,
    Finished
}

[Serializable]
public class DialogueLine
{
    public string speaker;
    public string message;
    public float duration = 2f;
    public bool changeCelestIAState;
    public CelestIAState celestiaState = CelestIAState.Normal;

    public DialogueLine(string speaker, string message, float duration = 2f)
    {
        this.speaker = speaker;
        this.message = message;
        this.duration = duration;
    }
}
