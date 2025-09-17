using System;
using System.Numerics;
using Core;
using Unity.VisualScripting;
using UnityEngine;
using IState = Core.IState;
using Quaternion = UnityEngine.Quaternion;
using StateMachine = Core.StateMachine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour, IUpdateObserver, ILateUpdateObserver
{
    [Header("Components")] 
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerAnimation playerAnimation;

    [Header("Base Movement")] 
    [SerializeField] private float sprintAcceleration = 50f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float drag = 20f;
    [SerializeField] private float gravity = 25f;
    [SerializeField] private float terminalVelocity = 50f;
    [SerializeField] private float jumpSpeed = 0.8f;
    [SerializeField] public float movingThreshold = 0.01f;
    [SerializeField] private bool holdToSprint = true;
    
    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1.35f;

    [SerializeField] private LayerMask ceilingLayer;

    [SerializeField] private float cameraCrouchOffsetY = 0.175f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    
    [Header("Animation")]
    [SerializeField] private float playerModelRotationSpeed = 10f;
    [SerializeField] private float rotateToTargetTime = 0.25f;

    [Header("Camera Settings")]
    [SerializeField] private float lookSenseH = 0.1f;
    [SerializeField] private float lookSenseV = 0.1f;
    [SerializeField] private float lookLimitV = 89f;
    
    [Header("Environment Details")] 
    [SerializeField] private LayerMask groundLayers;

    [field: SerializeField] public bool InGroundedState { get; private set; }
    
    public float PlayerHeight
    {
        get => characterController.height;
        private set => characterController.height = value;
    }
    
    public bool IsTryingToCrouch { get; set; }

    public float AntiBump { get; private set; }
    
    private PlayerLocomotionInput _playerLocomotionInput;
    private StateMachine _stateMachine;
    
    private Vector2 _cameraRotation = Vector2.zero;
    private Vector2 _playerTargetRotation = Vector2.zero;
    private Vector3 _extraVelocity = Vector3.zero;

    private bool _jumpedLastFrame = false;
    private float _verticalVelocity = 0f;
    
    private float _currentHeight;
    private float _standingHeight;
    private Vector3 initialCameraPosition;
    private Vector3 initialCenter;
    private float _crouchBlend;
    
    private Vector2 _movementInput;
    private Vector2 _lookInput;
    private bool _jumpInput;
    private bool _crouchInput;
    
    private void HandleMoveInput(Vector2 input) => _movementInput = input;
    private void HandleLookInput(Vector2 input) => _lookInput = input;
    private void HandleJumpStarted() => _jumpInput = true;
    private void HandleJumpCanceled() => _jumpInput = false;
    private void HandleCrouchStarted() => _crouchInput = !_crouchInput;
    
    private bool CanStandFromCrouch() => !_crouchInput && !CheckIfCeilingIsAbove();

    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();

        AntiBump = sprintSpeed;
        IsTryingToCrouch = false;
        
        SetupStateMachine();
    }

    private void Start()
    {
        initialCameraPosition  = playerCamera.transform.localPosition;
        initialCenter = characterController.center;
        _standingHeight = _currentHeight = PlayerHeight;
    }

    private void OnEnable()
    {
        PlayerLocomotionInput.OnMovementInput += HandleMoveInput;
        PlayerLocomotionInput.OnLookInput += HandleLookInput;
        PlayerLocomotionInput.OnJumpStarted += HandleJumpStarted;
        PlayerLocomotionInput.OnJumpCanceled += HandleJumpCanceled;
        PlayerLocomotionInput.OnCrouchStarted += HandleCrouchStarted;
        
        UpdatePublisher.RegisterObserver(this);
        LateUpdatePublisher.RegisterObserver(this);
    }

    private void OnDisable()
    {
        PlayerLocomotionInput.OnMovementInput -= HandleMoveInput;
        PlayerLocomotionInput.OnLookInput -= HandleLookInput;
        PlayerLocomotionInput.OnJumpStarted += HandleJumpStarted;
        PlayerLocomotionInput.OnJumpCanceled += HandleJumpCanceled;
        PlayerLocomotionInput.OnCrouchStarted -= HandleCrouchStarted;
        
        UpdatePublisher.UnregisterObserver(this);
        LateUpdatePublisher.UnregisterObserver(this);
    }
    
    private void SetupStateMachine()
    {
        _stateMachine = new StateMachine();
        
        var idleState = new Core.IdleState(this, playerAnimation);
        var walkingState = new Core.WalkingState(this, playerAnimation);
        var jumpState = new Core.JumpState(this, playerAnimation);
        var fallingState = new Core.FallingState(this, playerAnimation);
        var crouchState = new Core.CrouchState(this, playerAnimation);
            
        AddTransition(walkingState, idleState, new FuncPredicate(() => !(_movementInput != Vector2.zero) || !IsMovingLaterally()));
        AddTransition(crouchState, idleState, new FuncPredicate(CanStandFromCrouch));
        AddTransition(fallingState, idleState, new FuncPredicate(IsGrounded));
        
        AddTransition(idleState, walkingState, new FuncPredicate(() => (_movementInput != Vector2.zero)));
        AddTransition(idleState, jumpState, new FuncPredicate(() => (!IsGrounded() || _jumpedLastFrame) && characterController.velocity.y > 0f));
        AddTransition(walkingState, jumpState, new FuncPredicate(() => (!IsGrounded() || _jumpedLastFrame) && characterController.velocity.y > 0f));
        
        // AddTransition(walkingState, jumpState, new FuncPredicate(() => (!IsGrounded() || _jumpedLastFrame) && characterController.velocity.y > 0f));
        // AddTransition(jumpState, crouchState, new FuncPredicate(() => _jumpedLastFrame && characterController.velocity.y > 0f && CanJumpFromCrouching()));
        
        AnyTransition(fallingState,new FuncPredicate(() => (!IsGrounded() || _jumpedLastFrame) && characterController.velocity.y <= 0f && !CheckIfCeilingIsAbove()));
        //AnyTransition(jumpState, new FuncPredicate(() => (!IsGrounded() || _jumpedLastFrame) && characterController.velocity.y > 0f));
        AnyTransition(crouchState, new FuncPredicate(() => IsGrounded() && _crouchInput));
        //AnyTransition(idleState, new FuncPredicate(() => (!IsMovementInput || !IsMovingLaterally()) && CanStandFromCrouch()));
        
        _stateMachine.SetState(idleState);
    }
   

    
    private void AddTransition(IState from, IState to, IPredicate condition) =>
        _stateMachine.AddTransition(from, to, condition);

    private void AnyTransition(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);
    
    

    public void ObservedUpdate()
    {
        _stateMachine.Update();

        CheckIfCeilingIsAbove();
        
        HandleCrouching(IsTryingToCrouch);
        HandleVerticalMovement();
        HandleLateralMovement();
    }
    

    public void HandleCrouching(bool shouldCrouch)
    {
        var targetBlend = shouldCrouch ? 1f : 0f;
        
        var step = Time.deltaTime * crouchTransitionSpeed;
        _crouchBlend = Mathf.MoveTowards(_crouchBlend, targetBlend, step);

        var heightDelta = _standingHeight - crouchHeight;
        var halfHeightDelta = heightDelta * 0.5f;

        var newHeight = _standingHeight - _crouchBlend * heightDelta;

        var centerOffset = Vector3.up * ( _crouchBlend * halfHeightDelta );
        var newCenter = initialCenter - centerOffset;

        var totalCameraDown = halfHeightDelta + cameraCrouchOffsetY;
        var cameraOffset = Vector3.up * (_crouchBlend * totalCameraDown);
        var newCameraPos = initialCameraPosition - cameraOffset;

        PlayerHeight = newHeight;
        characterController.center = newCenter;
        playerCamera.transform.localPosition = newCameraPos;
    }


    private bool CheckIfCeilingIsAbove()
    {
        var heightDifference = _standingHeight - PlayerHeight;
        var castOrigin = transform.position + new Vector3(0, PlayerHeight, 0);
        return Physics.Raycast(castOrigin, Vector3.up, heightDifference, ceilingLayer, QueryTriggerInteraction.Ignore);
    }

    private void HandleLateralMovement()
    {
        var cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z)
            .normalized;

        var cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z).normalized;

        var movementDirection = cameraRightXZ * _movementInput.x + 
                                cameraForwardXZ * _movementInput.y;

        var movementDelta = movementDirection * sprintAcceleration * Time.deltaTime;
        var newVelocity = characterController.velocity + movementDelta;

        var currentDrag = newVelocity.normalized * drag * Time.deltaTime;

        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0, newVelocity.z), sprintSpeed);
        newVelocity.y += _verticalVelocity;
        newVelocity += new Vector3(_extraVelocity.x, 0, _extraVelocity.z);

        characterController.Move(newVelocity * Time.deltaTime);
        _extraVelocity = Vector3.zero;
    }

    private void HandleVerticalMovement()
    {
        _verticalVelocity -= gravity * Time.deltaTime;

        if (IsGrounded() && _verticalVelocity < 0)
            _verticalVelocity = -AntiBump;
        

        if (_jumpInput && InGroundedState)
        {
            if (!CheckIfCeilingIsAbove())
                _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
            _jumpedLastFrame = true;
        }
        

        if (Mathf.Abs(_verticalVelocity) > Mathf.Abs(terminalVelocity))
        {
            _verticalVelocity = -1f * Mathf.Abs(terminalVelocity);
        }

        _verticalVelocity += _extraVelocity.y;
    }

    public void AddVelocity(Vector3 vector)
    {
        _extraVelocity += vector;
    }

    public void SetGroundedState(bool isGrounded)
    {
        InGroundedState = isGrounded;
    }
    
    public void ObservedLateUpdate()
    {
        if (_jumpInput) _crouchInput = false;
        _jumpInput = false;
        
        UpdateCameraRotation();
    }

    public void SetJumpedLastFrame(bool jumpedLastFrame)
    {
        _jumpedLastFrame = jumpedLastFrame;
    }

    private void UpdateCameraRotation()
    {
        _cameraRotation.x += lookSenseH * _lookInput.x;
        _cameraRotation.y = Mathf.Clamp(_cameraRotation.y - lookSenseV * _lookInput.y,
            -lookLimitV, lookLimitV);

        RotatePlayerToTarget();
        
        _playerTargetRotation.x += transform.eulerAngles.x + lookSenseH * _lookInput.x;

        playerCamera.transform.rotation = Quaternion.Euler(_cameraRotation.y, _cameraRotation.x, 0f);

    }

    private void RotatePlayerToTarget()
    {
        var targetRotationX = Quaternion.Euler(0f, _playerTargetRotation.x, 0f);
        transform.rotation =
            Quaternion.Lerp(transform.rotation, targetRotationX, playerModelRotationSpeed * Time.deltaTime);
    }

    private bool IsMovingLaterally()
    {
        var lateralVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        return lateralVelocity.magnitude > movingThreshold;
    }
    
    private bool IsGrounded()
    {
        var grounded = InGroundedState ? IsGroundedWhileGrounded() : IsGroundedWhileAirborne();
        return grounded;
    }

    private bool IsGroundedWhileGrounded()
    {
        var spherePosition = new Vector3(transform.position.x, transform.position.y - characterController.radius,
            transform.position.z);

        var grounded = Physics.CheckSphere(spherePosition, characterController.radius, groundLayers,
            QueryTriggerInteraction.Ignore);

        return grounded;
    }

    private bool IsGroundedWhileAirborne()
    {
        var normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
        var angle = Vector3.Angle(normal, Vector3.up);
        var validAngle = angle <= characterController.slopeLimit;

        return characterController.isGrounded && validAngle;
    }
}
