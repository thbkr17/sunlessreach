using UnityEngine;
using SunlessReach.Player;

namespace SunlessReach.Environment
{
    public class HazardZone : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float damageInterval = 0.5f;
        [SerializeField] private float knockbackForce = 8f;

        private float _damageTimer;

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _damageTimer -= Time.deltaTime;
            if (_damageTimer > 0) return;

            _damageTimer = damageInterval;

            var health = other.GetComponent<PlayerHealth>();
            if (health == null) health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, Vector3.up, knockbackForce);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                _damageTimer = 0;
        }

#if UNITY_EDITOR
        // Gizmo so a hazard with no visual prop is still visible in the scene view.
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.18f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(1f, 0f, 0f, 0.85f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
#endif
    }
}
