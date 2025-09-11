using UnityEngine;
using UnityEngine.AI;

public sealed class FleeState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;

    private float _repathTimer;

    public string Name => "Flee";

    public FleeState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _repathTimer = 0f;
        _ai.Locomotion.SetSpeedFlee();
        _ai.Locomotion.Resume();
        _ai.Animation.PlayRun();

        Vector3 fleePoint = ComputeFleePoint();
        _ai.Locomotion.SetDestination(fleePoint);
    }

    public void Update()
    {
        _repathTimer += Time.deltaTime;

        if (_repathTimer >= _ai.Config.RepathIntervalPatrol)
        {
            _repathTimer = 0f;
            _ai.Locomotion.SetDestination(ComputeFleePoint());
        }

        if (Vector3.Distance(_ai.transform.position, _bb.LastKnownTargetPos) >= _ai.Config.FleeDistance)
        {
            _ai.StateMachine.ChangeState(new PatrolState(_ai, _bb));
            return;
        }

        if (_ai.Health.IsDead)
        {
           _ai.StateMachine.ChangeState(new DeathState(_ai, _bb));
        }
    }

    public void Exit() { }

    private Vector3 ComputeFleePoint()
    {
        Vector3 away;
        if (_bb.Target != null)
            away = (_ai.transform.position - _bb.Target.position).normalized;
        else
            away = (_ai.transform.position - _bb.LastKnownTargetPos).normalized;

        if (away.sqrMagnitude < 0.0001f) away = _ai.transform.forward;

        Vector3 candidate = _ai.transform.position + away * _ai.Config.FleeDistance;
        if (NavMesh.SamplePosition(candidate, out var hit, _ai.Config.FleeDistance, NavMesh.AllAreas))
            return hit.position;
        return _ai.transform.position + away * (_ai.Config.FleeDistance * 0.5f);
    }
}