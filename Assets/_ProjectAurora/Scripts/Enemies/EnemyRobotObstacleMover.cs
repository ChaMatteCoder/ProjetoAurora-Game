using UnityEngine;

/// Movimento para frente dos robos-obstaculo (Round 7).
/// Kinematic/scripted (sem Rigidbody/NavMesh): avanca no +Z em velocidade menor que a do
/// player (o player alcanca e desvia — previsivel e justo). O collider de dano esta no
/// proprio root, entao acompanha o visual automaticamente.
/// Tambem e o controlador de performance do robo: Animator e movimento so ficam ativos
/// quando o player esta proximo; ao ficar para tras, tudo desliga.
public class EnemyRobotObstacleMover : MonoBehaviour
{
    [Tooltip("Velocidade de avanco (menor que a do player: 8-16).")]
    public float moveSpeed = 2.2f;
    [Tooltip("Avanco maximo a partir da posicao inicial (0 = sem limite de trecho).")]
    public float maxTravel = 45f;
    [Tooltip("Ativa quando o player esta a menos que isto ATRAS do robo.")]
    public float activateDistance = 70f;
    [Tooltip("Desativa quando o player ja passou isto A FRENTE do robo.")]
    public float deactivateBehind = 20f;
    public bool keepLaneX = true;

    private Vector3 startPos;
    private float laneX;
    private Animator anim;
    private bool moving;
    private Transform playerT;

    private void Awake()
    {
        startPos = transform.position;
        laneX = startPos.x;
        anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            playerT = GameManager.Instance.player.transform;
        }
        SetActiveState(false);
    }

    private void Update()
    {
        if (playerT == null)
        {
            return;
        }

        float playerZ = playerT.position.z;
        float myZ = transform.position.z;

        // janela de atividade em torno do player
        bool shouldBeActive = playerZ > myZ - activateDistance && playerZ < myZ + deactivateBehind;
        if (shouldBeActive != moving)
        {
            SetActiveState(shouldBeActive);
        }

        if (!moving)
        {
            return;
        }

        // avanco limitado ao trecho
        float traveled = myZ - startPos.z;
        if (maxTravel <= 0f || traveled < maxTravel)
        {
            Vector3 p = transform.position + Vector3.forward * (moveSpeed * Time.deltaTime);
            if (keepLaneX)
            {
                p.x = laneX;
            }
            p.y = startPos.y;
            transform.position = p;
        }
    }

    private void SetActiveState(bool active)
    {
        moving = active;
        if (anim != null && anim.enabled != active)
        {
            anim.enabled = active;
        }
    }
}
