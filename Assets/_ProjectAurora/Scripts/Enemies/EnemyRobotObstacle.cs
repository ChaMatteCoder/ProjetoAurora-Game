using UnityEngine;

/// Robo-OBSTACULO da gameplay (Round 6). Diferente do perseguidor: ocupa uma faixa, tem
/// collider de dano e usa o sistema de dano existente via o componente `Obstacle` (nao cria
/// sistema paralelo). Anima com a passada mecanica procedural em cadencia normal. Marcador
/// de configuracao — garante collider ligado e cadencia coerente. Opcionalmente patrulha
/// lateralmente de forma curta e segura (dentro da propria faixa).
[RequireComponent(typeof(Obstacle))]
public class EnemyRobotObstacle : MonoBehaviour
{
    [Tooltip("Cadencia da passada (normal/levemente lenta para obstaculo).")]
    public float cadence = 3.2f;

    [Header("Patrulha lateral opcional (dentro da propria faixa)")]
    public bool patrol = false;
    public float patrolAmplitude = 0.6f;
    public float patrolSpeed = 1.2f;

    private EnemyRobotProceduralAnimator proc;
    private Vector3 basePos;
    private float phase;

    private void Awake()
    {
        proc = GetComponentInChildren<EnemyRobotProceduralAnimator>();
        if (proc != null)
        {
            proc.SetCadence(cadence);
        }

        // collider de dano precisa estar ativo (o robo perseguidor desliga; aqui e o oposto)
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col.isTrigger)
            {
                col.enabled = true;
            }
        }

        basePos = transform.position;
        phase = Random.value * 6f;
    }

    private void Update()
    {
        if (!patrol)
        {
            return;
        }

        float x = Mathf.Sin(Time.time * patrolSpeed + phase) * patrolAmplitude;
        transform.position = new Vector3(basePos.x + x, basePos.y, basePos.z);
    }
}
