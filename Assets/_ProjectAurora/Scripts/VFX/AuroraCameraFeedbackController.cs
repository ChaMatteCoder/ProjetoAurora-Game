using UnityEngine;

namespace ProjectAurora.VFX
{
    /// Camera shake leve e nao-destrutivo para feedback de gameplay (Etapa 23).
    ///
    /// Por que Update + LateUpdate: o CameraFollow faz lerp da posicao para um alvo
    /// ABSOLUTO dentro do LateUpdate dele. Se o shake fosse somado e deixado no
    /// transform, o follow do frame seguinte partiria da posicao "suja" e o tremor
    /// vazaria para o movimento normal da camera. Entao:
    ///   Update()      -> remove o offset do frame anterior (o follow so ve posicao limpa)
    ///   LateUpdate()  -> roda DEPOIS do CameraFollow (execution order 100) e aplica o offset
    /// O render acontece apos o LateUpdate, entao o tremor aparece sem nunca sujar o
    /// transform de forma permanente.
    ///
    /// Nao acumula: um novo shake pega o MAIOR entre o restante e o novo, nunca soma.
    [DefaultExecutionOrder(100)]
    public sealed class AuroraCameraFeedbackController : MonoBehaviour
    {
        /// Desliga todo o shake (acessibilidade / configuracao futura).
        public static bool ShakeEnabled = true;

        public static AuroraCameraFeedbackController Instance { get; private set; }

        [Header("Presets (amplitude em metros, duracao em segundos)")]
        [Tooltip("Dano no Dr. Elias — bem sutil, nao pode enjoar.")]
        public ShakePreset damage = new ShakePreset(0.10f, 0.22f, 26f);
        [Tooltip("Impacto de porta fechando.")]
        public ShakePreset doorImpact = new ShakePreset(0.16f, 0.35f, 18f);
        [Tooltip("Colapso da Ponte Tecnica.")]
        public ShakePreset bridgeCollapse = new ShakePreset(0.22f, 0.70f, 12f);
        [Tooltip("Ativacao dos robos (cutscene).")]
        public ShakePreset robotActivation = new ShakePreset(0.14f, 0.40f, 16f);
        [Tooltip("Pulso do Terminal Central.")]
        public ShakePreset terminalPulse = new ShakePreset(0.08f, 0.50f, 9f);

        [Header("Limites de seguranca")]
        [Tooltip("Amplitude maxima absoluta, independente do preset (anti-enjoo).")]
        public float maxAmplitude = 0.28f;

        private float amplitude;
        private float duration;
        private float frequency;
        private float elapsed;
        private Vector3 appliedOffset;
        private float seed;

        [System.Serializable]
        public struct ShakePreset
        {
            public float amplitude;
            public float duration;
            public float frequency;

            public ShakePreset(float amplitude, float duration, float frequency)
            {
                this.amplitude = amplitude;
                this.duration = duration;
                this.frequency = frequency;
            }
        }

        private void Awake()
        {
            Instance = this;
            seed = Random.value * 100f;
        }

        private void OnDisable()
        {
            // nunca deixar a camera deslocada ao desligar
            RemoveAppliedOffset();
            elapsed = duration;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // devolve a camera ao estado limpo ANTES do CameraFollow rodar
            RemoveAppliedOffset();
        }

        private void LateUpdate()
        {
            if (elapsed >= duration || amplitude <= 0f)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            float decay = 1f - t; // amortece ate zero
            float amp = Mathf.Min(amplitude, maxAmplitude) * decay * decay;

            float time = Time.unscaledTime * frequency;
            // Perlin centrado em zero -> tremor suave, sem "pulo" de random puro
            float x = (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(seed + 37f, time) - 0.5f) * 2f;

            appliedOffset = new Vector3(x, y, 0f) * amp;
            transform.position += appliedOffset;
        }

        private void RemoveAppliedOffset()
        {
            if (appliedOffset != Vector3.zero)
            {
                transform.position -= appliedOffset;
                appliedOffset = Vector3.zero;
            }
        }

        /// Dispara um shake. Nao acumula: mantem o mais forte entre o atual e o novo.
        public void Play(ShakePreset preset)
        {
            if (!ShakeEnabled || preset.amplitude <= 0f || preset.duration <= 0f)
            {
                return;
            }

            float remaining = Mathf.Max(0f, duration - elapsed);
            float currentStrength = remaining > 0f ? amplitude : 0f;
            if (preset.amplitude < currentStrength)
            {
                return; // ja existe um shake mais forte em andamento
            }

            amplitude = Mathf.Min(preset.amplitude, maxAmplitude);
            duration = preset.duration;
            frequency = Mathf.Max(1f, preset.frequency);
            elapsed = 0f;
        }

        public void PlayDamageShake() => Play(damage);
        public void PlayDoorImpactShake() => Play(doorImpact);
        public void PlayBridgeCollapseShake() => Play(bridgeCollapse);
        public void PlayRobotActivationShake() => Play(robotActivation);
        public void PlayTerminalPulseShake() => Play(terminalPulse);

        // Atalhos estaticos seguros (no-op se nao houver controlador na cena)
        public static void Damage() => Instance?.PlayDamageShake();
        public static void DoorImpact() => Instance?.PlayDoorImpactShake();
        public static void BridgeCollapse() => Instance?.PlayBridgeCollapseShake();
        public static void RobotActivation() => Instance?.PlayRobotActivationShake();
        public static void TerminalPulse() => Instance?.PlayTerminalPulseShake();
    }
}
