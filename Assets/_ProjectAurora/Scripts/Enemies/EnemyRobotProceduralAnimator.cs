using UnityEngine;

/// Animacao MECANICA PROCEDURAL para o robo inimigo (Round 6).
/// O modelo roboot.fbx e um mesh estatico nao-riggado (18 partes, sem ossos), entao a
/// animacao Mixamo Walking.fbx (esqueletica humanoide) nao pode deforma-lo. Em vez disso,
/// damos vida ao robo no nivel de transform: passada vertical (bob), balanco de pitch/yaw
/// e pulso emissivo no nucleo. Aplicado no transform "Visual" (filho do root, que e
/// posicionado pelo director/obstaculo). Le muito bem para inimigos de runner vistos de
/// tras em fog.
public class EnemyRobotProceduralAnimator : MonoBehaviour
{
    [Tooltip("Cadencia da passada (passos por segundo aprox). Perseguidor usa mais alto.")]
    public float cadence = 4.5f;
    public float bobAmplitude = 0.07f;
    public float pitchAmplitude = 3.5f;
    public float yawAmplitude = 2.5f;
    public float forwardLean = 6f;
    [Tooltip("Nucleos/olhos emissivos que pulsam (opcional).")]
    public Renderer[] glowRenderers;
    public Color glowColor = new Color(1f, 0.15f, 0.12f);
    public float glowPulseSpeed = 3f;

    private Vector3 baseLocalPos;
    private float phase;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        baseLocalPos = transform.localPosition;
        phase = Random.value * 10f; // dessincroniza robos entre si
        if (glowRenderers != null && glowRenderers.Length > 0)
        {
            mpb = new MaterialPropertyBlock();
        }
    }

    private void Update()
    {
        float t = Time.time * cadence + phase;

        // bob vertical (passada) — dobro da cadencia (dois passos por ciclo)
        float bob = Mathf.Abs(Mathf.Sin(t)) * bobAmplitude;
        transform.localPosition = baseLocalPos + new Vector3(0f, bob, 0f);

        // balanco mecanico + leve inclinacao pra frente (postura de perseguicao)
        float pitch = forwardLean + Mathf.Sin(t) * pitchAmplitude;
        float yaw = Mathf.Sin(t * 0.5f) * yawAmplitude;
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        // pulso emissivo do nucleo
        if (mpb != null)
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * glowPulseSpeed + phase);
            Color c = glowColor * pulse * 2.2f;
            foreach (Renderer r in glowRenderers)
            {
                if (r == null)
                {
                    continue;
                }
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", c);
                mpb.SetColor("_BaseColor", glowColor * pulse);
                r.SetPropertyBlock(mpb);
            }
        }
    }

    public void SetCadence(float value)
    {
        cadence = Mathf.Max(0.5f, value);
    }
}
