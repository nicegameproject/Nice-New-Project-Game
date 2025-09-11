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
    }

    public void Update()
    {
        _despawnTimer -= Time.deltaTime;
        if (_despawnTimer <= 0f)
        {
            GameObject.Destroy(_ai.gameObject);
        }
    }

    public void Exit() { }
}