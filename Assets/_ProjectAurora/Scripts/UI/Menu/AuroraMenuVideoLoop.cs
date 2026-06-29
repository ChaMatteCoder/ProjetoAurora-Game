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
        [SerializeField] private float restartCooldown = 0.25f;
        [SerializeField] private float endRestartMargin = 1.5f;

        private float lastRestartTime;

        private void Awake()
        {
            ResolveReferences();
            ConfigurePlayer();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ConfigurePlayer();
            StartLoop();
        }

        private void OnDisable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
        }

        private void Update()
        {
            if (videoPlayer == null || loopClip == null || targetTexture == null)
            {
                return;
            }

            if (!videoPlayer.isPrepared)
            {
                if (!videoPlayer.isPlaying && Time.unscaledTime - lastRestartTime >= restartCooldown)
                {
                    StartLoop();
                }
                return;
            }

            if (videoPlayer.isPlaying && loopClip.length > 0d && videoPlayer.time >= loopClip.length - endRestartMargin)
            {
                RestartLoop();
                return;
            }

            if (!videoPlayer.isPlaying && Time.unscaledTime - lastRestartTime >= restartCooldown)
            {
                RestartLoop();
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
                targetImage.raycastTarget = false;
            }
        }

        private void StartLoop()
        {
            if (videoPlayer == null || loopClip == null || targetTexture == null)
            {
                Debug.LogWarning("Aurora menu video loop is missing VideoPlayer, clip, or RenderTexture.", this);
                return;
            }

            lastRestartTime = Time.unscaledTime;
            videoPlayer.Stop();
            videoPlayer.time = 0d;
            videoPlayer.Play();
        }

        private void RestartLoop()
        {
            lastRestartTime = Time.unscaledTime;
            videoPlayer.time = 0d;
            videoPlayer.Play();
        }
    }
}
