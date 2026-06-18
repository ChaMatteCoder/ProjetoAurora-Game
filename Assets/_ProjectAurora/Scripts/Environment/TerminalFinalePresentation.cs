using System.Collections;
using UnityEngine;

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

    public IEnumerator PlayPrelude()
    {
        if (corruptionLayer != null)
        {
            corruptionLayer.SetActive(true);
        }

        foreach (Light sceneLight in corruptedLights)
        {
            if (sceneLight != null)
            {
                sceneLight.enabled = true;
            }
        }

        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null)
        {
            yield break;
        }

        CameraFollow follow = gameplayCamera.GetComponent<CameraFollow>();
        if (follow != null)
        {
            follow.enabled = false;
        }

        if (panelShot != null && panelFocus != null)
        {
            yield return MoveCamera(
                gameplayCamera,
                panelShot,
                panelFocus,
                panelMoveDuration);
        }

        yield return new WaitForSecondsRealtime(0.2f);

        if (coreShot != null && coreFocus != null)
        {
            yield return MoveCamera(
                gameplayCamera,
                coreShot,
                coreFocus,
                coreRevealDuration);
        }
    }

    private static IEnumerator MoveCamera(
        Camera gameplayCamera,
        Transform shot,
        Transform focus,
        float duration)
    {
        Vector3 startPosition = gameplayCamera.transform.position;
        Quaternion startRotation = gameplayCamera.transform.rotation;
        Quaternion targetRotation =
            Quaternion.LookRotation(focus.position - shot.position);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.05f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / safeDuration);
            gameplayCamera.transform.position =
                Vector3.Lerp(startPosition, shot.position, t);
            gameplayCamera.transform.rotation =
                Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        gameplayCamera.transform.SetPositionAndRotation(
            shot.position,
            targetRotation);
    }
}
