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
        if (bb.HeardNoise)
        {
            bb.Suspicion01 = Mathf.Clamp01(bb.Suspicion01 + 0.25f * Time.deltaTime);
            bb.LastKnownTargetPos = bb.HeardNoisePos;
        }
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