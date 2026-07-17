using System;
using UnityEngine;

namespace ProjectAurora.VFX
{
    /// Liga/desliga emissores ambientais conforme o Z do player (Onda 2 / Etapa 26).
    ///
    /// Regra do projeto: nenhum VFX ambiental roda desde o frame 0. Cada zona define
    /// uma faixa de Z; quando o player entra (com margem), os ParticleSystems da zona
    /// dao Play; quando sai, StopEmitting — as particulas ja emitidas morrem sozinhas
    /// e o sistema fica dormente (0 sistemas tocando fora da zona ativa).
    ///
    /// Sem FindObjectOfType por frame: o player e cacheado; a varredura e O(zonas).
    public sealed class AuroraSectorVFXController : MonoBehaviour
    {
        [Serializable]
        public sealed class Zone
        {
            public string label = "Setor";
            public float zStart;
            public float zEnd;
            public ParticleSystem[] emitters;
            [NonSerialized] public bool active;
        }

        [Tooltip("Margem (m) antes/depois da faixa em que a zona ja liga/ainda fica ligada.")]
        public float margin = 60f;
        public Zone[] zones = new Zone[0];

        private Transform player;
        private float nextScanAt;

        private void OnEnable()
        {
            // estado inicial coerente: tudo parado
            foreach (Zone z in zones)
            {
                SetZone(z, false, true);
            }
        }

        private void Update()
        {
            // 5 Hz e suficiente para faixas de 450m (player anda ~8 m/s)
            if (Time.time < nextScanAt)
            {
                return;
            }
            nextScanAt = Time.time + 0.2f;

            if (player == null)
            {
                GameManager gm = GameManager.Instance;
                if (gm == null || gm.player == null)
                {
                    return;
                }
                player = gm.player.transform;
            }

            float z = player.position.z;
            foreach (Zone zone in zones)
            {
                bool inside = z >= zone.zStart - margin && z <= zone.zEnd + margin;
                if (inside != zone.active)
                {
                    SetZone(zone, inside, false);
                }
            }
        }

        private static void SetZone(Zone zone, bool on, bool clear)
        {
            zone.active = on;
            if (zone.emitters == null)
            {
                return;
            }

            foreach (ParticleSystem ps in zone.emitters)
            {
                if (ps == null)
                {
                    continue;
                }

                if (on)
                {
                    if (!ps.isPlaying)
                    {
                        ps.Play(true);
                    }
                }
                else
                {
                    // fora da zona: para de emitir; particulas vivas terminam sozinhas.
                    ps.Stop(true, clear
                        ? ParticleSystemStopBehavior.StopEmittingAndClear
                        : ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        /// Quantas zonas estao ativas agora (para testes/telemetria).
        public int ActiveZoneCount
        {
            get
            {
                int n = 0;
                foreach (Zone z in zones)
                {
                    if (z.active) n++;
                }
                return n;
            }
        }
    }
}
