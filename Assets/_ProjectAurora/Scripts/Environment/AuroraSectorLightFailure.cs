using System.Collections.Generic;
using UnityEngine;

namespace ProjectAurora.Environment
{
    /// Falha progressiva das luzes do teto (Round 18).
    ///
    /// Pedido de playtest: "depois do Setor A, algumas luzes comecam a falhar,
    /// influenciando cenario e iluminacao, algumas piscando — sensacao de que tudo
    /// esta desmoronando".
    ///
    /// As luminarias de teto (Ceiling Light L/R) sao EMISSIVAS (M_F01_WhiteEmission,
    /// sem Light real). Este controlador:
    ///   1. Modula a EMISSAO de cada luminaria via MaterialPropertyBlock — o cenario
    ///      lampeja/apaga (leitura visual do colapso).
    ///   2. Mantem um POOL de Point lights REAIS que segue as luminarias que estao
    ///      falhando perto do player — assim a ILUMINACAO real do corredor tambem
    ///      pisca e escurece, nao so o material.
    ///
    /// A intensidade da falha cresce com Z: Setor A (z &lt; failStartZ) fica intacto
    /// (estabelece o "antes"); dai em diante cada vez mais luminarias falham, com
    /// piscadas mais violentas, dropouts e algumas mortas de vez.
    ///
    /// Performance: so as luminarias dentro de uma janela ao redor do player recebem
    /// MPB por frame; o pool de luzes reais e pequeno e sem sombras.
    ///
    /// Requisito: o material precisa ter _EMISSION SERIALIZADO (M_F01_WhiteEmission ja tem).
    [DefaultExecutionOrder(60)]
    public class AuroraSectorLightFailure : MonoBehaviour
    {
        [Header("Coleta de luminarias")]
        [Tooltip("Nomes de objeto que falham: teto (Ceiling Light L/R, branco) e os " +
            "glows verticais dos pilares laterais (Arch Vertical Glow, cyan). Cada uma " +
            "pisca na SUA cor — a emissao-base e lida do proprio material.")]
        public string[] fixtureNames =
        {
            "Ceiling Light L", "Ceiling Light R", "Arch Vertical Glow"
        };

        [Header("Progressao da falha (Z mundial)")]
        [Tooltip("Z onde a falha comeca. Antes disso (Setor A) tudo fica intacto.")]
        public float failStartZ = 450f;
        [Tooltip("Z onde a falha atinge o maximo.")]
        public float failFullZ = 1500f;
        [Range(0f, 1f)]
        [Tooltip("Fracao maxima de luminarias que chegam a falhar na zona pior " +
            "(o resto fica aceso para nao cegar a leitura da pista).")]
        public float maxFailFraction = 0.68f;

        [Header("Emissao")]
        [Tooltip("Fallback caso o material nao tenha _EmissionColor. A cor real de cada " +
            "luminaria e lida do proprio material na coleta (teto branco / arch cyan).")]
        public Color healthyEmission = new Color(2.5f, 2.88f, 3.2f);
        [Tooltip("Tint das luminarias moribundas (frio/morto).")]
        public Color deadTint = new Color(0.55f, 0.62f, 0.8f);
        [Tooltip("Lampejo de emergencia (avermelhado) nas piscadas fundas.")]
        public Color emergencyTint = new Color(1f, 0.22f, 0.12f);

        [Header("Janela de atualizacao")]
        public float windowAhead = 75f;
        public float windowBehind = 25f;

        [Header("Pool de luzes reais (influencia a iluminacao)")]
        [Tooltip("Quantas Point lights reais acompanham as luminarias falhando.")]
        public int realLightPool = 8;
        [Tooltip("Intensidade de pico da luz real (quando a luminaria esta acesa).")]
        public float realLightIntensity = 12f;
        public float realLightRange = 17f;

        private struct Fixture
        {
            public Renderer renderer;
            public float z;
            public float seed;          // 0..1 hash — timing/fase do flicker (irregular)
            public float rank;          // 0..1 baixa-discrepancia por Z — decide QUEM falha (uniforme)
            public Color baseEmission;  // emissao saudavel do proprio material (branco/cyan)
            public Vector3 pos;         // posicao mundial (estatica) — usada pela luz real
        }

        private Fixture[] fixtures;
        private MaterialPropertyBlock mpb;
        private Transform player;
        private Light[] pool;
        private int windowStart;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // buffers reutilizados por frame (sem alocacao)
        private readonly List<int> activeFailing = new List<int>(64);

        private void Awake()
        {
            CollectFixtures();
            mpb = new MaterialPropertyBlock();
            if (healthyEmission.maxColorComponent <= 0.001f)
            {
                ReadHealthyEmission();
            }
            BuildLightPool();
        }

        private void Start()
        {
            ResolvePlayer();
        }

        private void CollectFixtures()
        {
            var list = new List<Fixture>();
            var nameSet = new HashSet<string>(fixtureNames);
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r == null || !nameSet.Contains(r.name))
                {
                    continue;
                }
                Material m = r.sharedMaterial;
                Color baseEmission = (m != null && m.HasProperty(EmissionColorId))
                    ? m.GetColor(EmissionColorId)
                    : healthyEmission;
                list.Add(new Fixture
                {
                    renderer = r,
                    z = r.transform.position.z,
                    seed = Hash01(list.Count),
                    baseEmission = baseEmission,
                    pos = r.transform.position
                });
            }
            list.Sort((a, b) => a.z.CompareTo(b.z));
            fixtures = list.ToArray();

            // rank de baixa-discrepancia (razao aurea) ao longo de Z: garante que, em
            // qualquer trecho, a fracao de luminarias falhando ~= a fracao de falha —
            // sem os buracos de 100m que o hash independente deixava. O par de cada bay
            // (L/R, mesmo Z) recebe ranks vizinhos para nao apagar os dois juntos.
            const float golden = 0.6180339887f;
            for (int i = 0; i < fixtures.Length; i++)
            {
                Fixture f = fixtures[i];
                f.rank = Frac(i * golden);
                fixtures[i] = f;
            }
        }

        private static float Frac(float v)
        {
            return v - Mathf.Floor(v);
        }

        private void ReadHealthyEmission()
        {
            if (fixtures.Length > 0 && fixtures[0].renderer != null)
            {
                Material m = fixtures[0].renderer.sharedMaterial;
                if (m != null && m.HasProperty(EmissionColorId))
                {
                    healthyEmission = m.GetColor(EmissionColorId);
                }
            }
            if (healthyEmission.maxColorComponent <= 0.001f)
            {
                healthyEmission = new Color(2.5f, 2.88f, 3.2f);
            }
        }

        private void BuildLightPool()
        {
            pool = new Light[Mathf.Max(0, realLightPool)];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("FailLight_" + i);
                go.transform.SetParent(transform);
                Light l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = realLightRange;
                l.intensity = 0f;
                l.shadows = LightShadows.None;
                l.color = new Color(0.82f, 0.9f, 1f);
                l.enabled = false;
                pool[i] = l;
            }
        }

        private void ResolvePlayer()
        {
            if (player != null)
            {
                return;
            }
            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                player = GameManager.Instance.player.transform;
            }
            else
            {
                var go = GameObject.Find("Dr. Elias - Player");
                if (go != null)
                {
                    player = go.transform;
                }
            }
        }

        /// 0 antes do Setor A, sobe ate 1 em failFullZ.
        private float FailureAt(float z)
        {
            if (z <= failStartZ)
            {
                return 0f;
            }
            return Mathf.Clamp01((z - failStartZ) / Mathf.Max(1f, failFullZ - failStartZ));
        }

        private void LateUpdate()
        {
            if (fixtures == null || fixtures.Length == 0)
            {
                return;
            }
            ResolvePlayer();
            if (player == null)
            {
                return;
            }

            float t = Time.time;
            float pz = player.position.z;
            float from = pz - windowBehind;
            float to = pz + windowAhead;

            // restaura luminarias que sairam da janela pela frente
            int startIdx = LowerBound(from);
            RestoreHealthy(windowStart, startIdx);
            windowStart = startIdx;

            activeFailing.Clear();

            for (int i = startIdx; i < fixtures.Length && fixtures[i].z <= to; i++)
            {
                Renderer r = fixtures[i].renderer;
                if (r == null)
                {
                    continue;
                }

                float fail = FailureAt(fixtures[i].z);
                float threshold = fail * maxFailFraction;
                bool affected = fixtures[i].rank < threshold;

                Color baseEmission = fixtures[i].baseEmission;

                if (!affected)
                {
                    // saudavel — leve respiro deeper para nao ficar "morto-vivo" estatico
                    float breathe = 1f - 0.06f * fail * (0.5f + 0.5f * Mathf.Sin(t * 1.7f + fixtures[i].seed * 12f));
                    ApplyEmission(r, baseEmission * breathe);
                    continue;
                }

                // severidade: quanto mais fundo abaixo do limiar, mais "morta" a luminaria
                float sev = threshold > 0.0001f ? 1f - fixtures[i].rank / threshold : 0f;
                float flick = Flicker(t, fixtures[i].seed, sev);

                Color tint = Color.Lerp(baseEmission, baseEmission * deadTint, sev * 0.7f);
                // lampejo de emergencia avermelhado nas mortas que dao arco
                if (sev > 0.6f && flick > 1.05f)
                {
                    tint = Color.Lerp(tint, emergencyTint * baseEmission.maxColorComponent, 0.5f);
                }
                ApplyEmission(r, tint * flick);

                activeFailing.Add(i);
                // guarda o flick no seed-buffer? nao; recomputo no pool com o mesmo t
            }

            DriveRealLights(t, pz);
        }

        private void ApplyEmission(Renderer r, Color emission)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColorId, emission);
            r.SetPropertyBlock(mpb);
        }

        /// Posiciona o pool de luzes reais nas luminarias falhando mais proximas do
        /// player e faz a intensidade acompanhar a piscada — a iluminacao real reage.
        private void DriveRealLights(float t, float pz)
        {
            if (pool == null || pool.Length == 0)
            {
                return;
            }

            // ordena as falhando por proximidade ao player (parcial: pega as N menores)
            int n = Mathf.Min(pool.Length, activeFailing.Count);
            // selection parcial simples (activeFailing costuma ter < 40 itens)
            for (int a = 0; a < n; a++)
            {
                int best = a;
                float bestD = DistToPlayer(activeFailing[a], pz);
                for (int b = a + 1; b < activeFailing.Count; b++)
                {
                    float d = DistToPlayer(activeFailing[b], pz);
                    if (d < bestD)
                    {
                        bestD = d;
                        best = b;
                    }
                }
                (activeFailing[a], activeFailing[best]) = (activeFailing[best], activeFailing[a]);
            }

            for (int i = 0; i < pool.Length; i++)
            {
                Light l = pool[i];
                if (i < n)
                {
                    Fixture f = fixtures[activeFailing[i]];
                    // recomputa severidade/flicker com os MESMOS valores da emissao
                    float fail = FailureAt(f.z);
                    float threshold = fail * maxFailFraction;
                    float sev = threshold > 0.0001f ? 1f - f.rank / threshold : 0f;
                    float flick = Flicker(t, f.seed, sev);

                    // luz real NA posicao da propria luminaria (teto no alto / arch na
                    // parede), puxada um pouco para o centro para lancar luz na pista.
                    Vector3 p = f.pos;
                    p.x *= 0.72f;
                    l.transform.position = p;
                    l.intensity = Mathf.Clamp01(flick) * realLightIntensity;
                    // cor da luz herda a emissao da luminaria (teto branco / arch cyan),
                    // normalizada; mortas em arco puxam para o vermelho de emergencia.
                    Color lc = NormalizeColor(f.baseEmission);
                    l.color = sev > 0.6f
                        ? Color.Lerp(lc, new Color(1f, 0.5f, 0.42f), 0.4f)
                        : lc;
                    l.enabled = l.intensity > 0.05f;
                }
                else if (l.enabled)
                {
                    l.enabled = false;
                }
            }
        }

        private float DistToPlayer(int fixtureIndex, float pz)
        {
            return Mathf.Abs(fixtures[fixtureIndex].z - pz);
        }

        private void RestoreHealthy(int fromIndex, int toIndex)
        {
            if (mpb == null)
            {
                return;
            }
            for (int i = Mathf.Max(0, fromIndex); i < Mathf.Min(toIndex, fixtures.Length); i++)
            {
                Renderer r = fixtures[i].renderer;
                if (r != null)
                {
                    ApplyEmission(r, fixtures[i].baseEmission);
                }
            }
        }

        /// Normaliza uma cor de emissao HDR para uso como cor de Light (0..1),
        /// preservando o matiz (teto branco-azulado / arch cyan).
        private static Color NormalizeColor(Color c)
        {
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (m <= 0.0001f)
            {
                return new Color(0.82f, 0.9f, 1f);
            }
            return new Color(c.r / m, c.g / m, c.b / m, 1f);
        }

        /// Modelo de morte de lampada fluorescente: instabilidade lenta + sputter rapido
        /// + dropouts periodicos + arcos ocasionais. sev empurra para "mais apagada".
        private float Flicker(float t, float seed, float sev)
        {
            float slow = Mathf.PerlinNoise(seed * 13.1f, t * 6.5f);
            float fast = Mathf.PerlinNoise(seed * 4.7f, t * 22f);
            float v = 0.45f * slow + 0.55f * fast;

            // dropouts: cortes bruscos periodicos (frequencia varia por luminaria)
            float drop = Mathf.Sin(t * (2.5f + seed * 5f) + seed * 10f);
            if (drop > 0.62f)
            {
                v *= 0.12f;
            }

            // arcos: picos raros de sobretensao
            float spark = Mathf.PerlinNoise(seed * 9.3f, t * 46f);
            if (spark > 0.86f)
            {
                v = Mathf.Max(v, 1.15f);
            }

            // piso cai com a severidade (mortas ficam quase sempre no escuro)
            float floorV = Mathf.Lerp(0.32f, 0.02f, sev);
            v = Mathf.Lerp(floorV, 1f, Mathf.Clamp01(v));

            if (sev > 0.82f)
            {
                // luminaria "morta": so acende em arcos raros
                v = spark > 0.9f ? Mathf.Max(v, 1.1f) : v * 0.06f;
            }
            return Mathf.Clamp(v, 0f, 1.25f);
        }

        private int LowerBound(float z)
        {
            int lo = 0, hi = fixtures.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (fixtures[mid].z < z)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }
            return lo;
        }

        private static float Hash01(int i)
        {
            // hash inteiro deterministico -> 0..1 (bem espalhado, sem Random)
            uint x = (uint)(i * 747796405 + 2891336453);
            x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737u;
            x = (x >> 22) ^ x;
            return (x & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }
}
