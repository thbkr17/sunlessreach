using UnityEngine;
using SunlessReach.Core;

namespace SunlessReach.CameraSystem
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 offset = new Vector3(0, 2, -15);
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSmooth = 0.5f;

        private Vector3 _velocity;
        private float _currentLookAhead;
        private float _lookAheadVelocity;

        private float _shakeMagnitude;
        private float _shakeDuration;
        private float _shakeRemaining;
        private Vector3 _appliedShake;

        public void SetTarget(Transform t) => target = t;

        private void OnEnable()
        {
            EventBus.OnScreenShake += AddShake;
        }

        private void OnDisable()
        {
            EventBus.OnScreenShake -= AddShake;
        }

        public void AddShake(float magnitude, float duration)
        {
            if (magnitude > _shakeMagnitude) _shakeMagnitude = magnitude;
            if (duration > _shakeRemaining) { _shakeRemaining = duration; _shakeDuration = duration; }
        }

        private void LateUpdate()
        {
            // Strip last frame's shake before recomputing follow
            transform.position -= _appliedShake;

            if (target != null)
            {
                float facingDir = target.localScale.x > 0 ? 1 : -1;
                _currentLookAhead = Mathf.SmoothDamp(_currentLookAhead, facingDir * lookAheadDistance, ref _lookAheadVelocity, lookAheadSmooth);

                Vector3 targetPos = target.position + offset;
                targetPos.x += _currentLookAhead;
                targetPos.z = offset.z;

                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
            }

            if (_shakeRemaining > 0f)
            {
                _shakeRemaining -= Time.unscaledDeltaTime;
                float t = _shakeDuration > 0 ? Mathf.Clamp01(_shakeRemaining / _shakeDuration) : 0;
                Vector2 noise = Random.insideUnitCircle;
                _appliedShake = new Vector3(noise.x, noise.y, 0f) * (_shakeMagnitude * t);
                if (_shakeRemaining <= 0f)
                {
                    _shakeMagnitude = 0f;
                    _appliedShake = Vector3.zero;
                }
            }
            else
            {
                _appliedShake = Vector3.zero;
            }

            transform.position += _appliedShake;
        }
    }
}
