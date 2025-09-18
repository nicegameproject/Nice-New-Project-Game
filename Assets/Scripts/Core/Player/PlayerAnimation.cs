using System;
using Core;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour, IUpdateObserver
{
    [SerializeField] private Animator animator;
    [SerializeField] private float locomotionBlendSpeed = 0.02f;

    public bool IsCrouching { get; set; }
    public bool IsJumping { get; set; }
    public bool IsFalling { get; set; }
    public bool IsGrounded { get; set; }
    
    private static readonly int inputXHash = Animator.StringToHash("InputX");
    private static readonly int inputYHash = Animator.StringToHash("InputY");
    private static readonly int inputMagnitudeHash = Animator.StringToHash("InputMagnitude");
    private static readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int isFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int isCrouchingHash = Animator.StringToHash("IsCrouching");
    
    private Vector3 _currentBlendInput = Vector3.zero;
    private Vector3 _movementInput;

    private float _moveBlendValue = 1.0f;

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
        var inputTarget = _movementInput * _moveBlendValue;
        _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);
        
        animator.SetBool(isGroundedHash, IsGrounded);
        animator.SetBool(isJumpingHash, IsJumping);
        animator.SetBool(isFallingHash, IsFalling);
        animator.SetBool(isCrouchingHash, IsCrouching);
        
        animator.SetFloat(inputXHash, _currentBlendInput.x);
        animator.SetFloat(inputYHash, _currentBlendInput.y);
        animator.SetFloat(inputMagnitudeHash, _currentBlendInput.magnitude);
    }

    public void SetMoveBlendValue(float value)
    {
        _moveBlendValue = value;
    }
}
