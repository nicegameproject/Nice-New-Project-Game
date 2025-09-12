using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour, IUpdateObserver, ILateUpdateObserver
{
    [Header("Components")] 
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera playerCamera;

    [Header("Base Movement")] 
    [SerializeField] private float sprintAcceleration = 50f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float drag = 20f;
    
    [Header("Animation")]
    [SerializeField] private float playerModelRotationSpeed = 10f;
    [SerializeField] private float rotateToTargetTime = 0.25f;

    [Header("Camera Settings")]
    [SerializeField] private float lookSenseH = 0.1f;
    [SerializeField] private float lookSenseV = 0.1f;
    [SerializeField] private float lookLimitV = 89f;
    
    private PlayerLocomotionInput _playerLocomotionInput;
    
    private Vector2 _cameraRotation = Vector2.zero;
    private Vector2 _playerTargetRotation = Vector2.zero;
    
    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
    }

    private void OnEnable()
    {
        UpdatePublisher.RegisterObserver(this);
        LateUpdatePublisher.RegisterObserver(this);
    }

    private void OnDisable()
    {
        UpdatePublisher.UnregisterObserver(this);
        LateUpdatePublisher.UnregisterObserver(this);
    }

    public void ObservedUpdate()
    {
        HandleLateralMovement();
    }
    
    
    private void HandleLateralMovement()
    {

        var cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z)
            .normalized;

        var cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z).normalized;

        var movementDirection = cameraRightXZ * _playerLocomotionInput.MovementInput.x + 
                                cameraForwardXZ * _playerLocomotionInput.MovementInput.y;

        var movementDelta = movementDirection * sprintAcceleration * Time.deltaTime;
        var newVelocity = characterController.velocity + movementDelta;

        var currentDrag = newVelocity.normalized * drag * Time.deltaTime;

        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0, newVelocity.z), sprintSpeed);
        

        characterController.Move(newVelocity * Time.deltaTime);
    }

    private void HandleVerticalMovement()
    {
        
    }
    
    public void ObservedLateUpdate()
    {
        UpdateCameraRotation();
    }

    private void UpdateCameraRotation()
    {
        _cameraRotation.x += lookSenseH * _playerLocomotionInput.LookInput.x;
        _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _playerLocomotionInput.LookInput.y,
            -lookLimitV, lookLimitV);

        RotatePlayerToTarget();
        
        _playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * _playerLocomotionInput.LookInput.x;

        playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);

    }

    private void RotatePlayerToTarget()
    {
        var targetRotationX = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
        transform.rotation =
            Quaternion.Lerp(transform.rotation, targetRotationX, playerModelRotationSpeed * Time.deltaTime);
    }
}
