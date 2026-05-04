using UnityEngine;
using System.Collections;
using SunlessReach.Core;

namespace SunlessReach.Enemies
{
    public class LostKingBoss : BossBase
    {
        public override string BossName => "THE LOST KING";

        [Header("Attack Settings")]
        [SerializeField] private float meleeRange = 3f;
        [SerializeField] private float attackInterval = 2f;
        [SerializeField] private float swingWindup = 0.5f;     // telegraph before the blade lands
        [SerializeField] private float swingActiveTime = 0.16f;
        [SerializeField] private float swingRecover = 0.25f;

        [Header("Hitboxes")]
        [SerializeField] private GameObject slamHitbox;

        [Header("Audio")]
        [SerializeField] private AudioClip swingClip;
        [SerializeField] private float swingVolume = 0.75f;

        [Header("Falling fireballs (phase 2+)")]
        [SerializeField] private GameObject fireballPrefab;          // the Projectile prefab
        [SerializeField] private float fireballSpawnHeight = 116f;   // ceiling Y to drop from
        [SerializeField] private Vector2 fireballArenaX = new Vector2(-188f, -167f);
        [SerializeField] private float fireballFallSpeed = 9.1f;
        [SerializeField] private float fireballIntervalP2 = 1.2f;    // limited rate, phase 2 & 3
        [Range(0f, 1f)]
        [SerializeField] private float fireballPlayerBias = 0.85f;   // 1 = lands right on the player

        [Header("Dash attack (phase 2+)")]
        [SerializeField] private float dashAttackSpeed = 16f;
        [SerializeField] private float dashAttackDuration = 0.35f;
        [SerializeField] private float dashAttackInterval = 2.2f;
        [SerializeField] private float dashAttackMinRange = 3f;      // only dash if player is at least this far

        [Header("Jump attack + floor fire (phase 3)")]
        [SerializeField] private GameObject floorFireVisual;         // a Hun0FX fire prefab (e.g. FX_Fire_03)
        [SerializeField] private AudioClip floorFireClip;            // PyroParticles FireExplosion9 (impact)
        [SerializeField] private float floorFireClipVolume = 0.65f;
        [SerializeField] private AudioClip floorFireLoopClip;        // PyroParticles FireLoop3 (the lasting fire)
        [SerializeField] private float floorFireLoopVolume = 0.45f;
        [SerializeField] private float jumpAttackInterval = 3.2f;
        [SerializeField] private float jumpRiseSpeed = 11f;
        [SerializeField] private float jumpHorizontalSpeed = 6f;
        [SerializeField] private float jumpSlamSpeed = 22f;
        [SerializeField] private Vector3 floorFireScale = new Vector3(2f, 1.8f, 1.5f);
        [SerializeField] private int floorFireDamage = 1;
        [SerializeField] private float floorFireHarmlessTime = 1f;   // small & safe window
        [SerializeField] private float floorFireBurnTime = 3.5f;

        [Header("Visual Flip")]
        // Child holding the model; rotated to face the player (the root has non-uniform scale, so it can't be mirrored).
        [SerializeField] private Transform visualToFlip;
        [SerializeField] private float visualYawRight = 90f;
        [SerializeField] private float visualYawLeft  = -90f;

        private float _attackTimer;
        private bool _isAttacking;
        private float _phaseAttackInterval;
        private int _currentDamage;
        private float _fireballInterval;   // 0 = disabled (phase 1)
        private float _fireballTimer;
        private bool _dashEnabled;
        private float _dashTimer;
        private bool _jumpEnabled;
        private float _jumpTimer;
        private Animator _animator;
        private AudioSource _audio;

        protected override void Awake()
        {
            base.Awake();
            _phaseAttackInterval = attackInterval;
            _currentDamage = 2;
            DisableHitboxes();
            SetHitboxDamage(_currentDamage);
            _animator = GetComponentInChildren<Animator>(true);
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.loop = false;
            _audio.spatialBlend = 0f;
        }

        private void PlaySwingAnim()
        {
            if (_animator != null) _animator.SetTrigger("Slash");
            if (_audio != null && swingClip != null)
            {
                _audio.pitch = Random.Range(0.95f, 1.05f);
                _audio.PlayOneShot(swingClip, swingVolume * Core.AudioPrefs.SfxVolume);
            }
        }

        private void SetGroundedAnim(bool grounded)
        {
            if (_animator != null) _animator.SetBool("Grounded", grounded);
        }

        private void UpdateLocomotionAnim()
        {
            if (_animator == null) return;
            float speed01 = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / 6f);
            _animator.SetFloat("Speed", speed01);
        }

        private void Update()
        {
            if (isDead || playerTransform == null) return;
            if (!encountered) return;

            // Falling fireballs, independent of the melee state machine.
            if (_fireballInterval > 0f && fireballPrefab != null)
            {
                _fireballTimer -= Time.deltaTime;
                if (_fireballTimer <= 0f)
                {
                    DropFireball();
                    _fireballTimer = _fireballInterval;
                }
            }

            if (_isAttacking) return;

            FacePlayer();
            UpdateVisualFacing();
            UpdateLocomotionAnim();
            _attackTimer -= Time.deltaTime;
            if (_dashEnabled) _dashTimer -= Time.deltaTime;
            if (_jumpEnabled) _jumpTimer -= Time.deltaTime;

            float dist = DistanceToPlayer();
            if (_attackTimer <= 0 && dist < meleeRange)
            {
                StartCoroutine(MeleeSwing());
            }
            else if (_jumpEnabled && _jumpTimer <= 0f)
            {
                StartCoroutine(JumpAttack());
            }
            else if (_dashEnabled && _dashTimer <= 0f && dist >= dashAttackMinRange)
            {
                StartCoroutine(DashAttack());
            }

            if (dist > meleeRange && !_isAttacking)
            {
                Vector3 dir = DirectionToPlayer();
                var vel = rb.linearVelocity;
                vel.x = dir.x * 4f;
                rb.linearVelocity = vel;
            }
        }

        private void UpdateVisualFacing()
        {
            if (visualToFlip == null || playerTransform == null) return;
            float dx = playerTransform.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f) return;
            float yaw = dx >= 0f ? visualYawRight : visualYawLeft;
            visualToFlip.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private IEnumerator MeleeSwing()
        {
            _isAttacking = true;
            rb.linearVelocity = Vector3.zero;
            PlaySwingAnim();

            // Telegraph the swing before the blade hits, so it's dodgeable.
            yield return new WaitForSeconds(swingWindup);

            if (slamHitbox != null) slamHitbox.SetActive(true);
            EventBus.RaiseScreenShake(0.35f, 0.2f);
            yield return new WaitForSeconds(swingActiveTime);
            if (slamHitbox != null) slamHitbox.SetActive(false);

            yield return new WaitForSeconds(swingRecover);
            _isAttacking = false;
            _attackTimer = _phaseAttackInterval;
        }

        protected override void OnPhaseChange(int newPhase)
        {
            switch (newPhase)
            {
                case 2:
                    _phaseAttackInterval = attackInterval * 0.7f;
                    _fireballInterval = fireballIntervalP2;
                    _fireballTimer = 0.5f;   // first one drops shortly after the phase begins
                    _dashEnabled = true;
                    _dashTimer = 1.5f;
                    break;
                case 3:
                    _phaseAttackInterval = attackInterval * 0.4f;
                    _fireballInterval = fireballIntervalP2;   // keep fireballs at the phase-2 rate
                    _jumpEnabled = true;
                    _jumpTimer = 2f;
                    break;
            }
        }

        protected override void OnFightReset()
        {
            StopAllCoroutines();
            _isAttacking = false;
            _phaseAttackInterval = attackInterval;
            _fireballInterval = 0f;
            _fireballTimer = 0f;
            _dashEnabled = false;
            _dashTimer = 0f;
            _jumpEnabled = false;
            _jumpTimer = 0f;
            _attackTimer = 0f;
            if (rb != null) { rb.useGravity = true; rb.linearVelocity = Vector3.zero; }
            DisableHitboxes();
            _animator?.SetFloat("Speed", 0f);
            _animator?.SetBool("Grounded", true);
        }

        private void DropFireball()
        {
            float x = Random.Range(fireballArenaX.x, fireballArenaX.y);
            if (playerTransform != null)
            {
                float px = Mathf.Clamp(playerTransform.position.x, fireballArenaX.x, fireballArenaX.y);
                x = Mathf.Lerp(x, px, fireballPlayerBias);
            }

            var pos = new Vector3(x, fireballSpawnHeight, 0f);
            var fb = Instantiate(fireballPrefab, pos, Quaternion.identity);
            var rb2 = fb.GetComponent<Rigidbody>();
            if (rb2 != null) rb2.linearVelocity = new Vector3(Random.Range(-0.6f, 0.6f), -fireballFallSpeed, 0f);
            Destroy(fb, 6f);
        }

        private IEnumerator DashAttack()
        {
            _isAttacking = true;
            rb.linearVelocity = Vector3.zero;
            float dir = Mathf.Sign(DirectionToPlayer().x);
            if (dir == 0f) dir = 1f;

            PlaySwingAnim();
            EventBus.RaiseScreenShake(0.2f, 0.15f);
            yield return new WaitForSeconds(0.35f);

            if (slamHitbox != null) slamHitbox.SetActive(true);
            float t = 0f;
            while (t < dashAttackDuration && !isDead)
            {
                rb.linearVelocity = new Vector3(dir * dashAttackSpeed, rb.linearVelocity.y, 0f);
                t += Time.deltaTime;
                yield return null;
            }
            if (slamHitbox != null) slamHitbox.SetActive(false);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, rb.linearVelocity.y, 0f);

            yield return new WaitForSeconds(0.3f);
            _isAttacking = false;
            _dashTimer = dashAttackInterval * (currentPhase >= 3 ? 0.7f : 1f);   // dashes more often in the final phase
            _attackTimer = _phaseAttackInterval * 0.5f;
        }

        private IEnumerator JumpAttack()
        {
            _isAttacking = true;
            rb.linearVelocity = Vector3.zero;
            float startY = transform.position.y;
            float dir = Mathf.Sign(DirectionToPlayer().x);
            if (dir == 0f) dir = 1f;

            if (_audio != null && swingClip != null)
            {
                _audio.pitch = Random.Range(0.9f, 1.0f);
                _audio.PlayOneShot(swingClip, swingVolume * Core.AudioPrefs.SfxVolume);
            }
            EventBus.RaiseScreenShake(0.15f, 0.12f);
            yield return new WaitForSeconds(0.3f);

            bool hadGravity = rb.useGravity;
            rb.useGravity = false;
            SetGroundedAnim(false);
            if (_animator != null) _animator.SetTrigger("Jump");

            float t = 0f;
            while (t < 0.45f && !isDead)
            {
                rb.linearVelocity = new Vector3(dir * jumpHorizontalSpeed, jumpRiseSpeed, 0f);
                t += Time.deltaTime;
                yield return null;
            }
            rb.linearVelocity = new Vector3(dir * jumpHorizontalSpeed * 0.4f, 0f, 0f);
            yield return new WaitForSeconds(0.15f);

            float airTimeout = 0f;
            while (transform.position.y > startY + 0.05f && airTimeout < 1.5f && !isDead)
            {
                rb.linearVelocity = new Vector3(0f, -jumpSlamSpeed, 0f);
                airTimeout += Time.deltaTime;
                yield return null;
            }
            var p = transform.position; p.y = startY; transform.position = p; rb.position = p;
            rb.linearVelocity = Vector3.zero;
            rb.useGravity = hadGravity;
            SetGroundedAnim(true);

            EventBus.RaiseScreenShake(0.8f, 0.35f);
            if (slamHitbox != null) slamHitbox.SetActive(true);
            var firePos = transform.position;
            firePos.y = startY - 1.3f;   // roughly at the boss's feet
            FloorFire.Spawn(floorFireVisual, firePos, floorFireScale, floorFireDamage,
                            floorFireHarmlessTime, floorFireBurnTime,
                            floorFireLoopClip, floorFireLoopVolume);
            if (floorFireClip != null) Core.UiSfx.Play(floorFireClip, floorFireClipVolume);
            yield return new WaitForSeconds(0.18f);
            if (slamHitbox != null) slamHitbox.SetActive(false);

            yield return new WaitForSeconds(0.35f);
            _isAttacking = false;
            _jumpTimer = jumpAttackInterval;
            _attackTimer = _phaseAttackInterval * 0.6f;
        }

        private void SetHitboxDamage(int damage)
        {
            if (slamHitbox != null)
            {
                var dealer = slamHitbox.GetComponent<Combat.DamageDealer>();
                if (dealer != null) dealer.SetDamage(damage);
            }
        }

        private void DisableHitboxes()
        {
            if (slamHitbox != null) slamHitbox.SetActive(false);
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            DisableHitboxes();
            StopAllCoroutines();
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            yield return new WaitForSeconds(2f);
            EventBus.RaiseVictory();
            Destroy(gameObject);
        }
    }
}
