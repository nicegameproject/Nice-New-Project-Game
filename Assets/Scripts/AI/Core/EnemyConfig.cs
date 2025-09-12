using System;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/Enemy Config", fileName = "EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Movement")]
    public float WalkSpeed = 2.0f;
    public float RunSpeed = 4.0f;
    public float FleeSpeed = 5.0f;
    public float Acceleration = 8.0f;
    public float AngularSpeed = 360f;
    public float StoppingDistance = 1.0f;
    public float RepathIntervalPatrol = 1.0f;
    public float RepathIntervalChase = 0.25f;

    [Header("Perception")]
    public float ViewDistance = 20f;
    [Range(0f, 180f)] public float ViewAngle = 90f;
    public LayerMask VisionObstacles = 0;
    public float LostSightDelay = 1.25f;

    [Header("Hearing")]
    public float HearingRadius = 12f;

    [Header("Behavior")]
    public float IdleMinTime = 2f;
    public float IdleMaxTime = 4f;
    public float SearchDuration = 6f;
    [Range(20f, 50f)] public float SearchRadius = 20f;
    public float PreferredAttackRange = 2.0f;

    [Header("Combat Mode")]
    public EnemyAttackMode AttackMode = EnemyAttackMode.Melee;

    public MeleeAttackDefinition MeleeAttack;
    public RangedAttackDefinition RangeAttacks;
    public ExplosionAttackDefinition ExplosionAttacks;
    public LaserAttackDefinition LaserAttack;

    [Header("Flee")]
    public float FleeHealthThreshold = 0.2f;
    public float FleeDistance = 12f;

    [Header("Debug")]
    public bool DrawGizmos = true;
}

public enum EnemyAttackMode
{
    Melee,
    Ranged,
    Explosion,
    Laser
}

[Serializable]
public abstract class AttackDefinitionBase
{
    public string Id;
    public float AttackCooldown = 1.0f;
    public float Windup = 0.2f;
    public float Damage = 10f;
}

[Serializable]
public class MeleeAttackDefinition : AttackDefinitionBase
{
    public float HitRadius = 1.0f;
    public LayerMask HitMask => LayerMask.GetMask("Player");
}

[Serializable]
public class RangedAttackDefinition : AttackDefinitionBase
{
    public GameObject ProjectilePrefab;
    public float ProjectileSpeed = 20f;
    public LayerMask HitMask => LayerMask.GetMask("Player");
}

[Serializable]
public class ExplosionAttackDefinition : AttackDefinitionBase
{
    public float ExplosionRadius = 5f;
    public LayerMask HitMask => LayerMask.GetMask("Player");
    [Header("VFX")]
    public GameObject ExplosionVfxPrefab;
    public float VfxLifetime = 3f;
}

[Serializable]
public class LaserAttackDefinition : AttackDefinitionBase
{
    public GameObject LaserPrefab;
    public float Duration = 2f;
    public LayerMask HitMask => LayerMask.GetMask("Player");
}