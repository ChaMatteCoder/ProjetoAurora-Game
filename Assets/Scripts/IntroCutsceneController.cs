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

    private Camera gameplayCamera;
    private CameraFollow cameraFollow;
    private Color initialAmbient;
    private readonly List<Light> tintedLights = new List<Light>();
    private readonly List<Color> tintedLightColors = new List<Color>();
    private bool skipRequested;
    private bool dialogueDone;
    private bool finished;

    public void Begin()
    {
        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        // Esc pula toda a introducao (Space/Enter continuam pulando linha a linha via DialogueManager)
        if (!finished && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            skipRequested = true;
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

        // ---- SHOT 01: establishing da sala do Dr. Elias (mesa holografica, janela ao fundo) ----
        yield return PlayShot(
            basePos + new Vector3(0.5f, 1.85f, 3.4f), basePos + new Vector3(0.15f, 1.72f, 2.8f),
            basePos + new Vector3(0f, 1.45f, 0.4f), basePos + new Vector3(0f, 1.4f, 0.2f),
            establishingDuration);

        // ---- SHOT 02: close no Dr. Elias, primeiro bloco de dialogo em paralelo ----
        dialogueDone = false;
        dialogue.Play(new[]
        {
            new DialogueLine(
                "CELESTIA",
                "Doutor Elias, mantenha a rota. Detectando obstáculos à frente.",
                1.7f),
            new DialogueLine("DR. ELIAS", "CelestIA, iniciar diagnóstico do núcleo Aurora.", 1.5f),
            new DialogueLine("CELESTIA", "Diagnóstico iniciado.", 1.2f)
        }, true, () => dialogueDone = true);

        yield return PlayShot(
            basePos + new Vector3(0.95f, 1.72f, 1.5f), basePos + new Vector3(0.6f, 1.64f, 1.15f),
            basePos + new Vector3(0f, 1.55f, 0.1f), basePos + new Vector3(0f, 1.52f, 0f),
            characterDuration);

        while (!dialogueDone && !skipRequested)
        {
            yield return null;
        }

        // ---- SHOT 03: detalhe do perigo + alerta vermelho ----
        SetAlertLighting();
        if (!skipRequested && sirenSource != null && sirenSource.clip != null)
        {
            sirenSource.Play();
        }

        dialogueDone = false;
        if (!skipRequested)
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
            }, true, () => dialogueDone = true);
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

        while (!dialogueDone && !skipRequested)
        {
            yield return null;
        }

        if (skipRequested)
        {
            dialogue.StopCurrent();
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

    private IEnumerator SlideOfficeDoorOpen()
    {
        if (officeDoorSlab == null)
        {
            yield break;
        }

        Vector3 closed = officeDoorSlab.localPosition;
        Vector3 open = closed + new Vector3(doorSlideDistance, 0f, 0f);
        if (skipRequested)
        {
            officeDoorSlab.localPosition = open;
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.3f, doorSlideDuration);
        while (elapsed < duration && !skipRequested)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            officeDoorSlab.localPosition = Vector3.Lerp(closed, open, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        officeDoorSlab.localPosition = open;
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
