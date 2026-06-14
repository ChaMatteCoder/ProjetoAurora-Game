using UnityEngine;

[DisallowMultipleComponent]
public class DrEliasAnimationController : MonoBehaviour
{
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");
    private static readonly int IsRunningParameter =
        Animator.StringToHash("IsRunning");
    private static readonly int IsJumpingParameter =
        Animator.StringToHash("IsJumping");
    private static readonly int RunningState =
        Animator.StringToHash("Base Layer.Running");

    public PlayerRunner runner;
    public Animator animator;
    public float referenceRunSpeed = 8f;
    public float landingBlendDuration = 0.08f;
    public bool stabilizeAnimatedForwardMotion = true;

    private Transform hips;
    private Vector3 animatorLocalPosition;
    private Quaternion animatorLocalRotation;
    private float hipsForwardAnchor;
    private float runningClipLength = 0.7f;
    private float runningCycle;
    private bool hasHipsForwardAnchor;

    private void Awake()
    {
        if (runner == null)
        {
            runner = GetComponent<PlayerRunner>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animatorLocalPosition = animator.transform.localPosition;
            animatorLocalRotation = animator.transform.localRotation;
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            runningClipLength = FindRunningClipLength();
        }
    }

    private void OnEnable()
    {
        if (runner != null)
        {
            runner.Jumped += PlayJump;
            runner.Landed += ResumeRunningAfterLanding;
        }
    }

    private void OnDisable()
    {
        if (runner != null)
        {
            runner.Jumped -= PlayJump;
            runner.Landed -= ResumeRunningAfterLanding;
        }
    }

    private void Update()
    {
        if (runner == null || animator == null)
        {
            return;
        }

        bool isRunning = runner.IsAutoRunning;
        float runPlaybackSpeed = isRunning
            ? Mathf.Clamp(runner.CurrentForwardSpeed / referenceRunSpeed, 0.85f, 1.5f)
            : 1f;

        animator.SetBool(IsRunningParameter, isRunning);
        animator.SetBool(IsJumpingParameter, runner.IsJumping);
        animator.speed = isRunning && !runner.IsJumping ? runPlaybackSpeed : 1f;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!runner.IsJumping && state.fullPathHash == RunningState)
        {
            runningCycle = Mathf.Repeat(state.normalizedTime, 1f);
        }
        else if (isRunning)
        {
            runningCycle = Mathf.Repeat(
                runningCycle + Time.deltaTime * runPlaybackSpeed / runningClipLength,
                1f);
        }
    }

    private void PlayJump()
    {
        if (animator == null)
        {
            return;
        }

        animator.speed = 1f;
        animator.SetBool(IsJumpingParameter, true);
        animator.ResetTrigger(JumpTrigger);
        animator.SetTrigger(JumpTrigger);
    }

    private void ResumeRunningAfterLanding()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsJumpingParameter, false);
        if (runner != null && runner.IsAutoRunning)
        {
            animator.CrossFadeInFixedTime(
                RunningState,
                landingBlendDuration,
                0,
                runningCycle * runningClipLength);
        }
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            return;
        }

        animator.transform.localPosition = animatorLocalPosition;
        animator.transform.localRotation = animatorLocalRotation;

        if (!stabilizeAnimatedForwardMotion || hips == null)
        {
            return;
        }

        Vector3 hipsInAnimatorSpace =
            animator.transform.InverseTransformPoint(hips.position);
        if (!hasHipsForwardAnchor)
        {
            hipsForwardAnchor = hipsInAnimatorSpace.z;
            hasHipsForwardAnchor = true;
            return;
        }

        hipsInAnimatorSpace.z = hipsForwardAnchor;
        hips.position = animator.transform.TransformPoint(hipsInAnimatorSpace);
    }

    private float FindRunningClipLength()
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null)
        {
            return runningClipLength;
        }

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip != null && clip.name.IndexOf(
                    "Running",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Mathf.Max(clip.length, 0.01f);
            }
        }

        return runningClipLength;
    }
}
