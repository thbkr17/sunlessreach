using UnityEngine;
using SunlessReach.Player;

namespace SunlessReach.Enemies
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockbackForce = 6f;
        [SerializeField] private AudioClip explosionClip;   // played when the projectile is fired
        [SerializeField] private float explosionVolume = 0.35f;

        private void Start()
        {
            if (explosionClip != null)
                Core.UiSfx.Play(explosionClip, explosionVolume);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var health = other.GetComponent<PlayerHealth>();
                if (health == null) health = other.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    Vector3 dir = (other.transform.position - transform.position).normalized;
                    dir.z = 0;
                    health.TakeDamage(damage, dir, knockbackForce);
                }
                Explode();
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                Explode();
            }
        }

        private void Explode()
        {
            Destroy(gameObject);
        }
    }
}
