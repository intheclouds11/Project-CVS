using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Serialization;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    [Header("Gamepad Settings")]
    public float MovementDeadzone = 0.1f;
    public float AimActiveThreshold = 0.6f;
    public float AimReleaseThreshold = 0.2f;

    public Vector2 Translation { get; private set; }
    public Vector2 Direction { get; private set; }
    public bool DashWasPressed { get; private set; }

    public bool AttackWasPressed { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool AttackWasReleased { get; private set; }
    public bool CritSpecialWasPressed { get; private set; }
    public bool ActivateAbilityWasPressed { get; private set; }

    public bool InteractWasPressed { get; private set; }
    public bool PauseWasPressed { get; private set; }
    public bool RespawnWasPressed { get; private set; }
    public bool OpenInventoryWasPressed { get; private set; }
    public bool ActivateExpressionUpWasPressed { get; private set; }
    public bool ActivateExpressionDownWasPressed { get; private set; }
    public bool ActivateExpressionLeftWasPressed { get; private set; }
    public bool ActivateExpressionRightWasPressed { get; private set; }
    
    // Dev tools
    public bool ToggleChargeHUDWasPressed { get; private set; }
    public bool ToggleGodModeWasPressed { get; private set; }

    public bool UsingGamepad { get; private set; }
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
        if (IsGamepadInUse())
            UsingGamepad = true;
        else if (Keyboard.current.wasUpdatedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
            UsingGamepad = false;

        // Shared actions
        Translation = _inputs.Player.Translation.ReadValue<Vector2>();
        PauseWasPressed = _inputs.Player.Pause.WasPerformedThisFrame();
        DashWasPressed = _inputs.Player.Dash.WasPerformedThisFrame();
        ActivateAbilityWasPressed = _inputs.Player.ActivateAbility.WasPerformedThisFrame();
        CritSpecialWasPressed = _inputs.Player.CritSpecial.WasPerformedThisFrame();
        InteractWasPressed = _inputs.Player.Interact.WasPerformedThisFrame();

        if (UsingGamepad)
        {
            // Device specific
            AttackWasPressed = _inputs.Player.Direction.ReadValue<Vector2>().magnitude >= AimActiveThreshold;
            AttackHeld = AttackWasPressed;
            AttackWasReleased = _inputs.Player.Direction.ReadValue<Vector2>().magnitude <= AimReleaseThreshold;
            Direction = _inputs.Player.Direction.ReadValue<Vector2>();
            RespawnWasPressed = InteractWasPressed;
        }
        else
        {
            // Device specific
            AttackWasPressed = _inputs.Player.Attack.WasPerformedThisFrame();
            AttackHeld = _inputs.Player.Attack.IsPressed();
            AttackWasReleased = _inputs.Player.Attack.WasReleasedThisFrame();
            RespawnWasPressed = DashWasPressed || InteractWasPressed || AttackWasPressed;
        }

        if (Debug.isDebugBuild)
        {
            ToggleChargeHUDWasPressed = _inputs.Player.ToggleChargeHUD.WasPerformedThisFrame();
            ToggleGodModeWasPressed = _inputs.Player.ToggleGodMode.WasPerformedThisFrame();
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

    public void Vibrate(float lowFreq, float highFreq, float duration)
    {
        StartCoroutine(VibrateCoroutine(lowFreq, highFreq, duration));
    }

    private IEnumerator VibrateCoroutine(float low, float high, float time)
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) yield break;

        gamepad.SetMotorSpeeds(low, high);
        gamepad.ResumeHaptics();
        yield return new WaitForSeconds(time);
        gamepad.PauseHaptics();
        gamepad.SetMotorSpeeds(0, 0);
    }

    private bool IsGamepadInUse()
    {
        return Gamepad.current != null && (Gamepad.current.buttonNorth.wasPressedThisFrame ||
                                           Gamepad.current.buttonSouth.wasPressedThisFrame ||
                                           Gamepad.current.buttonWest.wasPressedThisFrame ||
                                           Gamepad.current.buttonEast.wasPressedThisFrame ||
                                           Gamepad.current.startButton.wasPressedThisFrame ||
                                           Gamepad.current.selectButton.wasPressedThisFrame ||
                                           Gamepad.current.dpad.ReadValue() != Vector2.zero ||
                                           Gamepad.current.leftTrigger.IsActuated() ||
                                           Gamepad.current.rightTrigger.IsActuated() ||
                                           Gamepad.current.leftStick.IsActuated() ||
                                           Gamepad.current.rightStick.IsActuated());
    }
}