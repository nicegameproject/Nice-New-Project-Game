using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public sealed class SearchState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;

    private Vector3 _currentSearchPoint;
    private bool _goingToLastKnown;

    public string Name => "Search";

    public SearchState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _ai.Animation.PlayWalk();
        _ai.Locomotion.SetSpeedWalk();
        _ai.Locomotion.Resume();

        _goingToLastKnown = true;

        _currentSearchPoint = _bb.LastKnownTargetPos;
        _ai.Locomotion.SetDestination(_currentSearchPoint);

        _ai.StartCoroutine(Run());
    }

    public void Update() { }

    public void Exit() { }

    private IEnumerator Run()
    {
        while (IsCurrent())
        {
            if (_ai.Health.IsDead)
            {
                _ai.StateMachine.ChangeState(new DeathState(_ai, _bb));
                yield break;
            }

            if (_bb.HasLineOfSight)
            {
                _ai.StateMachine.ChangeState(new ChaseState(_ai, _bb));
                yield break;
            }

            if (_bb.HeardNoise)
            {
                if (_bb.Target != null)
                    _ai.Locomotion.FaceTowards(_bb.Target.position);

                yield return new WaitForSeconds(0.2f);

                _ai.StateMachine.ChangeState(new ChaseState(_ai, _bb));
                yield break;
            }

            if (ShouldFlee())
            {
                _ai.StateMachine.ChangeState(new FleeState(_ai, _bb));
                yield break;
            }

          

            if (_ai.Locomotion.HasReachedDestination())
            {
                if (_goingToLastKnown)
                {
                    _goingToLastKnown = false;
                    _currentSearchPoint = RandomPointInRadius(_bb.LastKnownTargetPos, _ai.Config.SearchRadius);
                    _ai.Locomotion.SetDestination(_currentSearchPoint);
                }
                else
                {
                    _ai.StateMachine.ChangeState(new PatrolState(_ai, _bb));
                    yield break;
                }
            }

            yield return null;
        }
    }

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

    private bool IsCurrent() => ReferenceEquals(_ai.StateMachine.Current, this);
}