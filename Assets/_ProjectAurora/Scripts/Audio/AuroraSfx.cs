using UnityEngine;

/// Serviço central de efeitos sonoros (2D) do Projeto Aurora.
/// Colocado uma vez na cena (com os clipes atribuídos no Inspector) e acessado
/// por métodos estáticos. Respeita o volume de efeitos das Configurações.
/// Inclui um loop de "proximidade de DataFile" cujo volume/pitch sobe conforme
/// o Dr. Elias se aproxima do arquivo mais próximo.
public class AuroraSfx : MonoBehaviour
{
    public static AuroraSfx Instance { get; private set; }

    [Header("One-shots")]
    public AudioClip diagnostico;      // pós "Diagnóstico iniciado" (busca rápida)
    public AudioClip eReady;           // quando o E fica disponível
    public AudioClip coin;             // pegar AuroraCoin
    public AudioClip dataFilePickup;   // pegar DataFile

    [Header("DataFile — proximidade (loop)")]
    public AudioClip dataFileNear;
    [Tooltip("Distância (m) a partir da qual o som de proximidade começa a ser ouvido.")]
    public float nearRange = 30f;
    [Range(0f, 1f)] public float nearMaxVolume = 1.0f;

    [Header("Volumes")]
    [Range(0f, 1f)] public float coinVolume = 0.22f; // sutil: nao rouba trilha/dublagem
    [Range(0f, 1f)] public float uiVolume = 0.9f;

    private AudioSource oneShot;
    private AudioSource near;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        oneShot = gameObject.AddComponent<AudioSource>();
        oneShot.playOnAwake = false;
        oneShot.spatialBlend = 0f;

        near = gameObject.AddComponent<AudioSource>();
        near.playOnAwake = false;
        near.spatialBlend = 0f;
        near.loop = true;
        near.volume = 0f;
        if (dataFileNear != null)
        {
            near.clip = dataFileNear;
            near.Play();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Shot(AudioClip clip, float vol)
    {
        if (clip != null && oneShot != null)
        {
            oneShot.PlayOneShot(clip, vol * AuroraSettingsService.EffectsVolume);
        }
    }

    public static void PlayDiagnostico() { if (Instance != null) Instance.Shot(Instance.diagnostico, Instance.uiVolume); }
    public static void PlayEReady() { if (Instance != null) Instance.Shot(Instance.eReady, Instance.uiVolume); }
    public static void PlayCoin() { if (Instance != null) Instance.Shot(Instance.coin, Instance.coinVolume); }
    public static void PlayDataFilePickup() { if (Instance != null) Instance.Shot(Instance.dataFilePickup, Instance.uiVolume); }

    /// Atualiza o loop de proximidade com a distância ao DataFile mais próximo.
    /// distance = float.MaxValue quando não há nenhum ativo por perto.
    public static void ReportNearestDataFile(float distance)
    {
        if (Instance == null || Instance.near == null) return;
        float t = 1f - Mathf.Clamp01(distance / Mathf.Max(0.1f, Instance.nearRange));
        Instance.near.volume = t * t * Instance.nearMaxVolume * AuroraSettingsService.EffectsVolume;
        Instance.near.pitch = Mathf.Lerp(0.85f, 1.3f, t);
    }
}
