using UnityEngine;

/// Anti-softlock de portas (Round 14).
/// Se o jogador ficar preso (autorun ligado mas sem avancar em Z) por alguns segundos
/// contra uma porta interativa fechada que ele nao abriu com E, a porta e "arrombada":
/// leva 1 de dano e a porta abre, para o jogo nunca travar infinitamente.
public class SoftlockDoorGuard : MonoBehaviour
{
    [Tooltip("Tempo parado (sem avancar em Z) antes de arrombar a porta.")]
    public float stuckSeconds = 2.5f;
    [Tooltip("Avanco minimo em Z considerado 'progresso'.")]
    public float progressEpsilon = 0.3f;
    [Tooltip("Distancia a frente para procurar a porta bloqueando.")]
    public float lookAheadZ = 5f;
    [Tooltip("Rearma o guard apos arrombar (evita dano repetido na mesma porta).")]
    public float rearmSeconds = 3f;

    private float lastZ;
    private float stuckTimer;
    private float rearmTimer;
    private bool initialized;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || gm.player == null)
        {
            initialized = false;
            stuckTimer = 0f;
            return;
        }

        if (rearmTimer > 0f)
        {
            rearmTimer -= Time.deltaTime;
        }

        float z = gm.player.transform.position.z;
        if (!initialized)
        {
            lastZ = z;
            stuckTimer = 0f;
            initialized = true;
            return;
        }

        // so conta como "preso" quando deveria estar correndo
        bool shouldMove = gm.player.IsAutoRunning;
        if (!shouldMove || z - lastZ > progressEpsilon)
        {
            stuckTimer = 0f;
            lastZ = z;
            return;
        }

        stuckTimer += Time.deltaTime;
        lastZ = Mathf.Max(lastZ, z);

        if (stuckTimer >= stuckSeconds && rearmTimer <= 0f)
        {
            if (TryBreachDoorAhead(gm, z))
            {
                stuckTimer = 0f;
                rearmTimer = rearmSeconds;
            }
        }
    }

    private bool TryBreachDoorAhead(GameManager gm, float playerZ)
    {
        AuroraDoorController nearest = null;
        float bestDz = float.MaxValue;

        foreach (AuroraDoorController door in
            FindObjectsByType<AuroraDoorController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            // portas legacy/desativadas nunca devem ser arrombadas (coroutine em objeto
            // inativo falha e o jogador leva dano de uma porta que nem existe em jogo)
            if (door == null || !door.gameObject.activeInHierarchy || door.IsOpen)
            {
                continue;
            }

            float dz = door.transform.position.z - playerZ;
            // porta logo a frente (ou praticamente encostada) na direcao da corrida
            if (dz > -2f && dz < lookAheadZ && dz < bestDz)
            {
                bestDz = dz;
                nearest = door;
            }
        }

        if (nearest == null)
        {
            return false;
        }

        // arromba: dano + abre para liberar o caminho
        gm.DamagePlayer();
        nearest.SetLocked(false);
        nearest.Open();
        Debug.Log("[SoftlockGuard] Porta arrombada apos ficar preso: " + nearest.name);
        return true;
    }
}
