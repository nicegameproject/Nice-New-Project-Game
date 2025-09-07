using UnityEngine;

public sealed class SpawnState : IAIState
{
    private readonly AIController _ai;
    private readonly Blackboard _bb;
    private float _timer;

    public string Name => "Spawn";

    public SpawnState(AIController ai, Blackboard bb)
    {
        _ai = ai;
        _bb = bb;
    }

    public void Enter()
    {
        _timer = 0.1f;
        _ai.Locomotion.StopImmediate();
        _ai.Animation.PlayIdle();
    }

    public void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _ai.StateMachine.ChangeState(new PatrolState(_ai, _bb));
        }
    }

    public void Exit()
    {
        
    }
}