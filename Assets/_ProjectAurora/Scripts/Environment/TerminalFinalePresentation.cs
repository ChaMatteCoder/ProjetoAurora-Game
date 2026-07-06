using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Encenação cinematográfica do clímax no Terminal Central (Round 15).
/// Prelúdio: revela painel e núcleo. Depois, robôs ENTRAM no terminal e se aproximam —
/// a ameaça é sugerida por enquadramento (pés, sombras, garra, luz vermelha, terminal
/// piscando). NUNCA mostra o Dr. Elias sendo pego (os robôs param a alguns metros).
public class TerminalFinalePresentation : MonoBehaviour
{
    public Transform panelShot;
    public Transform panelFocus;
    public Transform coreShot;
    public Transform coreFocus;
    public GameObject corruptionLayer;
    public Light[] corruptedLights;
    public float panelMoveDuration = 0.85f;
    public float coreRevealDuration = 1.25f;

    [Header("Robôs se aproximando (Round 15, retimed Round 16)")]
    public GameObject robotPrefab;
    public Transform playerAnchor;           // referencia da posicao do Dr. Elias (console)
    public float robotStartZ = 2570f;        // Round 16: entram PELA porta da perseguicao (z2566)
    public float robotStopZ = 2668f;         // chegam a ~4m do console apenas no climax
    public float robotApproachDuration = 16f; // legado (nao usado no modo sincronizado)
    public Light[] redAlertLights;

    [Header("Sincronizacao com o dialogo (Round 16)")]
    [Tooltip("Os robos so chegam quando esta fala comeca ('Nao...').")]
    public string climaxVoiceId = "ELI_010";
    [Tooltip("Ate o climax, os robos rondam lentamente ate este z (nunca alem).")]
    public float stalkZ = 2640f;
    public float stalkSpeed = 0.55f;
    public float climaxRushSeconds = 3.0f;

    private Camera cam;
    private readonly List<Transform> robots = new List<Transform>();
    private readonly List<Animator> robotAnimators = new List<Animator>();

    public IEnumerator PlayPrelude()
    {
        if (corruptionLayer != null)
        {
            corruptionLayer.SetActive(true);
        }
        foreach (Light sceneLight in corruptedLights)
        {
            if (sceneLight != null) sceneLight.enabled = true;
        }

        cam = Camera.main;
        if (cam == null)
        {
            yield break;
        }
        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = false;

        if (panelShot != null && panelFocus != null)
        {
            yield return MoveCamera(cam, panelShot.position, panelFocus.position, panelMoveDuration);
        }
        yield return new WaitForSecondsRealtime(0.2f);
        if (coreShot != null && coreFocus != null)
        {
            yield return MoveCamera(cam, coreShot.position, coreFocus.position, coreRevealDuration);
        }
    }

    /// Inicia (fire-and-forget) a aproximação dos robôs sincronizada com o diálogo final:
    /// eles ENTRAM pela porta da perseguição (que se abre), rondam lentamente durante as
    /// falas e SÓ chegam ao Dr. Elias quando o "Não..." (climaxVoiceId) começa.
    public void BeginRobotApproach()
    {
        StartCoroutine(RobotApproachRoutine());
    }

    private IEnumerator RobotApproachRoutine()
    {
        if (cam == null) cam = Camera.main;
        Vector3 anchor = playerAnchor != null ? playerAnchor.position : new Vector3(0f, 0f, 2675f);

        // Round 16: liga as luzes vermelhas do climax e ABRE a porta da perseguicao —
        // e por ela que os robos (barrados antes) entram; nada de porta fechada as costas.
        if (redAlertLights != null)
        {
            foreach (Light l in redAlertLights)
            {
                if (l != null) l.enabled = true;
            }
        }
        yield return OpenPursuitGate();

        SpawnRobots(anchor);
        SetRobotsRun(0.55f);

        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        int shot = 0;
        float shotTimer = 0f;
        const float shotLength = 3.4f;
        float graceTimer = 0f;
        float robotZ = robotStartZ;

        // ===== FASE 1: RONDA — avanco lento durante as falas, ate stalkZ no maximo =====
        while (true)
        {
            string line = voice != null && voice.IsPlaying ? voice.CurrentLineId : string.Empty;
            if (line == climaxVoiceId)
            {
                break; // "Nao..." comecou -> investida final
            }
            if (voice == null || (!voice.IsPlaying && graceTimer > 2f))
            {
                break; // dialogo acabou/fallback de texto: nao prende a cena
            }
            graceTimer = voice != null && voice.IsPlaying ? 0f : graceTimer + Time.unscaledDeltaTime;

            robotZ = Mathf.Min(stalkZ, robotZ + stalkSpeed * Time.unscaledDeltaTime);
            PositionRobots(robotZ);
            bool moving = robotZ < stalkZ - 0.05f;
            SetRobotsRun(moving ? 0.55f : 0f); // parados de fato quando aguardam (sem andar no lugar)

            PulseRedLights();
            Vector3 leadPos = robots.Count > 0 && robots[0] != null ? robots[0].position : new Vector3(0f, 0f, robotZ);
            ApplyThreatShot(shot, leadPos, anchor);
            shotTimer += Time.unscaledDeltaTime;
            if (shotTimer >= shotLength)
            {
                shotTimer = 0f;
                shot = (shot + 1) % 4;
            }
            yield return null;
        }

        // ===== FASE 2: CLIMAX — investida ate o Dr. Elias durante o "Nao..." =====
        SetRobotsRun(1.1f);
        float rush = Mathf.Max(1.2f, climaxRushSeconds);
        float elapsed = 0f;
        float rushFrom = robotZ;
        while (elapsed < rush)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / rush);
            robotZ = Mathf.Lerp(rushFrom, robotStopZ, t * t); // ease-in: aceleram
            PositionRobots(robotZ);
            PulseRedLights();
            ApplyCensuraShot(anchor);
            yield return null;
        }

        // segura o plano-censura (robos diante da "visao" dele) ate o fim
        SetRobotsRun(0f);
        float hold = 0f;
        while (hold < 2.5f)
        {
            hold += Time.unscaledDeltaTime;
            PulseRedLights();
            ApplyCensuraShot(anchor);
            yield return null;
        }
    }

    /// Abre a porta da perseguicao (TerminalContainmentGate) — os robos entram por ela.
    private IEnumerator OpenPursuitGate()
    {
        GameObject slabGo = GameObject.Find("TerminalContainmentGate/Gate_Slab");
        if (slabGo == null)
        {
            var gate = GameObject.Find("TerminalContainmentGate");
            if (gate != null)
            {
                foreach (Transform c in gate.transform)
                {
                    if (c.name.Contains("Slab")) { slabGo = c.gameObject; break; }
                }
            }
        }
        if (slabGo == null)
        {
            yield break;
        }

        Vector3 closed = slabGo.transform.position;
        Vector3 open = closed + Vector3.up * 5.4f;
        float elapsed = 0f;
        const float dur = 1.2f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            slabGo.transform.position = Vector3.Lerp(closed, open, Mathf.SmoothStep(0f, 1f, elapsed / dur));
            yield return null;
        }
        slabGo.transform.position = open;
    }

    private void PositionRobots(float z)
    {
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] == null) continue;
            Vector3 p = robots[i].position;
            robots[i].position = new Vector3(p.x, p.y, z - i * 1.6f);
        }
    }

    private void SetRobotsRun(float speed)
    {
        foreach (Animator anim in robotAnimators)
        {
            if (anim == null) continue;
            anim.SetBool("IsRunning", speed > 0.01f);
            anim.speed = Mathf.Max(0.01f, speed);
        }
    }

    private void PulseRedLights()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
        if (redAlertLights != null)
        {
            foreach (Light l in redAlertLights)
            {
                if (l != null) l.intensity = Mathf.Lerp(1.2f, 3.2f, pulse);
            }
        }
    }

    /// Enquadramentos de ameaça durante a ronda (nunca mostram o Dr. Elias):
    /// 0 = pés/base avançando · 1 = silhueta na luz vermelha · 2 = garra/braço ·
    /// 3 = corredor com a porta aberta ao fundo (por onde entraram).
    private void ApplyThreatShot(int shot, Vector3 robotPos, Vector3 anchor)
    {
        if (cam == null) return;
        Vector3 pos; Vector3 look;
        switch (shot)
        {
            case 0:
                pos = robotPos + new Vector3(1.6f, 0.35f, -2.6f);
                look = robotPos + new Vector3(0f, 0.2f, 0.5f);
                break;
            case 1:
                pos = robotPos + new Vector3(-1.8f, 1.1f, 3.2f);
                look = robotPos + new Vector3(0f, 1.4f, 0f);
                break;
            case 2:
                pos = robotPos + new Vector3(0.9f, 1.7f, 1.3f);
                look = robotPos + new Vector3(0.2f, 1.6f, -0.2f);
                break;
            default: // corredor: robos vindo com a porta ABERTA ao fundo (olha para -z)
                pos = robotPos + new Vector3(3.2f, 1.3f, 7.0f);
                look = robotPos + new Vector3(0f, 1.2f, -2f);
                break;
        }
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation(look - pos);
    }

    /// Plano-censura do climax: camera NA posicao/olhar do Dr. Elias (POV) — os robos se
    /// aproximam da lente; ele nunca aparece em quadro quando chegam.
    private void ApplyCensuraShot(Vector3 anchor)
    {
        if (cam == null) return;
        Vector3 pos = anchor + new Vector3(0f, 1.55f, 0.35f);
        Vector3 look = robots.Count > 0 && robots[0] != null
            ? robots[0].position + Vector3.up * 1.5f
            : anchor + new Vector3(0f, 1.4f, -6f);
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation(look - pos);
    }

    private void SpawnRobots(Vector3 anchor)
    {
        if (robotPrefab == null)
        {
            return; // sem prefab: a sequência de câmera ainda cobre luz/terminal
        }

        float[] lanes = { 0f, -2.6f, 2.6f };
        for (int i = 0; i < 3; i++)
        {
            GameObject go = Instantiate(robotPrefab, new Vector3(lanes[i], 0f, robotStartZ - i * 1.4f),
                Quaternion.identity);
            go.name = "FinaleRobot_" + i;
            // visual apenas: desliga qualquer collider para nao empurrar o player
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }
            robots.Add(go.transform);
            Animator anim = go.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsRunning", true);
                robotAnimators.Add(anim);
            }
        }
    }

    private static IEnumerator MoveCamera(Camera gameplayCamera, Vector3 shotPos, Vector3 focus, float duration)
    {
        Vector3 startPosition = gameplayCamera.transform.position;
        Quaternion startRotation = gameplayCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(focus - shotPos);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.05f, duration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / safeDuration);
            gameplayCamera.transform.position = Vector3.Lerp(startPosition, shotPos, t);
            gameplayCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        gameplayCamera.transform.SetPositionAndRotation(shotPos, targetRotation);
    }
}
