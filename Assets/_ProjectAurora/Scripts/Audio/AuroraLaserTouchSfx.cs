using UnityEngine;

/// Som de toque para lasers baseados em Obstacle (fora do sistema LaserHazard).
///
/// Os 21 lasers "Laser_*" do ManualPass causam dano via Obstacle e nao tinham som
/// nenhum ao serem encostados. Este componente coexiste com o Obstacle no mesmo
/// collider (ambos recebem OnTriggerEnter) e so cuida do audio 3D — nada de dano,
/// nada de logica de gameplay. Mesmo padrao de fonte 3D do LaserHazard (Round 9).
[DisallowMultipleComponent]
public class AuroraLaserTouchSfx : MonoBehaviour
{
    [Tooltip("Clipes sorteados ao encostar no laser.")]
    public AudioClip[] touchClips;
    [Range(0f, 1f)] public float volume = 0.85f;
    [Tooltip("Segundos minimos entre toques (evita spam ao raspar no feixe).")]
    public float cooldown = 0.4f;

    private AudioSource source;
    private float lastPlayedAt = -10f;

    private void OnTriggerEnter(Collider other)
    {
        if (touchClips == null || touchClips.Length == 0)
        {
            return;
        }
        if (Time.time - lastPlayedAt < cooldown)
        {
            return;
        }
        if (other.GetComponentInParent<PlayerHealth>() == null)
        {
            return;
        }

        if (source == null)
        {
            source = gameObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 3f;
            source.maxDistance = 30f;
            source.rolloffMode = AudioRolloffMode.Linear;
        }

        AudioClip clip = touchClips[Random.Range(0, touchClips.Length)];
        if (clip != null)
        {
            lastPlayedAt = Time.time;
            source.PlayOneShot(clip, volume * AuroraSettingsService.EffectsVolume);
        }
    }
}
