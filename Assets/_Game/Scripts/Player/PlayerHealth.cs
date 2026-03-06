using UnityEngine;
using SunlessReach.Data;
using SunlessReach.Core;

namespace SunlessReach.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private GameState gameState;
        [SerializeField] private float invincibilityDuration = 1f;
        [SerializeField] private AudioClip hurtClip;
        [SerializeField] private float hurtVolume = 0.85f;

        private float _invincibleUntil;
        private PlayerController _controller;
        private AudioSource _audio;

        public bool IsInvincible => Time.time < _invincibleUntil;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            if (gameState == null)
                gameState = Resources.FindObjectsOfTypeAll<GameState>()[0];
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
        }

        private void Start()
        {
            if (gameState == null) return;
            gameState.currentHearts = gameState.maxHearts;
            gameState.currentSouls = 0;
            EventBus.RaiseHealthChanged(gameState.currentHearts, gameState.maxHearts);
            EventBus.RaiseSoulsChanged(gameState.currentSouls, gameState.maxSouls);
            EventBus.RaiseMoneyChanged(gameState.currentGold);
        }

        public void TakeDamage(int damage, Vector3 knockbackDir, float knockbackForce)
        {
            if (IsInvincible) return;
            if (_controller.CurrentState == PlayerState.Dead) return;

            gameState.currentHearts -= damage;
            gameState.currentHearts = Mathf.Max(0, gameState.currentHearts);
            EventBus.RaiseHealthChanged(gameState.currentHearts, gameState.maxHearts);
            EventBus.RaiseHitstop(0.06f);
            EventBus.RaiseScreenShake(0.32f, 0.18f);

            if (_audio != null && hurtClip != null)
            {
                _audio.pitch = Random.Range(0.96f, 1.04f);
                _audio.PlayOneShot(hurtClip, hurtVolume * Core.AudioPrefs.SfxVolume);
            }

            SetInvincible(invincibilityDuration);

            if (gameState.currentHearts <= 0)
            {
                Die();
            }
            else
            {
                _controller.ApplyKnockback(knockbackDir, knockbackForce);
            }
        }

        public void SetInvincible(float duration)
        {
            _invincibleUntil = Time.time + duration;
        }

        private void Die()
        {
            _controller.ForceState(PlayerState.Dead);
            _controller.SetVelocity(Vector3.zero);
            EventBus.RaisePlayerDied();
        }

        // Instant death, ignores i-frames (the kill plane uses this).
        public void KillInstant()
        {
            if (_controller.CurrentState == PlayerState.Dead) return;
            gameState.currentHearts = 0;
            EventBus.RaiseHealthChanged(gameState.currentHearts, gameState.maxHearts);
            Die();
        }

        public void Respawn(Vector3 position)
        {
            transform.position = position;
            // Move the Rigidbody too, or the next physics step snaps us back to the death spot.
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = position;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            gameState.currentHearts = gameState.maxHearts;
            gameState.currentGold = 0;
            _controller.ForceState(PlayerState.Idle);
            _controller.SetVelocity(Vector3.zero);
            EventBus.RaiseHealthChanged(gameState.currentHearts, gameState.maxHearts);
            EventBus.RaiseMoneyChanged(gameState.currentGold);
            EventBus.RaisePlayerRespawned();
        }
    }
}
