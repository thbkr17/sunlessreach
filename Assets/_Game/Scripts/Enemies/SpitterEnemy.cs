using UnityEngine;

namespace SunlessReach.Enemies
{
    public class SpitterEnemy : EnemyBase
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 2f;
        [SerializeField] private float projectileSpeed = 8f;

        private float _fireTimer;

        protected override void Awake()
        {
            base.Awake();
            damageable.Initialize(1);
        }

        protected override void UpdateAI()
        {
            float dist = DistanceToPlayer();

            if (dist < enemyData.detectionRange && playerTransform != null)
            {
                FaceDirection(DirectionToPlayer().x);
                _fireTimer -= Time.deltaTime;

                if (_fireTimer <= 0)
                {
                    Fire();
                    _fireTimer = 1f / fireRate;
                }
            }
        }

        private void Fire()
        {
            if (projectilePrefab == null) return;

            // Play the attack animation if one's wired up.
            var driver = GetComponent<EnemyAnimatorDriver>();
            if (driver != null) driver.TriggerAttack();

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.right * facingDirection;
            var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = DirectionToPlayer();
                rb.linearVelocity = dir * projectileSpeed;
            }

            Destroy(proj, 5f);
        }
    }
}
