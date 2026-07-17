using UnityEngine;

namespace ProjectAurora.VFX
{
    /// Pulso de emissao via MaterialPropertyBlock para OBJETOS REPETIDOS (politica B).
    ///
    /// Validado pela matriz da Onda 1: MPB _EmissionColor renderiza DESDE QUE a keyword
    /// _EMISSION esteja serializada no material-asset. Este componente NAO habilita a
    /// keyword em runtime (nao funcionaria de forma confiavel em build) — apenas checa
    /// e avisa uma vez se o material nao estiver preparado.
    ///
    /// Custo aceito e documentado: cada Renderer com MPB sai do caminho do SRP Batcher.
    /// Usar em poucos objetos por vez (robos ativos, feixes proximos), nunca em massa.
    public sealed class AuroraMaterialPulseController : MonoBehaviour
    {
        [Tooltip("Renderers a pulsar. Se vazio, usa os do proprio GameObject e filhos.")]
        public Renderer[] targets;
        [ColorUsage(false, true)]
        public Color emissionColor = new Color(2.4f, 0.25f, 0.12f); // vermelho HDR
        [Tooltip("Intensidade minima/maxima do pulso (multiplica emissionColor).")]
        public float minIntensity = 0.35f;
        public float maxIntensity = 1.0f;
        [Tooltip("Ciclos por segundo.")]
        public float speed = 0.8f;
        [Tooltip("Desliga o Update quando o player esta longe (0 = sempre ativo).")]
        public float activeRange = 60f;

        private MaterialPropertyBlock mpb;
        private Transform player;
        private bool warned;
        private float phase;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            mpb = new MaterialPropertyBlock();
            if (targets == null || targets.Length == 0)
            {
                targets = GetComponentsInChildren<Renderer>(true);
            }
            phase = Random.value * 10f; // dessincroniza instancias vizinhas
        }

        private void OnDisable()
        {
            // restaura o estado original: MPB vazio devolve o material serializado
            if (targets == null || mpb == null)
            {
                return;
            }
            mpb.Clear();
            foreach (Renderer r in targets)
            {
                if (r != null)
                {
                    r.SetPropertyBlock(null);
                }
            }
        }

        private void Update()
        {
            if (targets == null || targets.Length == 0)
            {
                enabled = false;
                return;
            }

            // culling barato por distancia do player (evita Update em objetos longe)
            if (activeRange > 0f)
            {
                if (player == null)
                {
                    GameManager gm = GameManager.Instance;
                    if (gm != null && gm.player != null)
                    {
                        player = gm.player.transform;
                    }
                }
                if (player != null &&
                    Mathf.Abs(player.position.z - transform.position.z) > activeRange)
                {
                    return;
                }
            }

            float wave = 0.5f + 0.5f * Mathf.Sin((Time.time + phase) * speed * 2f * Mathf.PI);
            float k = Mathf.Lerp(minIntensity, maxIntensity, wave);
            mpb.SetColor(EmissionId, emissionColor * k);

            foreach (Renderer r in targets)
            {
                if (r == null)
                {
                    continue;
                }

                if (!warned && r.sharedMaterial != null &&
                    !r.sharedMaterial.IsKeywordEnabled("_EMISSION"))
                {
                    warned = true;
                    Debug.LogWarning("[AuroraMaterialPulse] Material '" + r.sharedMaterial.name +
                        "' sem keyword _EMISSION serializada — o pulso nao aparecera. " +
                        "Habilite a emissao no material-asset.", r);
                }

                r.SetPropertyBlock(mpb);
            }
        }
    }
}
