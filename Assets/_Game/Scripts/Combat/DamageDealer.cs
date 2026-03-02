using UnityEngine;
using SunlessReach.Player;

namespace SunlessReach.Combat
{
    [RequireComponent(typeof(Collider))]
    public class DamageDealer : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockbackForce = 8f;
        [SerializeField] private bool useParentPosition = true;

        private Damageable _hitThisActivation;
        private PlayerHealth _hitPlayerThisActivation;

        public void SetDamage(int dmg)
        {
            damage = dmg;
        }

        private void OnEnable()
        {
            _hitThisActivation = null;
            _hitPlayerThisActivation = null;
            // OnTriggerEnter misses targets already inside the hitbox when it's enabled, so sweep once with OverlapBox.
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Vector3 center = transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
            var hits = Physics.OverlapBox(center, halfExtents, transform.rotation, ~0, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (TryDealDamage(hit)) break;
            }
        }

        private void OnTriggerEnter(Collider other) => TryDealDamage(other);

        private bool TryDealDamage(Collider other)
        {
            // Skip the attacker - the hitbox is a child of whoever's swinging.
            if (other.transform.root == transform.root) return false;

            // Enemies / bosses.
            var damageable = other.GetComponent<Damageable>();
            if (damageable == null)
                damageable = other.GetComponentInParent<Damageable>();

            if (damageable != null)
            {
                if (damageable.IsDead) return false;
                if (damageable == _hitThisActivation) return false;

                Vector3 src = useParentPosition && transform.parent != null
                    ? transform.parent.position
                    : transform.position;
                Vector3 dir = (other.transform.position - src).normalized;
                dir.z = 0;

                damageable.TakeDamage(damage, dir, knockbackForce);
                _hitThisActivation = damageable;
                return true;
            }

            // Player uses a separate health system.
            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null) return false;
            if (playerHealth.IsInvincible) return false;
            if (playerHealth == _hitPlayerThisActivation) return false;

            Vector3 sourcePos = useParentPosition && transform.parent != null
                ? transform.parent.position
                : transform.position;
            Vector3 knockbackDir = (other.transform.position - sourcePos).normalized;
            knockbackDir.z = 0;

            playerHealth.TakeDamage(damage, knockbackDir, knockbackForce);
            _hitPlayerThisActivation = playerHealth;
            return true;
        }
    }
}
