using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Serialization;

public class InputManager : MonoBehaviour
{
    public enum ControlScheme
    {
        MouseKeyboard,
        Gamepad
    }

    public static InputManager Instance;

    [Header("Gamepad Settings")]
    public float MovementDeadzone = 0.1f;
    public float AimActiveThreshold = 0.6f;
    public float AimReleaseThreshold = 0.2f;

    public Vector2 Translation { get; private set; }
    public Vector2 Direction { get; private set; }
    public bool AttackWasPressed { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool AttackWasReleased { get; private set; }
    public bool PauseWasPressed { get; private set; }
    public bool DashWasPressed { get; private set; }
    public bool ActivateAbilityWasPressed { get; private set; }
    public bool InteractWasPressed { get; private set; }
    public bool RespawnWasPressed { get; private set; }
    public bool ToggleChargeHUDWasPressed { get; private set; }
    public bool OpenInventoryWasPressed { get; private set; }
    public bool ActivateExpressionUpWasPressed { get; private set; }
    public bool ActivateExpressionDownWasPressed { get; private set; }
    public bool ActivateExpressionLeftWasPressed { get; private set; }
    public bool ActivateExpressionRightWasPressed { get; private set; }

    public ControlScheme Scheme { get; private set; }
    private MyInputs _inputs;


    private void Awake()
    {
        Instance = this;
        _inputs = new MyInputs();
        _inputs.Enable();
    }

    private void OnDisable()
    {
        _inputs.Disable();
    }

    private void Update()
    {
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            Scheme = ControlScheme.Gamepad;
        else if (Keyboard.current.wasUpdatedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
            Scheme = ControlScheme.MouseKeyboard;

        Translation = _inputs.Player.Translation.ReadValue<Vector2>();
        PauseWasPressed = _inputs.Player.Pause.WasPerformedThisFrame();
        DashWasPressed = _inputs.Player.Dash.WasPerformedThisFrame();
        ActivateAbilityWasPressed = _inputs.Player.ActivateAbility.WasPerformedThisFrame();
        InteractWasPressed = _inputs.Player.Interact.WasPerformedThisFrame();
        ToggleChargeHUDWasPressed = _inputs.Player.ToggleChargeHUD.WasPerformedThisFrame();

        if (Scheme == ControlScheme.Gamepad)
        {
            AttackWasPressed = _inputs.Player.Direction.ReadValue<Vector2>().magnitude >= AimActiveThreshold;
            AttackHeld = AttackWasPressed;
            AttackWasReleased = _inputs.Player.Direction.ReadValue<Vector2>().magnitude <= AimReleaseThreshold;
            Direction = _inputs.Player.Direction.ReadValue<Vector2>();
            RespawnWasPressed = InteractWasPressed;
        }
        else
        {
            AttackWasPressed = _inputs.Player.Attack.WasPerformedThisFrame();
            AttackHeld = _inputs.Player.Attack.IsPressed();
            AttackWasReleased = _inputs.Player.Attack.WasReleasedThisFrame();
            RespawnWasPressed = DashWasPressed || InteractWasPressed || AttackWasPressed;
        }
    }

    public bool IsDirectionActive()
    {
        return Direction.magnitude >= AimActiveThreshold;
    }

    public bool IsMovementActive()
    {
        return Translation.magnitude >= MovementDeadzone;
    }
}