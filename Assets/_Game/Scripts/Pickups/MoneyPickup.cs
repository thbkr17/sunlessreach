using UnityEngine;

namespace SunlessReach.Pickups
{
    public class MoneyPickup : MonoBehaviour
    {
        [SerializeField] private int moneyAmount = 5;
        [SerializeField] private float magnetRange = 3f;
        [SerializeField] private float magnetSpeed = 10f;
        [SerializeField] private float initialPopForce = 5f;
        [SerializeField] private AudioClip pickupClip;       // "Buy Items" SFX
        [SerializeField] private float pickupVolume = 0.6f;

        private Transform _playerTransform;
        private Rigidbody _rb;
        private bool _magnetized;
        private float _spawnTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _spawnTime = Time.time;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;

            if (_rb != null)
            {
                Vector3 pop = new Vector3(Random.Range(-1f, 1f), 1f, 0).normalized * initialPopForce;
                _rb.AddForce(pop, ForceMode.VelocityChange);
            }
        }

        private void Update()
        {
            if (_playerTransform == null || Time.time - _spawnTime < 0.3f) return;

            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            if (dist < magnetRange)
            {
                _magnetized = true;
            }

            if (_magnetized)
            {
                if (_rb != null) _rb.isKinematic = true;
                Vector3 dir = (_playerTransform.position - transform.position).normalized;
                transform.position += dir * magnetSpeed * Time.deltaTime;

                if (dist < 0.5f)
                {
                    Collect();
                }
            }
        }

        private void Collect()
        {
            var gameState = FindAnyObjectByType<GameManager>()?.GameState;
            if (gameState != null)
            {
                gameState.AddGold(moneyAmount);
            }
            if (pickupClip != null) Core.UiSfx.Play(pickupClip, pickupVolume);
            Destroy(gameObject);
        }

        public void SetAmount(int amount)
        {
            moneyAmount = amount;
        }
    }
}
