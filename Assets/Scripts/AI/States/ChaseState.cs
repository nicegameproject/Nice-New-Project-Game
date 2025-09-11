using UnityEngine;

public sealed class ChaseState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;
    private float _repathTimer;
    private float _lostTimer;

    public string Name => "Chase";

    public ChaseState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _repathTimer = 0f;
        _lostTimer = 0f;

        _ai.Animation.PlayRun();
        _ai.Locomotion.SetSpeedRun();
        _ai.Locomotion.Resume();
    }

    public void Update()
    {
        _repathTimer += Time.deltaTime;

        if (_bb.HasLineOfSight)
        {
            _lostTimer = 0f;
            _bb.LastKnownTargetPos = _bb.Target != null ? _bb.Target.position : _bb.LastKnownTargetPos;
        }
        else
        {
            _lostTimer += Time.deltaTime;
        }

        if (_repathTimer >= _ai.Config.RepathIntervalChase)
        {
            _repathTimer = 0f;
            _ai.Locomotion.SetDestination(_bb.LastKnownTargetPos);
        }

        if (_bb.Target != null) _ai.Locomotion.FaceTowards(_bb.Target.position);

        bool canAttack = _ai.Combat.TrySelectAttack(_bb.DistanceToTarget, out var attackCandidate);
        if (_bb.HasLineOfSight && canAttack)
        {
            _ai.StateMachine.ChangeState(new AttackState(_ai, _bb));
            return;
        }

        if (_lostTimer >= _ai.Config.LostSightDelay)
        {
            _ai.StateMachine.ChangeState(new SearchState(_ai, _bb));
            return;
        }

        if (ShouldFlee())
        {
            _ai.StateMachine.ChangeState(new FleeState(_ai, _bb));
            return;
        }

        if (_ai.Health.IsDead)
        {
            _ai.StateMachine.ChangeState(new DeathState(_ai, _bb));
        }
    }

    public void Exit() { }

    private bool ShouldFlee()
    {
        float hpPct = Mathf.Approximately(_ai.Health.Max, 0f) ? 0f : (_ai.Health.Current / _ai.Health.Max);
        return hpPct <= _ai.Config.FleeHealthThreshold && !_ai.Health.IsDead;
    }
}