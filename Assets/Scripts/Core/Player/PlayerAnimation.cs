using System;
using Core;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour, IUpdateObserver
{
    [SerializeField] private Animator animator;
    [SerializeField] private float locomotionBlendSpeed = 0.02f;
    
    private PlayerLocomotionInput _playerLocomotionInput;

    public bool IsJumping { get; set; }
    public bool IsFalling { get; set; }
    public bool IsGrounded => _playerController.InGroundedState;
    
    private PlayerController _playerController;
    
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int InputMagnitudeHash = Animator.StringToHash("InputMagnitude");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    
    private Vector3 _currentBlendInput = Vector3.zero;
    
    private const float _sprintMaxBlendValue = 1.5f;
    private const float _runMaxBlendValue = 1.0f;
    private const float _walkMaxBlendValue = 0.5f;
    
    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
        _playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        UpdatePublisher.RegisterObserver(this);
    }

    private void OnDisable()
    {
        UpdatePublisher.UnregisterObserver(this);
    }

    public void ObservedUpdate()
    {
        UpdateAnimationState();
    }
    
    private void UpdateAnimationState()
    {
        var inputTarget = _playerLocomotionInput.MovementInput * _runMaxBlendValue;

        _currentBlendInput = Vector3.Lerp(_currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);
        
        animator.SetBool(IsGroundedHash, IsGrounded);
        animator.SetBool(IsJumpingHash, IsJumping);
        animator.SetBool(IsFallingHash, IsFalling);
        
        animator.SetFloat(InputXHash, _currentBlendInput.x);
        animator.SetFloat(InputYHash, _currentBlendInput.y);
        animator.SetFloat(InputMagnitudeHash, _currentBlendInput.magnitude);
    }
}
