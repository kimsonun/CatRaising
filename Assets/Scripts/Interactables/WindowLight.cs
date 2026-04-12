using UnityEngine;
using CatRaising.Core;

namespace CatRaising.Interactables
{
    /// <summary>
    /// Emits a soft isometric light cone from a window furniture instance.
    /// Creates a child sprite that projects a trapezoidal light patch onto the floor,
    /// angled to match the isometric perspective.
    ///
    /// SETUP:
    /// 1. Create a "WindowLightSprite" — a soft gradient trapezoid texture
    ///    (wider at the bottom, narrower at top, fading to transparent at edges).
    ///    Recommended: 128x256 pixel sprite at 100 PPU.
    /// 2. Add this component to the window furniture prefab (or attach at runtime).
    /// 3. Assign the light sprite in the Inspector.
    /// 4. Adjust lightOffset, lightScale, and lightColor to taste.
    ///
    /// Alternatively, this component can auto-generate a simple gradient quad
    /// if no sprite is assigned — see CreateFallbackLightSprite().
    /// </summary>
    public class WindowLight : MonoBehaviour
    {
        [Header("Light Appearance")]
        [Tooltip("Sprite for the light cone. If null, a fallback gradient is generated.")]
        [SerializeField] private Sprite lightSprite;
        [Tooltip("Color and opacity of the light")]
        [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.8f, 0.25f);
        [Tooltip("Scale of the light sprite (x = width, y = height)")]
        [SerializeField] private Vector2 lightScale = new Vector2(0.4f, 0.7f);
        [Tooltip("Offset from window position (down and toward camera for isometric)")]
        [SerializeField] private Vector2 lightOffset = new Vector2(-0.15f, -0.5f);
        [SerializeField] private Vector2 lightOffsetFlipped = new Vector2(0.15f, -0.5f);
        [SerializeField] private Quaternion lightRotation = Quaternion.Euler(0f, 0f, -30f);
        [SerializeField] private Quaternion lightRotationFlipped = Quaternion.Euler(0f, 0f, 30f);

        [Header("Animation")]
        [Tooltip("Enable subtle pulsing animation")]
        [SerializeField] private bool enablePulse = true;
        [Tooltip("Pulse speed")]
        [SerializeField] private float pulseSpeed = 0.8f;
        [Tooltip("Pulse intensity range (min-max alpha multiplier)")]
        [SerializeField] private Vector2 pulseRange = new Vector2(0.85f, 1.0f);

        [Header("Sorting")]
        [Tooltip("Sorting order offset relative to the window (should be behind furniture)")]
        [SerializeField] private int sortingOrderOffset = -50;

        public FurnitureInstance furnitureInstance;
        private SpriteRenderer _lightRenderer;
        private float _baseAlpha;

        private void Start()
        {
            furnitureInstance = gameObject.GetComponent<FurnitureInstance>();
            CreateLightObject();
        }

        private void Update()
        {
            if (!enablePulse || _lightRenderer == null) return;

            // Subtle breathing pulse for warm, living light
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
            float alpha = Mathf.Lerp(pulseRange.x, pulseRange.y, t) * _baseAlpha;

            Color c = _lightRenderer.color;
            c.a = alpha;
            _lightRenderer.color = c;
        }

        private void CreateLightObject()
        {
            // Create child object for the light
            var lightObj = new GameObject("WindowLightCone");
            lightObj.transform.SetParent(transform);

            if (furnitureInstance != null && furnitureInstance.IsFlipped)
            {
                lightOffset = lightOffsetFlipped;
                lightRotation = lightRotationFlipped;
            }
            lightObj.transform.localPosition = new Vector3(lightOffset.x, lightOffset.y, 0f);
            lightObj.transform.localScale = new Vector3(lightScale.x, lightScale.y, 1f);
            lightObj.transform.localRotation = lightRotation;

            _lightRenderer = lightObj.AddComponent<SpriteRenderer>();

            if (lightSprite != null)
            {
                _lightRenderer.sprite = lightSprite;
            }
            else
            {
                _lightRenderer.sprite = CreateFallbackLightSprite();
            }

            _lightRenderer.color = lightColor;
            _baseAlpha = lightColor.a;

            // Sort behind furniture but in front of floor
            var parentRenderer = GetComponentInChildren<SpriteRenderer>();
            if (parentRenderer != null)
            {
                _lightRenderer.sortingLayerID = parentRenderer.sortingLayerID;
                _lightRenderer.sortingOrder = parentRenderer.sortingOrder + sortingOrderOffset;
            }
            else
            {
                _lightRenderer.sortingOrder = -50;
            }
        }

        /// <summary>
        /// Generate a simple radial gradient sprite as fallback if no light sprite is assigned.
        /// Creates a soft elliptical glow.
        /// </summary>
        private Sprite CreateFallbackLightSprite()
        {
            int width = 128;
            int height = 256;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(width * 0.5f, height * 0.7f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / (width * 0.5f);
                    float dy = (y - center.y) / (height * 0.5f);

                    // Elliptical distance (wider horizontally at bottom)
                    float widthFactor = 1f + (1f - (float)y / height) * 0.5f;
                    float dist = Mathf.Sqrt((dx * dx) / (widthFactor * widthFactor) + dy * dy);

                    // Smooth falloff
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha; // Quadratic falloff for softer edges

                    // Fade more at top (light source) for a cone shape
                    float topFade = Mathf.Clamp01((float)y / (height * 0.3f));
                    alpha *= topFade;

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height),
                                 new Vector2(0.5f, 0.7f), 100f);
        }

        /// <summary>
        /// Flip the light direction (used when window is flipped horizontally).
        /// </summary>
        public void SetFlipped(bool flipped)
        {
            if (_lightRenderer != null)
            {
                Vector3 pos = _lightRenderer.transform.localPosition;
                pos.x = flipped ? -Mathf.Abs(lightOffset.x) : Mathf.Abs(lightOffset.x);
                _lightRenderer.transform.localPosition = pos;
            }
        }
    }
}
