using UnityEngine;
using UnityEngine.Rendering.Universal;
using CatRaising.Core;
using UnityEngine.UI;

namespace CatRaising.Systems
{
    /// <summary>
    /// Controls a sped-up day/night visual cycle independent of real-world time.
    /// Uses its own internal clock that runs much faster than reality, so players
    /// can experience the full day/night cycle in minutes instead of hours.
    ///
    /// The HUD clock (TimeManager) still shows real time — this only affects
    /// lighting, background tinting, and the sun/moon icon.
    /// </summary>
    public class DayNightController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The main 2D global light in the scene")]
        [SerializeField] private Light2D globalLight;
        [Tooltip("The room background SpriteRenderer")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [Tooltip("Optional overlay sprite for ambient effects")]
        [SerializeField] private SpriteRenderer ambientOverlay;

        [Header("Cycle Speed")]
        [Tooltip("How many in-game minutes pass per real second. 60 = 1 full day in 24 real minutes. 240 = 1 full day in 6 real minutes.")]
        [SerializeField] private float gameMinutesPerRealSecond = 120f;
        [Tooltip("Starting hour when the game loads (0-24). Set to -1 to use real time as starting point.")]
        [SerializeField] private float startingHour = -1f;

        [Header("Transition")]
        [Tooltip("How quickly the lighting lerps to new values")]
        [SerializeField] private float transitionSpeed = 2f;

        [Header("Light Intensities by Phase")]
        [SerializeField] private float morningIntensity = 0.9f;
        [SerializeField] private float afternoonIntensity = 1.0f;
        [SerializeField] private float eveningIntensity = 0.7f;
        [SerializeField] private float nightIntensity = 0.4f;

        [Header("Background Tints by Phase")]
        [SerializeField] private Color morningTint = new Color(1f, 0.95f, 0.85f);
        [SerializeField] private Color afternoonTint = new Color(1f, 1f, 0.98f);
        [SerializeField] private Color eveningTint = new Color(1f, 0.82f, 0.6f);
        [SerializeField] private Color nightTint = new Color(0.55f, 0.58f, 0.82f);

        [Header("UI")]
        [SerializeField] private Image sunMoonIcon;
        [SerializeField] private Sprite[] sunMoonSprites;
        [SerializeField] private GameObject[] arrows;

        // Internal accelerated clock
        private float _simulatedTimeOfDay; // 0.0 = midnight, 0.5 = noon, 1.0 = next midnight
        private TimeManager.DayPhase _currentPhase;
        private TimeManager.DayPhase _lastPhase;

        // Interpolation targets
        private Color _targetLightColor;
        private float _targetIntensity;
        private Color _targetBackgroundTint;

        /// <summary>
        /// Current simulated hour (0-23, fractional).
        /// </summary>
        public float SimulatedHour => _simulatedTimeOfDay * 24f;

        /// <summary>
        /// Current visual day phase (driven by the sped-up clock).
        /// </summary>
        public TimeManager.DayPhase VisualPhase => _currentPhase;

        private void Start()
        {
            // Initialize simulated time
            if (startingHour >= 0f && startingHour <= 24f)
            {
                _simulatedTimeOfDay = startingHour / 24f;
            }
            else
            {
                // Start from real time
                var now = System.DateTime.Now;
                _simulatedTimeOfDay = (float)(now.Hour * 3600 + now.Minute * 60 + now.Second) / 86400f;
            }

            _currentPhase = CalculatePhase(_simulatedTimeOfDay);
            _lastPhase = _currentPhase;
            ApplyPhase(_currentPhase, instant: true);

            Debug.Log($"[DayNight] Started. Simulated hour: {SimulatedHour:F1}, Phase: {_currentPhase}, Speed: {gameMinutesPerRealSecond}x");
        }

        private void Update()
        {
            // Advance the simulated clock
            float minutesElapsed = gameMinutesPerRealSecond * Time.deltaTime;
            float dayFractionElapsed = minutesElapsed / (24f * 60f); // Convert minutes to fraction of day
            _simulatedTimeOfDay += dayFractionElapsed;

            // Wrap around midnight
            if (_simulatedTimeOfDay >= 1f)
                _simulatedTimeOfDay -= 1f;

            // Check for phase change
            _currentPhase = CalculatePhase(_simulatedTimeOfDay);
            if (_currentPhase != _lastPhase)
            {
                ApplyPhase(_currentPhase, instant: false);
                Debug.Log($"[DayNight] Phase changed: {_lastPhase} → {_currentPhase} (Simulated hour: {SimulatedHour:F1})");
                _lastPhase = _currentPhase;
            }

            // Smooth transition toward target values
            float t = transitionSpeed * Time.deltaTime;

            if (globalLight != null)
            {
                globalLight.color = Color.Lerp(globalLight.color, _targetLightColor, t);
                globalLight.intensity = Mathf.Lerp(globalLight.intensity, _targetIntensity, t);
            }

            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = Color.Lerp(backgroundRenderer.color, _targetBackgroundTint, t);
            }
        }

        /// <summary>
        /// Calculate day phase from a normalized time-of-day value (0-1).
        /// </summary>
        private TimeManager.DayPhase CalculatePhase(float normalizedTime)
        {
            float hour = normalizedTime * 24f;

            if (hour >= 6f && hour < 12f)
                return TimeManager.DayPhase.Morning;
            else if (hour >= 12f && hour < 17f)
                return TimeManager.DayPhase.Afternoon;
            else if (hour >= 17f && hour < 21f)
                return TimeManager.DayPhase.Evening;
            else
                return TimeManager.DayPhase.Night;
        }

        /// <summary>
        /// Set target lighting values for a given phase.
        /// </summary>
        private void ApplyPhase(TimeManager.DayPhase phase, bool instant)
        {
            switch (phase)
            {
                case TimeManager.DayPhase.Morning:
                    _targetLightColor = morningTint;
                    _targetIntensity = morningIntensity;
                    _targetBackgroundTint = morningTint;
                    if (sunMoonIcon != null && sunMoonSprites.Length > 0) sunMoonIcon.sprite = sunMoonSprites[0];
                    SetArrow(0);
                    break;

                case TimeManager.DayPhase.Afternoon:
                    _targetLightColor = afternoonTint;
                    _targetIntensity = afternoonIntensity;
                    _targetBackgroundTint = afternoonTint;
                    if (sunMoonIcon != null && sunMoonSprites.Length > 1) sunMoonIcon.sprite = sunMoonSprites[1];
                    SetArrow(1);
                    break;

                case TimeManager.DayPhase.Evening:
                    _targetLightColor = eveningTint;
                    _targetIntensity = eveningIntensity;
                    _targetBackgroundTint = eveningTint;
                    if (sunMoonIcon != null && sunMoonSprites.Length > 2) sunMoonIcon.sprite = sunMoonSprites[2];
                    SetArrow(1);
                    break;

                case TimeManager.DayPhase.Night:
                    _targetLightColor = nightTint;
                    _targetIntensity = nightIntensity;
                    _targetBackgroundTint = nightTint;
                    if (sunMoonIcon != null && sunMoonSprites.Length > 3) sunMoonIcon.sprite = sunMoonSprites[3];
                    SetArrow(2);
                    break;
            }

            if (instant)
            {
                if (globalLight != null)
                {
                    globalLight.color = _targetLightColor;
                    globalLight.intensity = _targetIntensity;
                }

                if (backgroundRenderer != null)
                {
                    backgroundRenderer.color = _targetBackgroundTint;
                }
            }
        }

        private void SetArrow(int activeIndex)
        {
            if (arrows == null) return;
            for (int i = 0; i < arrows.Length; i++)
            {
                if (arrows[i] != null)
                    arrows[i].SetActive(i == activeIndex);
            }
        }
    }
}
