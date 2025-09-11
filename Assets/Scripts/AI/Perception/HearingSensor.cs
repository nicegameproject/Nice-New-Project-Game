using UnityEngine;
using System;

[DisallowMultipleComponent]
public class HearingSensor : MonoBehaviour
{
    private EnemyConfig _config;

    public static event Action<Vector3, float> OnNoise; 

    public void ApplyConfig(EnemyConfig config)
    {
        _config = config;
    }

    public void Tick(Blackboard bb)
    {
        if (!bb.HeardNoise) return;

        var registry = PlayerRegistry.Instance;
        var players = registry != null ? registry.Players : null;
        if (players == null || players.Count == 0) return;

        float bestDist = Mathf.Infinity;
        int bestIdx = -1;

        for (int i = 0; i < players.Count && i < bb.TrackedTargets.Length; i++)
        {
            var pc = players[i];
            if (pc == null || pc.TransformRef == null) continue;
            float d = Vector3.Distance(bb.HeardNoisePos, pc.TransformRef.position);
            if (d < bestDist)
            {
                bestDist = d;
                bestIdx = i;
            }
        }

        if (bestIdx >= 0)
        {
            var pc = players[bestIdx];
            var entry = bb.GetOrEnsureEntry(bestIdx, pc);
            entry.HeardNoise = true;
            entry.HeardNoisePos = bb.HeardNoisePos;
            entry.Suspicion01 = Mathf.Clamp01(entry.Suspicion01 + 0.25f * Time.deltaTime);
            entry.LastKnownPos = bb.HeardNoisePos;
        }

        bb.SelectBestTarget();
    }

    void OnEnable()
    {
        OnNoise += HandleNoise;
    }

    void OnDisable()
    {
        OnNoise -= HandleNoise;
    }

    private void HandleNoise(Vector3 pos, float radius)
    {
        if (_config == null) return;
        float effective = Mathf.Max(radius, _config.HearingRadius);
        float dist = Vector3.Distance(transform.position, pos);
        if (dist <= effective)
        {
            _heardPos = pos;
            _heardTimer = 1.5f;
        }
    }

    private Vector3 _heardPos;
    private float _heardTimer;

    void Update()
    {
        if (_heardTimer > 0f)
        {
            _heardTimer -= Time.deltaTime;
        }
    }

    public void PullHeardInfo(Blackboard bb)
    {
        if (_heardTimer > 0f)
        {
            bb.HeardNoise = true;
            bb.HeardNoisePos = _heardPos;
        }
        else
        {
            bb.HeardNoise = false;
        }
    }

    public static void EmitNoise(Vector3 position, float radius)
    {
        OnNoise?.Invoke(position, radius);
    }
}