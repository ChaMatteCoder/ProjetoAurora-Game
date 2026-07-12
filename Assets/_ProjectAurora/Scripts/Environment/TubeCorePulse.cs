using UnityEngine;

/// Pulso do NÚCLEO INTERNO do Tubo Final (Terminal Central).
///
/// Correção crítica: versões anteriores tingiam o mesh inteiro do tubo
/// (carcaça + estrutura), o que o cliente rejeitou. O modelo `Tubo_Final_LP`
/// é mesh única com um material, então a carcaça não pode ser isolada por
/// submesh. Solução: um cilindro emissivo separado (`CoreInner_Emissive`)
/// vive dentro da gaiola de metal e é o ÚNICO objeto que muda de cor —
/// a estrutura externa (barras, base, topo) permanece 100% estável.
///
/// Este componente oscila SOMENTE o núcleo interno (albedo + emissão) e a
/// luz interna entre ciano (contenção estável) e vermelho (falha). Nunca
/// toca o Renderer da carcaça.
public class TubeCorePulse : MonoBehaviour
{
    [Header("Alvos (só o núcleo interno)")]
    [Tooltip("Renderer do cilindro emissivo interno. NÃO apontar para a carcaça do tubo.")]
    public Renderer innerCore;
    [Tooltip("Luz pontual dentro do núcleo (opcional).")]
    public Light coreLight;

    [Header("Cores de estado")]
    public Color stableColor = new Color(0.15f, 1.1f, 1.7f);   // ciano — contenção ativa
    public Color faultColor = new Color(1.5f, 0.12f, 0.06f);   // vermelho — falha de contenção

    [Header("Comportamento")]
    [Tooltip("Segundos de um ciclo completo ciano→vermelho→ciano.")]
    public float cycleSeconds = 8f;
    [Tooltip("Fração do ciclo passada perto do vermelho (0..1). <0.5 = mais tempo estável em ciano.")]
    [Range(0.15f, 0.85f)] public float faultBias = 0.35f;
    public float emissionIntensity = 3.0f;
    public float lightBaseIntensity = 3.5f;
    public float lightFaultIntensity = 6.0f;

    private Material innerMat;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (innerCore == null) return;
        innerMat = innerCore.material;   // instancia só o material do núcleo interno
        innerMat.EnableKeyword("_EMISSION");
        innerMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    private void Update()
    {
        if (innerMat == null) return;

        // senoide 0..1; faultBias<0.5 mantém mais tempo no ciano
        float raw = 0.5f + 0.5f * Mathf.Sin(Time.time * (2f * Mathf.PI / Mathf.Max(0.1f, cycleSeconds)));
        float t = Mathf.Pow(raw, Mathf.Lerp(2.2f, 0.6f, faultBias));

        Color c = Color.Lerp(stableColor, faultColor, t);
        innerMat.SetColor(BaseColorId, c);
        innerMat.SetColor(EmissionId, c * emissionIntensity);

        if (coreLight != null)
        {
            coreLight.color = c;
            coreLight.intensity = Mathf.Lerp(lightBaseIntensity, lightFaultIntensity, t);
        }
    }

    private void OnDestroy()
    {
        if (innerMat != null) Destroy(innerMat);
    }
}
