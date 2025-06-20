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
    public float AimStart = 0.6f;
    public float AimRelease = 0.2f;

    public Vector2 Translation { get; private set; }
    public Vector2 Direction { get; private set; }
    public bool AttackWasPressed { get; private set; }
    public bool AttackWasReleased { get; private set; }
    public bool PauseWasPressed { get; private set; }
    public bool DashWasPressed { get; private set; }
    public bool ActivateAbilityWasPressed { get; private set; }
    public bool InteractWasPressed { get; private set; }
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

        if (Scheme == ControlScheme.Gamepad)
        {
            AttackWasPressed = _inputs.Player.Direction.ReadValue<Vector2>().magnitude >= AimStart;
            AttackWasReleased = _inputs.Player.Direction.ReadValue<Vector2>().magnitude <= AimRelease;
            Direction = _inputs.Player.Direction.ReadValue<Vector2>();
        }
        else
        {
            AttackWasPressed = _inputs.Player.Attack.WasPerformedThisFrame();
            AttackWasReleased = _inputs.Player.Attack.WasReleasedThisFrame();
        }

        Translation = _inputs.Player.Translation.ReadValue<Vector2>();
        PauseWasPressed = _inputs.Player.Pause.WasPerformedThisFrame();
        DashWasPressed = _inputs.Player.Dash.WasPerformedThisFrame();
        ActivateAbilityWasPressed = _inputs.Player.ActivateAbility.WasPerformedThisFrame();
        InteractWasPressed = _inputs.Player.Interact.WasPerformedThisFrame();
    }

    public bool IsAiming()
    {
        return Direction.magnitude >= AimStart;
    }
}