using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ProjectAurora.UI.Menu
{
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class AuroraMenuVideoLoop : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private VideoClip loopClip;
        [SerializeField] private RenderTexture targetTexture;
        [SerializeField] private RawImage targetImage;
        [SerializeField] private float restartCooldown = 0.35f;
        [SerializeField] private float freezeWatchdogSeconds = 1.25f;

        private float lastRestartTime;
        private float lastProgressTime;
        private double lastObservedVideoTime = -1d;
        private bool waitingForPrepare;
        private bool warnedMissingSetup;

        private void Awake()
        {
            ResolveReferences();
            ConfigurePlayer();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ConfigurePlayer();
            RegisterEvents();
            PlayFromStart();
        }

        private void OnDisable()
        {
            UnregisterEvents();
            waitingForPrepare = false;

            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
        }

        private void Update()
        {
            if (!HasValidSetup())
            {
                WarnMissingSetupOnce();
                return;
            }

            if (waitingForPrepare)
            {
                return;
            }

            if (!videoPlayer.isPlaying && Time.unscaledTime - lastRestartTime >= restartCooldown)
            {
                PlayFromStart();
                return;
            }

            double currentTime = videoPlayer.time;
            if (currentTime > lastObservedVideoTime + 0.02d || currentTime < lastObservedVideoTime - 0.5d)
            {
                lastObservedVideoTime = currentTime;
                lastProgressTime = Time.unscaledTime;
                return;
            }

            if (videoPlayer.isPlaying && Time.unscaledTime - lastProgressTime >= freezeWatchdogSeconds)
            {
                Debug.LogWarning("Aurora menu video stopped advancing; restarting Dr.Elias_Loop playback.", this);
                PlayFromStart();
            }
        }

        private void ResolveReferences()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
            }
        }

        private void ConfigurePlayer()
        {
            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = loopClip;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = targetTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.playbackSpeed = 1f;

            if (targetImage != null)
            {
                targetImage.texture = targetTexture;
                targetImage.color = Color.white;
                targetImage.raycastTarget = false;
            }
        }

        private void RegisterEvents()
        {
            if (videoPlayer == null)
            {
                return;
            }

            UnregisterEvents();
            videoPlayer.prepareCompleted += HandlePrepareCompleted;
            videoPlayer.loopPointReached += HandleLoopPointReached;
            videoPlayer.errorReceived += HandleVideoError;
        }

        private void UnregisterEvents()
        {
            if (videoPlayer == null)
            {
                return;
            }

            videoPlayer.prepareCompleted -= HandlePrepareCompleted;
            videoPlayer.loopPointReached -= HandleLoopPointReached;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        private bool HasValidSetup()
        {
            return videoPlayer != null && loopClip != null && targetTexture != null;
        }

        private void WarnMissingSetupOnce()
        {
            if (warnedMissingSetup)
            {
                return;
            }

            warnedMissingSetup = true;
            Debug.LogWarning("Aurora menu video loop is missing VideoPlayer, Dr.Elias_Loop clip, or RenderTexture.", this);
        }

        private void PlayFromStart()
        {
            if (!HasValidSetup())
            {
                WarnMissingSetupOnce();
                return;
            }

            lastRestartTime = Time.unscaledTime;
            lastProgressTime = Time.unscaledTime;
            lastObservedVideoTime = -1d;
            waitingForPrepare = true;

            videoPlayer.Stop();
            videoPlayer.time = 0d;
            videoPlayer.Prepare();
        }

        private void HandlePrepareCompleted(VideoPlayer source)
        {
            waitingForPrepare = false;
            lastProgressTime = Time.unscaledTime;
            lastObservedVideoTime = -1d;
            source.time = 0d;
            source.Play();
        }

        private void HandleLoopPointReached(VideoPlayer source)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            PlayFromStart();
        }

        private void HandleVideoError(VideoPlayer source, string message)
        {
            waitingForPrepare = false;
            Debug.LogWarning($"Aurora menu video could not play Dr.Elias_Loop: {message}", this);
        }
    }
}
