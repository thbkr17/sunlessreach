using UnityEngine;
using SunlessReach.Data;

namespace SunlessReach.Core
{
    public static class UiSfx
    {
        private static AudioSource _src;
        private static AudioClip _clickClip;
        private static AudioClip _scrollClip;
        private static bool _resolvedClips;

        private static AudioSource Source()
        {
            if (_src == null)
            {
                var go = new GameObject("UiSfxSource");
                Object.DontDestroyOnLoad(go);
                _src = go.AddComponent<AudioSource>();
                _src.playOnAwake = false;
                _src.loop = false;
                _src.spatialBlend = 0f;
            }
            return _src;
        }

        private static void ResolveClips()
        {
            if (_resolvedClips) return;
            var all = Resources.FindObjectsOfTypeAll<GameState>();
            if (all.Length > 0)
            {
                _clickClip = all[0].uiClickClip;
                _scrollClip = all[0].uiScrollClip;
            }
            _resolvedClips = true;
        }

        public static void PlayClick()
        {
            ResolveClips();
            if (_clickClip == null) return;
            var s = Source();
            s.pitch = Random.Range(0.97f, 1.03f);
            s.PlayOneShot(_clickClip, 0.35f * AudioPrefs.SfxVolume);
        }

        public static void PlayScroll()
        {
            ResolveClips();
            if (_scrollClip == null) return;
            var s = Source();
            s.pitch = Random.Range(0.98f, 1.02f);
            s.PlayOneShot(_scrollClip, 0.30f * AudioPrefs.SfxVolume);
        }

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var s = Source();
            s.pitch = 1f;
            s.PlayOneShot(clip, Mathf.Clamp01(volume) * AudioPrefs.SfxVolume);
        }
    }
}
