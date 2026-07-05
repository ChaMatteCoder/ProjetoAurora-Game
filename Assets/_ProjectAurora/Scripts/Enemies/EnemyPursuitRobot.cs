using UnityEngine;

/// Robo PERSEGUIDOR visual (Round 6). NAO tem dano, NAO tem collider solido, NAO usa
/// NavMesh nem fisica. E um "replay atrasado" do movimento do player: o RobotPursuitDirector
/// amostra a posicao que o player tinha 'delay' segundos atras e posiciona este robo ali,
/// com um recuo em Z e um pequeno offset de formacao. Assim ele reproduz mudanca de faixa
/// e pulo com atraso, sempre atras do player, sem nunca colidir com obstaculos.
public class EnemyPursuitRobot : MonoBehaviour
{
    [Tooltip("Atraso (s) com que este robo repete o caminho do player.")]
    public float delay = 0.6f;
    [Tooltip("Recuo adicional em Z atras da amostra (mundo).")]
    public float backOffset = 3f;
    [Tooltip("Offset lateral de formacao (mundo).")]
    public float lateralOffset = 0f;
    public float verticalOffset = 0f;
    [Tooltip("Suavizacao do movimento (maior = mais firme).")]
    public float followSharpness = 14f;

    [Tooltip("Perseguidor lider: mantido visivel na camera (mais perto, com clamp de distancia).")]
    public bool isLeadPursuer;

    public EnemyRobotProceduralAnimator ProceduralAnimator { get; private set; }
    public Animator CachedAnimator { get; private set; }

    private bool initialized;
    private bool airborne;

    private void Awake()
    {
        ProceduralAnimator = GetComponentInChildren<EnemyRobotProceduralAnimator>();
        CachedAnimator = GetComponentInChildren<Animator>();
        // garante que nenhum collider deste perseguidor participe da gameplay
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }
    }

    /// Chamado pelo director a cada frame com a posicao-alvo (ja atrasada + offsets).
    public void ApplyTarget(Vector3 targetPosition, float deltaTime)
    {
        if (!initialized)
        {
            transform.position = targetPosition;
            initialized = true;
            return;
        }

        float k = 1f - Mathf.Exp(-followSharpness * deltaTime); // suavizacao estavel por frame
        transform.position = Vector3.Lerp(transform.position, targetPosition, k);
        // sempre olhando na direcao da corrida (+Z): vemos as costas do robo
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    /// Replica o pulo do player (chamado pelo director com o Y da amostra atrasada).
    public void ApplyAirborneState(float sampleY)
    {
        bool nowAirborne = sampleY > 0.3f;
        if (nowAirborne == airborne || CachedAnimator == null)
        {
            airborne = nowAirborne;
            return;
        }

        airborne = nowAirborne;
        if (airborne)
        {
            CachedAnimator.SetTrigger("Jump");
            CachedAnimator.SetBool("IsJumping", true);
        }
        else
        {
            CachedAnimator.SetBool("IsJumping", false);
        }
    }

    public void ResetInitialization()
    {
        initialized = false;
    }
}
