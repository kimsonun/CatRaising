using UnityEngine;
using CatRaising.Cat;

namespace CatRaising.UI
{
    /// <summary>
    /// Displays a thought bubble above the cat when any need is low.
    /// Shows an icon for the most urgent need.
    /// Priority: Hunger > Thirst > Happiness > Cleanliness.
    /// 
    /// SETUP:
    /// 1. Create a child GameObject on the Cat called "NeedBubble"
    /// 2. Add a SpriteRenderer for the bubble background
    /// 3. Add a child "Icon" with its own SpriteRenderer for the need icon
    /// 4. Assign sprites in the Inspector for each need type + bubble background
    /// </summary>
    public class NeedBubble : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CatNeeds catNeeds;
        [SerializeField] private SpriteRenderer bubbleRenderer;
        [SerializeField] private SpriteRenderer iconRenderer;

        [Header("Need Icons")]
        [Tooltip("Icon shown when hunger is low (e.g., a fish or bowl)")]
        [SerializeField] private Sprite hungerIcon;
        [Tooltip("Icon shown when thirst is low (e.g., a water drop)")]
        [SerializeField] private Sprite thirstIcon;
        [Tooltip("Icon shown when happiness is low (e.g., a sad face)")]
        [SerializeField] private Sprite happinessIcon;
        [Tooltip("Icon shown when cleanliness is low (e.g., sparkle or soap)")]
        [SerializeField] private Sprite cleanlinessIcon;

        [Header("Settings")]
        [Tooltip("Threshold below which a need is considered low")]
        [SerializeField] private float lowThreshold = 30f;

        [Header("Animation")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float fadeSpeed = 5f;
        [SerializeField] private float popInDuration = 0.2f;

        private Vector3 _baseLocalPos;
        private bool _isShowing = false;
        private NeedType _currentNeed;
        private float _showTimer = 0f;
        private float _currentAlpha = 0f;
        private float _currentScale = 0f;

        private void Start()
        {
            _baseLocalPos = transform.localPosition;

            if (catNeeds == null)
                catNeeds = GetComponentInParent<CatNeeds>();

            // Start hidden
            SetVisuals(0f, 0f);
        }

        private void Update()
        {
            if (catNeeds == null) return;

            // Determine if any need is low and which one to show
            NeedType? urgentNeed = GetMostUrgentLowNeed();

            if (urgentNeed.HasValue)
            {
                if (!_isShowing || _currentNeed != urgentNeed.Value)
                {
                    // Show or swap icon
                    _currentNeed = urgentNeed.Value;
                    SetIcon(_currentNeed);
                    _isShowing = true;
                    _showTimer = 0f;
                }
            }
            else
            {
                _isShowing = false;
            }

            // Animate visibility
            float targetAlpha = _isShowing ? 1f : 0f;
            float targetScale = _isShowing ? 1f : 0f;

            _currentAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            _currentScale = Mathf.Lerp(_currentScale, targetScale, fadeSpeed * 2f * Time.deltaTime);

            SetVisuals(_currentAlpha, _currentScale);

            // Bobbing animation
            if (_isShowing)
            {
                _showTimer += Time.deltaTime;
                float bobOffset = Mathf.Sin(_showTimer * bobSpeed) * bobAmplitude;
                transform.localPosition = _baseLocalPos + Vector3.up * bobOffset;
            }
        }

        /// <summary>
        /// Get the most urgent low need, or null if all needs are fine.
        /// Priority: Hunger > Thirst > Happiness > Cleanliness.
        /// </summary>
        private NeedType? GetMostUrgentLowNeed()
        {
            if (catNeeds.Hunger < lowThreshold) return NeedType.Hunger;
            if (catNeeds.Thirst < lowThreshold) return NeedType.Thirst;
            if (catNeeds.Happiness < lowThreshold) return NeedType.Happiness;
            if (catNeeds.Cleanliness < lowThreshold) return NeedType.Cleanliness;
            return null;
        }

        /// <summary>
        /// Set the icon sprite based on need type.
        /// </summary>
        private void SetIcon(NeedType need)
        {
            if (iconRenderer == null) return;

            iconRenderer.sprite = need switch
            {
                NeedType.Hunger => hungerIcon,
                NeedType.Thirst => thirstIcon,
                NeedType.Happiness => happinessIcon,
                NeedType.Cleanliness => cleanlinessIcon,
                _ => null
            };
        }

        /// <summary>
        /// Set the alpha and scale of all renderers.
        /// </summary>
        private void SetVisuals(float alpha, float scale)
        {
            if (bubbleRenderer != null)
            {
                Color c = bubbleRenderer.color;
                c.a = alpha;
                bubbleRenderer.color = c;
            }

            if (iconRenderer != null)
            {
                Color c = iconRenderer.color;
                c.a = alpha;
                iconRenderer.color = c;
            }

            transform.localScale = Vector3.one * scale;
        }
    }
}
