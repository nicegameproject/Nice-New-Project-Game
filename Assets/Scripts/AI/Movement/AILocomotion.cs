using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class AILocomotion : MonoBehaviour
{
    private NavMeshAgent _agent;
    private EnemyConfig _config;

    public NavMeshAgent Agent => _agent;

    private float _targetSpeed;
    private float _startSpeed;
    private float _currentBlendDuration;
    private float _currentBlendElapsed;
    private bool _isBlending;
    private float _cachedAcceleration; 

    private const float MinSpeedBlendTime = 0.05f;
    private const float MaxSpeedBlendTime = 1.0f;

    public bool IsSpeedBlending => _isBlending;
    public float CurrentSpeed => _agent != null ? _agent.speed : 0f;
    public float TargetSpeed => _targetSpeed;

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

        _cachedAcceleration = Mathf.Max(0.01f, config.Acceleration);
       
        _targetSpeed = _agent.speed;
        _startSpeed = _agent.speed;
        _isBlending = false;
    }

    private void Update()
    {
        if (_agent == null) return;
        if (_isBlending)
        {
            _currentBlendElapsed += Time.deltaTime;
            float t = _currentBlendDuration <= 0f ? 1f : Mathf.Clamp01(_currentBlendElapsed / _currentBlendDuration);
           
            float eased = t * t * (3f - 2f * t);
            float newSpeed = Mathf.Lerp(_startSpeed, _targetSpeed, eased);
            _agent.speed = newSpeed;

            if (t >= 1f)
            {
                _agent.speed = _targetSpeed;
                _isBlending = false;
            }
        }
    }


    public void SetSpeedToZero(float blendTime = -1f)
    {
        if (!CanChangeSpeed()) return;
        BeginSpeedBlend(0f, blendTime);
    }

    public void SetSpeedWalk(float blendTime = -1f)
    {
        if (!CanChangeSpeed()) return;
        BeginSpeedBlend(_config.WalkSpeed, blendTime);
    }

    public void SetSpeedRun(float blendTime = -1f)
    {
        if (!CanChangeSpeed()) return;
        BeginSpeedBlend(_config.RunSpeed, blendTime);
    }

    public void SetSpeedFlee(float blendTime = -1f)
    {
        if (!CanChangeSpeed()) return;
        BeginSpeedBlend(_config.FleeSpeed, blendTime);
    }

    public void ForceSpeed(float value)
    {
        if (!CanChangeSpeed()) return;
        value = Mathf.Max(0f, value);
        _agent.speed = value;
        _targetSpeed = value;
        _startSpeed = value;
        _isBlending = false;
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

    public void FaceTowards(Vector3 position, float turnSpeed = 360f)
    {
        Vector3 dir = position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        var targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }


    private bool CanChangeSpeed()
    {
        return _agent != null && _config != null;
    }

    private void BeginSpeedBlend(float target, float overrideBlendTime)
    {
        target = Mathf.Max(0f, target);
        if (overrideBlendTime == 0f)
        {
            ForceSpeed(target);
            return;
        }

        float duration = ResolveBlendDuration(target, overrideBlendTime);
        if (duration <= 0f)
        {
            ForceSpeed(target);
            return;
        }

        _startSpeed = _agent.speed;
        _targetSpeed = target;
        _currentBlendDuration = duration;
        _currentBlendElapsed = 0f;
        _isBlending = true;
    }

    private float ResolveBlendDuration(float targetSpeed, float overrideBlendTime)
    {
        if (overrideBlendTime >= 0f) return overrideBlendTime;

        float acceleration = _config != null ? Mathf.Max(0.01f, _config.Acceleration) : (_cachedAcceleration > 0 ? _cachedAcceleration : 8f);
        float delta = Mathf.Abs(targetSpeed - _agent.speed);

        if (delta < 0.01f) return 0f;

        float time = delta / acceleration;
        return Mathf.Clamp(time, MinSpeedBlendTime, MaxSpeedBlendTime);
    }
}