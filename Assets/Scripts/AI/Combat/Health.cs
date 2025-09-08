using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _current;

    private float hitStunDuration = 2f;
    private EnemyConfig _config;
    private AIController _ai;
    private Coroutine _recoverRoutine;

    public float Max => _maxHealth;
    public float Current => _current;

    void Awake()
    {
        _ai = GetComponent<AIController>();
        _current = Mathf.Max(1f, _maxHealth);
    }

    public void ApplyConfig(EnemyConfig config)
    {
        _config = config;
        if (_config != null)
        {
            _maxHealth = Mathf.Max(1f, _ai != null ? _ai.Blackboard.MaxHealth : 100f);
        }
        _current = _maxHealth;
        if (_ai != null)
        {
            _ai.Blackboard.MaxHealth = _maxHealth;
            _ai.Blackboard.CurrentHealth = _current;
        }
    }

    public void ApplyDamage(float amount, GameObject source)
    {
        if (_current <= 0f) return;

        _current -= Mathf.Abs(amount);
        if (_ai != null) _ai.Blackboard.CurrentHealth = _current;

        if (_ai != null && _ai.Locomotion != null)
        {
            _ai.Locomotion.StopImmediate();
            _ai.Locomotion.SetSpeedToZero();
        }

        if (_current <= 0f)
        {
            _current = 0f;
            if (_ai != null) _ai.OnDeath();
            return;
        }

        if (_ai != null && _ai.Animation != null)
            _ai.Animation.PlayTakeHit();


        if (_recoverRoutine != null) StopCoroutine(_recoverRoutine);
        _recoverRoutine = StartCoroutine(RecoverMovementAfterStun());

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
        if (_ai == null || _current <= 0f) return;

        _ai.Locomotion.Resume();
        _ai.Locomotion.SetSpeedWalk();
        _ai.Animation.PlayIdle();
    }

    [ContextMenu("Apply Damage")]
    public void DealDamage()
    {
        ApplyDamage(10, gameObject);
    }

    [ContextMenu("Emit Sound")]
    public void PlayEmitNoise()
    {
        HearingSensor.EmitNoise(transform.position, 6f);
    }
}