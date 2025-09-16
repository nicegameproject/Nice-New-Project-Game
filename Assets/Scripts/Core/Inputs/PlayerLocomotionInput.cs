using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-2)]
public class PlayerLocomotionInput : MonoBehaviour, PlayerControls.IPlayerLocomotionMapActions, ILateUpdateObserver
{
    [SerializeField] private bool holdToSprint = true;
    public Vector2 MovementInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SprintToggledOn { get; private set; }
    public bool CrouchToggledOn { get; private set; }

    private PlayerControls _playerControls;
    
    private void OnEnable()
    {
        
        if (PlayerInputManager.Instance?.PlayerControls == null)
        {
            Debug.LogError("Player controls is not initialized - cannot enable");
            return;
        }
        
        _playerControls = PlayerInputManager.Instance.PlayerControls;

        _playerControls.PlayerLocomotionMap.Enable();
        _playerControls.PlayerLocomotionMap.SetCallbacks(this);
        
        LateUpdatePublisher.RegisterObserver(this);
    }

    private void OnDisable()
    {
        if (_playerControls == null)
        {
            Debug.LogError("Player controls is not initialized - cannot disable");
            return;
        }
        
        _playerControls.PlayerLocomotionMap.Disable();
        _playerControls.PlayerLocomotionMap.RemoveCallbacks(this);
        
        LateUpdatePublisher.UnregisterObserver(this);
    }

  
    public void ObservedLateUpdate()
    {
        if (JumpPressed) CrouchToggledOn = false;
        JumpPressed = false;
    }
    
    public void OnMovement(InputAction.CallbackContext context)
    {
        MovementInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        JumpPressed = true;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    public void OnToggleSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SprintToggledOn = holdToSprint || !SprintToggledOn;
        }
        else if (context.canceled)
        {
            SprintToggledOn = !holdToSprint && SprintToggledOn;
        }
    }

    public void OnToggleCrouch(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        CrouchToggledOn = !CrouchToggledOn;
    }
    
}
