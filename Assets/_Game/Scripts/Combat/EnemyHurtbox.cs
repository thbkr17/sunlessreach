using UnityEngine;
using SunlessReach.Player;

namespace SunlessReach.Combat
{
    public class EnemyHurtbox : MonoBehaviour
    {
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float knockbackForce = 8f;

        // Cached so OnTriggerStay's dead-enemy check is a field read, not a lookup.
        private Damageable _ownerDamageable;

        private void Awake()
        {
            _ownerDamageable = GetComponentInParent<Damageable>();
        }

        private void OnTriggerEnter(Collider other) => TryDamage(other);
        private void OnTriggerStay(Collider other) => TryDamage(other);

        private void TryDamage(Collider other)
        {
            // No contact damage while the enemy is dead, so the player can walk over the corpse.
            if (_ownerDamageable != null && _ownerDamageable.IsDead) return;
            if (contactDamage <= 0) return;   // contact damage disabled (e.g. the boss body)

            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null) return;
            if (playerHealth.IsInvincible) return;

            Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
            knockbackDir.z = 0;
            playerHealth.TakeDamage(contactDamage, knockbackDir, knockbackForce);
        }

        public void SetContactDamage(int damage)
        {
            contactDamage = damage;
        }
    }
}
