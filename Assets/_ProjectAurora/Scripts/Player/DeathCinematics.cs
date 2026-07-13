using System.Collections;
using UnityEngine;

/// Cinemática de morte (Game Over): quando o Dr. Elias morre, desliga o
/// controlador de animação do corredor (que sobrescreveria a pose), toca a
/// animação "Dying Backwards" e leva a câmera suavemente até a frente do rosto
/// dele, mantendo o enquadramento enquanto o corpo cai. O escurecimento da tela
/// fica a cargo do GameOverManager (fade na duração da música de Game Over).
public class DeathCinematics : MonoBehaviour
{
    [Tooltip("Estado do Animator (DrElias_RunJump) com o clipe Dying Backwards.")]
    public string dyingStateName = "Dying";
    public float cameraMoveDuration = 2.2f;
    [Tooltip("Offset da câmera em relação à cabeça (player corre para +Z, então +Z = de frente). " +
             "Elevado o bastante para não clipar pelo corpo quando ele cai deitado.")]
    public Vector3 faceOffset = new Vector3(0f, 0.9f, 2.0f);
    [Tooltip("Altura mínima da câmera durante a cinemática (evita atravessar o chão/corpo).")]
    public float minCameraHeight = 0.85f;
    [Tooltip("Segundos extras seguindo o rosto durante a queda.")]
    public float followSeconds = 6f;

    private PlayerHealth health;
    private Animator animator;
    private DrEliasAnimationController animController;
    private bool started;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        animController = GetComponent<DrEliasAnimationController>();
        animator = GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        if (health != null) health.OnDeath += BeginDeathSequence;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDeath -= BeginDeathSequence;
    }

    private void BeginDeathSequence()
    {
        if (started) return;
        started = true;

        if (animController != null) animController.enabled = false; // solta o Animator
        if (animator != null)
        {
            animator.speed = 1f;
            animator.applyRootMotion = false;
            animator.CrossFadeInFixedTime(dyingStateName, 0.25f);
        }
        StartCoroutine(MoveCameraToFace());
    }

    private IEnumerator MoveCameraToFace()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = false;

        Transform head = animator != null && animator.isHuman
            ? animator.GetBoneTransform(HumanBodyBones.Head)
            : null;
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        float t = 0f;
        while (t < cameraMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / cameraMoveDuration));
            Vector3 headPos = head != null ? head.position : transform.position + Vector3.up * 1.6f;
            Vector3 target = headPos + faceOffset;
            target.y = Mathf.Max(target.y, minCameraHeight);
            Quaternion look = Quaternion.LookRotation(headPos - target);
            cam.transform.position = Vector3.Lerp(startPos, target, k);
            cam.transform.rotation = Quaternion.Slerp(startRot, look, k);
            yield return null;
        }

        // acompanha o rosto durante a queda (a cabeça desce/recua na animação)
        float hold = followSeconds;
        while (hold > 0f)
        {
            hold -= Time.deltaTime;
            Vector3 headPos = head != null ? head.position : transform.position + Vector3.up * 0.6f;
            Vector3 target = headPos + faceOffset;
            target.y = Mathf.Max(target.y, minCameraHeight);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target, 4f * Time.deltaTime);
            cam.transform.rotation = Quaternion.Slerp(
                cam.transform.rotation,
                Quaternion.LookRotation(headPos - cam.transform.position),
                4f * Time.deltaTime);
            yield return null;
        }
    }
}
