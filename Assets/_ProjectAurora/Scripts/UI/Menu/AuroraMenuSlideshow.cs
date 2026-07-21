using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAurora.UI.Menu
{
    /// Fundo do menu principal (Round 19): sequencia de imagens com movimento
    /// Ken Burns (zoom suave alternando in/out + leve pan) e crossfade
    /// cinematografico entre elas. Substitui o loop de video Dr.Elias_Loop.
    ///
    /// Estrutura esperada (montada pelo AuroraMenuBackgroundBuilder):
    ///   Menu_Background            [RectMask2D]  <- este componente
    ///     ├── Slide_A  (RawImage, stretch total)
    ///     └── Slide_B  (RawImage, stretch total)
    ///
    /// As duas camadas se revezam: enquanto a da frente exibe a imagem atual com
    /// Ken Burns, a de tras ja recebe a proxima e o alpha cruza entre elas.
    ///
    /// Usa tempo NAO-escalado: o menu continua animando mesmo com Time.timeScale
    /// alterado (pausa, transicoes de cena).
    [DefaultExecutionOrder(-10)]
    public sealed class AuroraMenuSlideshow : MonoBehaviour
    {
        [Header("Imagens (ordem de exibicao)")]
        [SerializeField] private Texture[] slides;

        [Header("Camadas de crossfade")]
        [SerializeField] private RawImage layerA;
        [SerializeField] private RawImage layerB;

        [Header("Ritmo")]
        [Tooltip("Segundos que cada imagem fica em tela, ja incluindo o crossfade de saida.")]
        [SerializeField] private float holdSeconds = 6.5f;
        [Tooltip("Duracao do crossfade entre imagens.")]
        [SerializeField] private float fadeSeconds = 2.2f;
        [Tooltip("Fade-in inicial ao abrir o menu (a partir do preto).")]
        [SerializeField] private float openFadeSeconds = 1.4f;

        [Header("Ken Burns")]
        [Tooltip("Escala base. Acima de 1 garante que o pan nunca revele a borda da imagem.")]
        [SerializeField] private float baseScale = 1.06f;
        [Tooltip("Quanto amplia/reduz durante a exibicao (0.10 = 10%).")]
        [SerializeField] private float zoomAmount = 0.10f;
        [Tooltip("Deslocamento maximo do pan, em fracao do tamanho da tela.")]
        [SerializeField] private float panAmount = 0.03f;

        private RawImage front;
        private RawImage back;
        private int index;
        private Coroutine loopRoutine;

        // direcoes de pan alternadas por slide — mantem o movimento variado sem
        // parecer aleatorio (diagonal suave, invertendo a cada imagem)
        private static readonly Vector2[] PanDirections =
        {
            new Vector2(-1f, -0.45f),
            new Vector2(1f, 0.4f),
            new Vector2(-0.85f, 0.6f),
            new Vector2(0.9f, -0.55f),
            new Vector2(-0.5f, 0.9f)
        };

        private void Reset()
        {
            holdSeconds = 6.5f;
            fadeSeconds = 2.2f;
        }

        private void OnEnable()
        {
            if (!HasValidSetup())
            {
                Debug.LogWarning("[MenuSlideshow] Configuracao incompleta: precisa de 2 camadas RawImage e ao menos 1 imagem.", this);
                return;
            }

            front = layerA;
            back = layerB;
            index = 0;

            SetAlpha(front, 0f);
            SetAlpha(back, 0f);
            front.texture = slides[0];
            back.texture = slides.Length > 1 ? slides[1] : slides[0];

            loopRoutine = StartCoroutine(RunSlideshow());
        }

        private void OnDisable()
        {
            if (loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
                loopRoutine = null;
            }
        }

        private bool HasValidSetup()
        {
            return layerA != null && layerB != null && slides != null && slides.Length > 0;
        }

        private IEnumerator RunSlideshow()
        {
            // entrada: primeira imagem surge do preto enquanto ja inicia o Ken Burns
            front.texture = slides[0];
            StartKenBurns(front, 0);
            yield return FadeLayer(front, 0f, 1f, openFadeSeconds);

            while (true)
            {
                // tempo de exibicao (descontando o fade que ja rolou na entrada)
                float visible = Mathf.Max(0.1f, holdSeconds - fadeSeconds);
                yield return WaitUnscaled(visible);

                if (slides.Length == 1)
                {
                    // uma imagem so: mantem o Ken Burns em loop, sem transicao
                    StartKenBurns(front, index);
                    continue;
                }

                int nextIndex = (index + 1) % slides.Length;
                back.texture = slides[nextIndex];
                SetAlpha(back, 0f);
                StartKenBurns(back, nextIndex);

                // crossfade: a de tras entra enquanto a da frente sai
                float t = 0f;
                while (t < fadeSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / fadeSeconds);
                    float eased = Mathf.SmoothStep(0f, 1f, k);
                    SetAlpha(back, eased);
                    SetAlpha(front, 1f - eased);
                    yield return null;
                }

                SetAlpha(back, 1f);
                SetAlpha(front, 0f);

                // troca de papeis
                RawImage swap = front;
                front = back;
                back = swap;
                index = nextIndex;
            }
        }

        /// Inicia o movimento Ken Burns de uma camada. Slides pares dao zoom-in,
        /// impares zoom-out — o intercalado evita a sensacao de "sempre a mesma coisa".
        private void StartKenBurns(RawImage layer, int slideIndex)
        {
            StartCoroutine(KenBurnsRoutine(layer, slideIndex));
        }

        private IEnumerator KenBurnsRoutine(RawImage layer, int slideIndex)
        {
            RectTransform rt = layer.rectTransform;
            bool zoomIn = (slideIndex % 2) == 0;

            float from = zoomIn ? baseScale : baseScale + zoomAmount;
            float to = zoomIn ? baseScale + zoomAmount : baseScale;

            Vector2 dir = PanDirections[slideIndex % PanDirections.Length].normalized;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Vector2 panFrom = -dir * (panAmount * screen.x * 0.5f);
            Vector2 panTo = dir * (panAmount * screen.x * 0.5f);

            // dura o ciclo inteiro do slide (exibicao + a transicao de entrada/saida)
            float duration = holdSeconds + fadeSeconds;
            float t = 0f;

            rt.localScale = Vector3.one * from;
            rt.anchoredPosition = panFrom;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                // easing suave nas pontas: movimento cinematografico, sem "arranque"
                float eased = Mathf.SmoothStep(0f, 1f, k);
                float s = Mathf.Lerp(from, to, eased);
                rt.localScale = new Vector3(s, s, 1f);
                rt.anchoredPosition = Vector2.Lerp(panFrom, panTo, eased);
                yield return null;
            }
        }

        private IEnumerator FadeLayer(RawImage layer, float from, float to, float seconds)
        {
            if (seconds <= 0f)
            {
                SetAlpha(layer, to);
                yield break;
            }

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seconds));
                SetAlpha(layer, Mathf.Lerp(from, to, k));
                yield return null;
            }
            SetAlpha(layer, to);
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static void SetAlpha(RawImage image, float alpha)
        {
            if (image == null)
            {
                return;
            }
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

#if UNITY_EDITOR
        /// Usado pelo builder de Editor para injetar as referencias.
        public void ConfigureFromEditor(Texture[] newSlides, RawImage a, RawImage b)
        {
            slides = newSlides;
            layerA = a;
            layerB = b;
        }
#endif
    }
}
