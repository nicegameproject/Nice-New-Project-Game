using UnityEngine;

[System.Serializable]
public sealed class Blackboard
{
    // Cel
    public Transform Target;
    public Vector3 LastKnownTargetPos;
    public float TimeSinceSeen;
    public bool HasLineOfSight;
    public float DistanceToTarget;

    // Podejrzenie/alert
    public float Suspicion01; 

    // S³uch
    public bool HeardNoise;
    public Vector3 HeardNoisePos;

    // Nawigacja
    public Vector3 CurrentDestination;
    public bool PathValid;

    // Walka/¿ycie
    public float CurrentHealth;
    public float MaxHealth = 100f;
    public bool IsDead;

    // Timery pomocnicze
    public float RepathTimer;

    public void Reset()
    {
        LastKnownTargetPos = Vector3.zero;
        TimeSinceSeen = 0f;
        HasLineOfSight = false;
        DistanceToTarget = Mathf.Infinity;
        Suspicion01 = 0f;
        HeardNoise = false;
        HeardNoisePos = Vector3.zero;
        CurrentDestination = Vector3.zero;
        PathValid = false;
        IsDead = false;
        RepathTimer = 0f;
        CurrentHealth = MaxHealth;
    }
}