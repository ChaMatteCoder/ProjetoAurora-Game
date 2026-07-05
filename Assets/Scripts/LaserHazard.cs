using UnityEngine;

public class LaserHazard : MonoBehaviour
{
    public bool isActive = true;
    public int damage = 1;
    public GameObject visual;
    public Collider damageCollider;
    public Color activeColor = new Color(1f, 0.02f, 0.02f);
    public Color inactiveColor = new Color(0.12f, 0.12f, 0.12f);

    [Header("SFX (Round 9) — sorteio aleatorio a cada evento")]
    [Tooltip("Tocados quando o player atravessa o laser ATIVO.")]
    public AudioClip[] impactClips;
    [Tooltip("Tocados quando o laser e desativado (painel com E).")]
    public AudioClip[] deactivateClips;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;

    private AudioSource sfxSource;

    private AudioSource SfxSource
    {
        get
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.GetComponent<AudioSource>();
                if (sfxSource == null)
                {
                    sfxSource = gameObject.AddComponent<AudioSource>();
                }

                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 1f;          // 3D: som vem do laser
                sfxSource.minDistance = 3f;
                sfxSource.maxDistance = 30f;
                sfxSource.rolloffMode = AudioRolloffMode.Linear;
            }

            return sfxSource;
        }
    }

    public void Deactivate()
    {
        isActive = false;
        if (damageCollider != null)
        {
            damageCollider.enabled = false;
        }

        SetColor(inactiveColor);
        PlayRandom(deactivateClips);
    }

    public void Activate()
    {
        isActive = true;
        if (damageCollider != null)
        {
            damageCollider.enabled = true;
        }

        SetColor(activeColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
        {
            return;
        }

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            PlayRandom(impactClips); // feedback do toque no feixe, mesmo em janela de invulnerabilidade
            health.TakeDamage();
        }
    }

    private void PlayRandom(AudioClip[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return;
        }

        AudioClip clip = pool[Random.Range(0, pool.Length)];
        if (clip != null)
        {
            // multiplicador global das Configuracoes (Round 10)
            SfxSource.PlayOneShot(clip, sfxVolume * AuroraSettingsService.EffectsVolume);
        }
    }

    private void SetColor(Color color)
    {
        if (visual == null)
        {
            return;
        }

        // Cobre visuais compostos (varios feixes sob um pai) e materiais emissivos:
        // sem atualizar _EmissionColor, um laser desativado continuaria brilhando.
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            renderer.material.color = color;
            if (renderer.material.HasProperty("_BaseColor"))
            {
                renderer.material.SetColor("_BaseColor", color);
            }
            if (renderer.material.IsKeywordEnabled("_EMISSION"))
            {
                renderer.material.SetColor("_EmissionColor", color * 2.5f);
            }
        }
    }
}
