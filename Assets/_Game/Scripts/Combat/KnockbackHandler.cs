using UnityEngine;

namespace SunlessReach.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class KnockbackHandler : MonoBehaviour
    {
        private Rigidbody _rb;
        private Damageable _damageable;
        private float _knockbackEndTime;
        private bool _inKnockback;

        public bool InKnockback => _inKnockback;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _damageable = GetComponent<Damageable>();
        }

        private void OnEnable()
        {
            if (_damageable != null)
                _damageable.OnDamaged += HandleKnockback;
        }

        private void OnDisable()
        {
            if (_damageable != null)
                _damageable.OnDamaged -= HandleKnockback;
        }

        private void Update()
        {
            if (_inKnockback && Time.time >= _knockbackEndTime)
            {
                _inKnockback = false;
            }
        }

        private void HandleKnockback(int damage, Vector3 direction, float force)
        {
            _rb.linearVelocity = Vector3.zero;
            Vector3 knockback = direction.normalized * force;
            knockback.y = Mathf.Max(knockback.y, force * 0.3f);
            knockback.z = 0;
            _rb.AddForce(knockback, ForceMode.VelocityChange);
            _inKnockback = true;
            _knockbackEndTime = Time.time + 0.2f;
        }
    }
}
