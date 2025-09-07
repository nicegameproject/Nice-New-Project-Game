using UnityEngine;
using UnityEngine.AI;

public sealed class PatrolState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;

    private float _repathTimer;
    private float _waitTimer;
    private float _waitDuration;

    private Vector3 _wanderCenter;
    private float _wanderRadius;
    private const int MaxRandomPointTries = 8;

    public string Name => "Patrol";

    public PatrolState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _ai.Locomotion.SetSpeedWalk();
        _ai.Locomotion.SetStoppingDistance(0f);
        _ai.Locomotion.Resume();
        _ai.Animation.PlayWalk();

        _repathTimer = 0f;
        _waitTimer = 0f;
        _waitDuration = 0f;

        _wanderCenter = _ai.transform.position;
        _wanderRadius = _ai.Config != null ? _ai.Config.SearchRadius : 6f;

        SetNextRandomDestination();
    }

    public void Update()
    {
        _repathTimer += Time.deltaTime;

        _ai.Hearing.PullHeardInfo(_bb);

        if (_bb.Suspicion01 > 0.25f || _bb.HeardNoise)
        {
            _ai.StateMachine.ChangeState(new DetectState(_ai, _bb));
            return;
        }

        if (_waitDuration > 0f)
        {
            _waitTimer += Time.deltaTime;
            if (_waitTimer >= _waitDuration)
            {
                _waitDuration = 0f;
                SetNextRandomDestination();
            }
            return;
        }

        if (_repathTimer >= (_ai.Config != null ? _ai.Config.RepathIntervalPatrol : 1f))
        {
            _repathTimer = 0f;
            if (!_ai.Locomotion.Agent.hasPath || _ai.Locomotion.Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                SetNextRandomDestination();
            }
        }

        if (_ai.Locomotion.HasReachedDestination())
        {
            _waitDuration = Random.Range(0.5f, 1.5f);
            _waitTimer = 0f;
        }
    }

    public void Exit()
    {
    
    }

    private void SetNextRandomDestination()
    {
        if (TryGetRandomPoint(_wanderCenter, _wanderRadius, out Vector3 point))
        {
            _ai.Locomotion.SetDestination(point);
            _bb.CurrentDestination = point;
            _bb.PathValid = true;
        }
        else
        {
            _bb.PathValid = false;
        }
    }

    private bool TryGetRandomPoint(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < MaxRandomPointTries; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
            {
                var path = new NavMeshPath();
                if (_ai.Locomotion.Agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    result = hit.position;
                    return true;
                }
            }
        }
        result = Vector3.zero;
        return false;
    }
}