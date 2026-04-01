using UnityEngine;
using System.Collections.Generic;

namespace CatRaising.Systems
{
    /// <summary>
    /// Central audio manager for all sound effect hooks.
    /// Attach to a GameObject in the scene and assign clips in the Inspector.
    /// Other scripts call SoundEffectHooks.Instance.PlaySound("pet") to trigger sounds.
    /// 
    /// No audio files are required yet — this provides the wiring so sounds can be
    /// dropped in later without touching any other scripts.
    /// </summary>
    public class SoundEffectHooks : MonoBehaviour
    {
        public static SoundEffectHooks Instance { get; private set; }

        [Header("Audio Sources")]
        [Tooltip("Main AudioSource for one-shot sound effects")]
        [SerializeField] private AudioSource sfxSource;
        [Tooltip("Secondary AudioSource for ambient/looping sounds (purring, rain)")]
        [SerializeField] private AudioSource ambientSource;

        [Header("Cat Sounds")]
        [SerializeField] private AudioClip petSound;
        [SerializeField] private AudioClip[] meowVariants;
        [SerializeField] private AudioClip purrLoop;
        [SerializeField] private AudioClip yawnSound;
        [SerializeField] private AudioClip stretchSound;
        [SerializeField] private AudioClip hissSound;

        [Header("Interaction Sounds")]
        [SerializeField] private AudioClip feedSound;
        [SerializeField] private AudioClip waterPourSound;
        [SerializeField] private AudioClip eatCrunch;
        [SerializeField] private AudioClip waterLap;
        [SerializeField] private AudioClip toyJingle;
        [SerializeField] private AudioClip pounceLand;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip coinChime;
        [SerializeField] private AudioClip milestoneJingle;
        [SerializeField] private AudioClip pageFlip;

        [Header("Ambient")]
        [SerializeField] private AudioClip rainLoop;
        [SerializeField] private AudioClip fireCrackle;

        [Header("Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float ambientVolume = 0.6f;

        // Clip lookup for string-based playback
        private Dictionary<string, AudioClip> _clipMap;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create AudioSources if not assigned
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.playOnAwake = false;
                ambientSource.loop = true;
            }

            BuildClipMap();
        }

        /// <summary>
        /// Build the string → clip lookup map.
        /// </summary>
        private void BuildClipMap()
        {
            _clipMap = new Dictionary<string, AudioClip>
            {
                { "pet",            petSound },
                { "purr",           purrLoop },
                { "yawn",           yawnSound },
                { "stretch",        stretchSound },
                { "hiss",           hissSound },
                { "feed",           feedSound },
                { "water_pour",     waterPourSound },
                { "eat",            eatCrunch },
                { "drink",          waterLap },
                { "toy",            toyJingle },
                { "pounce",         pounceLand },
                { "button",         buttonClick },
                { "coin",           coinChime },
                { "milestone",      milestoneJingle },
                { "page_flip",      pageFlip },
            };
        }

        /// <summary>
        /// Play a sound effect by name. Safe to call even if the clip is null (no-op).
        /// </summary>
        /// <param name="clipName">Registered clip name (e.g., "pet", "feed", "meow")</param>
        /// <param name="volumeScale">Optional volume multiplier (0-1)</param>
        public void PlaySound(string clipName, float volumeScale = 1f)
        {
            // Special case: meow with random variant
            if (clipName == "meow")
            {
                PlayRandomMeow(volumeScale);
                return;
            }

            if (_clipMap.TryGetValue(clipName, out AudioClip clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
            }
            else
            {
                // Silent no-op when clip isn't assigned yet — expected during development
                Debug.Log($"[SoundEffectHooks] Clip '{clipName}' not assigned (this is OK during dev).");
            }
        }

        /// <summary>
        /// Play a random meow variant.
        /// </summary>
        public void PlayRandomMeow(float volumeScale = 1f)
        {
            if (meowVariants == null || meowVariants.Length == 0)
            {
                Debug.Log("[SoundEffectHooks] No meow variants assigned.");
                return;
            }

            AudioClip clip = meowVariants[Random.Range(0, meowVariants.Length)];
            if (clip != null)
            {
                // Slight pitch variation for natural feel
                sfxSource.pitch = Random.Range(0.9f, 1.1f);
                sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
                sfxSource.pitch = 1f; // Reset after scheduling
            }
        }

        /// <summary>
        /// Start looping an ambient sound (e.g., purring, rain).
        /// </summary>
        public void StartAmbient(string clipName)
        {
            AudioClip clip = null;

            switch (clipName)
            {
                case "purr": clip = purrLoop; break;
                case "rain": clip = rainLoop; break;
                case "fire": clip = fireCrackle; break;
            }

            if (clip == null)
            {
                Debug.Log($"[SoundEffectHooks] Ambient clip '{clipName}' not assigned.");
                return;
            }

            ambientSource.clip = clip;
            ambientSource.volume = ambientVolume * masterVolume;
            ambientSource.Play();
        }

        /// <summary>
        /// Stop the currently looping ambient sound.
        /// </summary>
        public void StopAmbient()
        {
            ambientSource.Stop();
        }

        /// <summary>
        /// Update master volume (e.g., from settings UI).
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ambientSource.volume = ambientVolume * masterVolume;
        }

        /// <summary>
        /// Update SFX volume.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Update ambient volume.
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            ambientSource.volume = ambientVolume * masterVolume;
        }
    }
}
