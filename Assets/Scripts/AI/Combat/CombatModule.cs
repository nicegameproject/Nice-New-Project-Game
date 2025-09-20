using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatModule : MonoBehaviour
{
    private EnemyConfig _config;
    private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

    private Vector3 _explosionGizmoCenter;
    private float _explosionGizmoRadius;
    private float _explosionGizmoUntilTime;
    private readonly List<Transform> _explosionGizmoHits = new List<Transform>(16);
    private const float _explosionGizmoDuration = 0.25f;

    [SerializeField] private Transform shootPoint;

    public void ApplyConfig(EnemyConfig config)
    {
        _config = config;
        _cooldowns.Clear();
        if (_config == null) return;

        switch (_config.AttackMode)
        {
            case EnemyAttackMode.Melee:
                RegisterAttack(_config.MeleeAttack);
                break;
            case EnemyAttackMode.Ranged:
                RegisterAttack(_config.RangeAttacks);
                break;
            case EnemyAttackMode.Explosion:
                RegisterAttack(_config.ExplosionAttacks);
                break;
            case EnemyAttackMode.Laser:
                RegisterAttack(_config.LaserAttack);
                break;
        }
    }

    private void RegisterAttack(AttackDefinitionBase def)
    {
        if (def == null || string.IsNullOrEmpty(def.Id)) return;
        if (!_cooldowns.ContainsKey(def.Id))
            _cooldowns[def.Id] = 0f;
    }

    void Update()
    {
        if (_cooldowns.Count == 0) return;
        _tempKeys.Clear();
        foreach (var k in _cooldowns.Keys) _tempKeys.Add(k);
        for (int i = 0; i < _tempKeys.Count; i++)
        {
            var k = _tempKeys[i];
            if (_cooldowns[k] > 0f) _cooldowns[k] -= Time.deltaTime;
        }
    }

    private static readonly List<string> _tempKeys = new List<string>(16);

    public bool TrySelectAttack(float distance, out AttackDefinitionBase selected)
    {
        selected = null;
        if (_config == null) return false;

        switch (_config.AttackMode)
        {
            case EnemyAttackMode.Melee:
                var melee = _config.MeleeAttack;
                if (melee != null &&
                    distance <= _config.PreferredAttackRange &&
                    GetCooldown(melee.Id) <= 0f)
                {
                    selected = melee;
                    return true;
                }
                break;
            case EnemyAttackMode.Ranged:
                var ranged = _config.RangeAttacks;
                if (ranged != null &&
                    distance <= _config.PreferredAttackRange &&
                    GetCooldown(ranged.Id) <= 0f)
                {
                    selected = ranged;
                    return true;
                }
                break;
            case EnemyAttackMode.Explosion:
                var expl = _config.ExplosionAttacks;
                if (expl != null &&
                    distance <= _config.PreferredAttackRange &&
                    GetCooldown(expl.Id) <= 0f)
                {
                    selected = expl;
                    return true;
                }
                break;
            case EnemyAttackMode.Laser:
                var laser = _config.LaserAttack;
                if (laser != null &&
                    distance <= _config.PreferredAttackRange &&
                    GetCooldown(laser.Id) <= 0f)
                {
                    selected = laser;
                    return true;
                }
                break;
        }

        return false;
    }

    public void ExecuteAttack(AttackDefinitionBase attack, Transform target)
    {
        if (attack == null) return;

        if (attack is MeleeAttackDefinition melee)
        {
            if (target == null) return;
            ExecuteMelee(melee, target);
        }
        else if (attack is RangedAttackDefinition ranged)
        {
            if (target == null) return;
            StartCoroutine(ExecuteRangedDelayed(ranged, target, ranged.FireDelay));
        }
        else if (attack is ExplosionAttackDefinition explosion)
        {
            ExecuteExplosion(explosion);
        }
        else if (attack is LaserAttackDefinition laser)
        {
            StartCoroutine(ExecuteLaserDelayed(laser, laser.FireDelay));
        }
        else
        {
            return;
        }

        SetCooldown(attack.Id, attack.AttackCooldown);
    }

    private IEnumerator ExecuteRangedDelayed(RangedAttackDefinition ranged, Transform target, float delay)
    {
        yield return new WaitForSeconds(delay);
        ExecuteRanged(ranged, target);
    }

    private IEnumerator ExecuteLaserDelayed(LaserAttackDefinition laser, float delay)
    {
        yield return new WaitForSeconds(delay);
        ExecuteLaser(laser);
    }

    private void ExecuteMelee(MeleeAttackDefinition melee, Transform target)
    {
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 flatDir = target.position - transform.position;
        flatDir.y = 0f;
        float planarDistance = flatDir.magnitude;
        float radius = Mathf.Max(0.1f, melee.HitRadius);

        if (planarDistance <= radius)
        {
            TryApplyDamageSingle(target, melee.Damage);
            return;
        }

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

    private void ExecuteRanged(RangedAttackDefinition ranged, Transform target)
    {
        var proj = Instantiate(ranged.ProjectilePrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
        Vector3 velocityDir = (target.position + Vector3.up - proj.transform.position);
        velocityDir.Normalize();
        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = velocityDir * ranged.ProjectileSpeed;
        }

        var projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Configure(ranged.Damage, gameObject, ranged.HitMask);
        }
    }

    private void ExecuteLaser(LaserAttackDefinition laser)
    {
        StartCoroutine(LaserRoutine(laser));
    }

    private void ExecuteExplosion(ExplosionAttackDefinition explosion)
    {
        var selfHealth = GetComponent<Health>();
        if (selfHealth != null && !selfHealth.IsDead)
        {
            selfHealth.ForceKill(gameObject);
        }

        if (explosion.ExplosionVfxPrefab != null)
        {
            var vfx = Instantiate(explosion.ExplosionVfxPrefab, transform.position, Quaternion.identity);
            if (explosion.VfxLifetime > 0f)
            {
                Destroy(vfx, explosion.VfxLifetime);
            }
        }

        var hits = Physics.OverlapSphere(transform.position, explosion.ExplosionRadius, explosion.HitMask, QueryTriggerInteraction.Collide);

#if UNITY_EDITOR
        if (_config == null || _config.DrawGizmos)
        {
            _explosionGizmoCenter = transform.position;
            _explosionGizmoRadius = explosion.ExplosionRadius;
            _explosionGizmoUntilTime = Time.time + _explosionGizmoDuration;
            _explosionGizmoHits.Clear();
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null) _explosionGizmoHits.Add(hits[i].transform);
            }
        }
#endif

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.transform == transform) continue;

            var dmg = hit.transform.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.ApplyDamage(explosion.Damage, gameObject);
            }
        }
    }

    private static readonly RaycastHit[] _laserHitBuffer = new RaycastHit[8];

    private IEnumerator LaserRoutine(LaserAttackDefinition laser)
    {
        GameObject laserObj;
        if (laser != null && laser.LaserPrefab != null)
        {
            laserObj = Instantiate(laser.LaserPrefab, transform);
            laserObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        else
        {
            laserObj = new GameObject("Laser");
            laserObj.transform.SetParent(transform, false);
        }

        var lr = laserObj.GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int obstacleMask = obstacleLayer >= 0 ? (1 << obstacleLayer) : 0;

        float elapsed = 0f;
        float currentLength = 0f;
        GameObject impactVfxInstance = null;
        const float impactActivationEpsilon = 0.05f;

        float dps = laser.Damage;

        while (elapsed < laser.Duration)
        {
            Vector3 origin = shootPoint.position;
            Vector3 dir = transform.forward;

            bool hitObstacle = false;
            RaycastHit obstacleHit = default;

            if (obstacleMask != 0 &&
                Physics.Raycast(origin, dir, out RaycastHit hit, laser.MaxBeamDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitObstacle = true;
                obstacleHit = hit;
                float targetDistance = hit.distance;

                currentLength = Mathf.MoveTowards(currentLength, targetDistance, laser.BeamSpeed * Time.deltaTime);
                Vector3 endPos = origin + dir * currentLength;

                lr.SetPosition(0, origin);
                lr.SetPosition(1, endPos);

                if ((currentLength + impactActivationEpsilon) >= targetDistance && laser.ImpactVfxPrefab != null)
                {
                    if (impactVfxInstance == null)
                        impactVfxInstance = Instantiate(laser.ImpactVfxPrefab);

                    impactVfxInstance.transform.SetPositionAndRotation(
                        hit.point,
                        Quaternion.LookRotation(hit.normal, Vector3.up));
                }
                else
                {
                    if (impactVfxInstance != null)
                    {
                        Destroy(impactVfxInstance);
                        impactVfxInstance = null;
                    }
                }
            }
            else
            {
                float targetDistance = laser.MaxBeamDistance;
                currentLength = Mathf.MoveTowards(currentLength, targetDistance, laser.BeamSpeed * Time.deltaTime);
                Vector3 endPos = origin + dir * currentLength;

                lr.SetPosition(0, origin);
                lr.SetPosition(1, endPos);

                if (impactVfxInstance != null)
                {
                    Destroy(impactVfxInstance);
                    impactVfxInstance = null;
                }
            }

            if (laser.HitMask != 0 && currentLength > 0.05f && dps > 0f)
            {
                float damageDistance = currentLength;
                int hits = Physics.RaycastNonAlloc(origin, dir, _laserHitBuffer, damageDistance, laser.HitMask, QueryTriggerInteraction.Ignore);
                float frameDamage = dps * Time.deltaTime;

                for (int i = 0; i < hits; i++)
                {
                    var h = _laserHitBuffer[i];
                    if (!h.collider) continue;
                    if (h.transform == transform) continue;

                    var dmgComp = h.transform.GetComponentInParent<IDamageable>();
                    if (dmgComp != null)
                    {
                        dmgComp.ApplyDamage(frameDamage, gameObject);
                    }

                    // Jeœli przeszkoda zatrzymuj¹ca te¿ jest w HitMask, mo¿na przerwaæ po pierwszym.
                    // (Jeœli nie chcesz przerwania – usuñ ten warunek.)
                    if (hitObstacle && Mathf.Approximately(h.distance, obstacleHit.distance))
                        break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (impactVfxInstance != null)
            Destroy(impactVfxInstance);

        Destroy(laserObj);
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
        if (string.IsNullOrEmpty(id)) return 0f;
        if (_cooldowns.TryGetValue(id, out var v)) return v;
        return 0f;
    }

    private void SetCooldown(string id, float value)
    {
        if (string.IsNullOrEmpty(id)) return;
        _cooldowns[id] = value;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (_config != null && !_config.DrawGizmos) return;
        if (Time.time > _explosionGizmoUntilTime) return;

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(_explosionGizmoCenter, _explosionGizmoRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.75f);
        for (int i = 0; i < _explosionGizmoHits.Count; i++)
        {
            var t = _explosionGizmoHits[i];
            if (t == null) continue;
            Gizmos.DrawWireSphere(t.position, 0.25f);
        }
    }
#endif
}