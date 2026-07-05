using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAurora.UI.Menu
{
    /// Painel de configuracoes — REUTILIZADO no MainMenu e no Pause da gameplay.
    /// Le/aplica via AuroraSettingsService (persistente em PlayerPrefs).
    public class AuroraMenuSettingsController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider voiceSlider;

        [Header("Video")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;

        [Header("Acoes")]
        [SerializeField] private Button resetButton;

        private bool syncing;

        private void Awake()
        {
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(v => { if (!syncing) AuroraSettingsService.SetMasterVolume(v); });
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(v => { if (!syncing) AuroraSettingsService.SetMusicVolume(v); });
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(v => { if (!syncing) AuroraSettingsService.SetSfxVolume(v); });
            if (voiceSlider != null) voiceSlider.onValueChanged.AddListener(v => { if (!syncing) AuroraSettingsService.SetVoiceVolume(v); });
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(v => { if (!syncing) AuroraSettingsService.SetFullscreen(v); });
            if (vsyncToggle != null) vsyncToggle.onValueChanged.AddListener(v => { if (!syncing) AuroraSettingsService.SetVSync(v); });
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                qualityDropdown.onValueChanged.AddListener(v => { if (!syncing) AuroraSettingsService.SetQuality(v); });
            }
            if (resetButton != null) resetButton.onClick.AddListener(() => { AuroraSettingsService.ResetToDefaults(); SyncFromService(); });
        }

        private void OnEnable()
        {
            SyncFromService();
        }

        private void OnDisable()
        {
            AuroraSettingsService.Save();
        }

        private void SyncFromService()
        {
            syncing = true;
            if (masterSlider != null) masterSlider.value = AuroraSettingsService.MasterVolume;
            if (musicSlider != null) musicSlider.value = AuroraSettingsService.MusicVolume;
            if (sfxSlider != null) sfxSlider.value = AuroraSettingsService.EffectsVolume;
            if (voiceSlider != null) voiceSlider.value = AuroraSettingsService.VoiceVolume;
            if (fullscreenToggle != null) fullscreenToggle.isOn = AuroraSettingsService.Fullscreen;
            if (vsyncToggle != null) vsyncToggle.isOn = AuroraSettingsService.VSync;
            if (qualityDropdown != null) qualityDropdown.SetValueWithoutNotify(AuroraSettingsService.Quality);
            syncing = false;
        }
    }
}
