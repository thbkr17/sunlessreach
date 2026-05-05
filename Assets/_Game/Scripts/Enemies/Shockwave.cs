using UnityEngine;
using SunlessReach.Player;

namespace SunlessReach.Enemies
{
    public class Shockwave : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockbackForce = 10f;

        private float _timer;

        private void Start()
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float scale = 1 + _timer * speed;
            transform.localScale = new Vector3(scale, 0.5f, 0.5f);

            if (_timer >= lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var health = other.GetComponent<PlayerHealth>();
            if (health == null) health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;
                dir.y = 0.5f;
                dir.z = 0;
                health.TakeDamage(damage, dir, knockbackForce);
            }
        }
    }
}
