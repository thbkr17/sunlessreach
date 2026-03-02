using UnityEngine;
using System;

namespace SunlessReach.Combat
{
    public class Damageable : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        private float _invulnerableUntil;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        public event Action<int, Vector3, float> OnDamaged;  // damage, knockbackDir, force
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void Initialize(int health)
        {
            maxHealth = health;
            CurrentHealth = health;
        }

        public void SetInvulnerable(float duration)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + duration);
        }

        public void TakeDamage(int damage, Vector3 knockbackDir, float knockbackForce)
        {
            if (IsDead) return;
            if (IsInvulnerable) return;
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Max(0, CurrentHealth);

            OnDamaged?.Invoke(damage, knockbackDir, knockbackForce);

            if (CurrentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
        }
    }
}
