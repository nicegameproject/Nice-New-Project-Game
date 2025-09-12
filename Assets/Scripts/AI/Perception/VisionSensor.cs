using UnityEngine;

[DisallowMultipleComponent]
public class VisionSensor : MonoBehaviour
{
    private EnemyConfig _config;
    private bool _inTick; // guard reentrancy

    public void ApplyConfig(EnemyConfig config)
    {
        _config = config;
    }

    public void Tick(Blackboard bb)
    {
        if (_config == null) return;
        if (_inTick) return; // zabezpieczenie przed przypadkow¹ rekurencj¹
        _inTick = true;
        try
        {
            var registry = PlayerRegistry.Instance;
            var players = registry != null ? registry.Players : null;
            if (players == null || players.Count == 0)
            {
                bb.ClearCurrent();
                return;
            }

            bb.SyncWithPlayers(players);

            for (int i = 0; i < bb.TrackedCount && i < players.Count; i++)
            {
                var pc = players[i];
                if (pc == null || pc.TransformRef == null) continue;

                var entry = bb.GetOrEnsureEntry(i, pc);
                if (entry == null) continue;

                Vector3 origin = transform.position;
                Vector3 targetPos = pc.TransformRef.position;
                Vector3 toTarget = targetPos - origin;

                float distSqr = toTarget.sqrMagnitude;
                float viewDist = _config.ViewDistance;
                float viewDistSqr = viewDist * viewDist;

                if (distSqr > viewDistSqr)
                {
                    entry.HasLineOfSight = false;
                    entry.Distance = Mathf.Sqrt(distSqr);
                    continue;
                }

                float dist = Mathf.Sqrt(distSqr);
                entry.Distance = dist;

                Vector3 dirNorm = toTarget / dist;
                float angle = Vector3.Angle(transform.forward, dirNorm);
                if (angle > _config.ViewAngle)
                {
                    entry.HasLineOfSight = false;
                    continue;
                }

                // Raycast przeszkód
                bool blocked = Physics.Raycast(origin, dirNorm, dist, _config.VisionObstacles);
                bool hasLOS = !blocked;
                entry.HasLineOfSight = hasLOS;

                if (hasLOS)
                {
                    entry.LastKnownPos = targetPos;
                }
            }

            bb.SelectBestTarget();
        }
        finally
        {
            _inTick = false;
        }
    }
}