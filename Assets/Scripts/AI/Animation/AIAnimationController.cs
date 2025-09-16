using UnityEngine;

[DisallowMultipleComponent]
public class AIAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator _animator;

    private bool _isDead;
    private MovementAnimState _lastMoveState = MovementAnimState.None;

    private enum MovementAnimState { None, Walk, Run, Idle }

    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private static readonly int HashDeath = Animator.StringToHash("Death");
    private static readonly int HashWalk = Animator.StringToHash("Walk");
    private static readonly int HashRun = Animator.StringToHash("Run");
    private static readonly int HashIdle = Animator.StringToHash("Idle");
    private static readonly int HashTakeHit = Animator.StringToHash("TakeHit");
    private static readonly int HashHitVariant = Animator.StringToHash("HitVariant");

    void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    public void PlayWalk() => TrySetMoveState(MovementAnimState.Walk, true);
    public void PlayRun() => TrySetMoveState(MovementAnimState.Run, true);
    public void PlayIdle() => TrySetMoveState(MovementAnimState.Idle, true);

    public void PlayAttack()
    {
        if (_isDead) return;
        SetTrigger(HashAttack);
    }

    public void PlayDeath()
    {
        if (_isDead) return;
        _isDead = true;
        SetTrigger(HashDeath);
    }

    public void PlayTakeHit()
    {
        if (_isDead) return;
        if (_animator == null) return;

        var info = _animator.GetCurrentAnimatorStateInfo(0);
        if ((info.IsName("TakeHit") || info.IsName("TakeHit2")) && info.normalizedTime < 0.8f)
            return;

        int variant = Random.Range(0, 2);
        _animator.SetInteger(HashHitVariant, variant);
        SetTrigger(HashTakeHit);
    }

    public bool IsAttackPlaying()
    {
        if (_animator == null) return false;
        var info = _animator.GetCurrentAnimatorStateInfo(0);
        if ((info.shortNameHash == HashAttack || info.IsName("Attack")) && info.normalizedTime < 0.8f)
            return true;
        return false;
    }

    private void TrySetMoveState(MovementAnimState state, bool force)
    {
        if (_isDead) return;
        if (!force && state == _lastMoveState) return;

        switch (state)
        {
            case MovementAnimState.Walk: SetTrigger(HashWalk); break;
            case MovementAnimState.Run: SetTrigger(HashRun); break;
            case MovementAnimState.Idle: SetTrigger(HashIdle); break;
            case MovementAnimState.None:

                break;
        }

        _lastMoveState = state;
    }

    private void SetTrigger(int hash)
    {
        if (_animator == null) return;
        _animator.ResetTrigger(hash);
        _animator.SetTrigger(hash);
    }

    private void ResetTriggerAnimation(int hash)
    {
        if (_animator == null) return;
        _animator.ResetTrigger(hash);
    }

    public void ResetIdleAnimation()
    {
        ResetTriggerAnimation(HashIdle);
    }

    public void ResetWalkAnimation()
    {
        ResetTriggerAnimation(HashWalk);
    }

    private void OnValidate()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

}