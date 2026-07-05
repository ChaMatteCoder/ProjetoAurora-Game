using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public UIManager ui;
    public CelestIAHudController celestIAHud;

    // Prioridade de mensagens (Round 4): mensagens de baixa prioridade nao interrompem
    // sequencias protegidas (narrativa/intro). 0 = painel/recovery, 1 = dano, 2 = narrativa/intro/final.
    public const int PriorityLow = 0;
    public const int PriorityDamage = 1;
    public const int PriorityStory = 2;

    public bool IsPlaying { get; private set; }
    public bool AllowSkip { get; private set; }

    private readonly Queue<DialogueLine[]> queuedSequences = new Queue<DialogueLine[]>();
    private Coroutine activeRoutine;
    private bool skipRequested;
    private int currentPriority;

    public Coroutine Play(DialogueLine[] lines, bool allowSkip = false, Action onComplete = null,
        bool interrupt = true, int priority = PriorityStory)
    {
        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return null;
        }

        // Mensagens contextuais atrasadas perdem o sentido; não entram na fila de uma história protegida.
        if (IsPlaying && priority < currentPriority)
        {
            return activeRoutine;
        }

        if (interrupt)
        {
            StopCurrent();
        }

        currentPriority = priority;
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
            Play(lines, false, null, false, PriorityStory);
        }
    }

    public void ShowTemporary(string speaker, string message, float duration)
    {
        ShowTemporary(speaker, message, duration, PriorityDamage);
    }

    public void ShowTemporary(string speaker, string message, float duration, int priority)
    {
        Play(new[] { new DialogueLine(speaker, message, duration) }, false, null, true, priority);
    }

    public void ShowPersistent(string speaker, string message)
    {
        StopAll();
        ui.SetDialogue(speaker, message);
    }

    public void ClearQueue()
    {
        queuedSequences.Clear();
    }

    public void StopAll()
    {
        ClearQueue();
        StopCurrent();
        ui?.auroraHud?.HideCommunicationCardSoon();
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
        currentPriority = PriorityLow;
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
        else
        {
            // Round 11: sem proxima sequencia, a ultima mensagem nao fica fixa no card
            ui?.auroraHud?.HideCommunicationCardSoon();
        }
    }
}
