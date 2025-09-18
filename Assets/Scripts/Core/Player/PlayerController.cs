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
    [field: SerializeField] public float SprintAcceleration { get; private set; } = 50;
    [field: SerializeField] public float SprintSpeed { get; private set; } = 8f;
    [field: SerializeField] public float RunAcceleration { get; private set; } = 35;
    [field: SerializeField] public float RunSpeed { get; private set; } = 4f;
    [field: SerializeField] public float WalkAcceleration { get; private set; } = 25;
    [field: SerializeField] public float WalkSpeed { get; private set; } = 2f;
    [SerializeField] private float drag = 20f;
    [SerializeField] private float gravity = 25f;
    [SerializeField] private float terminalVelocity = 50f;
    [SerializeField] private float jumpSpeed = 0.8f;
    [SerializeField] private float movingThreshold = 0.01f;
    // [SerializeField] private bool holdToSprint = true;
    
    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1.35f;

    [SerializeField] private LayerMask ceilingLayer;

    [SerializeField] private float cameraCrouchOffsetY = 0.175f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    
    [Header("Animation")]
    [SerializeField] private float playerModelRotationSpeed = 10f;
    //[SerializeField] private float rotateToTargetTime = 0.25f;
    [field: SerializeField] public float SprintMaxBlendValue { get; private set; } = 1.5f;
    [field: SerializeField] public float RunMaxBlendValue { get; private set; } = 1f;
    [field: SerializeField] public float WalkMaxBlendValue { get; private set; } = 0.5f;

    [Header("Camera Settings")]
    [SerializeField] private float lookSenseH = 0.1f;
    [SerializeField] private float lookSenseV = 0.1f;
    [SerializeField] private float lookLimitV = 89f;
    
    [Header("Environment Details")] 
    [SerializeField] private LayerMask groundLayers;

    [field: SerializeField] public bool InGroundedState { get; set; }
    
    public float PlayerHeight
    {
        get => characterController.height;
        private set => characterController.height = value;
    }
    
    public bool IsCrouching { get; set; }
    public float AntiBump { get; private set; }
    
    private Core.StateMachine _stateMachine;
    
    private Vector2 _cameraRotation = Vector2.zero;
    private Vector2 _playerTargetRotation = Vector2.zero;
    private Vector3 _extraVelocity = Vector3.zero;
    private float _currentSpeed;
    private float _currentAcceleration;

    private bool _jumpedLastFrame = false;
    private float _verticalVelocity = 0f;
    
    private float _standingHeight;
    private Vector3 _initialCameraPosition;
    private Vector3 _initialCenter;
    private float _crouchBlend;
    
    private Vector2 _movementInput;
    private Vector2 _lookInput;
    private bool _jumpInput;
    private bool _crouchInput;
    private bool _sprintInput;
    
    private void HandleMoveInput(Vector2 input) => _movementInput = input;
    private void HandleLookInput(Vector2 input) => _lookInput = input;
    private void HandleJumpStarted() => _jumpInput = true;
    private void HandleJumpCanceled() => _jumpInput = false;
    private void HandleCrouchStarted() => _crouchInput = true;
    private void HandleCrouchCanceled() => _crouchInput = false;
    private void HandleSprintStarted() => _sprintInput = true;
    private void HandleSprintCanceled() => _sprintInput = false;

    private void Awake()
    {
        AntiBump = SprintSpeed;
        IsCrouching = false;
        
        _currentSpeed = RunSpeed;
        _currentAcceleration = RunAcceleration;
        
        SetupStateMachine();
    }

    private void Start()
    {
        _initialCameraPosition  = playerCamera.transform.localPosition;
        _initialCenter = characterController.center;
        _standingHeight = PlayerHeight;
    }

    private void OnEnable()
    {
        SubscribeInputs();
        UpdatePublisher.RegisterObserver(this);
        LateUpdatePublisher.RegisterObserver(this);
    }

    private void SubscribeInputs()
    {
        PlayerLocomotionInput.OnMovementInput += HandleMoveInput;
        PlayerLocomotionInput.OnLookInput += HandleLookInput;
        PlayerLocomotionInput.OnJumpStarted += HandleJumpStarted;
        PlayerLocomotionInput.OnJumpCanceled += HandleJumpCanceled;
        PlayerLocomotionInput.OnCrouchStarted += HandleCrouchStarted;
        PlayerLocomotionInput.OnCrouchCanceled += HandleCrouchCanceled;
        PlayerLocomotionInput.OnSprintStarted += HandleSprintStarted;
        PlayerLocomotionInput.OnSprintCanceled += HandleSprintCanceled;
    }

    private void OnDisable()
    {
        UnsubscribeInputs();
        
        UpdatePublisher.UnregisterObserver(this);
        LateUpdatePublisher.UnregisterObserver(this);
    }

    private void OnDestroy()
    {
        UnsubscribeInputs();
        
        UpdatePublisher.UnregisterObserver(this);
        LateUpdatePublisher.UnregisterObserver(this);
    }

    private void UnsubscribeInputs()
    {
        PlayerLocomotionInput.OnMovementInput -= HandleMoveInput;
        PlayerLocomotionInput.OnLookInput -= HandleLookInput;
        PlayerLocomotionInput.OnJumpStarted -= HandleJumpStarted;
        PlayerLocomotionInput.OnJumpCanceled -= HandleJumpCanceled;
        PlayerLocomotionInput.OnCrouchStarted -= HandleCrouchStarted;
        PlayerLocomotionInput.OnCrouchCanceled -= HandleCrouchCanceled;
        PlayerLocomotionInput.OnSprintStarted -= HandleSprintStarted;
        PlayerLocomotionInput.OnSprintCanceled -= HandleSprintCanceled;
    }
    
    private void SetupStateMachine()
    {
        _stateMachine = new StateMachine();
        
        var idleState = new Core.IdleState(this, playerAnimation);
        var runningState = new Core.RunningState(this, playerAnimation);
        var sprintState = new Core.SprintState(this, playerAnimation);
        var jumpState = new Core.JumpState(this, playerAnimation);
        var fallingState = new Core.FallingState(this, playerAnimation);
        var crouchState = new Core.CrouchState(this, playerAnimation);
            
        AddTransition(runningState, idleState, new FuncPredicate(() => !CanMove() || !IsMovingLaterally()));
        AddTransition(sprintState, idleState, new FuncPredicate(() => !CanMove() || !IsMovingLaterally()));
        AddTransition(sprintState, runningState, new FuncPredicate(() => IsMovingLaterally() && !_sprintInput));
        AddTransition(crouchState, idleState, new FuncPredicate(() => !_crouchInput && !CheckIfCeilingIsAbove()));
        AddTransition(fallingState, idleState, new FuncPredicate(IsGrounded));
        
        AddTransition(idleState, runningState, new FuncPredicate(() => CanMove()));
        AddTransition(idleState, sprintState, new FuncPredicate(() => IsMovingLaterally() && _sprintInput));
        AddTransition(runningState, sprintState, new FuncPredicate(() => IsMovingLaterally() && _sprintInput));
        
        AnyTransition(jumpState, new FuncPredicate(() => (!IsGrounded() || _jumpedLastFrame) && characterController.velocity.y > 0f));
        AnyTransition(fallingState,new FuncPredicate(() => (!IsGrounded() || _jumpedLastFrame) && characterController.velocity.y <= 0f));
        AnyTransition(crouchState, new FuncPredicate(() => IsGrounded() && _crouchInput));
        
        _stateMachine.SetState(idleState);
    }

    private bool CanMove() => _movementInput != Vector2.zero;
    
    private void AddTransition(IState from, IState to, IPredicate condition) =>
        _stateMachine.AddTransition(from, to, condition);

    private void AnyTransition(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

    public void ObservedUpdate()
    {
        _stateMachine.Update();

        CheckIfCeilingIsAbove();
        
        HandleCrouching();
        HandleVerticalMovement();
        HandleLateralMovement();
    }
    

    private void HandleCrouching()
    {
        var targetBlend = IsCrouching ? 1f : 0f;
        
        var step = Time.deltaTime * crouchTransitionSpeed;
        _crouchBlend = Mathf.MoveTowards(_crouchBlend, targetBlend, step);

        var heightDelta = _standingHeight - crouchHeight;
        var halfHeightDelta = heightDelta * 0.5f;

        var newHeight = _standingHeight - _crouchBlend * heightDelta;

        var centerOffset = Vector3.up * ( _crouchBlend * halfHeightDelta );
        var newCenter = _initialCenter - centerOffset;

        var totalCameraDown = halfHeightDelta + cameraCrouchOffsetY;
        var cameraOffset = Vector3.up * (_crouchBlend * totalCameraDown);
        var newCameraPos = _initialCameraPosition - cameraOffset;

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

        var movementDelta = movementDirection * _currentAcceleration * Time.deltaTime;
        var newVelocity = characterController.velocity + movementDelta;

        var currentDrag = newVelocity.normalized * drag * Time.deltaTime;

        newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
        newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0, newVelocity.z), _currentSpeed);
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
        

        if (_jumpInput && InGroundedState && !CheckIfCeilingIsAbove())
        {
            _verticalVelocity += Mathf.Sqrt(jumpSpeed * 3 * gravity);
            _jumpedLastFrame = true;
        }

        if (Mathf.Abs(_verticalVelocity) > Mathf.Abs(terminalVelocity))
        {
            _verticalVelocity = -1f * Mathf.Abs(terminalVelocity);
        }

        _verticalVelocity += _extraVelocity.y;
    }


    public void ObservedLateUpdate()
    {
        _jumpInput = false;
        UpdateCameraRotation();
    }

    public void AddVelocity(Vector3 vector) => _extraVelocity += vector;
    public void SetJumpedLastFrame(bool jumpedLastFrame) => jumpedLastFrame = jumpedLastFrame;
    public void SetSpeed(float newSpeed) => _currentSpeed = newSpeed;
    public void SetAcceleration(float newAcceleration) => _currentAcceleration = newAcceleration;

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