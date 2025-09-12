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
        _ai.Locomotion.Agent.updateRotation = false;

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
        while (true)
        {
            FaceTargetFast();

            IAIState pendingTransition;
            if (CheckTransitions(out pendingTransition, effectiveRangeOverride: GetEffectiveRange(), onlyWhenNotAttacking: true))
            {
                DoTransition(pendingTransition);
                yield break;
            }

            if (_currentAttack == null)
            {
                _ai.Combat.TrySelectAttack(_bb.DistanceToTarget, out _currentAttack);

                if (_currentAttack == null)
                {
                    yield return WaitWithChecks(0.1f);
                    if (!IsThisStateCurrent()) yield break;
                    continue;
                }
            }

            _ai.Animation.PlayAttack();

            float windup = Mathf.Max(0f, _currentAttack.Windup);
            if (windup > 0f)
                yield return WaitWithChecks(windup);

            if (!IsThisStateCurrent()) yield break;

            if (_bb.Target != null)
                _ai.Combat.ExecuteAttack(_currentAttack, _bb.Target);

            while (_ai.Animation.IsAttackPlaying())
            {
                FaceTargetFast();

                if (CheckTransitions(out pendingTransition, effectiveRangeOverride: GetEffectiveRange(), onlyWhenNotAttacking: false))
                {
                    DoTransition(pendingTransition);
                    yield break;
                }

                yield return null;
                if (!IsThisStateCurrent()) yield break;
            }

            yield return WaitWithChecks(0.05f);
            if (!IsThisStateCurrent()) yield break;

            _currentAttack = null;
        }
    }

    private IEnumerator WaitWithChecks(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            IAIState next;
            if (CheckTransitions(out next, effectiveRangeOverride: GetEffectiveRange(), onlyWhenNotAttacking: !_ai.Animation.IsAttackPlaying()))
            {
                DoTransition(next);
                yield break;
            }

            t += Time.deltaTime;
            yield return null;

            if (!IsThisStateCurrent())
                yield break;
        }
    }

    private float GetEffectiveRange()
    {
        return _ai.Config.PreferredAttackRange;
    }

    private void FaceTargetFast()
    {
        if (_currentAttack != null && _currentAttack.Id == "Laser Attack")
        {
            if (_bb.Target != null)
                _ai.Locomotion.FaceTowards(_bb.Target.position, 10f);
        }
        else
        {
            if (_bb.Target != null)
                _ai.Locomotion.FaceTowards(_bb.Target.position, 200f);
        }

    }

    private bool CheckTransitions(out IAIState next, float effectiveRangeOverride, bool onlyWhenNotAttacking)
    {
        next = null;

        if (_ai.Health.IsDead)
        {
            next = new DeathState(_ai, _bb);
            return true;
        }

        if (ShouldFlee())
        {
            next = new FleeState(_ai, _bb);
            return true;
        }

        if (onlyWhenNotAttacking && !_ai.Animation.IsAttackPlaying())
        {
            if (!_bb.HasLineOfSight || _bb.DistanceToTarget > effectiveRangeOverride)
            {
                next = new ChaseState(_ai, _bb);
                return true;
            }
        }

        return false;
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

    private bool ShouldFlee()
    {
        float hpPct = Mathf.Approximately(_ai.Health.Max, 0f) ? 0f : (_ai.Health.Current / _ai.Health.Max);
        return hpPct <= _ai.Config.FleeHealthThreshold && !_ai.Health.IsDead;
    }
}