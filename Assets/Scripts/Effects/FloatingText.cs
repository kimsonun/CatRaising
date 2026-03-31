using UnityEngine;
using TMPro;

namespace CatRaising.Effects
{
    /// <summary>
    /// A floating text/emoji effect that rises and fades out.
    /// Used for hearts, "+1 happiness", emoji reactions, etc.
    /// Attach to a prefab and instantiate at the desired position.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Animation")]
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float floatDistance = 2f;
        [SerializeField] private float lifetime = 1.5f;
        [SerializeField] private float fadeStartPercent = 0.4f; // Start fading at 40% of lifetime
        [SerializeField] private float horizontalDrift = 0.3f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Settings")]
        [SerializeField] private float startScale = 0.5f;
        [SerializeField] private float maxScale = 1.2f;

        private float _elapsed = 0f;
        private Vector3 _startPosition;
        private float _randomDrift;
        private Color _startColor;

        private void Start()
        {
            _startPosition = transform.position;
            _randomDrift = Random.Range(-horizontalDrift, horizontalDrift);

            if (textMesh != null)
                _startColor = textMesh.color;
            else if (spriteRenderer != null)
                _startColor = spriteRenderer.color;

            // Auto-destroy after lifetime
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / lifetime;

            // Float upward with slight horizontal drift
            float yOffset = floatSpeed * _elapsed;
            float xOffset = Mathf.Sin(_elapsed * 3f) * _randomDrift;
            transform.position = _startPosition + new Vector3(xOffset, yOffset, 0f);

            // Scale animation (pop in, then settle)
            float scaleT = scaleCurve.Evaluate(Mathf.Clamp01(t * 3f)); // Quick pop
            float scale = Mathf.Lerp(startScale, maxScale, scaleT);

            // Shrink slightly at the end
            if (t > 0.7f)
                scale *= Mathf.Lerp(1f, 0.5f, (t - 0.7f) / 0.3f);

            transform.localScale = Vector3.one * scale;

            // Fade out
            if (t > fadeStartPercent)
            {
                float fadeT = (t - fadeStartPercent) / (1f - fadeStartPercent);
                float alpha = Mathf.Lerp(1f, 0f, fadeT);

                if (textMesh != null)
                {
                    Color c = _startColor;
                    c.a = alpha;
                    textMesh.color = c;
                }

                if (spriteRenderer != null)
                {
                    Color c = _startColor;
                    c.a = alpha;
                    spriteRenderer.color = c;
                }
            }
        }

        /// <summary>
        /// Set the text content (for TextMeshPro version).
        /// </summary>
        public void SetText(string text)
        {
            if (textMesh != null)
                textMesh.text = text;
        }

        /// <summary>
        /// Set the text color.
        /// </summary>
        public void SetColor(Color color)
        {
            _startColor = color;
            if (textMesh != null) textMesh.color = color;
            if (spriteRenderer != null) spriteRenderer.color = color;
        }

        /// <summary>
        /// Factory method: create a floating text at a world position.
        /// </summary>
        public static FloatingText Create(GameObject prefab, Vector3 position, string text, Color color)
        {
            if (prefab == null) return null;

            GameObject go = Instantiate(prefab, position, Quaternion.identity);
            FloatingText ft = go.GetComponent<FloatingText>();

            if (ft != null)
            {
                ft.SetText(text);
                ft.SetColor(color);
            }

            return ft;
        }
    }
}
