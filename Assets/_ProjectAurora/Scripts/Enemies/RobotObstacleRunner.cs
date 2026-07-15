using UnityEngine;

/// Robô-obstáculo da Sala de Máquinas: fica parado até o Dr. Elias se aproximar,
/// então corre em linha reta CONTRA a direção da corrida (−Z), travado na própria
/// lane (x fixo), com a animação Walking. Para de correr/animar quando sai da
/// câmera (passa alguns metros atrás do player).
///
/// NÃO confundir com os robôs de perseguição (RobotPursuitDirector) — estes aqui
/// são obstáculos: o dano vem do trigger + Obstacle no mesmo GameObject, e o
/// Rigidbody kinematic garante eventos de trigger confiáveis em movimento.
[RequireComponent(typeof(Obstacle))]
public class RobotObstacleRunner : MonoBehaviour
{
    [Tooltip("Distância (m) do player em que o robô 'liga' e começa a andar.")]
    public float activationDistance = 42f;
    [Tooltip("Velocidade de caminhada (m/s). Ritmo de caminhada, não corrida.")]
    public float runSpeed = 2.2f;
    [Tooltip("Velocidade natural do clipe Walking a speed=1 (medida: ~1.34 m/s).")]
    public float naturalWalkSpeed = 1.34f;
    [Tooltip("Nome do estado do Animator com o clipe Walking.")]
    public string walkStateName = "Walking";
    [Tooltip("Metros atrás do player para considerar 'saiu da câmera' e parar.")]
    public float despawnBehind = 6f;

    private Animator animator;
    private Rigidbody body;
    private Transform player;
    private bool running;
    private bool finished;
    private float fixedX, fixedY;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        body = GetComponent<Rigidbody>();
        fixedX = transform.position.x;
        fixedY = transform.position.y;
        // sem isso o Animator culled fora da tela congela em pose neutra
        if (animator != null) animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void Update()
    {
        if (finished) return;
        if (player == null)
        {
            var go = GameObject.Find("Dr. Elias - Player");
            if (go == null) return;
            player = go.transform;
        }

        float dz = transform.position.z - player.position.z;

        if (!running)
        {
            bool playing = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
            if (playing && dz > 0f && dz <= activationDistance)
            {
                running = true;
                // NÃO re-rotacionar: o robô já é colocado encarando o player (-Z) na
                // edição; o RobotVisual filho tem a orientação correta. Girar aqui o
                // virava de costas (dupla rotação).
                if (animator != null)
                {
                    // casa a cadência da caminhada com a velocidade real -> sem deslize de pés
                    animator.speed = naturalWalkSpeed > 0.01f ? runSpeed / naturalWalkSpeed : 1f;
                    animator.CrossFadeInFixedTime(walkStateName, 0.15f);
                }
            }
            return;
        }

        // linha reta na própria lane, contra a corrida — nunca muda de x
        Vector3 pos = transform.position + Vector3.back * (runSpeed * Time.deltaTime);
        pos.x = fixedX;
        pos.y = fixedY;
        if (body != null && body.isKinematic) body.MovePosition(pos);
        else transform.position = pos;

        if (dz < -despawnBehind)
        {
            finished = true;
            running = false;
            if (animator != null) animator.speed = 0f;
        }
    }
}
