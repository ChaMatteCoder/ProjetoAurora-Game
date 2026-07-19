using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Diretor da perseguicao por robos (Round 6).
/// - Ativa quando o player entra na Sala de Maquinas (pursuitStartZ), com cutscene curta
///   mostrando os robos surgindo atras.
/// - Robos perseguidores sao VISUAIS (EnemyPursuitRobot, sem colliders/dano): cada um
///   reproduz a posicao que o player tinha 'delay' segundos atras (ring buffer de historico)
///   + recuo em Z + offset de formacao. Assim imitam mudanca de faixa e pulo com atraso e
///   NUNCA colidem com obstaculos (repetem um caminho que ja provou ser livre).
/// - Termina no corredor do Terminal Central (pursuitEndZ): porta de contencao fecha atras
///   do player, cutscene mostra os robos parando bloqueados, perseguicao desativa.
/// Sem NavMesh, sem Rigidbody, sem Time.timeScale=0. Movimento centralizado aqui.
public class RobotPursuitDirector : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerRunner player;
    public GameObject robotPrefab;
    [Tooltip("Slab da porta de contencao do terminal (desce ao fechar).")]
    public Transform terminalGateSlab;

    [Header("Zonas (Z mundial)")]
    public float pursuitStartZ = 905f;
    public float pursuitEndZ = 2560f;

    [Header("Perseguidores")]
    public int robotCount = 3;
    [Tooltip("Delay/atraso de replay por robo (s). Tamanho define maximo de robos.")]
    public float[] delays = { 0.45f, 0.85f, 1.05f, 1.25f };
    [Tooltip("Offset lateral de formacao por robo (mundo). Robo 0 = lead (visivel na camera).")]
    public float[] lateralOffsets = { 1.5f, -2.4f, 2.4f, -1.1f };
    [Tooltip("Recuo Z adicional por robo.")]
    public float[] backOffsets = { 5.5f, 10f, 11.5f, 13f };
    public float animatorSpeedPursuit = 1.15f;
    public float animatorSpeedStopped = 0.5f;

    [Header("Lead Pursuer (robo 0 — sempre visivel)")]
    [Tooltip("Distancia MINIMA atras do player (nunca chega mais perto).")]
    public float leadMinBehind = 4.2f;
    [Tooltip("Distancia MAXIMA atras do player. A camera de runner fica ~8 atras: o lead " +
        "precisa viver ENTRE a camera e o player (<= ~6.5) para permanecer no quadro.")]
    public float leadMaxBehind = 6.2f;
    [Tooltip("Suavizacao extra do lead (mais alto = gruda no clamp, sem lag atras da camera).")]
    public float leadFollowSharpness = 22f;

    [Header("Arrancada pos-dano (Round 16 — perigo ao bater em obstaculo)")]
    [Tooltip("Distancia atras do player que o LEAD atinge na arrancada (quase nos calcanhares).")]
    public float surgeLeadBehind = 2.2f;
    [Tooltip("Tempo (s) para os robos fecharem a distancia apos o impacto.")]
    public float surgeAttack = 0.35f;
    [Tooltip("Tempo (s) colados no player antes de comecarem a recuar.")]
    public float surgeHold = 1.2f;
    [Tooltip("Tempo (s) para voltarem a formacao normal (player ja re-acelerou).")]
    public float surgeRelease = 1.9f;
    [Tooltip("Escala do backOffset dos demais robos no pico da arrancada (0.45 = fecham 55%).")]
    [Range(0.2f, 1f)] public float surgeBackOffsetScale = 0.45f;
    [Tooltip("Boost de velocidade de animacao no pico da arrancada.")]
    public float surgeAnimSpeedBoost = 0.3f;

    [Header("Silhueta pos-porta")]
    public float robotsLingerSeconds = 8f;

    [Header("Cutscenes")]
    public float startCutsceneHold = 2.4f;
    public float endCutsceneHold = 2.6f;
    public float cameraBlend = 0.8f;
    public float gateCloseDuration = 1.1f;
    public float gateCloseDrop = 5.2f;

    public bool PursuitActive { get; private set; }
    public bool PursuitFinished { get; private set; }

    private readonly List<Vector4> history = new List<Vector4>(2048); // (x,y,z,time)
    private readonly List<EnemyPursuitRobot> robots = new List<EnemyPursuitRobot>();
    private readonly List<Animator> robotAnimators = new List<Animator>();
    private bool startSequenceRunning;
    private bool endSequenceRunning;
    private Camera cam;
    private CameraFollow camFollow;

    // Arrancada pos-dano: 0 = formacao normal, 1 = colados no player.
    private float surge;
    private Coroutine surgeRoutine;
    private PlayerHealth playerHealth;
    private int lastKnownLives = -1;

    private void Start()
    {
        if (player == null && GameManager.Instance != null)
        {
            player = GameManager.Instance.player;
        }
        cam = Camera.main;
        camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                lastKnownLives = playerHealth.Lives;
                playerHealth.IntegrityChanged += OnPlayerIntegrityChanged;
            }
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.IntegrityChanged -= OnPlayerIntegrityChanged;
        }
    }

    /// Dano DURANTE a perseguicao = robos arrancam para cima do player (quase alcancam)
    /// enquanto ele esta lento, e recuam quando ele re-acelera. Punicao visceral sem
    /// dano extra — os perseguidores continuam 100% visuais (sem colliders).
    private void OnPlayerIntegrityChanged(int lives, int max)
    {
        bool tookDamage = lastKnownLives >= 0 && lives < lastKnownLives;
        lastKnownLives = lives;

        if (!tookDamage || !PursuitActive || endSequenceRunning || lives <= 0)
        {
            return;
        }

        if (surgeRoutine != null)
        {
            StopCoroutine(surgeRoutine);
        }
        surgeRoutine = StartCoroutine(SurgeRoutine());
    }

    private IEnumerator SurgeRoutine()
    {
        GameManager.Instance?.celestIA?.ShowTemporary(
            "CELESTIA: Unidades ganhando terreno. Não pare!", 2f, DialogueManager.PriorityLow);

        float t = 0f;
        while (t < surgeAttack)   // fecha rapido
        {
            if (!PursuitActive) { surge = 0f; ApplySurgeAnimSpeed(); yield break; }
            t += Time.deltaTime;
            surge = Mathf.SmoothStep(0f, 1f, t / surgeAttack);
            ApplySurgeAnimSpeed();
            yield return null;
        }

        t = 0f;
        while (t < surgeHold)     // colado nos calcanhares
        {
            if (!PursuitActive) { surge = 0f; ApplySurgeAnimSpeed(); yield break; }
            t += Time.deltaTime;
            surge = 1f;
            yield return null;
        }

        t = 0f;
        while (t < surgeRelease)  // recua conforme o player re-acelera
        {
            if (!PursuitActive) { surge = 0f; ApplySurgeAnimSpeed(); yield break; }
            t += Time.deltaTime;
            surge = 1f - Mathf.SmoothStep(0f, 1f, t / surgeRelease);
            ApplySurgeAnimSpeed();
            yield return null;
        }

        surge = 0f;
        ApplySurgeAnimSpeed();
        surgeRoutine = null;
    }

    private void ApplySurgeAnimSpeed()
    {
        for (int i = 0; i < robotAnimators.Count; i++)
        {
            if (robotAnimators[i] != null)
            {
                robotAnimators[i].speed = animatorSpeedPursuit * (1f + surgeAnimSpeedBoost * surge);
            }
        }
    }

    private void Update()
    {
        if (player == null || GameManager.Instance == null)
        {
            return;
        }

        float z = player.transform.position.z;

        // inicio da perseguicao (uma vez, apenas em gameplay livre).
        // '!endSequenceRunning' e essencial: EndSequence desativa PursuitActive logo no
        // inicio, e sem esse guard o StartSequence re-dispararia no meio do encerramento.
        if (!PursuitActive && !PursuitFinished && !startSequenceRunning && !endSequenceRunning &&
            GameManager.Instance.State == GameState.Playing && z >= pursuitStartZ)
        {
            startSequenceRunning = true;
            StartCoroutine(StartSequence());
        }

        if (!PursuitActive)
        {
            return;
        }

        // grava historico do player
        Vector3 p = player.transform.position;
        history.Add(new Vector4(p.x, p.y, p.z, Time.time));
        if (history.Count > 2000)
        {
            history.RemoveRange(0, 500);
        }

        // fim da perseguicao
        if (!endSequenceRunning && z >= pursuitEndZ)
        {
            endSequenceRunning = true;
            StartCoroutine(EndSequence());
            return;
        }

        // move perseguidores (replay atrasado + formacao)
        for (int i = 0; i < robots.Count; i++)
        {
            EnemyPursuitRobot robot = robots[i];
            if (robot == null || !robot.gameObject.activeSelf)
            {
                continue;
            }

            Vector3 sample = SampleHistory(Time.time - robot.delay);
            // arrancada pos-dano: backOffset encolhe no pico do surge (robos fecham a distancia)
            float effectiveBack = robot.backOffset * Mathf.Lerp(1f, surgeBackOffsetScale, surge);
            Vector3 target = new Vector3(
                sample.x + robot.lateralOffset,
                Mathf.Max(0f, sample.y) + robot.verticalOffset,
                sample.z - effectiveBack);

            // LEAD PURSUER: clamp de distancia para permanecer visivel no frustum da camera
            // (camera de runner fica ~8 atras do player; o lead vive entre min e max atras,
            // entao esta sempre A FRENTE da camera e dentro do quadro, sem nunca alcancar
            // o player nem bloquear a leitura da pista — offset lateral o tira do centro).
            // Durante o surge o clamp fecha ate surgeLeadBehind (quase nos calcanhares).
            if (robot.isLeadPursuer)
            {
                float playerZ = player.transform.position.z;
                float minBehind = Mathf.Lerp(leadMinBehind, surgeLeadBehind, surge);
                float maxBehind = Mathf.Lerp(leadMaxBehind, surgeLeadBehind + 1f, surge);
                target.z = Mathf.Clamp(target.z, playerZ - maxBehind, playerZ - minBehind);
            }

            robot.ApplyTarget(target, Time.deltaTime);
            robot.ApplyAirborneState(sample.y); // replica pulo com o mesmo atraso
        }
    }

    /// Posicao do player 'atTime' (interpolada no historico).
    private Vector3 SampleHistory(float atTime)
    {
        if (history.Count == 0)
        {
            return player.transform.position;
        }

        for (int i = history.Count - 1; i > 0; i--)
        {
            if (history[i - 1].w <= atTime)
            {
                Vector4 a = history[i - 1];
                Vector4 b = history[i];
                float t = Mathf.InverseLerp(a.w, b.w, atTime);
                return Vector3.Lerp(new Vector3(a.x, a.y, a.z), new Vector3(b.x, b.y, b.z), t);
            }
        }

        Vector4 first = history[0];
        return new Vector3(first.x, first.y, first.z);
    }

    // ================= inicio =================

    private IEnumerator StartSequence()
    {
        var health = player.GetComponent<PlayerHealth>();
        health?.SetExternalInvulnerability(true);

        SpawnRobots();
        // shake leve na ativacao dos robos (cutscene da perseguicao)
        ProjectAurora.VFX.AuroraCameraFeedbackController.RobotActivation();

        // pre-carrega historico com a posicao atual (robos nascem no rastro do player)
        history.Clear();
        Vector3 p0 = player.transform.position;
        for (int i = 0; i < 60; i++)
        {
            history.Add(new Vector4(p0.x, 0f, p0.z - (60 - i) * 0.3f, Time.time - (60 - i) * 0.033f));
        }

        PursuitActive = true;

        GameManager.Instance.celestIA?.ShowTemporary(
            "CELESTIA: Unidades autônomas ativadas. Dr. Elias, corra.", 2.6f);

        // camera: olha para tras, mostrando os robos surgindo
        if (cam != null && camFollow != null)
        {
            camFollow.enabled = false;
            Vector3 camStart = cam.transform.position;
            Quaternion rotStart = cam.transform.rotation;

            // (Orbita 360 revertida a pedido do cliente: enquadramento original,
            // suave e a favor da gameplay — frente-direita olhando para tras.)
            float t = 0f;
            while (t < startCutsceneHold)
            {
                t += Time.deltaTime;
                Vector3 pp = player.transform.position;
                Vector3 wanted = pp + new Vector3(1.6f, 2.4f, 4.5f);       // a frente-direita do player
                Vector3 look = pp + new Vector3(0f, 1.0f, -9f);            // olhando para tras (robos)
                float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t / cameraBlend));
                cam.transform.position = Vector3.Lerp(camStart, wanted, blend);
                cam.transform.rotation = Quaternion.Slerp(rotStart, Quaternion.LookRotation(look - wanted), blend);
                yield return null;
            }

            // volta suave para a camera de runner
            t = 0f;
            Vector3 fromPos = cam.transform.position;
            Quaternion fromRot = cam.transform.rotation;
            while (t < cameraBlend)
            {
                t += Time.deltaTime;
                Vector3 pp = player.transform.position;
                Vector3 wanted = pp + camFollow.offset;
                Quaternion wantedRot = Quaternion.LookRotation(pp + camFollow.lookOffset - wanted);
                float blend = Mathf.SmoothStep(0f, 1f, t / cameraBlend);
                cam.transform.position = Vector3.Lerp(fromPos, wanted, blend);
                cam.transform.rotation = Quaternion.Slerp(fromRot, wantedRot, blend);
                yield return null;
            }

            camFollow.enabled = true;
        }

        GameManager.Instance.celestIA?.ShowTemporary("CELESTIA: Elas estão atrás de você.", 2.2f);
        health?.SetExternalInvulnerability(false);
        startSequenceRunning = false;
    }

    private void SpawnRobots()
    {
        if (robotPrefab == null)
        {
            Debug.LogWarning("[Pursuit] robotPrefab nao atribuido.");
            return;
        }

        int count = Mathf.Clamp(robotCount, 1, Mathf.Min(delays.Length, Mathf.Min(lateralOffsets.Length, backOffsets.Length)));
        Vector3 basePos = player.transform.position;
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(robotPrefab, transform);
            go.name = "PursuitRobot_" + i;
            go.transform.position = basePos + new Vector3(lateralOffsets[i], 0f, -(backOffsets[i] + 6f));

            EnemyPursuitRobot robot = go.GetComponent<EnemyPursuitRobot>();
            if (robot == null)
            {
                robot = go.AddComponent<EnemyPursuitRobot>();
            }
            robot.delay = delays[i];
            robot.lateralOffset = lateralOffsets[i];
            robot.backOffset = backOffsets[i];
            robot.isLeadPursuer = i == 0;
            if (robot.isLeadPursuer)
            {
                robot.followSharpness = leadFollowSharpness;
            }
            robot.ResetInitialization();
            robots.Add(robot);
            // (Pulso vermelho removido a pedido do cliente: perseguidores mantem o
            // visual normal do material.)

            Animator anim = go.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.speed = animatorSpeedPursuit;
                anim.SetBool("IsRunning", true); // controller Run/Jump do Dr. Elias retargetado
                // lead sempre animando (visivel); secundarios cortam pose fora da camera
                anim.cullingMode = i == 0
                    ? AnimatorCullingMode.AlwaysAnimate
                    : AnimatorCullingMode.CullUpdateTransforms;
            }
            robotAnimators.Add(anim);

            // perseguidor e 100% visual: sem colliders, sem dano
            foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }
            foreach (Obstacle obst in go.GetComponentsInChildren<Obstacle>(true))
            {
                Destroy(obst);
            }
            // sombras off (performance; ficam no fog atras do player)
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    // ================= fim =================

    private IEnumerator EndSequence()
    {
        PursuitActive = false; // congela gravacao/replay
        var health = player.GetComponent<PlayerHealth>();
        health?.SetExternalInvulnerability(true);

        bool corrupted = GameManager.Instance.sectors != null && GameManager.Instance.sectors.CurrentSector >= 3;
        GameManager.Instance.celestIA?.ShowTemporary(
            corrupted ? "CELESTIA: Contenção restabelecida. Continue até o terminal."
                      : "CELESTIA: Acesso ao núcleo isolado.", 2.6f);

        float gateZ = terminalGateSlab != null ? terminalGateSlab.position.z : pursuitEndZ + 4f;

        // porta fecha (slab desce) + robos convergem para tras da porta e param
        Vector3 slabClosed = Vector3.zero, slabOpen = Vector3.zero;
        if (terminalGateSlab != null)
        {
            slabOpen = terminalGateSlab.localPosition;
            slabClosed = slabOpen + new Vector3(0f, -gateCloseDrop, 0f);
        }

        // camera olha para tras mostrando porta + robos
        Vector3 camStart = Vector3.zero; Quaternion rotStart = Quaternion.identity;
        bool camControlled = cam != null && camFollow != null;
        if (camControlled)
        {
            camFollow.enabled = false;
            camStart = cam.transform.position;
            rotStart = cam.transform.rotation;
        }

        float total = Mathf.Max(gateCloseDuration, cameraBlend) + endCutsceneHold;
        float t = 0f;
        // alvos finais dos robos: atras da porta, em linha
        var stopTargets = new List<Vector3>();
        for (int i = 0; i < robots.Count; i++)
        {
            stopTargets.Add(new Vector3(robots[i].lateralOffset * 0.6f, 0f, gateZ - 2.2f - (i % 2) * 1.6f));
        }

        bool gateImpactFired = false;
        while (t < total)
        {
            t += Time.deltaTime;

            // slab desce
            if (terminalGateSlab != null)
            {
                float gt = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t / gateCloseDuration));
                terminalGateSlab.localPosition = Vector3.Lerp(slabOpen, slabClosed, gt);

                // impacto UNICO no momento em que a porta assenta: shake + poeira + faiscas
                if (!gateImpactFired && t >= gateCloseDuration)
                {
                    gateImpactFired = true;
                    ProjectAurora.VFX.AuroraCameraFeedbackController.DoorImpact();
                    Vector3 basePoint = terminalGateSlab.position + Vector3.down * (gateCloseDrop * 0.5f);
                    ProjectAurora.VFX.AuroraVFXController.DoorOpen(basePoint);           // poeira
                    ProjectAurora.VFX.AuroraVFXController.LaserShutdown(basePoint + Vector3.up * 0.4f); // faiscas
                }
            }

            // robos correm ate a porta e desaceleram (loop defensivo contra mutacao das listas)
            int n = Mathf.Min(robots.Count, Mathf.Min(stopTargets.Count, robotAnimators.Count));
            for (int i = 0; i < n; i++)
            {
                if (robots[i] == null) continue;
                robots[i].ApplyTarget(stopTargets[i], Time.deltaTime * 0.9f);
                if (robotAnimators[i] != null)
                {
                    robotAnimators[i].speed = Mathf.Lerp(animatorSpeedPursuit, animatorSpeedStopped, Mathf.Min(1f, t / total));
                }
            }

            // camera
            if (camControlled)
            {
                Vector3 pp = player.transform.position;
                Vector3 wanted = pp + new Vector3(-1.8f, 2.2f, 5.5f);
                Vector3 look = new Vector3(0f, 1.6f, gateZ - 1f);
                float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t / cameraBlend));
                cam.transform.position = Vector3.Lerp(camStart, wanted, blend);
                cam.transform.rotation = Quaternion.Slerp(rotStart, Quaternion.LookRotation(look - wanted), blend);
            }

            yield return null;
        }

        // volta camera para runner
        if (camControlled)
        {
            float bt = 0f;
            Vector3 fromPos = cam.transform.position;
            Quaternion fromRot = cam.transform.rotation;
            while (bt < cameraBlend)
            {
                bt += Time.deltaTime;
                Vector3 pp = player.transform.position;
                Vector3 wanted = pp + camFollow.offset;
                Quaternion wantedRot = Quaternion.LookRotation(pp + camFollow.lookOffset - wanted);
                float blend = Mathf.SmoothStep(0f, 1f, bt / cameraBlend);
                cam.transform.position = Vector3.Lerp(fromPos, wanted, blend);
                cam.transform.rotation = Quaternion.Slerp(fromRot, wantedRot, blend);
                yield return null;
            }
            camFollow.enabled = true;
        }

        health?.SetExternalInvulnerability(false);
        PursuitFinished = true;

        // robos ficam como silhueta atras da porta por alguns segundos, depois desativam
        yield return new WaitForSeconds(Mathf.Max(1f, robotsLingerSeconds));
        foreach (EnemyPursuitRobot robot in robots)
        {
            if (robot != null)
            {
                robot.gameObject.SetActive(false);
            }
        }
    }
}
