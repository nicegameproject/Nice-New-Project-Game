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
        if (_config == null) return;

        var registry = PlayerRegistry.Instance;
        var players = registry != null ? registry.Players : null;
        if (players == null || players.Count == 0) { bb.ClearCurrent(); return; }

        bb.SyncWithPlayers(players);

        for (int i = 0; i < bb.TrackedCount; i++)
        {
            var pc = players[i];
            if (pc == null || pc.TransformRef == null) continue;

            var entry = bb.GetOrEnsureEntry(i, pc);
            if (entry == null) continue;

            Vector3 toTarget = pc.TransformRef.position - transform.position;
            float dist = toTarget.magnitude;
            entry.Distance = dist;

            bool withinDistance = dist <= _config.ViewDistance;
            bool withinAngle = false;

            if (withinDistance)
            {
                Vector3 forward = transform.forward;
                Vector3 dir = toTarget.normalized;
                float angle = Vector3.Angle(forward, dir);
                withinAngle = angle <= _config.ViewAngle;
            }

            bool hasLOS = false;
            if (withinDistance && withinAngle)
            {
                Vector3 origin = transform.position;
                Vector3 target = pc.TransformRef.position;
                Vector3 dir = (target - origin).normalized;
                float distCheck = Vector3.Distance(origin, target);

                if (!Physics.Raycast(origin, dir, distCheck, _config.VisionObstacles))
                {
                    hasLOS = true;
                }
            }

            entry.HasLineOfSight = hasLOS;

            if (hasLOS)
            {
                entry.LastKnownPos = pc.TransformRef.position;
                entry.TimeSinceSeen = 0f;
                entry.Suspicion01 = Mathf.Clamp01(entry.Suspicion01 + _config.SuspicionGainPerSecond * Time.deltaTime);
            }
            else
            {
                entry.TimeSinceSeen += Time.deltaTime;
                entry.Suspicion01 = Mathf.Clamp01(entry.Suspicion01 - _config.SuspicionLossPerSecond * Time.deltaTime);
            }
        }

        bb.SelectBestTarget();
    }
}