using UnityEngine;

namespace ProjectAurora.VFX
{
    /// Fachada unica de VFX de gameplay (Onda 1).
    ///
    /// Os sistemas existentes (PlayerHealth, SuitIntegrityRecovery, AuroraCoinCollectible,
    /// DataFileManager, PlayerInteraction) chamam os metodos ESTATICOS daqui. Se este
    /// objeto nao existir na cena, tudo vira no-op silencioso — nenhum sistema de gameplay
    /// quebra por falta de VFX (mesma filosofia do AuroraSfx).
    ///
    /// Nao duplica sistemas: nao toca vida, saldo, HUD nem audio. So dispara particulas
    /// e delega o shake ao AuroraCameraFeedbackController.
    [RequireComponent(typeof(AuroraVFXPool))]
    public sealed class AuroraVFXController : MonoBehaviour
    {
        public static AuroraVFXController Instance { get; private set; }

        [Header("Prefabs (opcionais — ausente = efeito ignorado)")]
        [Tooltip("Faiscas curtas no traje ao tomar dano.")]
        public GameObject playerDamage;
        [Tooltip("Energia ciano subindo pelo traje durante a recuperacao.")]
        public GameObject suitRecovery;
        [Tooltip("Burst de coleta — reutilizado por moeda e outros, configurado por cor/escala.")]
        public GameObject collectBurst;
        [Tooltip("Scan digital do DataFile (leitura, nao recompensa).")]
        public GameObject digitalScan;
        [Tooltip("Pulso do prompt de interacao E.")]
        public GameObject interactionPulse;
        [Tooltip("Faiscas curtas nos emissores quando um laser e desativado (Onda 2).")]
        public GameObject laserShutdown;
        [Tooltip("Poeira/vapor leve na abertura de portas (Onda 2).")]
        public GameObject doorDust;

        private AuroraVFXPool pool;
        private PlayerHealth boundHealth;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            pool = GetComponent<AuroraVFXPool>();
        }

        private void Start()
        {
            // StopAll na morte: efeitos vivos nao podem sobrar na cinematica de morte.
            // Restart de corrida recarrega a cena, entao o pool ja morre junto.
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.player != null)
            {
                boundHealth = gm.player.GetComponent<PlayerHealth>();
                if (boundHealth != null)
                {
                    boundHealth.OnDeath += HandlePlayerDeath;
                }
            }
        }

        private void HandlePlayerDeath()
        {
            StopAll();
        }

        private void OnDestroy()
        {
            if (boundHealth != null)
            {
                boundHealth.OnDeath -= HandlePlayerDeath;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private ParticleSystem Spawn(GameObject prefab, Vector3 pos, Transform parent)
        {
            if (prefab == null || pool == null)
            {
                return null;
            }
            return pool.Play(prefab, pos, Quaternion.identity, parent);
        }

        // ---- API estatica usada pelo gameplay ----

        /// Dano: faiscas no traje + shake discreto. Nao altera material permanentemente.
        public static void PlayerDamage(Vector3 position)
        {
            Instance?.Spawn(Instance.playerDamage, position, null);
            AuroraCameraFeedbackController.Damage();
        }

        /// Recuperacao do traje: efeito preso ao corpo (parent) para subir junto com ele.
        public static ParticleSystem SuitRecoveryStart(Transform body)
        {
            if (Instance == null || body == null)
            {
                return null;
            }
            return Instance.Spawn(Instance.suitRecovery, body.position, body);
        }

        /// Flash curto ao concluir um segmento de recuperacao. Reusa o burst de coleta
        /// (mesma familia visual ciano) em vez de um prefab so para isso.
        public static void SuitRecoveryComplete(Vector3 position)
        {
            Instance?.Spawn(Instance.collectBurst, position, null);
        }

        /// Coleta de AuroraCoin.
        public static void CoinCollect(Vector3 position)
        {
            Instance?.Spawn(Instance.collectBurst, position, null);
        }

        /// Coleta de DataFile — efeito proprio, diferente da moeda.
        public static void DataFileCollect(Vector3 position)
        {
            Instance?.Spawn(Instance.digitalScan, position, null);
        }

        /// Confirmacao ao pressionar E.
        public static void InteractionConfirm(Vector3 position)
        {
            Instance?.Spawn(Instance.interactionPulse, position, null);
        }

        /// Faiscas nos emissores ao desativar um laser (Onda 2).
        public static void LaserShutdown(Vector3 position)
        {
            Instance?.Spawn(Instance.laserShutdown, position, null);
        }

        /// Poeira leve na abertura de porta (Onda 2).
        public static void DoorOpen(Vector3 position)
        {
            Instance?.Spawn(Instance.doorDust, position, null);
        }

        /// Recolhe todos os efeitos vivos (morte, restart, troca de estado).
        public static void StopAll()
        {
            Instance?.pool?.ReleaseAll();
        }

        public static int ActiveEffects => Instance != null && Instance.pool != null ? Instance.pool.ActiveCount : 0;
    }
}
