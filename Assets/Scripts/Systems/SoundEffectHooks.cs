using UnityEngine;
using System.Collections.Generic;

namespace CatRaising.Systems
{
    /// <summary>
    /// Central audio manager for all sound effects and background music.
    /// Attach to a GameObject in the scene and assign clips in the Inspector.
    /// Other scripts call SoundEffectHooks.Instance.PlaySound("pet") to trigger sounds.
    /// </summary>
    public class SoundEffectHooks : MonoBehaviour
    {
        public static SoundEffectHooks Instance { get; private set; }

        [Header("Audio Sources")]
        [Tooltip("Main AudioSource for one-shot sound effects")]
        [SerializeField] private AudioSource sfxSource;
        [Tooltip("Secondary AudioSource for ambient/looping sounds (purring, rain)")]
        [SerializeField] private AudioSource ambientSource;
        [Tooltip("AudioSource for background music")]
        public AudioSource bgmSource;

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
        [SerializeField] private AudioClip woodPlaceSound;
        [SerializeField] private AudioClip buySound;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip coinChime;
        [SerializeField] private AudioClip milestoneJingle;
        [SerializeField] private AudioClip pageFlip;
        [SerializeField] private AudioClip fishMinigame;

        [Header("Background Music — Normal")]
        [Tooltip("Array of tracks to play randomly in the main game")]
        [SerializeField] private AudioClip[] normalBGM;

        [Header("Background Music — Mini-Game")]
        [Tooltip("Array of tracks to play randomly during mini-game rounds")]
        [SerializeField] private AudioClip[] miniGameBGM;

        [Header("Ambient")]
        [SerializeField] private AudioClip rainLoop;
        [SerializeField] private AudioClip fireCrackle;

        [Header("Settings")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;
        [Range(0f, 1f)]
        public float sfxVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float ambientVolume = 0.6f;
        [Range(0f, 1f)]
        public float bgmVolume = 0.4f;

        // Clip lookup for string-based playback
        private Dictionary<string, AudioClip> _clipMap;
        private int _lastNormalBGMIndex = -1;
        private int _lastMiniGameBGMIndex = -1;
        private bool _miniGameBGMActive = false;

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

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = false; // We handle looping manually via OnTrackEnd
            }

            BuildClipMap();
        }

        private void Start()
        {
            // Start normal BGM on game load
            PlayNormalBGM();
        }

        private void Update()
        {
            // Auto-advance BGM when current track finishes
            if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
            {
                if (_miniGameBGMActive)
                    PlayRandomMiniGameTrack();
                else
                    PlayNormalBGM();
            }
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
                { "wood_place",     woodPlaceSound },
                { "buy",            buySound },
                { "button",         buttonClick },
                { "coin",           coinChime },
                { "milestone",      milestoneJingle },
                { "page_flip",      pageFlip },
                { "fish_minigame",  fishMinigame }
            };
        }

        // ─── Sound Effects ─────────────────────────────────────

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
        /// Convenience: play the UI button click sound.
        /// Call from any button listener: SoundEffectHooks.Instance?.PlayButtonClick()
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySound("button");
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

        // ─── Ambient Sounds ────────────────────────────────────

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

        // ─── Background Music ──────────────────────────────────

        /// <summary>
        /// Play a random normal BGM track. Loops through the array randomly.
        /// </summary>
        public void PlayNormalBGM()
        {
            if (normalBGM == null || normalBGM.Length == 0) return;

            _miniGameBGMActive = false;
            int index = GetRandomIndex(normalBGM.Length, ref _lastNormalBGMIndex);
            PlayBGMTrack(normalBGM[index]);
        }

        /// <summary>
        /// Start mini-game BGM (stops normal BGM). Call each time a new round starts.
        /// </summary>
        public void StartMiniGameBGM()
        {
            _miniGameBGMActive = true;
            PlayRandomMiniGameTrack();
        }

        /// <summary>
        /// Stop mini-game BGM and resume normal BGM.
        /// </summary>
        public void StopMiniGameBGM()
        {
            _miniGameBGMActive = false;
            PlayNormalBGM();
        }

        private void PlayRandomMiniGameTrack()
        {
            if (miniGameBGM == null || miniGameBGM.Length == 0) return;

            int index = GetRandomIndex(miniGameBGM.Length, ref _lastMiniGameBGMIndex);
            PlayBGMTrack(miniGameBGM[index]);
        }

        private void PlayBGMTrack(AudioClip clip)
        {
            if (clip == null || bgmSource == null) return;

            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.Play();
        }

        /// <summary>
        /// Pick a random index, avoiding the last one played (for variety).
        /// </summary>
        private int GetRandomIndex(int count, ref int lastIndex)
        {
            if (count <= 1) return 0;
            int index;
            do { index = Random.Range(0, count); } while (index == lastIndex);
            lastIndex = index;
            return index;
        }

        // ─── Volume Controls ───────────────────────────────────

        /// <summary>
        /// Update master volume (e.g., from settings UI).
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ambientSource.volume = ambientVolume * masterVolume;
            if (bgmSource != null)
                bgmSource.volume = bgmVolume * masterVolume;
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

        /// <summary>
        /// Update BGM volume.
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource != null)
                bgmSource.volume = bgmVolume * masterVolume;
        }
    }
}
