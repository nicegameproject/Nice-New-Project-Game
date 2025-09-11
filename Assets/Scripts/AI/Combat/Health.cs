using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private HealthStats _healthTemplate;

    private HealthStats _runtime;

    private float hitStunDuration = 2f;
    private EnemyConfig _config;
    private AIController _ai;
    private Coroutine _recoverRoutine;

    [Header("Debug")]
    [SerializeField] private bool _logHealthChanges = true;
    [SerializeField] private float _currentHealthInspector;
    [SerializeField] private float _maxHealthInspector;
    [SerializeField, Range(0f, 1f)] private float _healthPercentInspector;

    public float Max => _runtime != null ? _runtime.MaxHealth : 0f;
    public float Current => _runtime != null ? _runtime.CurrentHealth : 0f;
    public bool IsDead => Current <= 0f;
    public float Percent => _runtime != null ? _runtime.HealthPercent : 0f;

    void Awake()
    {
        _ai = GetComponent<AIController>();
        EnsureRuntimeInstance();
    }

    void Update()
    {
        if (_runtime == null) return;
        _currentHealthInspector = _runtime.CurrentHealth;
        _maxHealthInspector = _runtime.MaxHealth;
        _healthPercentInspector = _runtime.HealthPercent;
    }

    private void EnsureRuntimeInstance()
    {
        if (_healthTemplate != null)
        {
            _runtime = Instantiate(_healthTemplate);
            _runtime.InitializeRuntime();
        }
        else
        {
            Debug.LogError("Health: No HealthStats template assigned!", this);
        }
    }

    private void LogHealth(string cause = null)
    {
        if (!_logHealthChanges || _runtime == null) return;

        if (string.IsNullOrEmpty(cause))
        {
            Debug.Log($"[{name}] Health: {_runtime.CurrentHealth}/{_runtime.MaxHealth} ({_runtime.HealthPercent:P0})", this);
        }
        else
        {
            Debug.Log($"[{name}] Health: {_runtime.CurrentHealth}/{_runtime.MaxHealth} ({_runtime.HealthPercent:P0}) after {cause}", this);
        }
    }

    public void ApplyConfig(EnemyConfig config)
    {
        _config = config;
        if (_runtime == null) EnsureRuntimeInstance();
        _runtime.InitializeRuntime();
        LogHealth("apply config");
    }

    public void ApplyDamage(float amount, GameObject source)
    {
        if (_runtime == null || IsDead) return;

        _runtime.CurrentHealth -= Mathf.Abs(amount);

        if (_runtime.CurrentHealth <= 0f)
        {
            _runtime.CurrentHealth = 0f;
            LogHealth($"damage {amount} (DEAD)");
            _ai?.OnDeath();
            return;
        }

        LogHealth($"damage {amount}");

        if (_ai?.Locomotion != null)
        {
            _ai.Locomotion.StopImmediate();
            _ai.Locomotion.SetSpeedToZero();
        }

        _ai?.Animation?.PlayTakeHit();

        if (_recoverRoutine != null) StopCoroutine(_recoverRoutine);
        _recoverRoutine = StartCoroutine(RecoverMovementAfterStun());
    }

    public void ForceKill(GameObject source)
    {
        if (_runtime == null) return;
        if (IsDead) return;
        _runtime.CurrentHealth = 0f;
        LogHealth("force kill");
        _ai?.OnDeath();
    }

    private IEnumerator RecoverMovementAfterStun()
    {
        yield return new WaitForSeconds(hitStunDuration);
        RestoreMovement();
        _recoverRoutine = null;
    }

    public void OnTakeHitAnimationEnd()
    {
        RestoreMovement();
    }

    private void RestoreMovement()
    {
        if (_ai == null || IsDead) return;
        _ai.Locomotion.Resume();
        _ai.Locomotion.SetSpeedWalk();
        _ai.Animation.PlayIdle();
    }

    [ContextMenu("Apply 10 Damage")]
    private void DebugDealDamage()
    {
        ApplyDamage(10f, gameObject);
    }

    [ContextMenu("Log Health Now")]
    private void DebugLogHealthNow()
    {
        LogHealth("manual");
    }

    [ContextMenu("Emit Sound")]
    public void PlayEmitNoise()
    {
        HearingSensor.EmitNoise(transform.position, 6f);
    }

    public void ResetHealth()
    {
        if (_runtime == null) EnsureRuntimeInstance();
        _runtime.InitializeRuntime();
        LogHealth("reset");
    }
}