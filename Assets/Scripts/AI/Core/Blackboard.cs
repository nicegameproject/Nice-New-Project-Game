using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public sealed class Blackboard
{
    [System.Serializable]
    public sealed class TrackedTarget
    {
        public PlayerCharacter Player;
        public Transform Transform;
        public Vector3 LastKnownPos;
        public float TimeSinceSeen;
        public bool HasLineOfSight;
        public float Distance;
        public float Suspicion01;
        public bool HeardNoise;
        public Vector3 HeardNoisePos;

        public void Reset()
        {
            Player = null;
            Transform = null;
            LastKnownPos = Vector3.zero;
            TimeSinceSeen = 0f;
            HasLineOfSight = false;
            Distance = Mathf.Infinity;
            Suspicion01 = 0f;
            HeardNoise = false;
            HeardNoisePos = Vector3.zero;
        }
    }

    public Transform Target;
    public Vector3 LastKnownTargetPos;
    public float TimeSinceSeen;
    public bool HasLineOfSight;
    public float DistanceToTarget;

    public float Suspicion01;

    public bool HeardNoise;
    public Vector3 HeardNoisePos;

    public Vector3 CurrentDestination;
    public bool PathValid;

    public float RepathTimer;

    public TrackedTarget[] TrackedTargets = new TrackedTarget[4];
    public int TrackedCount;

    public Blackboard()
    {
        for (int i = 0; i < TrackedTargets.Length; i++)
            TrackedTargets[i] = new TrackedTarget();
    }

    public void Reset()
    {
        Target = null;
        LastKnownTargetPos = Vector3.zero;
        TimeSinceSeen = 0f;
        HasLineOfSight = false;
        DistanceToTarget = Mathf.Infinity;
        Suspicion01 = 0f;
        HeardNoise = false;
        HeardNoisePos = Vector3.zero;
        CurrentDestination = Vector3.zero;
        PathValid = false;
        RepathTimer = 0f;

        for (int i = 0; i < TrackedTargets.Length; i++)
            TrackedTargets[i].Reset();

        TrackedCount = 0;
    }

    public TrackedTarget GetOrEnsureEntry(int index, PlayerCharacter pc)
    {
        if (index < 0 || index >= TrackedTargets.Length) return null;
        var entry = TrackedTargets[index];
        if (entry.Player != pc)
        {
            entry.Reset();
            entry.Player = pc;
            entry.Transform = pc != null ? pc.TransformRef : null;
        }
        TrackedCount = Mathf.Max(TrackedCount, index + 1);
        return entry;
    }

    public void SyncWithPlayers(IReadOnlyList<PlayerCharacter> players)
    {
        int count = players != null ? Mathf.Min(players.Count, TrackedTargets.Length) : 0;
        TrackedCount = count;

        for (int i = 0; i < count; i++)
        {
            var pc = players[i];
            var entry = TrackedTargets[i];
            if (entry.Player != pc)
            {
                entry.Reset();
                if (pc != null)
                {
                    entry.Player = pc;
                    entry.Transform = pc.TransformRef;
                }
            }
        }

        for (int i = count; i < TrackedTargets.Length; i++)
            TrackedTargets[i].Reset();

        if (Target == null) return;
        bool stillTracked = false;
        for (int i = 0; i < count; i++)
        {
            if (TrackedTargets[i].Transform == Target)
            {
                stillTracked = true;
                break;
            }
        }
        if (!stillTracked) ClearCurrent();
    }

    public void SelectBestTarget()
    {
        TrackedTarget bestLOS = null;
        float bestLOSDist = Mathf.Infinity;

        TrackedTarget bestSusp = null;
        float bestSuspVal = -1f;

        for (int i = 0; i < TrackedCount; i++)
        {
            var t = TrackedTargets[i];
            if (t.Transform == null) continue;

            if (t.HasLineOfSight && t.Distance < bestLOSDist)
            {
                bestLOSDist = t.Distance;
                bestLOS = t;
            }

            if (t.Suspicion01 > bestSuspVal)
            {
                bestSuspVal = t.Suspicion01;
                bestSusp = t;
            }
        }

        var chosen = bestLOS != null ? bestLOS : (bestSuspVal > 0.01f ? bestSusp : null);
        if (chosen != null)
            SetCurrentFromEntry(chosen);
        else
            ClearCurrent();
    }

    public void SetCurrentFromEntry(TrackedTarget t)
    {
        Target = t.Transform;
        LastKnownTargetPos = t.LastKnownPos;
        TimeSinceSeen = t.TimeSinceSeen;
        HasLineOfSight = t.HasLineOfSight;
        DistanceToTarget = t.Distance;
        Suspicion01 = t.Suspicion01;
    }

    public void ClearCurrent()
    {
        Target = null;
        LastKnownTargetPos = Vector3.zero;
        TimeSinceSeen = 0f;
        HasLineOfSight = false;
        DistanceToTarget = Mathf.Infinity;
        Suspicion01 = 0f;
    }
}