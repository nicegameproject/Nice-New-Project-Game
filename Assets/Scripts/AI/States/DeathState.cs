using System.Collections;
using UnityEngine;

public sealed class DeathState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;
    private float _despawnTimer;

    public string Name => "Death";

    public DeathState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _despawnTimer = 5f;
        _ai.Animation.PlayDeath();
        _ai.Locomotion.StopImmediate();
        if (_ai.Locomotion.Agent != null) _ai.Locomotion.Agent.enabled = false;

        _ai.StartCoroutine(Run());
    }

    public void Update() { }

    public void Exit() { }

    private IEnumerator Run()
    {
        while (IsCurrent())
        {
            _despawnTimer -= Time.deltaTime;
            if (_despawnTimer <= 0f)
            {
                GameObject.Destroy(_ai.gameObject);
                yield break;
            }
            yield return null;
        }
    }

    private bool IsCurrent() => ReferenceEquals(_ai.StateMachine.Current, this);
}