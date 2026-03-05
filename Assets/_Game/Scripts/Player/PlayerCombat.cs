using UnityEngine;
using SunlessReach.Data;
using SunlessReach.Combat;

namespace SunlessReach.Player
{
    public enum AttackDirection { Right = 0, Left = 1, Up = 2, Down = 3 }

    public class PlayerCombat : MonoBehaviour
    {
        public AttackDirection LastAttackDirection { get; private set; } = AttackDirection.Right;

        [Header("Attack")]
        [SerializeField] private float attackCooldown = 0.35f;
        [SerializeField] private float attackDuration = 0.2f;
        [SerializeField] private GameState gameState;

        [Header("Hitboxes")]
        [SerializeField] private GameObject hitboxRight;
        [SerializeField] private GameObject hitboxLeft;
        [SerializeField] private GameObject hitboxUp;
        [SerializeField] private GameObject hitboxDown;

        [Header("Pogo Bounce")]
        [SerializeField] private float pogoInvincibilityTime = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioClip swingClip;
        [SerializeField] private float swingVolume = 0.6f;

        private PlayerController _controller;
        private PlayerInputHandler _input;
        private PlayerHealth _health;
        private PlayerAbilities _abilities;
        private AudioSource _audio;
        private float _attackCooldownTimer;
        private LayerMask _enemyLayer;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _input = GetComponent<PlayerInputHandler>();
            _health = GetComponent<PlayerHealth>();
            _abilities = GetComponent<PlayerAbilities>();
            _enemyLayer = LayerMask.GetMask("Enemy");

            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.loop = false;
            _audio.spatialBlend = 0f;

            if (gameState == null)
                gameState = Resources.FindObjectsOfTypeAll<GameState>()[0];

            // Fallback if the hitboxes weren't assigned.
            if (hitboxRight == null) hitboxRight = transform.Find("HitboxRight")?.gameObject;
            if (hitboxLeft == null) hitboxLeft = transform.Find("HitboxLeft")?.gameObject;
            if (hitboxUp == null) hitboxUp = transform.Find("HitboxUp")?.gameObject;
            if (hitboxDown == null) hitboxDown = transform.Find("HitboxDown")?.gameObject;

            DisableAllHitboxes();
        }

        private void Update()
        {
            if (_controller.CurrentState == PlayerState.Dead) return;

            _attackCooldownTimer -= Time.deltaTime;

            if (_input.AttackPressed && _attackCooldownTimer <= 0 &&
                _controller.CurrentState != PlayerState.Dashing &&
                _controller.CurrentState != PlayerState.Healing)
            {
                Attack();
            }
        }

        private void Attack()
        {
            _attackCooldownTimer = attackCooldown;
            _controller.LockState(PlayerState.Attacking, attackDuration);

            if (_audio != null && swingClip != null)
            {
                _audio.pitch = Random.Range(0.95f, 1.05f);
                _audio.PlayOneShot(swingClip, swingVolume * Core.AudioPrefs.SfxVolume);
            }

            Vector2 move = _input.MoveInput;
            GameObject hitbox;

            bool isDownAttack = false;
            if (move.y > 0.5f)
            {
                hitbox = hitboxUp;
                LastAttackDirection = AttackDirection.Up;
            }
            else if (move.y < -0.5f && !_controller.IsGrounded)
            {
                hitbox = hitboxDown;
                isDownAttack = true;
                LastAttackDirection = AttackDirection.Down;
            }
            else if (_controller.FacingDirection > 0)
            {
                hitbox = hitboxRight;
                LastAttackDirection = AttackDirection.Right;
            }
            else
            {
                hitbox = hitboxLeft;
                LastAttackDirection = AttackDirection.Left;
            }

            StartCoroutine(ActivateHitbox(hitbox, isDownAttack));
        }

        private System.Collections.IEnumerator ActivateHitbox(GameObject hitbox, bool isDownAttack = false)
        {
            bool hasBounced = false;
            hitbox.SetActive(true);

            // OnTriggerEnter is unreliable with SetActive, so sweep with OverlapBox while the hitbox is active.
            var boxCollider = hitbox.GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                float elapsed = 0f;
                var hitEnemies = new System.Collections.Generic.HashSet<GameObject>();

                while (elapsed < attackDuration)
                {
                    Vector3 worldCenter = hitbox.transform.TransformPoint(boxCollider.center);
                    Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, hitbox.transform.lossyScale);

                    Collider[] hits = Physics.OverlapBox(worldCenter, halfExtents, hitbox.transform.rotation, _enemyLayer);
                    foreach (var hit in hits)
                    {
                        if (hitEnemies.Contains(hit.gameObject)) continue;
                        hitEnemies.Add(hit.gameObject);

                        var damageable = hit.GetComponent<Damageable>();
                        if (damageable == null)
                            damageable = hit.GetComponentInParent<Damageable>();

                        if (damageable != null && !damageable.IsDead)
                        {
                            Vector3 knockbackDir = (hit.transform.position - transform.position).normalized;
                            knockbackDir.z = 0;
                            damageable.TakeDamage(gameState.attackDamage, knockbackDir, 8f);

                            // Pogo: a down-attack on an enemy bounces you up and refreshes the air dash.
                            if (isDownAttack && !hasBounced)
                            {
                                hasBounced = true;
                                _controller.ApplyKnockback(Vector3.up, 8f);
                                if (_health != null)
                                    _health.SetInvincible(pogoInvincibilityTime);
                                _abilities?.ResetAirDash();
                            }
                        }
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            hitbox.SetActive(false);
        }

        private void DisableAllHitboxes()
        {
            if (hitboxRight) hitboxRight.SetActive(false);
            if (hitboxLeft) hitboxLeft.SetActive(false);
            if (hitboxUp) hitboxUp.SetActive(false);
            if (hitboxDown) hitboxDown.SetActive(false);
        }
    }
}
