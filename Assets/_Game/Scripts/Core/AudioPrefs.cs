using System;
using UnityEngine;

namespace SunlessReach.Core
{
    // Music / SFX volume (0..1), saved to PlayerPrefs.
    public static class AudioPrefs
    {
        private const string MusicKey = "vol_music";
        private const string SfxKey   = "vol_sfx";
        private const string FpsKey   = "show_fps";

        private static float _music = -1f;
        private static float _sfx   = -1f;
        private static int   _showFps = -1;

        public static event Action OnChanged;

        public static bool ShowFps
        {
            get { if (_showFps < 0) _showFps = PlayerPrefs.GetInt(FpsKey, 0); return _showFps == 1; }
            set
            {
                _showFps = value ? 1 : 0;
                PlayerPrefs.SetInt(FpsKey, _showFps);
                OnChanged?.Invoke();
            }
        }

        public static float MusicVolume
        {
            get { if (_music < 0f) _music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.5f)); return _music; }
            set
            {
                _music = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicKey, _music);
                OnChanged?.Invoke();
            }
        }

        public static float SfxVolume
        {
            get { if (_sfx < 0f) _sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 0.8f)); return _sfx; }
            set
            {
                _sfx = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxKey, _sfx);
                OnChanged?.Invoke();
            }
        }
    }
}
