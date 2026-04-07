using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CatRaising.Systems;

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
                _rectTransform.localScale = new Vector3(-1, 1, 1);

            // Scale variation
            float scale = golden ? 1.3f : Random.Range(0.7f, 1.1f);
            _rectTransform.localScale *= scale;
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

            // Quick shrink + destroy
            StartCoroutine(CatchAnimation());
        }

        private System.Collections.IEnumerator CatchAnimation()
        {
            float duration = 0.2f;
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
