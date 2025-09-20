using System.Collections;
using UnityEngine;

public sealed class AttackState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;

    private AttackDefinitionBase _currentAttack;
    private Coroutine _routine;

    public string Name => "Attack";

    public AttackState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _ai.Locomotion.SetStoppingDistance(_ai.Config.PreferredAttackRange + 0.05f);
        _ai.Locomotion.StopImmediate();
        _ai.Locomotion.SetSpeedToZero();

        _ai.Combat.TrySelectAttack(_bb.DistanceToTarget, out _currentAttack);
        _routine = _ai.StartCoroutine(AttackRoutine());
    }

    public void Update() { }

    public void Exit()
    {
        _ai.Animation.PlayWalk();

        if (_routine != null)
        {
            _ai.StopCoroutine(_routine);
            _routine = null;
        }

        _ai.Locomotion.SetStoppingDistance(_ai.Config.StoppingDistance);
        _ai.Locomotion.Agent.updateRotation = true;
    }

    private IEnumerator AttackRoutine()
    {
        while (IsThisStateCurrent())
        {
            if (_ai.Health.IsDead)
            {
                DoTransition(new DeathState(_ai, _bb));
                yield break;
            }

            if (_bb.DistanceToTarget > _ai.Config.PreferredAttackRange)
            {
                if (!_ai.Animation.IsAttackPlaying())
                {
                    DoTransition(new ChaseState(_ai, _bb));
                    yield break;
                }
                _ai.Locomotion.StopImmediate();
            }

            FaceTarget();

            if (!_ai.Animation.IsAttackPlaying())
            {
                if (_bb.DistanceToTarget > _ai.Config.PreferredAttackRange)
                {
                    DoTransition(new ChaseState(_ai, _bb));
                    yield break;
                }

                if (_currentAttack == null)
                {
                    _ai.Combat.TrySelectAttack(_bb.DistanceToTarget, out _currentAttack);
                }

                if (_currentAttack != null)
                {
                    _ai.Animation.PlayAttack();

                    if (_bb.Target != null)
                        _ai.Combat.ExecuteAttack(_currentAttack, _bb.Target);

                    _currentAttack = null;
                }
            }
            else
            {
                _ai.Locomotion.StopImmediate();
            }

            yield return null;
        }
    }

    private void FaceTarget()
    {
        if (_bb.Target != null)
            _ai.Locomotion.FaceTowards(_bb.Target.position, 300f);
    }

    private void DoTransition(IAIState next)
    {
        _ai.Locomotion.Agent.updateRotation = true;
        _ai.StateMachine.ChangeState(next);
    }

    private bool IsThisStateCurrent()
    {
        return ReferenceEquals(_ai.StateMachine.Current, this);
    }
}