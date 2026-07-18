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

    [Header("Skybox (mundo-preso — corrige a 'arena deslizando')")]
    [Tooltip("RenderTexture que recebe os frames do vídeo (720p, igual ao clip).")]
    [SerializeField] private RenderTexture skyRenderTexture;
    [Tooltip("Material Skybox/Panoramic da cena (MAT_AuroraSky).")]
    [SerializeField] private Material skyboxMaterial;
    [Tooltip("Imagem estática exibida até o primeiro frame do vídeo (AuroraSky.png).")]
    [SerializeField] private Texture fallbackTexture;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
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
        // ate o video preparar, o skybox mostra a imagem estatica (sem frame preto)
        ApplySkyboxTexture(fallbackTexture);
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
        // devolve a estatica: nao deixar o material apontando para uma RT congelada
        ApplySkyboxTexture(fallbackTexture);
    }

    private void ApplySkyboxTexture(Texture texture)
    {
        if (skyboxMaterial != null && texture != null)
        {
            skyboxMaterial.SetTexture(MainTexId, texture);
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
        // RenderTexture -> material do skybox: o céu fica preso ao MUNDO, como a
        // imagem estática original. CameraFarPlane grudava o quadro na TELA, então
        // trocar de lane fazia o cenário "deslizar" sobre um fundo imóvel.
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = skyRenderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.Stretch; // RT tem o mesmo aspect do clip
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
        if (videoPlayer == null || skyClip == null || skyRenderTexture == null || skyboxMaterial == null)
        {
            Debug.LogWarning("[AuroraSky] Video, clip, RenderTexture ou material do skybox nao configurados.", this);
            return;
        }

        if (videoPlayer.isPrepared)
        {
            ApplySkyboxTexture(skyRenderTexture);
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

    private void HandlePrepared(VideoPlayer source)
    {
        // primeiro frame pronto: o skybox passa a ler a RT do vídeo
        ApplySkyboxTexture(skyRenderTexture);
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
