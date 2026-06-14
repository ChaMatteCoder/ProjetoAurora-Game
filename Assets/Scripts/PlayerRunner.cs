using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerRunner : MonoBehaviour
{
    public float initialSpeed = 8f;
    public float maximumSpeed = 16f;
    public float speedRampDistance = 2700f;
    public float laneDistance = 3f;
    public float laneChangeSpeed = 10f;
    public float jumpForce = 7f;
    public float gravity = -20f;

    public event Action<int> LaneChanged;
    public event Action Jumped;
    public event Action Landed;

    public float CurrentForwardSpeed =>
        Mathf.Lerp(initialSpeed, maximumSpeed, Mathf.Clamp01(transform.position.z / speedRampDistance)) * speedMultiplier;
    public bool IsAutoRunning => autoRun;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool IsJumping { get; private set; }

    private CharacterController controller;
    private int currentLane = 1;
    private float verticalVelocity;
    private float speedMultiplier = 1f;
    private bool autoRun;
    private bool inputEnabled = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (inputEnabled && keyboard != null)
        {
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                MoveLane(-1);
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                MoveLane(1);
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TryJump();
            }
        }

        if (!IsJumping && controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        float targetX = (currentLane - 1) * laneDistance;
        float horizontal = (targetX - transform.position.x) * laneChangeSpeed;
        float forward = autoRun ? CurrentForwardSpeed : 0f;
        CollisionFlags collisionFlags =
            controller.Move(new Vector3(horizontal, verticalVelocity, forward) * Time.deltaTime);

        bool landedThisFrame =
            IsJumping &&
            verticalVelocity <= 0f &&
            ((collisionFlags & CollisionFlags.Below) != 0 || controller.isGrounded);
        if (landedThisFrame)
        {
            IsJumping = false;
            verticalVelocity = -2f;
            Landed?.Invoke();
        }
    }

    public void SetAutoRun(bool value) => autoRun = value;
    public void SetInputEnabled(bool value) => inputEnabled = value;
    public void SetSpeedMultiplier(float value) => speedMultiplier = Mathf.Clamp01(value);

    public bool TryJump()
    {
        if (!inputEnabled || controller == null || !controller.isGrounded || IsJumping)
        {
            return false;
        }

        IsJumping = true;
        verticalVelocity = jumpForce;
        Jumped?.Invoke();
        return true;
    }

    private void MoveLane(int direction)
    {
        int previous = currentLane;
        currentLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (previous != currentLane)
        {
            LaneChanged?.Invoke(direction);
        }
    }

    public static bool PausePressedThisFrame()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }
}
