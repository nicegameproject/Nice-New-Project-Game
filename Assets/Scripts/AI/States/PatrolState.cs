using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public sealed class PatrolState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;
    public string Name => "Patrol";

    public PatrolState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _ai.Animation.PlayWalk();
        _ai.Locomotion.SetSpeedWalk();
        _ai.Locomotion.Resume();

        SetNextRandomDestination();

        _ai.StartCoroutine(Run());
    }

    public void Update() { }

    public void Exit()
    {
        _ai.Animation.ResetWalkAnimation();
    }

    private IEnumerator Run()
    {
        while (IsCurrent())
        {
            if (_ai.Vision != null)
                _ai.Vision.Tick(_bb);

            _ai.Hearing.PullHeardInfo(_bb);

            if (_bb.HasLineOfSight)
            {
                _ai.StateMachine.ChangeState(new DetectState(_ai, _bb));
                yield break;
            }

            if (_bb.HeardNoise)
            {
                _ai.StateMachine.ChangeState(new DetectState(_ai, _bb));
                yield break;
            }

            if (_ai.Locomotion.HasReachedDestination())
            {
                _ai.StateMachine.ChangeState(new IdleState(_ai, _bb));
                yield break;
            }

            yield return null;
        }
    }

    private void SetNextRandomDestination()
    {
        if (TryGetRandomPoint(_ai.transform.position, _ai.Config.SearchRadius, out Vector3 point))
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
        for (int i = 0; i < 10; i++)
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

    private bool IsCurrent() => ReferenceEquals(_ai.StateMachine.Current, this);
}