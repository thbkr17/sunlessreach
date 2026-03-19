using UnityEngine;

namespace SunlessReach.Enemies
{
    public class FlyerEnemy : EnemyBase
    {
        private enum FlyerState { Hover, Swoop, Recover }

        [SerializeField] private float hoverHeight = 4f;
        [SerializeField] private float swoopSpeed = 15f;
        [SerializeField] private float swoopCooldown = 3f;
        [SerializeField] private float hoverAmplitude = 0.5f;
        [SerializeField] private float hoverFrequency = 2f;

        private FlyerState _state = FlyerState.Hover;
        private Vector3 _hoverCenter;
        private Vector3 _swoopTarget;
        private float _swoopCooldownTimer;
        private float _recoverTimer;
        private float _hoverTime;

        protected override void Awake()
        {
            base.Awake();
            rb.useGravity = false;
        }

        protected override void Start()
        {
            base.Start();
            _hoverCenter = transform.position;
        }

        protected override void UpdateAI()
        {
            switch (_state)
            {
                case FlyerState.Hover:
                    Hover();
                    break;
                case FlyerState.Swoop:
                    Swoop();
                    break;
                case FlyerState.Recover:
                    Recover();
                    break;
            }
        }

        private void Hover()
        {
            _hoverTime += Time.deltaTime;
            _swoopCooldownTimer -= Time.deltaTime;

            Vector3 targetPos = _hoverCenter + Vector3.up * Mathf.Sin(_hoverTime * hoverFrequency) * hoverAmplitude;

            float dist = DistanceToPlayer();
            if (dist < enemyData.detectionRange && playerTransform != null)
            {
                Vector3 dirToPlayer = DirectionToPlayer();
                FaceDirection(dirToPlayer.x);
                _hoverCenter += new Vector3(dirToPlayer.x, 0, 0) * enemyData.moveSpeed * 0.5f * Time.deltaTime;
                _hoverCenter.y = Mathf.Max(playerTransform.position.y + hoverHeight,
                    _hoverCenter.y + (playerTransform.position.y + hoverHeight - _hoverCenter.y) * Time.deltaTime);

                if (_swoopCooldownTimer <= 0 && dist < enemyData.attackRange * 3f)
                {
                    StartSwoop();
                }
            }

            rb.linearVelocity = (targetPos - transform.position) * 5f;
        }

        private void StartSwoop()
        {
            _state = FlyerState.Swoop;
            _swoopTarget = playerTransform.position;
        }

        private void Swoop()
        {
            Vector3 dir = (_swoopTarget - transform.position).normalized;
            dir.z = 0;
            rb.linearVelocity = dir * swoopSpeed;

            if (Vector3.Distance(transform.position, _swoopTarget) < 1f ||
                transform.position.y < _swoopTarget.y - 1f)
            {
                _state = FlyerState.Recover;
                _recoverTimer = 1f;
                _swoopCooldownTimer = swoopCooldown;
            }
        }

        private void Recover()
        {
            _recoverTimer -= Time.deltaTime;
            Vector3 targetPos = _hoverCenter;
            rb.linearVelocity = (targetPos - transform.position).normalized * enemyData.moveSpeed;

            if (_recoverTimer <= 0 || Vector3.Distance(transform.position, _hoverCenter) < 1f)
            {
                _state = FlyerState.Hover;
                _hoverCenter = transform.position;
            }
        }
    }
}
