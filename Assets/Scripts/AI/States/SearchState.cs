using UnityEngine;
using UnityEngine.AI;

public sealed class SearchState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;

    private float _timer;
    private float _repathTimer;
    private Vector3 _currentSearchPoint;

    public string Name => "Search";

    public SearchState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _timer = 0f;
        _repathTimer = 0f;
        _ai.Animation.PlayWalk();
        _ai.Locomotion.SetSpeedWalk();
        _ai.Locomotion.Resume();

        _currentSearchPoint = _bb.LastKnownTargetPos;
        _ai.Locomotion.SetDestination(_currentSearchPoint);
    }

    public void Update()
    {
        _timer += Time.deltaTime;
        _repathTimer += Time.deltaTime;

        if (_bb.HasLineOfSight)
        {
            _ai.StateMachine.ChangeState(new ChaseState(_ai, _bb));
            return;
        }

        if (_ai.Locomotion.HasReachedDestination())
        {
            _currentSearchPoint = RandomPointInRadius(_bb.LastKnownTargetPos, _ai.Config.SearchRadius);
            _ai.Locomotion.SetDestination(_currentSearchPoint);
        }
        else if (_repathTimer >= _ai.Config.RepathIntervalPatrol)
        {
            _repathTimer = 0f;
            _ai.Locomotion.SetDestination(_currentSearchPoint);
        }

        if (_timer >= _ai.Config.SearchDuration)
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

    private Vector3 RandomPointInRadius(Vector3 center, float radius)
    {
        Vector2 r = Random.insideUnitCircle * radius;
        Vector3 candidate = center + new Vector3(r.x, 0f, r.y);
        if (NavMesh.SamplePosition(candidate, out var hit, radius, NavMesh.AllAreas))
            return hit.position;
        return center;
    }

    private bool ShouldFlee()
    {
        float hpPct = Mathf.Approximately(_ai.Health.Max, 0f) ? 0f : (_ai.Health.Current / _ai.Health.Max);
        return hpPct <= _ai.Config.FleeHealthThreshold && !_ai.Health.IsDead;
    }
}