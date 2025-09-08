using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class AILocomotion : MonoBehaviour
{
    private NavMeshAgent _agent;
    private EnemyConfig _config;

    public NavMeshAgent Agent => _agent;


    public void Initialize(NavMeshAgent agent)
    {
        _agent = agent;
    }

    public void ApplyConfig(EnemyConfig config)
    {
        _config = config;
        if (_agent == null) return;

        _agent.speed = config.WalkSpeed;
        _agent.acceleration = config.Acceleration;
        _agent.angularSpeed = config.AngularSpeed;
        _agent.stoppingDistance = config.StoppingDistance;
        _agent.autoBraking = true;
        _agent.updateRotation = true;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }


    public void SetSpeedToZero()
    {
        if (_agent != null && _config != null)
            _agent.speed = 0;
    }

    public void SetSpeedWalk()
    {
        if (_agent != null && _config != null)
            _agent.speed = _config.WalkSpeed;
    }

    public void SetSpeedRun()
    {
        if (_agent != null && _config != null)
            _agent.speed = _config.RunSpeed;
    }

    public void SetSpeedFlee()
    {
        if (_agent != null && _config != null)
            _agent.speed = _config.FleeSpeed;
    }

    public void SetStoppingDistance(float value)
    {
        if (_agent != null) _agent.stoppingDistance = Mathf.Max(0f, value);
    }

    public void SetDestination(Vector3 pos)
    {
        if (_agent == null || !_agent.enabled) return;
        _agent.isStopped = false;
        _agent.SetDestination(pos);
    }

    public void StopImmediate()
    {
        if (_agent == null) return;
        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
    }

    public void Resume()
    {
        if (_agent == null) return;
        _agent.isStopped = false;
    }

    public bool HasReachedDestination()
    {
        if (_agent == null) return true;
        if (_agent.pathPending) return false;
        return _agent.remainingDistance <= _agent.stoppingDistance;
    }

    public void FaceTowards(Vector3 position, float turnSpeed = 720f)
    {
        Vector3 dir = position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        var targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }


}