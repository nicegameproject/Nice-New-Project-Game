using UnityEngine;

public sealed class AttackState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;

    private AttackDefinitionBase _currentAttack;
    private float _windupOrRetryTimer;

    public string Name => "Attack";

    public AttackState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _ai.Locomotion.StopImmediate();
        _ai.Locomotion.SetSpeedToZero();
        _ai.Locomotion.SetStoppingDistance(_ai.Config.PreferredAttackRange);

        _ai.Locomotion.Agent.updateRotation = false;

        if (_ai.Combat.TrySelectAttack(_bb.DistanceToTarget, out _currentAttack))
        {
            _windupOrRetryTimer = _currentAttack.Windup;
            _ai.Animation.PlayAttack();
        }
        else
        {
            _currentAttack = null;
            _windupOrRetryTimer = 0.1f;
        }
    }

    public void Update()
    {
        if (_bb.Target != null)
            _ai.Locomotion.FaceTowards(_bb.Target.position, 1080f);

        float effectiveRange = _currentAttack != null ? _currentAttack.Range : _ai.Config.PreferredAttackRange;

        _windupOrRetryTimer -= Time.deltaTime;
        if (_windupOrRetryTimer <= 0f)
        {
            if (_currentAttack != null)
            {
                _ai.Combat.ExecuteAttack(_currentAttack, _bb.Target);
                _currentAttack = null;
            }

            if (!_ai.Animation.IsAttackPlaying())
            {
                if (_ai.Combat.TrySelectAttack(_bb.DistanceToTarget, out _currentAttack))
                {
                    _windupOrRetryTimer = _currentAttack.Windup;
                    _ai.Animation.PlayAttack();
                }
                else
                {
                    _windupOrRetryTimer = 0.1f;
                }
            }
            else
            {
                _windupOrRetryTimer = 0.05f;
            }
        }

        if (!_ai.Animation.IsAttackPlaying())
        {
            if (!_bb.HasLineOfSight || _bb.DistanceToTarget > effectiveRange + 0.5f)
            {
                _ai.Locomotion.Agent.updateRotation = true;
                _ai.StateMachine.ChangeState(new ChaseState(_ai, _bb));
                return;
            }
        }

        if (ShouldFlee())
        {
            _ai.Locomotion.Agent.updateRotation = true;
            _ai.StateMachine.ChangeState(new FleeState(_ai, _bb));
            return;
        }

        if (_bb.IsDead)
        {
            _ai.Locomotion.Agent.updateRotation = true;
            _ai.StateMachine.ChangeState(new DeathState(_ai, _bb));
        }
    }

    public void Exit()
    {
        _ai.Locomotion.Agent.updateRotation = true;
    }

    private bool ShouldFlee()
    {
        float hpPct = Mathf.Approximately(_bb.MaxHealth, 0f) ? 0f : (_bb.CurrentHealth / _bb.MaxHealth);
        return hpPct <= _ai.Config.FleeHealthThreshold && !_bb.IsDead;
    }
}