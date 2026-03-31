using System;
using UnityEngine;

namespace CatRaising.Core
{
    /// <summary>
    /// Manages real-time synchronization for day/night cycle and time-based events.
    /// Syncs with the player's actual device clock.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        /// <summary>
        /// The four phases of the day, affecting cat behavior and visuals.
        /// </summary>
        public enum DayPhase
        {
            Morning,    // 6:00 - 11:59
            Afternoon,  // 12:00 - 16:59
            Evening,    // 17:00 - 20:59
            Night       // 21:00 - 5:59
        }

        /// <summary>
        /// Current phase of the day based on real-world time.
        /// </summary>
        public DayPhase CurrentPhase { get; private set; }

        /// <summary>
        /// Current real-world hour (0-23).
        /// </summary>
        public int CurrentHour => DateTime.Now.Hour;

        /// <summary>
        /// Normalized time of day (0.0 = midnight, 0.5 = noon, 1.0 = midnight).
        /// Useful for smooth visual transitions.
        /// </summary>
        public float NormalizedTimeOfDay
        {
            get
            {
                DateTime now = DateTime.Now;
                return (float)(now.Hour * 3600 + now.Minute * 60 + now.Second) / 86400f;
            }
        }

        /// <summary>
        /// Event fired when the day phase changes.
        /// </summary>
        public event Action<DayPhase> OnPhaseChanged;

        private DayPhase _lastPhase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CurrentPhase = CalculatePhase();
            _lastPhase = CurrentPhase;
            Debug.Log($"[TimeManager] Started. Current phase: {CurrentPhase} (Hour: {CurrentHour})");
        }

        private void Update()
        {
            CurrentPhase = CalculatePhase();

            if (CurrentPhase != _lastPhase)
            {
                Debug.Log($"[TimeManager] Phase changed: {_lastPhase} → {CurrentPhase}");
                OnPhaseChanged?.Invoke(CurrentPhase);
                _lastPhase = CurrentPhase;
            }
        }

        /// <summary>
        /// Calculate the current day phase based on real-world hour.
        /// </summary>
        private DayPhase CalculatePhase()
        {
            int hour = DateTime.Now.Hour;

            if (hour >= 6 && hour < 12)
                return DayPhase.Morning;
            else if (hour >= 12 && hour < 17)
                return DayPhase.Afternoon;
            else if (hour >= 17 && hour < 21)
                return DayPhase.Evening;
            else
                return DayPhase.Night;
        }

        /// <summary>
        /// Calculate how many real-world seconds have passed since the given time.
        /// Used for offline need decay.
        /// </summary>
        public float GetSecondsSince(DateTime time)
        {
            TimeSpan elapsed = DateTime.Now - time;
            return (float)elapsed.TotalSeconds;
        }

        /// <summary>
        /// Whether it's currently considered "daytime" (cat should be more active).
        /// </summary>
        public bool IsDaytime => CurrentPhase == DayPhase.Morning || CurrentPhase == DayPhase.Afternoon;

        /// <summary>
        /// Get a color tint representing the current time of day.
        /// Used by DayNightController for visual lighting.
        /// </summary>
        public Color GetTimeOfDayColor()
        {
            float t = NormalizedTimeOfDay;

            // Smooth color transitions through the day
            // Morning (6-12): warm golden
            // Afternoon (12-17): bright white
            // Evening (17-21): orange/warm
            // Night (21-6): cool blue

            if (CurrentPhase == DayPhase.Morning)
            {
                float phaseT = Mathf.InverseLerp(6f / 24f, 12f / 24f, t);
                return Color.Lerp(
                    new Color(1f, 0.92f, 0.7f),  // warm golden
                    new Color(1f, 1f, 0.95f),     // bright warm white
                    phaseT
                );
            }
            else if (CurrentPhase == DayPhase.Afternoon)
            {
                float phaseT = Mathf.InverseLerp(12f / 24f, 17f / 24f, t);
                return Color.Lerp(
                    new Color(1f, 1f, 0.95f),     // bright warm white
                    new Color(1f, 0.85f, 0.65f),  // warm orange
                    phaseT
                );
            }
            else if (CurrentPhase == DayPhase.Evening)
            {
                float phaseT = Mathf.InverseLerp(17f / 24f, 21f / 24f, t);
                return Color.Lerp(
                    new Color(1f, 0.85f, 0.65f),  // warm orange
                    new Color(0.6f, 0.65f, 0.85f), // cool blue
                    phaseT
                );
            }
            else // Night
            {
                // Night stays cool blue with slight variation
                return new Color(0.5f, 0.55f, 0.78f);
            }
        }
    }
}
