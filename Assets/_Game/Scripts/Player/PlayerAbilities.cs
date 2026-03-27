using UnityEngine;
using SunlessReach.Data;
using SunlessReach.Core;

namespace SunlessReach.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAbilities : MonoBehaviour
    {
        [Header("Dash")]
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.6f;

        [Header("Audio")]
        [SerializeField] private AudioClip dashClip;
        [SerializeField] private AudioClip doubleJumpClip;
        [SerializeField] private float abilityVolume = 0.7f;

        [Header("References")]
        [SerializeField] private GameState gameState;

        private PlayerController _controller;
        private PlayerInputHandler _input;
        private PlayerHealth _health;
        private Rigidbody _rb;
        private AudioSource _audio;

        private float _dashCooldownTimer;
        private float _dashTimer;
        private bool _isDashing;
        private bool _hasAirDashed;
        private bool _hasDoubleJumped;

        private int _playerLayer;
        private int _enemyLayer = -1;
        private bool _enemyCollisionIgnored;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _input = GetComponent<PlayerInputHandler>();
            _health = GetComponent<PlayerHealth>();
            _rb = GetComponent<Rigidbody>();
            _playerLayer = gameObject.layer;
            _enemyLayer = LayerMask.NameToLayer("Enemy");

            if (gameState == null)
                gameState = Resources.FindObjectsOfTypeAll<GameState>()[0];

            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
        }

        private void PlayAbilitySfx(AudioClip clip)
        {
            if (_audio == null || clip == null) return;
            _audio.pitch = Random.Range(0.96f, 1.04f);
            _audio.PlayOneShot(clip, abilityVolume * AudioPrefs.SfxVolume);
        }

        private void Update()
        {
            if (_controller.CurrentState == PlayerState.Dead)
            {
                SetEnemyCollisionIgnored(false);   // safety: don't get stuck phasing through enemies after a death mid-dash
                return;
            }

            _dashCooldownTimer -= Time.deltaTime;

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0)
                {
                    EndDash();
                }
                return;
            }

            if (_input.DashPressed && CanDash())
            {
                StartDash();
            }
        }

        private bool CanDash()
        {
            if (!gameState.hasDash) return false;
            if (_dashCooldownTimer > 0) return false;
            if (!_controller.IsGrounded && _hasAirDashed) return false;
            return true;
        }

        private void StartDash()
        {
            _isDashing = true;
            _dashTimer = dashDuration;
            _dashCooldownTimer = dashCooldown;

            if (!_controller.IsGrounded)
                _hasAirDashed = true;

            // I-frames during dash
            if (_health != null)
                _health.SetInvincible(dashDuration);

            float dir = _controller.FacingDirection;
            _rb.useGravity = false;
            _rb.linearVelocity = new Vector3(dir * dashSpeed, 0, 0);
            _controller.ForceState(PlayerState.Dashing);
            SetEnemyCollisionIgnored(true);   // dash straight through enemies
            PlayAbilitySfx(dashClip);
        }

        private void EndDash()
        {
            _isDashing = false;
            SetEnemyCollisionIgnored(false);
            _rb.useGravity = true;
            var vel = _rb.linearVelocity;
            vel.x *= 0.3f; // slow down after dash
            _rb.linearVelocity = vel;
            _controller.ForceState(PlayerState.Falling);
        }

        private void SetEnemyCollisionIgnored(bool ignore)
        {
            if (_enemyCollisionIgnored == ignore || _enemyLayer < 0) return;
            _enemyCollisionIgnored = ignore;
            Physics.IgnoreLayerCollision(_playerLayer, _enemyLayer, ignore);
        }

        public bool CanDoubleJump()
        {
            return gameState.hasDoubleJump && !_hasDoubleJumped && !_controller.IsGrounded;
        }

        public void DoDoubleJump(float force)
        {
            _hasDoubleJumped = true;
            var vel = _rb.linearVelocity;
            vel.y = force;
            _rb.linearVelocity = vel;
            PlayAbilitySfx(doubleJumpClip);
        }

        public void ResetAirAbilities()
        {
            _hasAirDashed = false;
            _hasDoubleJumped = false;
        }

        // Refresh the air dash without landing (used by the pogo bounce).
        public void ResetAirDash()
        {
            _hasAirDashed = false;
        }
    }
}
