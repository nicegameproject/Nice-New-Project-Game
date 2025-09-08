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
        _timer = 2f;
        _ai.Animation.PlayIdle();
        _ai.Locomotion.StopImmediate();    
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
        _ai.Animation.ResetIdleAnimation();
    }
}