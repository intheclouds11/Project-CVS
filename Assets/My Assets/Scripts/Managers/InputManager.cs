using System;
using System.Collections;
using System.Collections.Generic;
using Broccoli.Pipe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Serialization;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    
    [SerializeField]
    private float _inputsEnabledDelay = 1f;
    public bool WaitingToGivePlayerControl { get; private set; } = true;

    [Header("Gamepad Settings")]
    public float MovementDeadzone = 0.1f;
    public float AimActiveThreshold = 0.6f;
    public float AimReleaseThreshold = 0.2f;

    public bool InputsAllowed { get; private set; }

    public Vector2 Translation { get; private set; }
    public Vector2 Direction { get; private set; }
    public bool DashWasPressed { get; private set; }

    public bool AttackWasPressed { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool PrevAttackHeld { get; private set; }
    public bool AttackWasReleased { get; private set; }
    public bool CritSpecialWasPressed { get; private set; }
    public bool ActivateAbilityWasPressed { get; private set; }

    public bool InteractWasPressed { get; private set; }
    public bool GamepadEastButtonWasPressed { get; private set; }
    
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
    public bool ToggleEnemyAIWasPressed { get; private set; }
    public bool ToggleMusicWasPressed { get; private set; }
    public bool TimeScaleUpWasPressed { get; private set; }
    public bool TimeScaleDownWasPressed { get; private set; }
    public bool TimeScaleResetWasPressed { get; private set; }

    public bool UsingGamepad { get; private set; }
    private bool _gamepadWasUsed;
    private MyInputs _inputs;
    private Coroutine _vibrateCoroutine;


    private void Awake()
    {
        Instance = this;
        _inputs = new MyInputs();
        _inputs.Enable();
        PlayerSpawnManager.PlayerSpawned += OnPlayerSpawned;
    }

    private void OnPlayerSpawned(PlayerController obj)
    {
        WaitingToGivePlayerControl = true;
        DelayGivePlayerControl();
    }

    public void DelayGivePlayerControl()
    {
        StartCoroutine(GiveControlCoroutine());
    }
    
    private IEnumerator GiveControlCoroutine()
    {
        yield return new WaitForSeconds(_inputsEnabledDelay);

        WaitingToGivePlayerControl = false;

        if (!UIManager.Instance.IsAMenuOpen())
        {
            ToggleInputsAllowed(true);
        }
    }

    public void ToggleInputsAllowed(bool toggle)
    {
        if (toggle && WaitingToGivePlayerControl) return;
        InputsAllowed = toggle;
    }

    private void Update()
    {
        if (IsGamepadInUse())
            UsingGamepad = true;
        else if (Keyboard.current.wasUpdatedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
            UsingGamepad = false;

        if (!_gamepadWasUsed && UsingGamepad)
        {
            _gamepadWasUsed = true;
            Cursor.visible = false;
        }
        else if (_gamepadWasUsed && !UsingGamepad)
        {
            _gamepadWasUsed = false;
            Cursor.visible = true;
        }

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
            Direction = _inputs.Player.Direction.ReadValue<Vector2>();
            AttackHeld = Direction.magnitude >= AimActiveThreshold;
            AttackWasPressed = !PrevAttackHeld && AttackHeld;
            AttackWasReleased = PrevAttackHeld && Direction.magnitude <= AimReleaseThreshold;
            RespawnWasPressed = InteractWasPressed;
            GamepadEastButtonWasPressed = _inputs.UI.Back.WasPerformedThisFrame();
        }
        else
        {
            // Device specific
            AttackWasPressed = _inputs.Player.Attack.WasPerformedThisFrame();
            AttackHeld = _inputs.Player.Attack.IsPressed();
            AttackWasReleased = _inputs.Player.Attack.WasReleasedThisFrame();
            RespawnWasPressed = DashWasPressed || InteractWasPressed || AttackWasPressed;
        }

        PrevAttackHeld = AttackHeld;

        if (Debug.isDebugBuild)
        {
            ToggleChargeHUDWasPressed = _inputs.Player.ToggleChargeHUD.WasPerformedThisFrame();
            ToggleGodModeWasPressed = _inputs.Player.ToggleGodMode.WasPerformedThisFrame();
            ToggleEnemyAIWasPressed = _inputs.Player.ToggleEnemyAI.WasPerformedThisFrame();
            ToggleMusicWasPressed = _inputs.Player.ToggleMusic.WasPerformedThisFrame();
            TimeScaleUpWasPressed = _inputs.Player.IncreaseTimeScale.WasPerformedThisFrame();
            TimeScaleDownWasPressed = _inputs.Player.DecreaseTimeScale.WasPerformedThisFrame();
            TimeScaleResetWasPressed = _inputs.Player.ResetTimeScale.WasPerformedThisFrame();
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
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        if (_vibrateCoroutine != null) StopCoroutine(_vibrateCoroutine);
        _vibrateCoroutine = StartCoroutine(VibrateCoroutine(gamepad, lowFreq, highFreq, duration));
    }

    private IEnumerator VibrateCoroutine(Gamepad gamepad, float low, float high, float time)
    {
        gamepad.SetMotorSpeeds(low, high);
        gamepad.ResumeHaptics();
        yield return new WaitForSeconds(time);
        gamepad.PauseHaptics();
        gamepad.SetMotorSpeeds(0, 0);
        _vibrateCoroutine = null;
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