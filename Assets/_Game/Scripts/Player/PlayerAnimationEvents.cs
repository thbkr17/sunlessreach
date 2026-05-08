using UnityEngine;

namespace SunlessReach.Player
{
    public class PlayerAnimationEvents : MonoBehaviour
    {
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip[] landClips;
        [SerializeField] private float footstepVolume = 0.45f;
        [SerializeField] private float landVolume = 0.7f;

        private AudioSource _audio;

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.loop = false;
            _audio.spatialBlend = 0f;   // 2D - always audible to the player
        }

        // Animation-event entry points.
        public void OnFootstep(AnimationEvent _) => PlayRandom(footstepClips, footstepVolume);
        public void OnLand(AnimationEvent _)     => PlayRandom(landClips, landVolume);
        // Param-less fallbacks for events with no argument.
        public void OnFootstep() => PlayRandom(footstepClips, footstepVolume);
        public void OnLand()     => PlayRandom(landClips, landVolume);

        // Reuse the footstep SFX on jump.
        public void PlayFootstep() => PlayRandom(footstepClips, footstepVolume);

        private void PlayRandom(AudioClip[] clips, float volume)
        {
            if (_audio == null || clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;
            _audio.pitch = Random.Range(0.92f, 1.08f);
            _audio.PlayOneShot(clip, volume * Core.AudioPrefs.SfxVolume);
        }
    }
}
