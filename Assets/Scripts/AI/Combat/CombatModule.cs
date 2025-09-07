using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatModule : MonoBehaviour
{
    private EnemyConfig _config;
    private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

    public void ApplyConfig(EnemyConfig config)
    {
        _config = config;
        _cooldowns.Clear();
        if (_config == null) return;

        if (_config.AttackMode == EnemyAttackMode.Melee)
        {
            foreach (var a in _config.MeleeAttacks)
                _cooldowns[a.Id] = 0f;
        }
        else
        {
            foreach (var a in _config.RangedAttacks)
                _cooldowns[a.Id] = 0f;
        }
    }

    void Update()
    {
        if (_cooldowns.Count == 0) return;
        var keys = _cooldowns.Keys;
        var list = ListCache;
        list.Clear();
        foreach (var k in keys) list.Add(k);
        for (int i = 0; i < list.Count; i++)
        {
            var k = list[i];
            if (_cooldowns[k] > 0f) _cooldowns[k] -= Time.deltaTime;
        }
    }

    private static readonly List<string> ListCache = new List<string>(16);

    public bool TrySelectAttack(float distance, out AttackDefinitionBase selected)
    {
        selected = null;
        if (_config == null) return false;

        if (_config.AttackMode == EnemyAttackMode.Melee)
        {
            var list = _config.MeleeAttacks;
            if (list == null || list.Count == 0) return false;
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (distance <= a.Range && GetCooldown(a.Id) <= 0f)
                {
                    selected = a;
                    return true;
                }
            }
        }
        else
        {
            var list = _config.RangedAttacks;
            if (list == null || list.Count == 0) return false;
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (distance <= a.Range && GetCooldown(a.Id) <= 0f)
                {
                    selected = a;
                    return true;
                }
            }
        }
        return false;
    }

    public void ExecuteAttack(AttackDefinitionBase attack, Transform target)
    {
        if (attack == null || target == null) return;

        if (attack is MeleeAttackDefinition melee)
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 flatDir = target.position - transform.position;
            flatDir.y = 0f;
            float planarDistance = flatDir.magnitude;

            if (planarDistance <= Mathf.Max(0.1f, melee.HitRadius) + 0.05f)
            {
                TryApplyDamageSingle(target, melee.Damage);
            }
            else
            {
                flatDir.Normalize();
                float castDistance = Mathf.Min(planarDistance, melee.Range);
                float radius = Mathf.Max(0.1f, melee.HitRadius);

                if (Physics.SphereCast(origin, radius, flatDir, out var hit, castDistance, melee.HitMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform != transform)
                    {
                        var dmg = hit.transform.GetComponentInParent<IDamageable>();
                        dmg?.ApplyDamage(melee.Damage, gameObject);
                    }
                }
                else
                {
                    if (planarDistance <= melee.Range)
                    {
                        TryApplyDamageSingle(target, melee.Damage);
                    }
                }
            }
        }
        else if (attack is RangedAttackDefinition ranged)
        {
            if (ranged.ProjectilePrefab != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 1.4f;
                Vector3 targetPos = target.position + Vector3.up * 1.2f;
                var go = GameObject.Instantiate(ranged.ProjectilePrefab, spawnPos, Quaternion.LookRotation(targetPos - spawnPos));
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = go.transform.forward * ranged.ProjectileSpeed;
                }
            }
        }

        SetCooldown(attack.Id, attack.Cooldown);
    }

    private void TryApplyDamageSingle(Transform target, float damage)
    {
        var dmg = target.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.ApplyDamage(damage, gameObject);
        }
    }

    private float GetCooldown(string id)
    {
        if (_cooldowns.TryGetValue(id, out var v)) return v;
        return 0f;
    }

    private void SetCooldown(string id, float value)
    {
        _cooldowns[id] = value;
    }
}