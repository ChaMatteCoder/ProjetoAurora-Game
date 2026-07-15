using UnityEngine;

/// Animação idle dos painéis de laser (Round 16b): pulso suave da tela emissiva + da luz,
/// dando "vida" ao equipamento sem custo (uma senoide). Não altera materiais compartilhados
/// — usa MaterialPropertyBlock no glow da tela.
public class PanelScreenPulse : MonoBehaviour
{
    public Renderer screenGlow;
    public Light panelLight;
    public Color glowColor = new Color(0.12f, 0.85f, 1f);
    public float pulseSpeed = 2.2f;
    [Range(0f, 1f)] public float pulseDepth = 0.4f;
    public float glowIntensity = 2.4f;
    public float lightBaseIntensity = 1.4f;

    private MaterialPropertyBlock mpb;
    private float phase;
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        // fase por posição para os painéis não pulsarem em uníssono
        phase = (transform.position.z * 0.13f) % 6.28f;
    }

    private void Update()
    {
        float pulse = 1f - pulseDepth * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + phase));

        if (screenGlow != null)
        {
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }

            screenGlow.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionId, glowColor * glowIntensity * pulse);
            mpb.SetColor(BaseColorId, glowColor);
            screenGlow.SetPropertyBlock(mpb);
        }

        if (panelLight != null)
        {
            panelLight.intensity = lightBaseIntensity * pulse;
        }
    }
}
