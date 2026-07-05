using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
public class VoiceLinePlayer : MonoBehaviour
{
    private sealed class PlaybackRequest
    {
        public long playbackId;
        public string[] ids;
        public bool allowSkip;
        public Action onComplete;
        public VoicePlaybackOptions options;
        public bool completed;
        public bool cancelled;
    }

    public static VoiceLinePlayer Instance { get; private set; }

    [SerializeField] private VoiceLineDatabase database;
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private UIManager ui;

    [Header("Cooldown padrão por prioridade")]
    [Min(0f)] public float lowCooldown = 2f;
    [Min(0f)] public float contextCooldown = 1.5f;
    [Min(0f)] public float gameplayCooldown = 0.5f;

    private readonly List<PlaybackRequest> queue = new List<PlaybackRequest>();
    private readonly Dictionary<string, float> lastPlayedAt =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> missingClipWarnings =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private PlaybackRequest currentRequest;
    private Coroutine playbackRoutine;
    private Coroutine fadeRoutine;
    private bool skipRequested;
    private VoiceLineEntry currentLine;
    private long nextPlaybackId = 1;
    private long activePlaybackId;
    private float fadeRestoreVolume = 1f;

    public bool IsPlaying => currentRequest != null;
    public string CurrentLineId => currentLine == null ? string.Empty : currentLine.id;
    public VoiceSpeaker CurrentSpeaker => currentLine == null ? VoiceSpeaker.System : currentLine.speaker;
    public VoiceGroup CurrentGroup => currentRequest == null
        ? VoiceGroup.Gameplay
        : currentRequest.options.group;
    public VoicePriority CurrentPriority => currentRequest == null
        ? VoicePriority.Low
        : currentRequest.options.priority;
    public string CurrentOwnerStateId => currentRequest == null
        ? string.Empty
        : currentRequest.options.ownerStateId ?? string.Empty;
    public VoiceLineDatabase Database => database;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("VoiceLinePlayer");
        DontDestroyOnLoad(root);
        VoiceLinePlayer player = root.AddComponent<VoiceLinePlayer>();
        VoiceLineDatabase[] loadedDatabases = Resources.FindObjectsOfTypeAll<VoiceLineDatabase>();
        if (loadedDatabases.Length > 0)
        {
            player.database = loadedDatabases[0];
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSource();
        RefreshSceneBindings();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsPlaying || currentRequest == null || !currentRequest.allowSkip || currentLine == null ||
            !currentLine.canBeSkipped || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SkipCurrent();
        }
    }

    public void SetDatabase(VoiceLineDatabase value)
    {
        database = value;
    }

    public bool HasLine(string id)
    {
        EnsureDatabase();
        return database != null && database.Contains(id);
    }

    public bool HasLines(params string[] ids)
    {
        EnsureDatabase();
        if (database == null || ids == null || ids.Length == 0)
        {
            return false;
        }

        foreach (string id in ids)
        {
            if (!database.Contains(id))
            {
                return false;
            }
        }

        return true;
    }

    public bool Play(string id)
    {
        return Enqueue(new[] { id }, false, false, null, null) != null;
    }

    public bool Play(string id, VoicePlaybackOptions options)
    {
        return Enqueue(new[] { id }, false, false, null, options) != null;
    }

    public bool PlayQueued(string id)
    {
        return Enqueue(new[] { id }, true, false, null, null) != null;
    }

    public bool PlayQueued(string id, VoicePlaybackOptions options)
    {
        return Enqueue(new[] { id }, true, false, null, options) != null;
    }

    public Coroutine PlaySequence(params string[] ids)
    {
        return PlaySequence(ids, false, null, null);
    }

    public Coroutine PlaySequence(string[] ids, bool allowSkip, Action onComplete = null)
    {
        return PlaySequence(ids, allowSkip, onComplete, null);
    }

    public Coroutine PlaySequence(string[] ids, bool allowSkip, Action onComplete,
        VoicePlaybackOptions options)
    {
        return StartCoroutine(SequenceWaitRoutine(ids, allowSkip, onComplete, options));
    }

    public bool InterruptWith(string id, VoicePlaybackOptions options)
    {
        VoicePlaybackOptions resolved = options == null ? new VoicePlaybackOptions() : options.Clone();
        resolved.interruptCurrent = true;
        return Enqueue(new[] { id }, false, false, null, resolved) != null;
    }

    public void StopCurrent(float fadeOutTime = 0f)
    {
        CancelCurrent(fadeOutTime, true);
    }

    public void StopGroup(VoiceGroup group, float fadeOutTime = 0.1f)
    {
        ClearQueueByGroup(group);
        if (currentRequest != null && currentRequest.options.group == group)
        {
            CancelCurrent(fadeOutTime, true);
        }
    }

    public void ClearQueue()
    {
        foreach (PlaybackRequest request in queue)
        {
            CancelQueuedRequest(request);
        }
        queue.Clear();
    }

    public void ClearQueueByGroup(VoiceGroup group)
    {
        for (int i = queue.Count - 1; i >= 0; i--)
        {
            if (queue[i].options.group != group)
            {
                continue;
            }

            CancelQueuedRequest(queue[i]);
            queue.RemoveAt(i);
        }
    }

    public bool IsPlayingGroup(VoiceGroup group)
    {
        return currentRequest != null && currentRequest.options.group == group;
    }

    public void SkipCurrent()
    {
        if (currentRequest == null || !currentRequest.allowSkip || currentLine == null ||
            !currentLine.canBeSkipped)
        {
            return;
        }

        skipRequested = true;
        voiceAudioSource?.Stop();
    }

    public void StopAll(float fadeOutTime = 0f)
    {
        ClearQueue();
        if (currentRequest != null)
        {
            CancelCurrent(fadeOutTime, false);
        }
        else
        {
            StopFadeAndAudio();
        }
    }

    public static bool TryPlay(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && Instance != null && Instance.Play(id);
    }

    public static bool TryPlay(string id, VoicePlaybackOptions options)
    {
        return !string.IsNullOrWhiteSpace(id) && Instance != null && Instance.Play(id, options);
    }

    public static bool TryPlayQueued(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && Instance != null && Instance.PlayQueued(id);
    }

    public static bool TryPlayQueued(string id, VoicePlaybackOptions options)
    {
        return !string.IsNullOrWhiteSpace(id) && Instance != null && Instance.PlayQueued(id, options);
    }

    public static bool TryPlayContextForMessage(string message)
    {
        string id = ResolveContextId(message);
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        return TryPlayQueued(id, new VoicePlaybackOptions
        {
            group = VoiceGroup.Interaction,
            priority = VoicePriority.Context,
            interruptCurrent = false,
            clearQueueOfSameGroup = true,
            cancelOnStateExit = true,
            fadeOutTime = 0.08f,
            ownerStateId = "ContextMessage"
        });
    }

    public static string ResolveContextId(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        string normalized = message.ToUpperInvariant();
        if (normalized.Contains("INTEGRIDADE DO TRAJE")) return "CEL_046";
        if (normalized.Contains("ACESSO AUTORIZADO")) return "CEL_047";
        if (normalized.Contains("ACESSO LIBERADO")) return "CEL_048";
        if (normalized.Contains("EMISSORES") || normalized.Contains("LASER")) return "CEL_049";
        if (normalized.Contains("CAMINHO PARCIALMENTE")) return "CEL_050";
        if (normalized.Contains("BARREIRA DESLOCADA")) return "CEL_051";
        if (normalized.Contains("ROTA RECALCULADA")) return "CEL_052";
        if (normalized.Contains("SETOR A ESTABILIZADO")) return "CEL_053";
        return null;
    }

    private IEnumerator SequenceWaitRoutine(string[] ids, bool allowSkip, Action onComplete,
        VoicePlaybackOptions options)
    {
        bool forceQueue = options == null || !options.interruptCurrent;
        PlaybackRequest request = Enqueue(ids, forceQueue, allowSkip, onComplete, options);
        if (request == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        while (!request.completed)
        {
            yield return null;
        }
    }

    private PlaybackRequest Enqueue(string[] ids, bool forceQueue, bool allowSkip, Action onComplete,
        VoicePlaybackOptions options)
    {
        EnsureDatabase();
        if (database == null || ids == null || ids.Length == 0)
        {
            return null;
        }

        var validIds = new List<string>();
        VoicePriority highest = VoicePriority.Low;
        VoiceLineEntry firstEntry = null;
        foreach (string id in ids)
        {
            VoiceLineEntry entry = database.GetById(id);
            if (entry == null)
            {
                Debug.LogWarning($"[Voice] ID não encontrado no banco: {id}", this);
                continue;
            }

            if (firstEntry == null)
            {
                firstEntry = entry;
            }
            validIds.Add(entry.id);
            if (entry.priority > highest)
            {
                highest = entry.priority;
            }
        }

        if (validIds.Count == 0 || firstEntry == null)
        {
            return null;
        }

        VoicePlaybackOptions resolved = options == null
            ? CreateDefaultOptions(firstEntry, highest)
            : options.Clone();

        if (resolved.clearQueueOfSameGroup)
        {
            ClearQueueByGroup(resolved.group);
        }

        var request = new PlaybackRequest
        {
            playbackId = nextPlaybackId++,
            ids = validIds.ToArray(),
            allowSkip = allowSkip,
            onComplete = onComplete,
            options = resolved
        };

        if (currentRequest == null && fadeRoutine == null)
        {
            StartRequest(request);
            return request;
        }

        if (!forceQueue && currentRequest != null && ShouldInterrupt(currentRequest, request))
        {
            queue.Insert(0, request);
            CancelCurrent(resolved.fadeOutTime, true);
        }
        else
        {
            queue.Add(request);
        }

        return request;
    }

    private void StartRequest(PlaybackRequest request)
    {
        if (request == null || request.cancelled)
        {
            StartNextRequest();
            return;
        }

        currentRequest = request;
        activePlaybackId = request.playbackId;
        playbackRoutine = StartCoroutine(PlaybackRoutine(request));
    }

    private IEnumerator PlaybackRoutine(PlaybackRequest request)
    {
        foreach (string id in request.ids)
        {
            if (!IsCurrent(request))
            {
                yield break;
            }

            VoiceLineEntry entry = database.GetById(id);
            if (entry == null || IsOnCooldown(entry))
            {
                continue;
            }

            currentLine = entry;
            skipRequested = false;
            lastPlayedAt[entry.id] = Time.unscaledTime;
            ShowOnHud(entry);

            float duration;
            double dspEndTime = 0d;
            bool usesAudioClock = entry.clip != null;
            if (entry.clip != null)
            {
                EnsureAudioSource();
                voiceAudioSource.Stop();
                voiceAudioSource.clip = entry.clip;
                voiceAudioSource.Play();
                duration = Mathf.Max(entry.clip.length + entry.postDelay, entry.minDisplayTime);
                // O relógio DSP acompanha a reprodução real. Um frame longo durante o load
                // não pode consumir artificialmente a duração e cortar a linha seguinte.
                dspEndTime = AudioSettings.dspTime + duration;
            }
            else
            {
                duration = Mathf.Max(
                    Mathf.Clamp((entry.subtitleText ?? string.Empty).Length * 0.045f, 1.5f, 6f) + entry.postDelay,
                    entry.minDisplayTime);
                if (missingClipWarnings.Add(entry.id) && !entry.optional)
                {
                    Debug.LogWarning($"[Voice] Áudio ausente para {entry.id}; usando duração por caracteres.", this);
                }
            }

            float elapsed = 0f;
            while ((usesAudioClock ? AudioSettings.dspTime < dspEndTime : elapsed < duration) &&
                !skipRequested && IsCurrent(request))
            {
                if (!usesAudioClock)
                {
                    elapsed += Time.unscaledDeltaTime;
                }
                yield return null;
            }

            if (!IsCurrent(request))
            {
                yield break;
            }

            voiceAudioSource?.Stop();
        }

        FinishRequest(request, true);
    }

    private void FinishRequest(PlaybackRequest request, bool invokeCallback)
    {
        if (!IsCurrent(request))
        {
            return;
        }

        VoiceLineEntry finishedLine = currentLine;
        if (finishedLine != null)
        {
            // Round 11: fim natural de QUALQUER fala notifica a HUD (retorno do retrato do
            // Dr. Elias + fade do card quando nao houver proxima fala dentro do delay).
            RefreshSceneBindings();
            ui?.auroraHud?.EndVoiceLine(finishedLine);
        }

        currentLine = null;
        currentRequest = null;
        playbackRoutine = null;
        activePlaybackId = 0;
        request.completed = true;
        if (invokeCallback && !request.cancelled)
        {
            request.onComplete?.Invoke();
        }
        StartNextRequest();
    }

    private void CancelCurrent(float fadeOutTime, bool startNext)
    {
        if (currentRequest == null)
        {
            if (startNext && fadeRoutine == null)
            {
                StartNextRequest();
            }
            return;
        }

        PlaybackRequest stopped = currentRequest;
        VoiceLineEntry stoppedLine = currentLine;
        stopped.cancelled = true;
        stopped.completed = true;

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
        }

        playbackRoutine = null;
        currentRequest = null;
        currentLine = null;
        activePlaybackId = 0;
        ClearHud(stoppedLine);

        float fade = Mathf.Max(0f, fadeOutTime);
        if (fade > 0f && voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            fadeRestoreVolume = voiceAudioSource.volume;
            fadeRoutine = StartCoroutine(FadeOutRoutine(fade, startNext));
        }
        else
        {
            voiceAudioSource?.Stop();
            if (startNext)
            {
                StartNextRequest();
            }
        }
    }

    private IEnumerator FadeOutRoutine(float duration, bool startNext)
    {
        float startVolume = voiceAudioSource == null ? 1f : voiceAudioSource.volume;
        float elapsed = 0f;
        while (voiceAudioSource != null && voiceAudioSource.isPlaying && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            voiceAudioSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.volume = fadeRestoreVolume;
        }
        fadeRoutine = null;
        if (startNext)
        {
            StartNextRequest();
        }
    }

    private void StartNextRequest()
    {
        if (currentRequest != null || fadeRoutine != null || queue.Count == 0)
        {
            return;
        }

        int selected = 0;
        for (int i = 1; i < queue.Count; i++)
        {
            if (queue[i].options.priority > queue[selected].options.priority)
            {
                selected = i;
            }
        }

        PlaybackRequest next = queue[selected];
        queue.RemoveAt(selected);
        StartRequest(next);
    }

    private bool ShouldInterrupt(PlaybackRequest current, PlaybackRequest incoming)
    {
        if (incoming.options.priority == VoicePriority.Critical ||
            incoming.options.group == VoiceGroup.GameOver || incoming.options.group == VoiceGroup.Final)
        {
            return true;
        }

        if (!incoming.options.interruptCurrent)
        {
            return false;
        }

        if (current.options.priority >= VoicePriority.Cutscene)
        {
            return false;
        }

        if (current.options.group == VoiceGroup.Tutorial)
        {
            return incoming.options.group == VoiceGroup.Tutorial;
        }

        if (current.options.group == incoming.options.group)
        {
            return true;
        }

        return incoming.options.priority >= current.options.priority;
    }

    private bool IsCurrent(PlaybackRequest request)
    {
        return request != null && !request.cancelled && currentRequest == request &&
            activePlaybackId == request.playbackId;
    }

    private void CancelQueuedRequest(PlaybackRequest request)
    {
        if (request == null)
        {
            return;
        }

        request.cancelled = true;
        request.completed = true;
    }

    private VoicePlaybackOptions CreateDefaultOptions(VoiceLineEntry entry, VoicePriority highest)
    {
        VoiceGroup group = InferGroup(entry.id);
        return new VoicePlaybackOptions
        {
            group = group,
            priority = highest,
            interruptCurrent = entry.interruptCurrent,
            clearQueueOfSameGroup = false,
            cancelOnStateExit = group == VoiceGroup.Intro || group == VoiceGroup.Tutorial ||
                group == VoiceGroup.Interaction || group == VoiceGroup.Suit,
            blockGameplay = group == VoiceGroup.Intro || group == VoiceGroup.Final || group == VoiceGroup.GameOver,
            fadeOutTime = group == VoiceGroup.Tutorial ? 0.1f : 0.08f,
            ownerStateId = entry.id
        };
    }

    private static VoiceGroup InferGroup(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return VoiceGroup.Gameplay;
        string value = id.Trim().ToUpperInvariant();

        if (value == "CEL_056" || value == "CEL_057") return VoiceGroup.GameOver;
        if (value == "CEL_045" || value == "CEL_046") return VoiceGroup.Suit;
        if (IsIdRange(value, "CEL_", 47, 52) || value == "CEL_054") return VoiceGroup.Interaction;
        if (IsIdRange(value, "CEL_", 8, 19)) return VoiceGroup.Tutorial;
        if (IsIdRange(value, "CEL_", 36, 44) || IsIdRange(value, "ELI_", 7, 10)) return VoiceGroup.Final;
        if (IsIdRange(value, "CEL_", 20, 35) || IsIdRange(value, "ELI_", 4, 6))
            return VoiceGroup.SectorNarrative;
        if (IsIdRange(value, "CEL_", 2, 7) || IsIdRange(value, "ELI_", 1, 3)) return VoiceGroup.Intro;
        return VoiceGroup.Gameplay;
    }

    private static bool IsIdRange(string id, string prefix, int first, int last)
    {
        if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int number;
        return int.TryParse(id.Substring(prefix.Length), out number) && number >= first && number <= last;
    }

    private bool IsOnCooldown(VoiceLineEntry entry)
    {
        float cooldown = entry.cooldownSeconds > 0f ? entry.cooldownSeconds : GetDefaultCooldown(entry.priority);
        return cooldown > 0f && lastPlayedAt.TryGetValue(entry.id, out float last) &&
            Time.unscaledTime - last < cooldown;
    }

    private float GetDefaultCooldown(VoicePriority priority)
    {
        switch (priority)
        {
            case VoicePriority.Low: return lowCooldown;
            case VoicePriority.Context: return contextCooldown;
            case VoicePriority.Gameplay: return gameplayCooldown;
            default: return 0f;
        }
    }

    private void EnsureDatabase()
    {
        if (database != null)
        {
            return;
        }

        VoiceLineDatabase[] loaded = Resources.FindObjectsOfTypeAll<VoiceLineDatabase>();
        if (loaded.Length > 0)
        {
            database = loaded[0];
        }
    }

    private void EnsureAudioSource()
    {
        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();
            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.loop = false;
        voiceAudioSource.spatialBlend = 0f;
        voiceAudioSource.dopplerLevel = 0f;
    }

    private void StopFadeAndAudio()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.volume = fadeRestoreVolume;
        }
    }

    private void ShowOnHud(VoiceLineEntry entry)
    {
        RefreshSceneBindings();
        if (ui != null)
        {
            ui.SetVoiceLine(entry);
        }
        else
        {
            Debug.LogWarning($"[Voice] UIManager não encontrado ao tocar {entry.id}.", this);
        }
    }

    private void ClearHud(VoiceLineEntry entry)
    {
        RefreshSceneBindings();
        ui?.ClearVoiceLine(entry);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneBindings();
        for (int i = queue.Count - 1; i >= 0; i--)
        {
            if (!queue[i].options.cancelOnStateExit)
            {
                continue;
            }
            CancelQueuedRequest(queue[i]);
            queue.RemoveAt(i);
        }

        if (currentRequest != null && currentRequest.options.cancelOnStateExit)
        {
            CancelCurrent(0f, true);
        }
    }

    private void RefreshSceneBindings()
    {
        ui = GameManager.Instance != null ? GameManager.Instance.ui : FindAnyObjectByType<UIManager>();
    }
}
