using UnityEngine;
using UnityEngine.Rendering.Universal;
using CatRaising.Core;

namespace CatRaising.Systems
{
    /// <summary>
    /// Controls visual day/night cycle based on real-world time.
    /// Applies color tinting to the global light and background.
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

        [Header("Settings")]
        [Tooltip("How quickly the lighting transitions between phases")]
        [SerializeField] private float transitionSpeed = 0.5f;

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

        private Color _targetLightColor;
        private float _targetIntensity;
        private Color _targetBackgroundTint;

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += OnPhaseChanged;
                ApplyPhase(TimeManager.Instance.CurrentPhase, instant: true);
            }
            else
            {
                Debug.LogWarning("[DayNightController] No TimeManager found. Using default lighting.");
            }
        }

        private void Update()
        {
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
        /// Called when the time phase changes.
        /// </summary>
        private void OnPhaseChanged(TimeManager.DayPhase newPhase)
        {
            ApplyPhase(newPhase, instant: false);
            Debug.Log($"[DayNightController] Phase changed to {newPhase}");
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
                    break;

                case TimeManager.DayPhase.Afternoon:
                    _targetLightColor = afternoonTint;
                    _targetIntensity = afternoonIntensity;
                    _targetBackgroundTint = afternoonTint;
                    break;

                case TimeManager.DayPhase.Evening:
                    _targetLightColor = eveningTint;
                    _targetIntensity = eveningIntensity;
                    _targetBackgroundTint = eveningTint;
                    break;

                case TimeManager.DayPhase.Night:
                    _targetLightColor = nightTint;
                    _targetIntensity = nightIntensity;
                    _targetBackgroundTint = nightTint;
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

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }
}
