using UnityEngine;

namespace CatRaising.Core
{
    /// <summary>
    /// Manages audio volume using PlayerPrefs for persistence.
    /// Attach to a persistent object or let MainMenuUI/GameSceneUI access it statically.
    /// </summary>
    public static class AudioSettings
    {
        private const string VolumeKey = "MasterVolume";
        private const float DefaultVolume = 1f;

        /// <summary>
        /// Get or set the master volume (0-1). Automatically persists to PlayerPrefs.
        /// </summary>
        public static float MasterVolume
        {
            get => AudioListener.volume;
            set
            {
                float clamped = Mathf.Clamp01(value);
                AudioListener.volume = clamped;
                PlayerPrefs.SetFloat(VolumeKey, clamped);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Load saved volume on game start. Call this once at startup.
        /// </summary>
        public static void LoadSavedVolume()
        {
            float saved = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
            AudioListener.volume = saved;
        }
    }
}
