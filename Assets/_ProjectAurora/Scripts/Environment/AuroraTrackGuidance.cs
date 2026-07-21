using System.Collections.Generic;
using UnityEngine;

namespace ProjectAurora.Environment
{
    /// Sistema de guia visual da pista (Round 17).
    ///
    /// Substitui os chevrons estaticos por um "sistema de aproximacao" vivo: uma crista
    /// de luz percorre os marcadores SEMPRE no sentido da corrida (+Z), como as luzes de
    /// aproximacao de uma pista de pouso. Isso resolve tres problemas de leitura:
    ///   1. DIRECAO — a onda viaja para frente, entao a pista "puxa" o olhar para o objetivo.
    ///   2. RITMO   — marcadores em intervalo regular geram cadencia constante em velocidade.
    ///   3. VIDA    — a intensidade pulsa e a cor responde ao estado narrativo do setor.
    ///
    /// Performance: so os marcadores dentro de uma janela ao redor do player recebem
    /// MaterialPropertyBlock por frame (os demais ficam no brilho base e continuam em
    /// batch). Com janela de 90m sao ~12 marcadores (~48 renderers) por frame.
    ///
    /// Requisito: o material precisa ter a keyword _EMISSION SERIALIZADA no asset
    /// (M_F01_CyanEmission ja tem). Habilitar a keyword em runtime nao funciona no URP.
    [DefaultExecutionOrder(50)]
    public class AuroraTrackGuidance : MonoBehaviour
    {
        [Header("Marcadores (preenchido pelo builder; auto-coleta se vazio)")]
        [Tooltip("Renderers dos chevrons, ordenados por Z no Awake.")]
        public List<Renderer> markers = new List<Renderer>();
        [Tooltip("Prefixo usado na auto-coleta quando a lista esta vazia.")]
        public string markerNamePrefix = "Track Chevron";

        [Header("Fluxo da onda")]
        [Tooltip("Velocidade da crista de luz (m/s). Acima da velocidade do player para " +
            "dar sensacao de que a pista puxa para frente.")]
        public float flowSpeed = 30f;
        [Tooltip("Distancia entre cristas (m).")]
        public float wavelength = 42f;
        [Range(0.05f, 0.5f)]
        [Tooltip("Largura da crista como fracao do wavelength (menor = pulso mais seco).")]
        public float pulseWidth = 0.16f;

        [Header("Intensidade")]
        [Tooltip("Brilho de repouso — mantem a seta sempre legivel como guia.")]
        public float baseIntensity = 1.0f;
        [Tooltip("Brilho da crista. Acima de ~5 o canal vermelho satura e o glifo estoura " +
            "para branco, perdendo a identidade cyan do projeto.")]
        public float peakIntensity = 4.5f;

        [Header("Janela de atualizacao")]
        [Tooltip("Raio (m) ao redor do player em que os marcadores sao animados.")]
        public float activeWindow = 90f;

        [Header("Cores por estado narrativo")]
        [Tooltip("Vermelho baixo de proposito: multiplicado pela intensidade da crista, " +
            "um R alto satura primeiro e lava o glifo para branco.")]
        public Color normalColor = new Color(0.05f, 0.85f, 1f);
        public Color transitionColor = new Color(1f, 0.50f, 0.06f);
        public Color corruptedColor = new Color(1f, 0.13f, 0.10f);
        [Tooltip("Multiplicador de intensidade durante a perseguicao dos robos.")]
        public float pursuitBoost = 1.5f;
        [Tooltip("Suavizacao da troca de cor entre setores.")]
        public float colorBlendSharpness = 2.5f;

        private Transform player;
        private MaterialPropertyBlock mpb;
        private float[] markerZ;
        private Color currentColor;
        private int windowStart;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (markers == null || markers.Count == 0)
            {
                AutoCollect();
            }
            PruneAndSort();
            mpb = new MaterialPropertyBlock();
            currentColor = normalColor;
        }

        private void Start()
        {
            ResolvePlayer();
        }

        private void AutoCollect()
        {
            markers = new List<Renderer>();
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r != null && r.name.StartsWith(markerNamePrefix))
                {
                    markers.Add(r);
                }
            }
        }

        /// Remove entradas nulas (cena editada) e ordena por Z — a janela usa busca binaria.
        private void PruneAndSort()
        {
            markers.RemoveAll(r => r == null);
            markers.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
            markerZ = new float[markers.Count];
            for (int i = 0; i < markers.Count; i++)
            {
                markerZ[i] = markers[i].transform.position.z;
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
        }

        private void LateUpdate()
        {
            if (markers.Count == 0)
            {
                return;
            }
            ResolvePlayer();
            if (player == null)
            {
                return;
            }

            float playerZ = player.position.z;
            Color target = ResolveStateColor(out float boost);
            currentColor = Color.Lerp(currentColor, target,
                1f - Mathf.Exp(-colorBlendSharpness * Time.deltaTime));

            float t = Time.time;
            float from = playerZ - activeWindow;
            float to = playerZ + activeWindow;

            int i = LowerBound(from);
            // apaga o marcador que acabou de sair da janela pela frente (evita "congelar" aceso)
            RestoreBase(windowStart, i);
            windowStart = i;

            for (; i < markers.Count && markerZ[i] <= to; i++)
            {
                Renderer r = markers[i];
                if (r == null)
                {
                    continue;
                }

                // fase da onda: (z - v*t)/lambda  =>  crista viaja no sentido +Z
                float phase = Mathf.Repeat((markerZ[i] - t * flowSpeed) / wavelength, 1f);
                float d = Mathf.Min(phase, 1f - phase);           // distancia circular ate a crista
                float w = Mathf.Max(0.0001f, pulseWidth);
                float pulse = Mathf.Exp(-(d * d) / (w * w * 0.5f));

                float intensity = Mathf.Lerp(baseIntensity, peakIntensity, pulse) * boost;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionColorId, currentColor * intensity);
                r.SetPropertyBlock(mpb);
            }
        }

        /// Devolve os marcadores que saíram da janela ao brilho base (senao ficariam
        /// travados no ultimo valor de emissao aplicado).
        private void RestoreBase(int fromIndex, int toIndex)
        {
            if (mpb == null)
            {
                return;
            }
            for (int i = Mathf.Max(0, fromIndex); i < Mathf.Min(toIndex, markers.Count); i++)
            {
                Renderer r = markers[i];
                if (r == null)
                {
                    continue;
                }
                r.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionColorId, currentColor * baseIntensity);
                r.SetPropertyBlock(mpb);
            }
        }

        private int LowerBound(float z)
        {
            int lo = 0, hi = markerZ.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (markerZ[mid] < z)
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

        /// Cor coerente com a narrativa: cyan nos setores limpos, ambar na transicao,
        /// vermelho nos setores corrompidos. A perseguicao intensifica o pulso.
        private Color ResolveStateColor(out float boost)
        {
            boost = 1f;
            Color color = normalColor;

            GameManager game = GameManager.Instance;
            if (game != null && game.sectors != null)
            {
                int sector = game.sectors.CurrentSector;
                if (sector == 3)
                {
                    color = transitionColor;
                }
                else if (sector > 3)
                {
                    color = corruptedColor;
                }
            }

            RobotPursuitDirector pursuit = FindPursuit();
            if (pursuit != null && pursuit.PursuitActive)
            {
                boost = pursuitBoost;
                color = Color.Lerp(color, corruptedColor, 0.45f);
            }
            return color;
        }

        private RobotPursuitDirector cachedPursuit;
        private bool pursuitSearched;

        private RobotPursuitDirector FindPursuit()
        {
            if (!pursuitSearched)
            {
                pursuitSearched = true;
                cachedPursuit = FindFirstObjectByType<RobotPursuitDirector>(FindObjectsInactive.Include);
            }
            return cachedPursuit;
        }
    }
}
