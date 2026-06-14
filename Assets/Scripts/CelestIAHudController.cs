using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public enum CelestIAState
{
    Normal,
    Transition,
    Corrupted
}

public class CelestIAHudController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage display;
    public VideoClip normalClip;
    public VideoClip transitionClip;
    public VideoClip corruptedClip;

    private CelestIAState currentState = (CelestIAState)(-1);
    private Coroutine transitionRoutine;

    private void Start()
    {
        SetCelestIAState(CelestIAState.Normal);
    }

    public void SetCelestIAState(CelestIAState state)
    {
        if (state == currentState)
        {
            return;
        }

        currentState = state;
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        UIManager ui = GameManager.Instance == null ? FindFirstObjectByType<UIManager>() : GameManager.Instance.ui;
        if (state == CelestIAState.Transition)
        {
            transitionRoutine = StartCoroutine(PulseTransition(ui));
        }
        else
        {
            ui?.SetCelestIAColor(state == CelestIAState.Corrupted
                ? new Color(1f, 0.15f, 0.15f)
                : new Color(0.05f, 0.85f, 1f));
        }
        ui?.SetCelestIAState(state);

        VideoClip clip = state switch
        {
            CelestIAState.Transition => transitionClip,
            CelestIAState.Corrupted => corruptedClip,
            _ => normalClip
        };

        if (clip == null)
        {
            Debug.LogWarning($"PROJETO:AURORA - Vídeo da CelestIA ausente para o estado {state}.");
            display.gameObject.SetActive(false);
            return;
        }

        display.gameObject.SetActive(true);
        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }

    private System.Collections.IEnumerator PulseTransition(UIManager ui)
    {
        Color cyan = new Color(0.05f, 0.85f, 1f);
        Color red = new Color(1f, 0.12f, 0.12f);
        while (currentState == CelestIAState.Transition)
        {
            float t = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
            ui?.SetCelestIAColor(Color.Lerp(cyan, red, t));
            yield return null;
        }
    }
}
