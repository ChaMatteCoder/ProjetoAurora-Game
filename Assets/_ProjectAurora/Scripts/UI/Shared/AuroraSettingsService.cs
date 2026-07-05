using UnityEngine;

/// Servico de configuracoes compartilhado entre Menu e Gameplay (Round 10).
/// Sem AudioMixer no projeto: master via AudioListener, musica via AudioManager,
/// voz via AudioSource do VoiceLinePlayer, efeitos via multiplicador estatico
/// (consumido pelos emissores de SFX, ex.: LaserHazard). Persistencia em PlayerPrefs.
public static class AuroraSettingsService
{
    private const string KeyMaster = "Aurora_MasterVolume";
    private const string KeyMusic = "Aurora_MusicVolume";
    private const string KeySfx = "Aurora_SfxVolume";
    private const string KeyVoice = "Aurora_VoiceVolume";
    private const string KeyFullscreen = "Aurora_Fullscreen";
    private const string KeyVSync = "Aurora_VSync";
    private const string KeyQuality = "Aurora_Quality";

    private static bool loaded;
    private static float master = 1f;
    private static float music = 0.8f;
    private static float sfx = 1f;
    private static float voice = 1f;
    private static bool fullscreen = true;
    private static bool vsync = true;
    private static int quality = -1; // -1 = nivel atual do projeto

    /// Multiplicador global de SFX lido pelos emissores (LaserHazard etc.).
    public static float EffectsVolume { get { EnsureLoaded(); return sfx; } }

    public static float MasterVolume { get { EnsureLoaded(); return master; } }
    public static float MusicVolume { get { EnsureLoaded(); return music; } }
    public static float VoiceVolume { get { EnsureLoaded(); return voice; } }
    public static bool Fullscreen { get { EnsureLoaded(); return fullscreen; } }
    public static bool VSync { get { EnsureLoaded(); return vsync; } }
    public static int Quality { get { EnsureLoaded(); return quality < 0 ? QualitySettings.GetQualityLevel() : quality; } }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        music = PlayerPrefs.GetFloat(KeyMusic, 0.8f);
        sfx = PlayerPrefs.GetFloat(KeySfx, 1f);
        voice = PlayerPrefs.GetFloat(KeyVoice, 1f);
        fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        vsync = PlayerPrefs.GetInt(KeyVSync, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        quality = PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel());
    }

    public static void SetMasterVolume(float value)
    {
        EnsureLoaded();
        master = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyMaster, master);
        AudioListener.volume = master;
    }

    public static void SetMusicVolume(float value)
    {
        EnsureLoaded();
        music = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyMusic, music);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetUserVolume(music);
        }
        ApplyMenuMusic();
    }

    public static void SetSfxVolume(float value)
    {
        EnsureLoaded();
        sfx = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeySfx, sfx);
        // emissores leem EffectsVolume na hora de tocar
    }

    public static void SetVoiceVolume(float value)
    {
        EnsureLoaded();
        voice = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KeyVoice, voice);
        ApplyVoiceVolume();
    }

    public static void SetFullscreen(bool value)
    {
        EnsureLoaded();
        fullscreen = value;
        PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
        Screen.fullScreen = value;
    }

    public static void SetVSync(bool value)
    {
        EnsureLoaded();
        vsync = value;
        PlayerPrefs.SetInt(KeyVSync, value ? 1 : 0);
        QualitySettings.vSyncCount = value ? 1 : 0;
    }

    public static void SetQuality(int level)
    {
        EnsureLoaded();
        quality = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
        PlayerPrefs.SetInt(KeyQuality, quality);
        QualitySettings.SetQualityLevel(quality, true);
    }

    public static void ResetToDefaults()
    {
        SetMasterVolume(1f);
        SetMusicVolume(0.8f);
        SetSfxVolume(1f);
        SetVoiceVolume(1f);
        SetFullscreen(true);
        SetVSync(true);
        SetQuality(QualitySettings.names.Length - 1);
        Save();
    }

    /// Aplica tudo (chamado no inicio de cada cena por AuroraSettingsApplier).
    public static void ApplyAll()
    {
        EnsureLoaded();
        AudioListener.volume = master;
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        if (quality >= 0 && quality != QualitySettings.GetQualityLevel())
        {
            QualitySettings.SetQualityLevel(quality, true);
        }
        if (Screen.fullScreen != fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetUserVolume(music);
        }
        ApplyMenuMusic();
        ApplyVoiceVolume();
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static void ApplyMenuMusic()
    {
        // musica do MENU (AudioSource proprio, fora do AudioManager)
        GameObject menuMusic = GameObject.Find("Audio_MenuMusic");
        if (menuMusic != null)
        {
            AudioSource src = menuMusic.GetComponent<AudioSource>();
            if (src != null)
            {
                src.volume = music * 0.69f; // preserva proporcao original (0.55 em music=0.8)
            }
        }
    }

    private static void ApplyVoiceVolume()
    {
        if (VoiceLinePlayer.Instance == null)
        {
            return;
        }

        foreach (AudioSource src in VoiceLinePlayer.Instance.GetComponentsInChildren<AudioSource>(true))
        {
            src.volume = voice;
        }
    }
}
