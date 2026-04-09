using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CatRaising.Systems;
using TMPro;

namespace CatRaising.MiniGame
{
    /// <summary>
    /// UI-based fish that swims across a RectTransform area.
    /// Uses Image + Button for tap detection instead of world-space colliders.
    /// 
    /// SETUP: Create a prefab with Image + Button + FishUI component.
    /// The prefab will be spawned as a child of the mini-game panel.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Button))]
    public class FishUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Settings")]
        [SerializeField] private float swimSpeed = 300f; // pixels per second
        [SerializeField] private int pointValue = 1;
        [SerializeField] private bool isGolden = false;

        [Header("Visual")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color goldenColor = new Color(1f, 0.85f, 0f);

        private Image _image;
        private RectTransform _rectTransform;
        private Vector2 _direction;
        private bool _caught = false;
        private float _bobTimer;
        private float _bobAmplitude = 8f; // pixels
        private float _bobSpeed;
        private float _baseY;
        private RectTransform _parentRect;
        private float pulseSpeed = 2f;
        private Image fishImage;
        private UIShineSweep shineEffect;

        public bool IsCaught => _caught;

        public void Initialize(Vector2 startAnchoredPos, Vector2 direction, float speed, bool golden,
                               Sprite fishSprite, Sprite goldenFishSprite)
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            _parentRect = transform.parent as RectTransform;

            _rectTransform.anchoredPosition = startAnchoredPos;
            _direction = direction.normalized;
            swimSpeed = speed;
            isGolden = golden;
            pointValue = golden ? 3 : 1;
            _baseY = startAnchoredPos.y;
            _bobSpeed = Random.Range(2f, 4f);
            _bobTimer = Random.Range(0f, Mathf.PI * 2f);

            Vector3 currentScale = _rectTransform.localScale;
            // Set sprite
            if (_image != null)
            {
                if (golden && goldenFishSprite != null)
                    _image.sprite = goldenFishSprite;
                else if (fishSprite != null)
                    _image.sprite = fishSprite;

                _image.color = golden ? goldenColor : normalColor;
                _image.SetNativeSize();
            }

            // Flip based on direction
            if (_direction.x < 0)
                _rectTransform.localScale = new Vector3(-currentScale.x, currentScale.y, currentScale.z);

            // Scale variation
            float scale = golden ? 1.3f : Random.Range(0.8f, 1.2f);
            _rectTransform.localScale *= scale;
            
            shineEffect = GetComponentInChildren<UIShineSweep>();
            fishImage = GetComponent<Image>();

            if (!golden)
            {
                shineEffect.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_caught) return;

            // Swim
            Vector2 pos = _rectTransform.anchoredPosition;
            pos += _direction * swimSpeed * Time.deltaTime;

            // Bob
            _bobTimer += Time.deltaTime * _bobSpeed;
            pos.y = _baseY + Mathf.Sin(_bobTimer) * _bobAmplitude;

            _rectTransform.anchoredPosition = pos;

            if (isGolden)
            {
                fishImage.color = Color.Lerp(goldenColor, normalColor, Mathf.PingPong(Time.time * pulseSpeed, 1f));
            }

            // Destroy if off-screen
            if (_parentRect != null)
            {
                float halfWidth = _parentRect.rect.width * 0.5f + 100f;
                if (Mathf.Abs(pos.x) > halfWidth)
                    Destroy(gameObject);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_caught) return;
            OnCaught();
        }

        private void OnCaught()
        {
            _caught = true;

            if (MiniGameManager.Instance != null)
                MiniGameManager.Instance.AddScore(pointValue);

            if (isGolden && AchievementManager.Instance != null)
                AchievementManager.Instance.TryUnlock(AchievementId.GoldenCatch);

            // Spawn floating score text
            SpawnScorePopup();

            // Quick shrink + destroy
            StartCoroutine(CatchAnimation());
        }

        /// <summary>
        /// Creates a floating "+1" or "+3" text at the fish's position that drifts upward and fades out.
        /// </summary>
        private void SpawnScorePopup()
        {
            if (_parentRect == null) return;

            // Create a new GameObject with TextMeshProUGUI
            var popupObj = new GameObject("ScorePopup");
            popupObj.transform.SetParent(_parentRect, false);

            var popupRect = popupObj.AddComponent<RectTransform>();
            popupRect.anchoredPosition = _rectTransform.anchoredPosition;
            popupRect.sizeDelta = new Vector2(120f, 60f);

            var popupText = popupObj.AddComponent<TextMeshProUGUI>();
            popupText.text = isGolden ? "+3" : "+1";
            popupText.fontSize = isGolden ? 48f : 36f;
            popupText.color = isGolden ? new Color(1f, 0.85f, 0f) : Color.black;
            popupText.alignment = TextAlignmentOptions.Center;
            popupText.fontStyle = FontStyles.Bold;
            popupText.enableAutoSizing = false;

            // Ensure it renders on top
            var canvas = popupObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999;

            StartCoroutine(FloatingScoreAnimation(popupRect, popupText));
        }

        private System.Collections.IEnumerator FloatingScoreAnimation(RectTransform rect, TextMeshProUGUI text)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;
            Color startColor = text.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Float upward
                rect.anchoredPosition = startPos + Vector2.up * 100f * t;
                // Fade out in the second half
                float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f);
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                // Scale up slightly at start, then shrink
                float scale = t < 0.2f ? Mathf.Lerp(0.5f, 1.2f, t / 0.2f) : Mathf.Lerp(1.2f, 1f, (t - 0.2f) / 0.8f);
                rect.localScale = Vector3.one * scale;

                yield return null;
            }
            Destroy(rect.gameObject);
        }

        private System.Collections.IEnumerator CatchAnimation()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startScale = _rectTransform.localScale;
            Vector2 startPos = _rectTransform.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _rectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                _rectTransform.anchoredPosition = startPos + Vector2.up * 50f * t;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
