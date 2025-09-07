using UnityEngine;

[DisallowMultipleComponent]
public class VisionSensor : MonoBehaviour
{
    private EnemyConfig _config;

    public void ApplyConfig(EnemyConfig config)
    {
        _config = config;
    }

    public void Tick(Blackboard bb)
    {
        if (_config == null || bb.Target == null) return;

        Vector3 toTarget = bb.Target.position - transform.position;
        float dist = toTarget.magnitude;
        bb.DistanceToTarget = dist;

        bool withinDistance = dist <= _config.ViewDistance;
        bool withinAngle = false;

        if (withinDistance)
        {
            Vector3 forward = transform.forward;
            Vector3 dir = toTarget.normalized;
            float angle = Vector3.Angle(forward, dir);
            withinAngle = angle <= _config.ViewAngle * 0.5f;
        }

        bool hasLOS = false;
        if (withinDistance && withinAngle)
        {
            Vector3 origin = transform.position + Vector3.up * 1.6f;
            Vector3 target = bb.Target.position + Vector3.up * 1.6f;
            Vector3 dir = (target - origin).normalized;
            float distCheck = Vector3.Distance(origin, target);

            if (!Physics.Raycast(origin, dir, distCheck, _config.VisionObstacles))
            {
                hasLOS = true;
            }
        }

        bb.HasLineOfSight = hasLOS;

        if (hasLOS)
        {
            bb.LastKnownTargetPos = bb.Target.position;
            bb.TimeSinceSeen = 0f;
            bb.Suspicion01 = Mathf.Clamp01(bb.Suspicion01 + _config.SuspicionGainPerSecond * Time.deltaTime);
        }
        else
        {
            bb.TimeSinceSeen += Time.deltaTime;
            bb.Suspicion01 = Mathf.Clamp01(bb.Suspicion01 - _config.SuspicionLossPerSecond * Time.deltaTime);
        }
    }
}