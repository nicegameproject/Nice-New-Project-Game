using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour, IUpdateObserver
{
    [SerializeField] private Animator animator;
    [SerializeField] private float locomotionBlendSpeed = 0.02f;

    private PlayerLocomotionInput _playerLocomotionInput;
    
    
    public static int InputXHash { get; private  set; } = Animator.StringToHash("InputX");
    public static int InputYHash { get; private set; } = Animator.StringToHash("InputY");
    public static int InputMagnitudeHash { get; private set; } = Animator.StringToHash("InputMagnitude");


    private Vector3 _currentBlendInput = Vector3.zero;
    
    private const float _sprintMaxBlendValue = 1.5f;
    private const float _runMaxBlendValue = 1.0f;
    private const float _walkMaxBlendValue = 0.5f;

    private void Awake()
    {
        _playerLocomotionInput = GetComponent<PlayerLocomotionInput>();
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
        
        animator.SetFloat(InputXHash, _currentBlendInput.x);
        animator.SetFloat(InputYHash, _currentBlendInput.y);
        animator.SetFloat(InputMagnitudeHash, _currentBlendInput.magnitude);
    }
}
