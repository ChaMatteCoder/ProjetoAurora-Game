using UnityEngine;
using UnityEngine.UI;

namespace ProjectAurora.VFX
{
    /// Pulso do prompt de interacao "E" (Onda 2 / parte A da Etapa 14).
    ///
    /// Pulsa apenas a COR dos Graphics indicados (brilho/cantos do card) — nada de
    /// escala nem layout: o card nao muda de tamanho e o texto nao desloca.
    /// O componente vive no proprio objeto do prompt: OnEnable comeca (o prompt so
    /// fica ativo quando ha interacao em alcance), OnDisable restaura as cores.
    public sealed class AuroraPromptPulse : MonoBehaviour
    {
        [Tooltip("Graphics a pulsar (glow/cantos). Se vazio, usa Images filhas exceto o fundo.")]
        public Graphic[] targets;
        [Tooltip("Multiplicador de brilho no pico do pulso.")]
        public float peakBoost = 1.6f;
        [Tooltip("Ciclos por segundo.")]
        public float speed = 1.4f;

        private Color[] baseColors;

        private void Awake()
        {
            if (targets == null || targets.Length == 0)
            {
                // fallback: todas as Images filhas (nao inclui a raiz/fundo)
                targets = GetComponentsInChildren<Image>(true);
            }
            CacheBaseColors();
        }

        private void CacheBaseColors()
        {
            baseColors = new Color[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                baseColors[i] = targets[i] != null ? targets[i].color : Color.white;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].color = baseColors[i];
                }
            }
        }

        private void Update()
        {
            // unscaled: o prompt pode aparecer com o jogo pausado/em cutscene leve
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed * 2f * Mathf.PI);
            float k = Mathf.Lerp(1f, peakBoost, wave);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }
                Color c = baseColors[i] * k;
                c.a = Mathf.Clamp01(baseColors[i].a * Mathf.Lerp(0.75f, 1f, wave));
                targets[i].color = c;
            }
        }
    }
}
