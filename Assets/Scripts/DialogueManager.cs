using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public UIManager ui;
    public CelestIAHudController celestIAHud;

    public bool IsPlaying { get; private set; }
    public bool AllowSkip { get; private set; }

    private readonly Queue<DialogueLine[]> queuedSequences = new Queue<DialogueLine[]>();
    private Coroutine activeRoutine;
    private bool skipRequested;

    public Coroutine Play(DialogueLine[] lines, bool allowSkip = false, Action onComplete = null, bool interrupt = true)
    {
        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return null;
        }

        if (interrupt)
        {
            StopCurrent();
        }

        activeRoutine = StartCoroutine(PlayRoutine(lines, allowSkip, onComplete));
        return activeRoutine;
    }

    public void Queue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        if (IsPlaying)
        {
            queuedSequences.Enqueue(lines);
        }
        else
        {
            Play(lines, false, null, false);
        }
    }

    public void ShowTemporary(string speaker, string message, float duration)
    {
        Play(new[] { new DialogueLine(speaker, message, duration) }, false);
    }

    public void ShowPersistent(string speaker, string message)
    {
        StopCurrent();
        ui.SetDialogue(speaker, message);
    }

    public void StopCurrent()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = null;
        IsPlaying = false;
        AllowSkip = false;
        skipRequested = false;
    }

    private void Update()
    {
        if (!IsPlaying || !AllowSkip || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            skipRequested = true;
        }
    }

    private IEnumerator PlayRoutine(DialogueLine[] lines, bool allowSkip, Action onComplete)
    {
        IsPlaying = true;
        AllowSkip = allowSkip;

        foreach (DialogueLine line in lines)
        {
            if (line.changeCelestIAState)
            {
                celestIAHud?.SetCelestIAState(line.celestiaState);
                ui.SetCelestIAState(line.celestiaState);
            }

            ui.SetDialogue(line.speaker, line.message);
            skipRequested = false;
            float elapsed = 0f;

            while (elapsed < Mathf.Max(0.1f, line.duration) && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        activeRoutine = null;
        IsPlaying = false;
        AllowSkip = false;
        onComplete?.Invoke();

        if (queuedSequences.Count > 0)
        {
            Play(queuedSequences.Dequeue(), false, null, false);
        }
    }
}
