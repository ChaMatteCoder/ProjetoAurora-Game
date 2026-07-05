using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class IntroCutsceneController : MonoBehaviour
{
    public DialogueManager dialogue;
    public PlayerRunner player;
    public TutorialManager tutorial;
    public AudioSource sirenSource;

    [Header("Cinematic Shots (Round 2)")]
    public float establishingDuration = 3.6f;
    public float characterDuration = 4.2f;
    public float dangerMinDuration = 2.5f;

    [Header("Office Exit (Round 2b)")]
    public Transform officeDoorSlab;
    public float doorSlideDistance = 4.7f;
    public float doorSlideDuration = 1.2f;

    [Header("Ajustes Round 8")]
    [Tooltip("Mesa/painel do escritorio: recolhe (afunda) quando a porta abre, para o Dr. Elias nao atravessar.")]
    public Transform deskCluster;
    public float deskStowDepth = 1.4f;
    [Tooltip("Duracao de cada shot extra enquanto a dublagem nao termina.")]
    public float fillerShotDuration = 2.8f;

    private Camera gameplayCamera;
    private CameraFollow cameraFollow;
    private Color initialAmbient;
    private readonly List<Light> tintedLights = new List<Light>();
    private readonly List<Color> tintedLightColors = new List<Color>();
    private bool skipRequested;
    private bool dialogueDone;
    private bool finished;

    private static readonly string[] OpeningVoiceIds = { "ELI_001", "CEL_002" };
    private static readonly string[] AlertVoiceIds =
    {
        "CEL_003", "ELI_002", "CEL_004", "CEL_005", "ELI_003", "CEL_006", "CEL_007"
    };

    public void Begin()
    {
        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        // A intro só é pulada por Esc. Enter/Espaço não cortam a primeira fala ao carregar a cena.
        if (!finished && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            skipRequested = true;
            VoiceLinePlayer.Instance?.StopGroup(VoiceGroup.Intro, 0.1f);
            dialogue?.StopAll();
        }
    }

    private IEnumerator IntroRoutine()
    {
        player.SetAutoRun(false);
        player.SetInputEnabled(false);
        PrepareCamera();
        initialAmbient = RenderSettings.ambientLight;
        AudioManager.Instance?.SetNarrativeVolume(0.15f);

        Vector3 basePos = player.transform.position;
        if (officeDoorSlab == null)
        {
            GameObject slabGo = GameObject.Find("OfficeDoor_Slab");
            officeDoorSlab = slabGo != null ? slabGo.transform : null;
        }
        if (deskCluster == null)
        {
            GameObject deskGo = GameObject.Find("Desk_Cluster");
            deskCluster = deskGo != null ? deskGo.transform : null;
        }

        // A primeira fala começa imediatamente com o carregamento da gameplay.
        dialogueDone = false;
        VoiceLinePlayer voice = VoiceLinePlayer.Instance;
        if (voice != null && voice.HasLines(OpeningVoiceIds))
        {
            voice.ClearQueue();
            voice.StopCurrent();
            voice.PlaySequence(OpeningVoiceIds, false, () => dialogueDone = true,
                IntroVoiceOptions("Intro_Opening"));
        }
        else
        {
            dialogue.Play(new[]
            {
                new DialogueLine("DR. ELIAS", "Celéstia, iniciar diagnóstico do núcleo Aurora.", 1.5f),
                new DialogueLine("CELESTIA", "Diagnóstico iniciado.", 1.2f)
            }, false, () => dialogueDone = true);
        }

        // ---- SHOT 01: establishing da sala do Dr. Elias (mesa holografica, janela ao fundo) ----
        yield return PlayShot(
            basePos + new Vector3(0.5f, 1.85f, 3.4f), basePos + new Vector3(0.15f, 1.72f, 2.8f),
            basePos + new Vector3(0f, 1.45f, 0.4f), basePos + new Vector3(0f, 1.4f, 0.2f),
            establishingDuration);

        // ---- SHOT 02: close no Dr. Elias, com o dialogo de abertura em andamento ----
        yield return PlayShot(
            basePos + new Vector3(0.95f, 1.72f, 1.5f), basePos + new Vector3(0.6f, 1.64f, 1.15f),
            basePos + new Vector3(0f, 1.55f, 0.1f), basePos + new Vector3(0f, 1.52f, 0f),
            characterDuration);

        // dublagem mais longa que os shots: cobre a espera com angulos extras da sala
        yield return WaitForDialogueWithFillers(basePos, OpeningFillerShots(basePos));

        // ---- SHOT 03: detalhe do perigo + alerta vermelho ----
        SetAlertLighting();
        if (!skipRequested && sirenSource != null && sirenSource.clip != null)
        {
            sirenSource.Play();
        }

        dialogueDone = false;
        if (!skipRequested)
        {
            if (voice != null && voice.HasLines(AlertVoiceIds))
            {
                voice.PlaySequence(AlertVoiceIds, false, () => dialogueDone = true,
                    IntroVoiceOptions("Intro_Alert"));
            }
            else
            {
                dialogue.Play(new[]
                {
                    new DialogueLine("CELESTIA", "Atenção. Oscilação detectada nos protocolos de contenção.", 1.7f),
                    new DialogueLine("DR. ELIAS", "Oscilação? Mostre a origem.", 1.4f),
                    new DialogueLine("CELESTIA", "Falha crítica no setor de segurança autônoma.", 1.7f),
                    new DialogueLine("CELESTIA", "Unidades robóticas não estão respondendo ao comando central.", 1.7f),
                    new DialogueLine("DR. ELIAS", "Abra a rota para o Terminal Central. Agora.", 1.5f),
                    new DialogueLine("CELESTIA", "Calculando rota segura.", 1.2f),
                    new DialogueLine("CELESTIA", "Rota definida. Dr. Elias, você precisa correr.", 1.8f)
                }, false, () => dialogueDone = true);
            }
        }
        else
        {
            dialogueDone = true;
        }

        // over-the-shoulder: Elias em primeiro plano, porta da sala ao fundo sob alerta vermelho
        yield return PlayShot(
            basePos + new Vector3(-1.7f, 1.78f, -2.3f), basePos + new Vector3(-1.35f, 1.7f, -1.5f),
            basePos + new Vector3(0.55f, 1.9f, 8.5f), basePos + new Vector3(0.45f, 1.85f, 8.5f),
            dangerMinDuration);

        // dublagem do alerta e longa: cobre com angulos extras (monitores, close dutch, porta)
        yield return WaitForDialogueWithFillers(basePos, AlertFillerShots(basePos));

        if (skipRequested)
        {
            voice?.StopGroup(VoiceGroup.Intro, 0.1f);
            dialogue.StopAll();
        }

        // ---- porta da sala desliza aberta ("Rota definida... voce precisa correr") ----
        yield return SlideOfficeDoorOpen();

        // ---- SHOT 04: retorno suave para a camera de runner ----
        finished = true;
        yield return RestoreGameplayCamera();
        sirenSource?.Stop();
        RestoreLighting();
        GameManager.Instance.EnterTutorial();
        tutorial.BeginTutorial();
    }

    private static VoicePlaybackOptions IntroVoiceOptions(string ownerStateId)
    {
        return new VoicePlaybackOptions
        {
            group = VoiceGroup.Intro,
            priority = VoicePriority.Cutscene,
            interruptCurrent = false,
            clearQueueOfSameGroup = true,
            cancelOnStateExit = true,
            blockGameplay = true,
            fadeOutTime = 0.1f,
            ownerStateId = ownerStateId
        };
    }

    private IEnumerator SlideOfficeDoorOpen()
    {
        if (officeDoorSlab == null)
        {
            yield break;
        }

        Vector3 closed = officeDoorSlab.localPosition;
        Vector3 open = closed + new Vector3(doorSlideDistance, 0f, 0f);

        // mesa/painel recolhe junto (afunda no piso) para liberar o caminho do Dr. Elias
        Vector3 deskUp = deskCluster != null ? deskCluster.localPosition : Vector3.zero;
        Vector3 deskDown = deskUp + new Vector3(0f, -Mathf.Max(0.5f, deskStowDepth), 0f);

        if (skipRequested)
        {
            officeDoorSlab.localPosition = open;
            if (deskCluster != null)
            {
                deskCluster.localPosition = deskDown;
            }
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.3f, doorSlideDuration);
        while (elapsed < duration && !skipRequested)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            officeDoorSlab.localPosition = Vector3.Lerp(closed, open, t);
            if (deskCluster != null)
            {
                deskCluster.localPosition = Vector3.Lerp(deskUp, deskDown, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        officeDoorSlab.localPosition = open;
        if (deskCluster != null)
        {
            deskCluster.localPosition = deskDown;
        }
    }

    // ===== Filler shots (Round 8): angulos extras enquanto a dublagem termina =====

    private struct FillerShot
    {
        public Vector3 fromPos, toPos, lookFrom, lookTo;
        public FillerShot(Vector3 fp, Vector3 tp, Vector3 lf, Vector3 lt)
        {
            fromPos = fp; toPos = tp; lookFrom = lf; lookTo = lt;
        }
    }

    private FillerShot[] OpeningFillerShots(Vector3 basePos)
    {
        return new[]
        {
            // varredura baixa sobre a mesa holografica (arco amplo)
            new FillerShot(basePos + new Vector3(-1.4f, 1.25f, 2.6f), basePos + new Vector3(1.4f, 1.40f, 2.0f),
                basePos + new Vector3(0f, 0.95f, 1.5f), basePos + new Vector3(0f, 0.90f, 1.5f)),
            // janela/skyline atras do Dr. Elias (travelling lateral)
            new FillerShot(basePos + new Vector3(1.6f, 1.80f, -3.0f), basePos + new Vector3(-0.4f, 2.10f, -4.6f),
                basePos + new Vector3(-1f, 3.0f, -10f), basePos + new Vector3(-1f, 3.1f, -10f)),
            // perfil do Dr. Elias com a sala ao fundo (orbita curta)
            new FillerShot(basePos + new Vector3(-1.9f, 1.55f, -0.4f), basePos + new Vector3(-1.1f, 1.68f, 0.9f),
                basePos + new Vector3(0.3f, 1.5f, 0f), basePos + new Vector3(0.2f, 1.52f, 0f))
        };
    }

    private FillerShot[] AlertFillerShots(Vector3 basePos)
    {
        return new[]
        {
            // parede de monitores sob alerta (travelling ao longo da parede)
            new FillerShot(basePos + new Vector3(-4.4f, 2.00f, -2.2f), basePos + new Vector3(-3.8f, 1.80f, 1.2f),
                basePos + new Vector3(-6.8f, 2.25f, -0.5f), basePos + new Vector3(-6.8f, 2.2f, 0.5f)),
            // close alto/dutch no Dr. Elias (tensao, descida)
            new FillerShot(basePos + new Vector3(1.9f, 2.70f, 1.2f), basePos + new Vector3(1.0f, 2.10f, 0.4f),
                basePos + new Vector3(0f, 1.60f, 0f), basePos + new Vector3(0f, 1.58f, 0f)),
            // porta de saida sob luz vermelha (aproximacao — para onde ele vai correr)
            new FillerShot(basePos + new Vector3(2.6f, 1.75f, 1.4f), basePos + new Vector3(1.6f, 1.60f, 3.8f),
                basePos + new Vector3(0f, 2.0f, 8.5f), basePos + new Vector3(0f, 1.9f, 8.5f))
        };
    }

    /// Enquanto a dublagem/dialogo nao termina, cicla shots extras em vez de segurar
    /// um frame estatico. Sai imediatamente quando o dialogo acaba ou em skip.
    private IEnumerator WaitForDialogueWithFillers(Vector3 basePos, FillerShot[] fillers)
    {
        int index = 0;
        while (!dialogueDone && !skipRequested)
        {
            FillerShot shot = fillers[index % fillers.Length];
            index++;

            float elapsed = 0f;
            float duration = Mathf.Max(1.2f, fillerShotDuration);
            // corte seco entre fillers (linguagem de montagem), movimento suave dentro do shot
            while (elapsed < duration && !dialogueDone && !skipRequested)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                Vector3 pos = Vector3.Lerp(shot.fromPos, shot.toPos, t);
                Vector3 look = Vector3.Lerp(shot.lookFrom, shot.lookTo, t);
                if (gameplayCamera != null)
                {
                    gameplayCamera.transform.position = pos;
                    gameplayCamera.transform.rotation = Quaternion.LookRotation(look - pos);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator PlayShot(Vector3 fromPos, Vector3 toPos, Vector3 lookFrom, Vector3 lookTo, float duration)
    {
        if (gameplayCamera == null || skipRequested)
        {
            yield break;
        }

        float elapsed = 0f;
        duration = Mathf.Max(0.5f, duration);
        while (elapsed < duration && !skipRequested)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector3 pos = Vector3.Lerp(fromPos, toPos, t);
            Vector3 look = Vector3.Lerp(lookFrom, lookTo, t);
            gameplayCamera.transform.position = pos;
            gameplayCamera.transform.rotation = Quaternion.LookRotation(look - pos);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PrepareCamera()
    {
        gameplayCamera = Camera.main;
        if (gameplayCamera == null)
        {
            return;
        }

        cameraFollow = gameplayCamera.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }
    }

    private IEnumerator RestoreGameplayCamera()
    {
        if (gameplayCamera == null)
        {
            yield break;
        }

        Vector3 offset = cameraFollow == null ? new Vector3(0f, 5f, -8f) : cameraFollow.offset;
        Vector3 targetPosition = player.transform.position + offset;
        Vector3 lookTarget = cameraFollow == null
            ? player.transform.position + Vector3.up * 1.2f
            : player.transform.position + cameraFollow.lookOffset;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - targetPosition);
        Vector3 startPosition = gameplayCamera.transform.position;
        Quaternion startRotation = gameplayCamera.transform.rotation;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed);
            gameplayCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            gameplayCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        if (cameraFollow != null)
        {
            cameraFollow.enabled = true;
        }
    }

    private void SetAlertLighting()
    {
        RenderSettings.ambientLight = new Color(0.5f, 0.08f, 0.08f);
        tintedLights.Clear();
        tintedLightColors.Clear();
        foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            tintedLights.Add(sceneLight);
            tintedLightColors.Add(sceneLight.color);
            sceneLight.color = Color.Lerp(sceneLight.color, Color.red, 0.65f);
        }
    }

    private void RestoreLighting()
    {
        RenderSettings.ambientLight = initialAmbient;
        for (int i = 0; i < tintedLights.Count; i++)
        {
            if (tintedLights[i] != null)
            {
                tintedLights[i].color = tintedLightColors[i];
            }
        }

        tintedLights.Clear();
        tintedLightColors.Clear();
    }
}
