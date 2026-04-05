using UnityEngine;
using CatRaising.Core;
using CatRaising.Systems;

namespace CatRaising.MiniGame
{
    /// <summary>
    /// Individual fish that swims across the screen. Tap to catch it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class Fish : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float swimSpeed = 2f;
        [SerializeField] private int pointValue = 1;
        [SerializeField] private bool isGolden = false;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color goldenColor = new Color(1f, 0.85f, 0f);

        [Header("Effects")]
        [SerializeField] private GameObject catchEffectPrefab;

        private Vector3 _direction;
        private bool _caught = false;
        private float _bobTimer;
        private float _bobAmplitude = 0.15f;
        private float _bobSpeed;
        private float _baseY;

        public void Initialize(Vector3 startPos, Vector3 direction, float speed, bool golden)
        {
            transform.position = startPos;
            _direction = direction.normalized;
            swimSpeed = speed;
            isGolden = golden;
            pointValue = golden ? 3 : 1;
            _baseY = startPos.y;
            _bobSpeed = Random.Range(2f, 4f);
            _bobTimer = Random.Range(0f, Mathf.PI * 2f);

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            // Flip sprite based on direction
            if (_direction.x < 0)
                spriteRenderer.flipX = true;

            // Golden tint
            if (golden)
                spriteRenderer.color = goldenColor;

            // Scale variation
            float scale = golden ? 1.3f : Random.Range(0.7f, 1.1f);
            transform.localScale = Vector3.one * scale;
        }

        private void Update()
        {
            if (_caught) return;

            // Swim forward
            transform.position += _direction * swimSpeed * Time.deltaTime;

            // Gentle bobbing
            _bobTimer += Time.deltaTime * _bobSpeed;
            float bobY = _baseY + Mathf.Sin(_bobTimer) * _bobAmplitude;
            transform.position = new Vector3(transform.position.x, bobY, transform.position.z);

            // Destroy if off screen
            if (Mathf.Abs(transform.position.x) > 12f)
                Destroy(gameObject);

            // Check for tap
            if (TouchInput.WasPressedThisFrame && !TouchInput.IsOverUI)
            {
                if (IsTapped())
                {
                    OnCaught();
                }
            }
        }

        private bool IsTapped()
        {
            Collider2D hit = Physics2D.OverlapPoint(TouchInput.WorldPosition);
            return hit != null && hit.gameObject == gameObject;
        }

        private void OnCaught()
        {
            _caught = true;

            // Add score
            if (MiniGameManager.Instance != null)
                MiniGameManager.Instance.AddScore(pointValue);

            // Golden catch achievement
            if (isGolden && AchievementManager.Instance != null)
                AchievementManager.Instance.TryUnlock(AchievementId.GoldenCatch);

            // Catch effect
            if (catchEffectPrefab != null)
                Instantiate(catchEffectPrefab, transform.position, Quaternion.identity);

            // Quick shrink + destroy
            StartCoroutine(CatchAnimation());
        }

        private System.Collections.IEnumerator CatchAnimation()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                transform.position += Vector3.up * 3f * Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
