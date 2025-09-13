using UnityEngine;

public sealed class IdleState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;
    private float _idleTimer;
    private float _targetIdleTime;

    public string Name => "Idle";

    public IdleState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _ai.Animation.PlayIdle();
        _ai.Locomotion.SetSpeedToZero();
        _ai.Locomotion.StopImmediate();

        _targetIdleTime = Random.Range(_ai.Config.IdleMinTime, _ai.Config.IdleMaxTime);
        _idleTimer = 0f;
    }

    public void Update()
    {
        _idleTimer += Time.deltaTime;

        _ai.Hearing.PullHeardInfo(_bb);

        if (_bb.Suspicion01 > 0.01f || _bb.HeardNoise)
        {
            _ai.StateMachine.ChangeState(new DetectState(_ai, _bb));
            return;
        }

        if (_idleTimer >= _targetIdleTime)
        {
            _ai.StateMachine.ChangeState(new PatrolState(_ai, _bb));
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