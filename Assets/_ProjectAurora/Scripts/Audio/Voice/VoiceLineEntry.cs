using System;
using UnityEngine;

public enum VoiceSpeaker
{
    CelestIA,
    DrElias,
    System
}

public enum VoicePriority
{
    Low,
    Context,
    Gameplay,
    Tutorial,
    Narrative,
    Cutscene,
    Critical
}

[Serializable]
public class VoiceLineEntry
{
    public string id;
    public VoiceSpeaker speaker;
    public string sceneUse;
    [TextArea(2, 5)] public string subtitleText;
    [TextArea(1, 3)] public string originalDirection;
    public AudioClip clip;
    public VoicePriority priority = VoicePriority.Gameplay;
    [Min(0f)] public float minDisplayTime = 1.5f;
    [Min(0f)] public float postDelay = 0.15f;
    [Min(0f)] public float cooldownSeconds;
    public bool optional;
    public bool interruptCurrent;
    public bool canBeSkipped = true;
    public DrEliasMood drEliasMood = DrEliasMood.Normal;
    public CelestIAVisualState celestIAStateHint = CelestIAVisualState.Auto;

    public string SpeakerDisplayName => speaker == VoiceSpeaker.DrElias
        ? "DR. ELIAS"
        : speaker == VoiceSpeaker.CelestIA ? "CELESTIA" : "SISTEMA";
}
