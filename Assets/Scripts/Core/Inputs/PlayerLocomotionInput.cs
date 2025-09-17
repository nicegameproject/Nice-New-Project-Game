using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
public class PlayerLocomotionInput : MonoBehaviour, PlayerControls.IPlayerLocomotionMapActions
{
    public static event Action<Vector2> OnMovementInput;
    public static event Action<Vector2> OnLookInput;
    public static event Action OnJumpStarted;
    public static event Action OnJumpCanceled;
    public static event Action OnSprintStarted;
    public static event Action OnSprintCanceled;
    public static event Action OnCrouchStarted;

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
    }

    private void OnDisable()
    {
        if (_playerControls == null) return;
        _playerControls.PlayerLocomotionMap.Disable();
        _playerControls.PlayerLocomotionMap.RemoveCallbacks(this);
    }

    private void OnDestroy()
    {
        if (_playerControls == null) return;
        _playerControls.PlayerLocomotionMap.Disable();
        _playerControls.PlayerLocomotionMap.RemoveCallbacks(this);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        OnLookInput?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        OnMovementInput?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.started) OnJumpStarted?.Invoke();
        if(context.canceled) OnJumpCanceled?.Invoke();
    }

    public void OnToggleSprint(InputAction.CallbackContext context)
    {
        if(context.started) OnSprintStarted?.Invoke();
        if(context.canceled) OnSprintCanceled?.Invoke();
    }

    public void OnToggleCrouch(InputAction.CallbackContext context)
    {
        if(context.started) OnCrouchStarted?.Invoke();
    }
}