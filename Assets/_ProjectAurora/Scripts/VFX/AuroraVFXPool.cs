using System.Collections.Generic;
using UnityEngine;

namespace ProjectAurora.VFX
{
    /// Pool simples de efeitos one-shot (Etapa 25 / Onda 1).
    ///
    /// Por que existe: ha centenas de AuroraCoins na cena. Instanciar/destruir um efeito por
    /// coleta geraria lixo constante durante a corrida. O pool reusa por prefab.
    ///
    /// Escopo deliberadamente pequeno: sem generics elaborados, sem interfaces, sem
    /// corrotinas por instancia. Um Update varre os ativos e devolve os que acabaram.
    public sealed class AuroraVFXPool : MonoBehaviour
    {
        public static AuroraVFXPool Instance { get; private set; }

        [Tooltip("Quantas instancias pre-criar por prefab na primeira vez que ele e usado.")]
        public int initialPerPrefab = 4;
        [Tooltip("Teto de instancias vivas por prefab (evita runaway).")]
        public int maxPerPrefab = 24;

        private readonly Dictionary<GameObject, Queue<ParticleSystem>> idle =
            new Dictionary<GameObject, Queue<ParticleSystem>>();
        private readonly Dictionary<GameObject, int> created = new Dictionary<GameObject, int>();
        private readonly List<Active> active = new List<Active>();

        private struct Active
        {
            public ParticleSystem ps;
            public GameObject prefab;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// Toca um efeito no mundo. Devolve null se o prefab for invalido ou o teto foi atingido.
        public ParticleSystem Play(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            ParticleSystem ps = Rent(prefab);
            if (ps == null)
            {
                return null;
            }

            Transform t = ps.transform;
            t.SetParent(parent, true);
            t.SetPositionAndRotation(position, rotation);
            ps.gameObject.SetActive(true);
            ps.Clear(true);
            ps.Play(true);
            active.Add(new Active { ps = ps, prefab = prefab });
            return ps;
        }

        private ParticleSystem Rent(GameObject prefab)
        {
            if (!idle.TryGetValue(prefab, out Queue<ParticleSystem> queue))
            {
                queue = new Queue<ParticleSystem>();
                idle[prefab] = queue;
                created[prefab] = 0;
                for (int i = 0; i < initialPerPrefab; i++)
                {
                    ParticleSystem pre = Create(prefab);
                    if (pre != null)
                    {
                        queue.Enqueue(pre);
                    }
                }
            }

            if (queue.Count > 0)
            {
                return queue.Dequeue();
            }

            if (created[prefab] >= maxPerPrefab)
            {
                return null; // teto: prefere perder um efeito a estourar memoria
            }

            return Create(prefab);
        }

        private ParticleSystem Create(GameObject prefab)
        {
            GameObject go = Instantiate(prefab, transform);
            go.SetActive(false);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = go.GetComponentInChildren<ParticleSystem>(true);
            }
            if (ps == null)
            {
                Debug.LogWarning("AuroraVFXPool: prefab sem ParticleSystem: " + prefab.name);
                Destroy(go);
                return null;
            }
            created[prefab] = created.TryGetValue(prefab, out int c) ? c + 1 : 1;
            return ps;
        }

        private void Update()
        {
            // varre de tras para frente para poder remover no meio
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Active a = active[i];
                if (a.ps == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (!a.ps.IsAlive(true))
                {
                    Release(a);
                    active.RemoveAt(i);
                }
            }
        }

        private void Release(Active a)
        {
            a.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            a.ps.gameObject.SetActive(false);
            a.ps.transform.SetParent(transform, false);
            if (idle.TryGetValue(a.prefab, out Queue<ParticleSystem> queue))
            {
                queue.Enqueue(a.ps);
            }
        }

        /// Interrompe e recolhe tudo (troca de estado, restart de corrida, morte).
        public void ReleaseAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].ps != null)
                {
                    Release(active[i]);
                }
            }
            active.Clear();
        }

        public int ActiveCount => active.Count;
    }
}
