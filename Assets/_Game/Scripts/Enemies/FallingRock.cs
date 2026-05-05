using UnityEngine;
using SunlessReach.Player;

namespace SunlessReach.Enemies
{
    public class FallingRock : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockbackForce = 8f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var health = other.GetComponent<PlayerHealth>();
                if (health == null) health = other.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage, Vector3.down, knockbackForce);
                }
            }

            if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.CompareTag("Player"))
            {
                Destroy(gameObject);
            }
        }
    }
}
