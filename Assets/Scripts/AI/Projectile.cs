using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _lifetime = 10f;

    private float _damage;
    private GameObject _source;
    private LayerMask _hitMask;
    private Transform _owner;

    public void Configure(float damage, GameObject source, LayerMask hitMask)
    {
        _damage = damage;
        _source = source;
        _hitMask = hitMask;
        _owner = source != null ? source.transform : null;

        if (_lifetime > 0f)
        {
            Destroy(gameObject, _lifetime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider col)
    {
        if (col == null) return;

        var go = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;

        if (_owner != null && (go.transform == _owner || go.transform.IsChildOf(_owner)))
            return;

        if (!IsInLayerMask(go.layer, _hitMask))
            return;

        var dmg = go.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.ApplyDamage(_damage, _source);
        }

        Destroy(gameObject);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
