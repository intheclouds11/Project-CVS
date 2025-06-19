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

    public static InputManager instance;
    public Vector2 translation;
    public Vector2 direction;
    public bool fireWasPressed;
    public bool pauseMenuInputPressed;

    public ControlScheme controlScheme { get; private set; }
    private MyInputs _inputs;


    private void Awake()
    {
        instance = this;
        _inputs = new MyInputs();
        _inputs.Enable();
    }
    
    private void OnDisable()
    {
        _inputs.Disable();
    }

    void Update()
    {
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            controlScheme = ControlScheme.Gamepad;
        }
        else if (Keyboard.current.wasUpdatedThisFrame || Mouse.current.leftButton.wasPressedThisFrame)
        {
            controlScheme = ControlScheme.MouseKeyboard;
        }

        translation = _inputs.Player.Translation.ReadValue<Vector2>();
        direction = _inputs.Player.Direction.ReadValue<Vector2>();
        fireWasPressed = _inputs.Player.Fire.WasPerformedThisFrame();
        pauseMenuInputPressed = _inputs.Player.Pause.WasPerformedThisFrame();
    }
}