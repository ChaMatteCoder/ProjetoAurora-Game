using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip gameplayMusic;
    [Range(0f, 1f)] public float volume = 0.5f;

    private AudioSource source;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volume;
    }

    private void Start()
    {
        RestartMusic();
        SetNarrativeVolume(0.15f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RestartMusic()
    {
        if (gameplayMusic == null)
        {
            Debug.LogWarning("PROJETO:AURORA - Música não encontrada. A gameplay continuará sem trilha.");
            return;
        }

        source.Stop();
        source.clip = gameplayMusic;
        source.time = 0f;
        source.volume = volume;
        source.Play();
    }

    public void BeginGameplayMusic()
    {
        if (source.clip == null || !source.isPlaying)
        {
            RestartMusic();
        }

        source.volume = volume;
    }

    public void SetNarrativeVolume(float multiplier)
    {
        source.volume = volume * Mathf.Clamp01(multiplier);
    }

    /// Volume de musica definido pelo usuario nas Configuracoes (Round 10).
    /// Preserva a proporcao do volume atual (ex.: reducao narrativa em curso).
    public void SetUserVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        float ratio = volume > 0.0001f ? source.volume / volume : 1f;
        volume = clamped;
        source.volume = volume * Mathf.Clamp01(ratio);
    }

    public void StopMusic()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        source.Stop();
        source.time = 0f;
    }

    public void FadeOutMusic(float duration)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeOutRoutine(Mathf.Max(0f, duration)));
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            source.Pause();
        }
        else if (source.clip != null)
        {
            source.UnPause();
        }
    }

    public void FadeForFinal()
    {
        source.volume = volume * 0.25f;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        if (source == null || !source.isPlaying)
        {
            fadeRoutine = null;
            yield break;
        }

        float startVolume = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, duration <= 0f ? 1f : elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.time = 0f;
        source.volume = volume;
        fadeRoutine = null;
    }
}
