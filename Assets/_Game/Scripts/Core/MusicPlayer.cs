using UnityEngine;
using UnityEngine.SceneManagement;
using SunlessReach.Enemies;

namespace SunlessReach.Core
{
    // Loops the background track, and a boss track while a boss fight is engaged (polled from the boss).
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private AudioClip bossMusicClip;
        [SerializeField] private float baseVolume = 0.8f;
        [SerializeField] private float bossVolume = 0.8f;

        private AudioSource _src;
        private bool _bossActive;
        private BossBase _boss;
        private float _bossSearchTimer;

        private void Awake()
        {
            _src = GetComponent<AudioSource>();
            if (_src == null) _src = gameObject.AddComponent<AudioSource>();
            _src.loop = true;
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            _src.priority = 0;
            PlayTrack(musicClip);
        }

        private void OnEnable()
        {
            AudioPrefs.OnChanged += ApplyVolume;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            AudioPrefs.OnChanged -= ApplyVolume;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _boss = null;
            _bossActive = false;
            PlayTrack(musicClip);
        }

        private void Update()
        {
            // Find the scene's boss (re-searched periodically until one exists).
            if (_boss == null)
            {
                _bossSearchTimer -= Time.unscaledDeltaTime;
                if (_bossSearchTimer <= 0f)
                {
                    _bossSearchTimer = 0.5f;
                    _boss = FindFirstObjectByType<BossBase>();
                }
            }

            bool wantBoss = bossMusicClip != null && _boss != null && _boss.IsEngaged;
            if (wantBoss != _bossActive)
            {
                _bossActive = wantBoss;
                PlayTrack(_bossActive ? bossMusicClip : musicClip);
            }
        }

        private void PlayTrack(AudioClip clip)
        {
            if (_src == null) return;
            if (clip == null) { _src.Stop(); return; }
            if (_src.clip == clip && _src.isPlaying) { ApplyVolume(); return; }
            _src.clip = clip;
            ApplyVolume();
            _src.Play();
        }

        private void ApplyVolume()
        {
            if (_src == null) return;
            float bv = _bossActive ? bossVolume : baseVolume;
            _src.volume = bv * AudioPrefs.MusicVolume;
        }
    }
}
