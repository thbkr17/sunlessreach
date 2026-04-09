using UnityEngine;
using SunlessReach.Player;

namespace SunlessReach.Environment
{
    // Bottom-of-the-world kill plane - instant death, ignores i-frames.
    public class DeathBarrier : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other) => TryKill(other);
        private void OnTriggerStay(Collider other) => TryKill(other);

        private void TryKill(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var health = other.GetComponent<PlayerHealth>();
            if (health == null) health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.KillInstant();
        }
    }
}
