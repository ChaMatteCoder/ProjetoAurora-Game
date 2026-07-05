using System;

public enum VoiceGroup
{
    Intro,
    Tutorial,
    Gameplay,
    Interaction,
    Suit,
    SectorNarrative,
    RobotChase,
    Final,
    GameOver
}

[Serializable]
public sealed class VoicePlaybackOptions
{
    public VoiceGroup group = VoiceGroup.Gameplay;
    public VoicePriority priority = VoicePriority.Gameplay;
    public bool interruptCurrent;
    public bool clearQueueOfSameGroup;
    public bool cancelOnStateExit;
    public bool blockGameplay;
    public float fadeOutTime = 0.1f;
    public string ownerStateId;

    public VoicePlaybackOptions Clone()
    {
        return new VoicePlaybackOptions
        {
            group = group,
            priority = priority,
            interruptCurrent = interruptCurrent,
            clearQueueOfSameGroup = clearQueueOfSameGroup,
            cancelOnStateExit = cancelOnStateExit,
            blockGameplay = blockGameplay,
            fadeOutTime = fadeOutTime,
            ownerStateId = ownerStateId
        };
    }
}
