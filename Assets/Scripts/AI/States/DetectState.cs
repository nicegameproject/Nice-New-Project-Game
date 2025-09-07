using UnityEngine;

public sealed class DetectState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;
    private float _timer;
    private bool _tauntStarted;

    public string Name => "Detect";

    public DetectState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _timer = 0f;
        _tauntStarted = false;

        _ai.Animation.PlayTaunt();
        _tauntStarted = true;

        _ai.Locomotion.StopImmediate();
        _ai.Locomotion.SetSpeedToZero();
    }

    public void Update()
    {
        if (_bb.IsDead)
        {
            _ai.StateMachine.ChangeState(new DeathState(_ai, _bb));
            return;
        }

        Vector3 focus = _bb.HeardNoise
            ? _bb.HeardNoisePos
            : (_bb.Target != null ? _bb.Target.position : _ai.transform.position + _ai.transform.forward);
        _ai.Locomotion.FaceTowards(focus);

        _timer += Time.deltaTime;

        if (_bb.LastKnownTargetPos != Vector3.zero && _timer > 0.2f)
        {
            _ai.Locomotion.SetDestination(_bb.LastKnownTargetPos);
        }

        if (_bb.HasLineOfSight && _bb.Suspicion01 >= 0.9f)
        {
            if (!_ai.Animation.IsTauntPlaying())
            {
                _ai.StateMachine.ChangeState(new ChaseState(_ai, _bb));
                return;
            }
        }

        if (_bb.Suspicion01 <= 0.01f && !_bb.HeardNoise)
        {
            _ai.StateMachine.ChangeState(new PatrolState(_ai, _bb));
            return;
        }

        if (ShouldFlee())
        {
            _ai.StateMachine.ChangeState(new FleeState(_ai, _bb));
            return;
        }
    }

    public void Exit()
    {
        // Mo¿na ew. przywróciæ ruch tutaj
    }

    private bool ShouldFlee()
    {
        float hpPct = Mathf.Approximately(_bb.MaxHealth, 0f) ? 0f : (_bb.CurrentHealth / _bb.MaxHealth);
        return hpPct <= _ai.Config.FleeHealthThreshold && !_bb.IsDead;
    }
}