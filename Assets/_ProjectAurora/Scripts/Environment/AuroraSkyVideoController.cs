using UnityEngine;
using UnityEngine.Video;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
public sealed class AuroraSkyVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private VideoClip skyClip;

    private bool eventsBound;

    public bool IsPrepared => videoPlayer != null && videoPlayer.isPrepared;
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;
    public long CurrentFrame => videoPlayer == null ? -1L : videoPlayer.frame;
    public VideoClip Clip => skyClip;

    private void Awake()
    {
        ResolveReferences();
        ConfigurePlayer();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigurePlayer();
        BindEvents();
        PrepareAndPlay();
    }

    private void OnDisable()
    {
        UnbindEvents();
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ResumePlayback();
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (!isPaused)
        {
            ResumePlayback();
        }
    }

    private void ResolveReferences()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void ConfigurePlayer()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = skyClip;
        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
        videoPlayer.targetCamera = targetCamera;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.targetCameraAlpha = 1f;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.controlledAudioTrackCount = 0;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.sendFrameReadyEvents = false;
    }

    private void PrepareAndPlay()
    {
        if (videoPlayer == null || skyClip == null || targetCamera == null)
        {
            Debug.LogWarning("[AuroraSky] Video, clip ou camera principal nao configurados.", this);
            return;
        }

        if (videoPlayer.isPrepared)
        {
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Prepare();
        }
    }

    private void ResumePlayback()
    {
        if (!isActiveAndEnabled || videoPlayer == null || videoPlayer.isPlaying)
        {
            return;
        }

        PrepareAndPlay();
    }

    private void BindEvents()
    {
        if (videoPlayer == null || eventsBound)
        {
            return;
        }

        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.errorReceived += HandleError;
        eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (videoPlayer == null || !eventsBound)
        {
            return;
        }

        videoPlayer.prepareCompleted -= HandlePrepared;
        videoPlayer.errorReceived -= HandleError;
        eventsBound = false;
    }

    private static void HandlePrepared(VideoPlayer source)
    {
        source.Play();
    }

    private void HandleError(VideoPlayer source, string message)
    {
        Debug.LogWarning("[AuroraSky] Falha na reproducao do background: " + message, this);
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(VideoPlayer player, Camera camera, VideoClip clip)
    {
        videoPlayer = player;
        targetCamera = camera;
        skyClip = clip;
        ConfigurePlayer();
    }
#endif
}
