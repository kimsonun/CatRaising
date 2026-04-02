using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Cat;

namespace CatRaising.UI
{
    /// <summary>
    /// Individual animated need bar UI component.
    /// Shows a fill bar with smooth transitions, icon, and optional percentage text.
    /// </summary>
    public class NeedBarUI : MonoBehaviour
    {
        [SerializeField] private NeedType needType;

        [Header("UI References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image backgroundImage;

        [Header("Colors")]
        [SerializeField] private Color fullColor = new Color(0.4f, 0.85f, 0.4f);     // Green
        [SerializeField] private Color midColor = new Color(1f, 0.8f, 0.2f);          // Yellow
        [SerializeField] private Color lowColor = new Color(0.9f, 0.3f, 0.3f);        // Red
        [SerializeField] private float lowThreshold = 0.3f;
        [SerializeField] private float midThreshold = 0.6f;

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool pulseWhenLow = true;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseMinAlpha = 0.6f;

        private float _targetFill = 1f;
        public float _currentFill = 1f;
        public bool _isLow = false;
        private CatNeeds _catNeeds;

        private void Start()
        {
            _catNeeds = FindAnyObjectByType<CatNeeds>();
        }

        private void Update()
        {
            // Smooth fill transition
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, smoothSpeed * Time.deltaTime);
            
            switch (needType)
            {
                case NeedType.Hunger:
                    _targetFill = _catNeeds.Hunger / 100f;
                    break;
                case NeedType.Thirst:
                    _targetFill = _catNeeds.Thirst / 100f;
                    break;
                case NeedType.Happiness:
                    _targetFill = _catNeeds.Happiness / 100f;
                    break;
                case NeedType.Cleanliness:
                    _targetFill = _catNeeds.Cleanliness / 100f;
                    break;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
                fillImage.color = GetFillColor(_currentFill);
            }

            // Pulse animation when low
            if (pulseWhenLow && _isLow && iconImage != null)
            {
                float alpha = Mathf.Lerp(pulseMinAlpha, 1f,
                    (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
                Color iconColor = iconImage.color;
                iconColor.a = alpha;
                iconImage.color = iconColor;
            }
        }

        /// <summary>
        /// Set the fill value (0-1 normalized).
        /// </summary>
        public void SetValue(float normalized)
        {
            _targetFill = Mathf.Clamp01(normalized);
            _isLow = _targetFill < lowThreshold;

            // Update label if present
            if (labelText != null)
            {
                labelText.text = $"{Mathf.RoundToInt(normalized * 100)}%";
            }
        }

        /// <summary>
        /// Set value from a 0-100 range.
        /// </summary>
        public void SetValue100(float value)
        {
            SetValue(value / 100f);
        }

        /// <summary>
        /// Set the icon sprite.
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (iconImage != null)
                iconImage.sprite = icon;
        }

        /// <summary>
        /// Set the label text (e.g., need name).
        /// </summary>
        public void SetLabel(string text)
        {
            if (labelText != null)
                labelText.text = text;
        }

        /// <summary>
        /// Get the interpolated color based on fill level.
        /// </summary>
        private Color GetFillColor(float fill)
        {
            if (fill < lowThreshold)
                return Color.Lerp(lowColor, midColor, fill / lowThreshold);
            else if (fill < midThreshold)
                return Color.Lerp(midColor, fullColor, (fill - lowThreshold) / (midThreshold - lowThreshold));
            else
                return fullColor;
        }

        /// <summary>
        /// Instantly set the fill with no animation (for initialization).
        /// </summary>
        public void SetValueImmediate(float normalized)
        {
            _targetFill = Mathf.Clamp01(normalized);
            _currentFill = _targetFill;
            _isLow = _targetFill < lowThreshold;

            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
                fillImage.color = GetFillColor(_currentFill);
            }
        }
    }
}
