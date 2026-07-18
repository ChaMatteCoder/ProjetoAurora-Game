using System.Collections;
using UnityEngine;

/// Cinemática breve na ENTRADA do Setor E (Ponte Técnica): a câmera sobe e se
/// afasta, inclinando para o céu aberto — o primeiro contato do jogador com o
/// espaço (o casco tampa o céu nos setores anteriores; aqui é a revelação).
///
/// Decisões de design:
/// - O Dr. Elias CONTINUA correndo (câmera-only): não interrompe a perseguição
///   nem cria dead-stop; os robôs perseguidores são 100% visuais.
/// - Invulnerabilidade externa durante a tomada: a pista sai do quadro por ~3s,
///   então o jogador não pode ser punido por um obstáculo que não vê.
/// - Mesmo padrão de câmera do RobotPursuitDirector (desliga CameraFollow,
///   anima, religa) — nenhum sistema novo de câmera.
public class AuroraBridgeRevealCinematic : MonoBehaviour
{
    [Tooltip("Z em que a cinemática dispara (entrada da Ponte Técnica).")]
    public float triggerZ = 1806f;
    [Tooltip("Subida/afastamento da câmera (s).")]
    public float riseTime = 2.2f;
    [Tooltip("Contemplação do céu (s).")]
    public float holdTime = 1.4f;
    [Tooltip("Retorno ao enquadramento de corrida (s).")]
    public float returnTime = 1.2f;
    [Tooltip("Altura extra da câmera no pico da tomada.")]
    public float riseHeight = 3.5f;
    [Tooltip("Recuo extra atrás do player no pico.")]
    public float pullBack = 3.0f;

    private bool played;
    private Camera cam;
    private CameraFollow follow;

    private void Update()
    {
        if (played)
        {
            enabled = false;
            return;
        }

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null)
        {
            return;
        }
        if (gm.State != GameState.Playing)
        {
            return; // só em corrida livre (nunca em cutscene/tutorial/morte)
        }
        if (gm.player.transform.position.z < triggerZ)
        {
            return;
        }

        played = true;
        StartCoroutine(RevealRoutine(gm));
    }

    private IEnumerator RevealRoutine(GameManager gm)
    {
        cam = Camera.main;
        follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        Transform player = gm.player.transform;
        var health = gm.player.GetComponent<PlayerHealth>();
        if (cam == null || follow == null)
        {
            yield break;
        }

        health?.SetExternalInvulnerability(true);
        follow.enabled = false;

        // Redesign (feedback do cliente): tomada COMPOSTA, nao "ceu na cara".
        // O alvo do olhar desliza do enquadramento normal de corrida para um ponto
        // a frente e moderadamente acima: o Dr. Elias e a ponte permanecem no terco
        // inferior do quadro enquanto o ceu preenche o topo. Tudo em ease suave e
        // relativo ao player (que segue correndo) — sem cortes, sem saltos.
        float t = 0f;
        while (t < riseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / riseTime);
            Vector3 pp = player.position;
            // posicao: do offset normal do follow para um pico suave acima/atras
            Vector3 basePos = pp + follow.offset;
            Vector3 peakPos = pp + follow.offset + new Vector3(0f, riseHeight, -pullBack);
            cam.transform.position = Vector3.Lerp(basePos, peakPos, k);
            // olhar: do alvo normal de corrida para um ponto a frente, pouco acima do
            // horizonte — a nebulosa do skybox vive perto do horizonte (mapeamento
            // 180°); mirar alto demais cai na borda esmagada da textura (cinza).
            Vector3 baseLook = pp + follow.lookOffset;
            Vector3 skyLook = pp + new Vector3(0f, 7.5f, 30f);
            Vector3 look = Vector3.Lerp(baseLook, skyLook, k);
            cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position);
            yield return null;
        }

        // contemplacao: mantem a composicao acompanhando o player, com um drift
        // vertical bem sutil para a tomada respirar
        t = 0f;
        while (t < holdTime)
        {
            t += Time.deltaTime;
            float drift = Mathf.Sin(t * 0.8f) * 0.25f;
            Vector3 pp = player.position;
            cam.transform.position = pp + follow.offset + new Vector3(0f, riseHeight + drift, -pullBack);
            Vector3 skyLook = pp + new Vector3(0f, 7.5f + drift * 2f, 30f);
            cam.transform.rotation = Quaternion.LookRotation(skyLook - cam.transform.position);
            yield return null;
        }

        // retorno suave ao enquadramento de corrida
        t = 0f;
        Vector3 fromOffset = new Vector3(0f, riseHeight, -pullBack);
        while (t < returnTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / returnTime);
            Vector3 pp = player.position;
            // interpola o OFFSET (nao posicoes absolutas): zero jitter com o player em movimento
            Vector3 offset = Vector3.Lerp(fromOffset, Vector3.zero, k);
            cam.transform.position = pp + follow.offset + offset;
            Vector3 skyLook = pp + new Vector3(0f, 7.5f, 30f);
            Vector3 baseLook = pp + follow.lookOffset;
            Vector3 look = Vector3.Lerp(skyLook, baseLook, k);
            cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position);
            yield return null;
        }

        follow.enabled = true;
        health?.SetExternalInvulnerability(false);
    }
}
