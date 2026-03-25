using UnityEngine;
using SunlessReach.Combat;

namespace SunlessReach.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        static readonly int SpeedHash  = Animator.StringToHash("Speed");
        static readonly int HitHash    = Animator.StringToHash("Hit");
        static readonly int DieHash    = Animator.StringToHash("Die");
        static readonly int AttackHash = Animator.StringToHash("Attack");

        private Rigidbody _rb;
        private Damageable _damageable;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _damageable = GetComponent<Damageable>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            if (_damageable != null)
            {
                _damageable.OnDamaged += HandleDamaged;
                _damageable.OnDeath   += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (_damageable != null)
            {
                _damageable.OnDamaged -= HandleDamaged;
                _damageable.OnDeath   -= HandleDeath;
            }
        }

        private void HandleDamaged(int damage, Vector3 dir, float force)
        {
            // Don't play a hit-react once dead.
            if (animator == null || (_damageable != null && _damageable.IsDead)) return;
            animator.ResetTrigger(HitHash);
            animator.SetTrigger(HitHash);
        }

        private void HandleDeath()
        {
            if (animator == null) return;
            animator.ResetTrigger(DieHash);
            animator.SetTrigger(DieHash);
        }

        // Plays the attack reaction; no-op if the controller has no Attack parameter.
        public void TriggerAttack()
        {
            if (animator == null) return;
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(AttackHash);
        }

        private void Update()
        {
            if (animator == null || _rb == null) return;
            animator.SetFloat(SpeedHash, Mathf.Abs(_rb.linearVelocity.x));
        }
    }
}
