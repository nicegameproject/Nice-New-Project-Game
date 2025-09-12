using System.Collections;
using UnityEngine;

public sealed class DetectState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;

    public string Name => "Detect";

    public DetectState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _ai.Locomotion.StopImmediate();
        _ai.Locomotion.SetSpeedToZero();

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
                if (_bb.Target != null)
                    _ai.Locomotion.FaceTowards(_bb.Target.position);

                yield return new WaitForSeconds(0.25f);

                _ai.StateMachine.ChangeState(new ChaseState(_ai, _bb));
                yield break;

            }

            if (_bb.HeardNoise)
            {
                if (_bb.Target != null)
                    _ai.Locomotion.FaceTowards(_bb.Target.position);

                yield return new WaitForSeconds(0.25f);

                _ai.StateMachine.ChangeState(new ChaseState(_ai, _bb));
                yield break;
            }

            if (ShouldFlee())
            {
                _ai.StateMachine.ChangeState(new FleeState(_ai, _bb));
                yield break;
            }

            yield return null;
        }
    }

    private bool ShouldFlee()
    {
        float hpPct = Mathf.Approximately(_ai.Health.Max, 0f) ? 0f : (_ai.Health.Current / _ai.Health.Max);
        return hpPct <= _ai.Config.FleeHealthThreshold && !_ai.Health.IsDead;
    }

    private bool IsCurrent() => ReferenceEquals(_ai.StateMachine.Current, this);
}