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

        var meleeList = _config.MeleeAttacks;
        if (meleeList == null) return;

        for (int i = 0; i < meleeList.Count; i++)
        {
            var a = meleeList[i];
            if (a != null && !string.IsNullOrEmpty(a.Id))
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

        var list = _config.MeleeAttacks;
        if (list == null || list.Count == 0) return false;

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;
            if (distance <= _config.PreferredAttackRange && GetCooldown(a.Id) <= 0f)
            {
                selected = a;
                return true;
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

            float radius = Mathf.Max(0.1f, melee.HitRadius);

            if (planarDistance <= radius)
            {
                TryApplyDamageSingle(target, melee.Damage);
            }
            else
            {
                flatDir.Normalize();
                float castDistance = Mathf.Min(planarDistance, _config != null ? _config.PreferredAttackRange : planarDistance);

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
                    if (_config == null || planarDistance <= _config.PreferredAttackRange)
                    {
                        TryApplyDamageSingle(target, melee.Damage);
                    }
                }
            }
        }
        else
        {
            return;
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