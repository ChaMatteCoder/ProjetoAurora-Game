using System.Collections;
using UnityEngine;

public class IntroCutsceneController : MonoBehaviour
{
    public DialogueManager dialogue;
    public PlayerRunner player;
    public TutorialManager tutorial;
    public AudioSource sirenSource;

    private Camera gameplayCamera;
    private CameraFollow cameraFollow;
    private Color initialAmbient;

    public void Begin()
    {
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        player.SetAutoRun(false);
        player.SetInputEnabled(false);
        PrepareCamera();
        initialAmbient = RenderSettings.ambientLight;
        AudioManager.Instance?.SetNarrativeVolume(0.15f);

        yield return dialogue.Play(new[]
        {
            new DialogueLine(
                "CELESTIA",
                "Doutor Elias, mantenha a rota. Detectando obstáculos à frente.",
                1.7f),
            new DialogueLine("DR. ELIAS", "CelestIA, iniciar diagnóstico do núcleo Aurora.", 1.5f),
            new DialogueLine("CELESTIA", "Diagnóstico iniciado.", 1.2f)
        }, true);

        SetAlertLighting();
        if (sirenSource != null && sirenSource.clip != null)
        {
            sirenSource.Play();
        }

        yield return dialogue.Play(new[]
        {
            new DialogueLine("CELESTIA", "Atenção. Oscilação detectada nos protocolos de contenção.", 1.7f),
            new DialogueLine("DR. ELIAS", "Oscilação? Mostre a origem.", 1.4f),
            new DialogueLine("CELESTIA", "Falha crítica no setor de segurança autônoma.", 1.7f),
            new DialogueLine("CELESTIA", "Unidades robóticas não estão respondendo ao comando central.", 1.7f),
            new DialogueLine("DR. ELIAS", "Abra a rota para o Terminal Central. Agora.", 1.5f),
            new DialogueLine("CELESTIA", "Calculando rota segura.", 1.2f),
            new DialogueLine("CELESTIA", "Rota definida. Dr. Elias, você precisa correr.", 1.8f)
        }, true);

        yield return RestoreGameplayCamera();
        sirenSource?.Stop();
        RenderSettings.ambientLight = initialAmbient;
        GameManager.Instance.EnterTutorial();
        tutorial.BeginTutorial();
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

        gameplayCamera.transform.position = player.transform.position + new Vector3(4.5f, 2.8f, -5f);
        gameplayCamera.transform.LookAt(player.transform.position + Vector3.up);
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

    private static void SetAlertLighting()
    {
        RenderSettings.ambientLight = new Color(0.5f, 0.08f, 0.08f);
        foreach (Light sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            sceneLight.color = Color.Lerp(sceneLight.color, Color.red, 0.65f);
        }
    }
}
