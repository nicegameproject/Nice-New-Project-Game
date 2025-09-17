using System;
using Core;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour, IUpdateObserver
{
    [SerializeField] private Animator animator;
    [SerializeField] private float locomotionBlendSpeed = 0.02f;
    
    private PlayerLocomotionInput _playerLocomotionInput;

    public bool IsCrouching { get; set; }
    public bool IsJumping { get; set; }
    public bool IsFalling { get; set; }
    public bool IsGrounded => _playerController.InGroundedState;
    
    private PlayerController _playerController;
    
    private static readonly int inputXHash = Animator.StringToHash("InputX");
    private static readonly int inputYHash = Animator.StringToHash("InputY");
    private static readonly int inputMagnitudeHash = Animator.StringToHash("InputMagnitude");
    private static readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int isFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int isCrouchingHash = Animator.StringToHash("IsCrouching");

    
    private Vector3 _currentBlendInput = Vector3.zero;
    
    
    private const float _sprintMaxBlendValue = 1.5f;
    private const float _runMaxBlendValue = 1.0f;
    private const float _walkMaxBlendValue = 0.5f;
    
    private Vector3 _movementInput;
    
    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        _playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        PlayerLocomotionInput.OnMovementInput += OnMovementInputChange;
        UpdatePublisher.RegisterObserver(this);
    }

    private void OnDisable()
    {
        PlayerLocomotionInput.OnMovementInput -= OnMovementInputChange;
        UpdatePublisher.UnregisterObserver(this);
    }

    public void ObservedUpdate()
    {
        UpdateAnimationState();
    }

    private void OnMovementInputChange(Vector2 input) => _movementInput = input;
    
    private void UpdateAnimationState()
    {
        var inputTarget = _movementInput * _runMaxBlendValue;

        _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);
        
        animator.SetBool(isGroundedHash, IsGrounded);
        animator.SetBool(isJumpingHash, IsJumping);
        animator.SetBool(isFallingHash, IsFalling);
        animator.SetBool(isCrouchingHash, IsCrouching);
        
        animator.SetFloat(inputXHash, _currentBlendInput.x);
        animator.SetFloat(inputYHash, _currentBlendInput.y);
        animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);
    }
}
