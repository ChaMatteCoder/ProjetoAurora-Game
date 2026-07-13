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
    [Tooltip("Distância (m) do player em que o robô 'liga' e começa a correr.")]
    public float activationDistance = 42f;
    public float runSpeed = 5.5f;
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
                transform.rotation = Quaternion.LookRotation(Vector3.back); // encara o player
                if (animator != null)
                {
                    animator.speed = 1f;
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
