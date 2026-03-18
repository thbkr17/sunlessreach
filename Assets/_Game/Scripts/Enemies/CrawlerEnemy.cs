using UnityEngine;

namespace SunlessReach.Enemies
{
    public class CrawlerEnemy : EnemyBase
    {
        private enum CrawlerState { Patrol, Chase }

        [SerializeField] private float patrolDistance = 5f;
        [SerializeField] private float wallCheckDistance = 0.5f;
        [SerializeField] private float edgeCheckDistance = 2f;

        private CrawlerState _state = CrawlerState.Patrol;
        private Vector3 _startPosition;

        protected override void Start()
        {
            base.Start();
            _startPosition = transform.position;
            edgeCheckDistance = 2f;
            wallCheckDistance = 0.6f;
        }

        protected override void UpdateAI()
        {
            float dist = DistanceToPlayer();

            if (dist < enemyData.detectionRange)
                _state = CrawlerState.Chase;
            else
                _state = CrawlerState.Patrol;

            switch (_state)
            {
                case CrawlerState.Patrol:
                    Patrol();
                    break;
                case CrawlerState.Chase:
                    Chase();
                    break;
            }
        }

        private void Patrol()
        {
            var vel = rb.linearVelocity;
            vel.x = facingDirection * enemyData.moveSpeed * 0.5f;
            rb.linearVelocity = vel;

            if (HitWall() || AtEdge() || TooFarFromStart())
            {
                facingDirection *= -1;
            }
        }

        private void Chase()
        {
            Vector3 dir = DirectionToPlayer();
            FaceDirection(dir.x);

            if (AtEdge() || HitWall())
            {
                facingDirection *= -1;
                var vel = rb.linearVelocity;
                vel.x = 0;
                rb.linearVelocity = vel;
                return;
            }

            var v = rb.linearVelocity;
            v.x = dir.x * enemyData.moveSpeed;
            rb.linearVelocity = v;
        }

        private bool HitWall()
        {
            Vector3 origin = transform.position + Vector3.up * 0.3f;
            return Physics.Raycast(origin, Vector3.right * facingDirection, wallCheckDistance, groundLayers);
        }

        private bool AtEdge()
        {
            // Raycast down, just ahead of the crawler.
            Vector3 basePos = groundCheck != null ? groundCheck.position : transform.position;
            Vector3 origin = basePos + Vector3.up * 0.15f + Vector3.right * facingDirection * 0.7f;
            return !Physics.Raycast(origin, Vector3.down, edgeCheckDistance, groundLayers);
        }

        private bool TooFarFromStart()
        {
            return Mathf.Abs(transform.position.x - _startPosition.x) > patrolDistance;
        }
    }
}
