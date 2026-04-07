using UnityEngine;

namespace SunlessReach.Environment
{
    public class OneWayPlatform : MonoBehaviour
    {
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;

            if (collision.transform.position.y < transform.position.y - 0.1f)
            {
                Physics.IgnoreCollision(_collider, collision.collider, true);
                Invoke(nameof(ReenableCollision), 0.3f);
            }
        }

        private void ReenableCollision()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var playerCollider = player.GetComponent<Collider>();
                if (playerCollider != null)
                    Physics.IgnoreCollision(_collider, playerCollider, false);
            }
        }
    }
}
