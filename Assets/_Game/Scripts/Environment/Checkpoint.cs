using UnityEngine;
using SunlessReach.Data;
using SunlessReach.Core;

namespace SunlessReach.Environment
{
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private GameState gameState;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Color activeColor = Color.yellow;
        [SerializeField] private Color inactiveColor = Color.gray;
        // Child effect (a campfire) shown once the checkpoint is active. If set, the tint feedback is skipped.
        [SerializeField] private GameObject fireEffect;

        [Header("Ambient fire loop (heard when near a lit campfire)")]
        [SerializeField] private AudioClip ambientLoopClip;
        [SerializeField] private float ambientVolume = 0.3f;
        [SerializeField] private float ambientFullVolumeDistance = 3f;   // <= this -> full volume
        [SerializeField] private float ambientMaxDistance = 16f;         // >= this -> silent
        [SerializeField] private bool startsLit;

        private bool _isActive;
        private AudioSource _ambient;
        private Transform _player;

        private void Awake()
        {
            if (ambientLoopClip != null)
            {
                _ambient = gameObject.AddComponent<AudioSource>();
                _ambient.clip = ambientLoopClip;
                _ambient.loop = true;
                _ambient.playOnAwake = false;
                _ambient.spatialBlend = 0f;                 // 2D; we fade by player distance manually
                _ambient.dopplerLevel = 0f;
                _ambient.volume = 0f;
            }
        }

        private void Start()
        {
            if (gameState == null)
                gameState = Resources.FindObjectsOfTypeAll<GameState>()[0];
            if (visualRenderer == null)
                visualRenderer = GetComponent<Renderer>();
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
            if (startsLit) _isActive = true;
            UpdateVisual();
            UpdateAmbient();
        }

        private void Update()
        {
            if (_ambient == null || !_isActive) return;
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform; else return;
            }
            float dist = Vector3.Distance(_player.position, transform.position);
            float t = Mathf.InverseLerp(ambientMaxDistance, ambientFullVolumeDistance, dist); // 0 far .. 1 near
            _ambient.volume = ambientVolume * AudioPrefs.SfxVolume * t;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || _isActive) return;

            Activate();
        }

        private void Activate()
        {
            _isActive = true;
            gameState.lastCheckpointPosition = transform.position;

            gameState.currentHearts = gameState.maxHearts;
            EventBus.RaiseHealthChanged(gameState.currentHearts, gameState.maxHearts);
            EventBus.RaiseCheckpointActivated(transform.position);
            UpdateVisual();
            UpdateAmbient();
        }

        private void UpdateVisual()
        {
            if (fireEffect != null)
            {
                fireEffect.SetActive(_isActive);
                return;
            }
            if (visualRenderer != null)
            {
                visualRenderer.material.color = _isActive ? activeColor : inactiveColor;
            }
        }

        private void UpdateAmbient()
        {
            if (_ambient == null) return;
            if (_isActive && !_ambient.isPlaying) _ambient.Play();
            else if (!_isActive && _ambient.isPlaying) _ambient.Stop();
        }
    }
}
