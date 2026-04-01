using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Core;
using CatRaising.Cat;

namespace CatRaising.UI
{
    /// <summary>
    /// Tutorial hint system for first-time players.
    /// Shows contextual hints based on game state:
    ///   - "Tap on your cat to pet it!"
    ///   - "Tap the food bowl to fill it!"
    ///   - "Drag the feather toy to play!"
    ///   - etc.
    /// 
    /// Hints auto-dismiss after a duration or when the action is performed.
    /// Each hint is shown only once per session.
    /// </summary>
    public class TutorialHints : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Image arrowImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [Tooltip("Time before the first hint appears")]
        [SerializeField] private float initialDelay = 3f;
        [Tooltip("How long each hint stays visible")]
        [SerializeField] private float hintDuration = 8f;
        [Tooltip("Delay between consecutive hints")]
        [SerializeField] private float hintCooldown = 15f;
        [Tooltip("Fade in/out duration")]
        [SerializeField] private float fadeDuration = 0.4f;

        [Header("References")]
        [SerializeField] private CatNeeds catNeeds;

        // Hint definitions
        private enum HintType
        {
            PetTheCat,
            FeedTheCat,
            WaterTheCat,
            PlayWithToy,
            CheckNeeds,
            COUNT // sentinel
        }

        private bool[] _hintsShown;
        private bool _tutorialComplete = false;
        private bool _isShowingHint = false;
        private float _timer = 0f;
        private float _cooldownTimer = 0f;
        private int _hintsCompleted = 0;

        private void Awake()
        {
            _hintsShown = new bool[(int)HintType.COUNT];

            if (hintPanel != null) hintPanel.SetActive(false);

            if (dismissButton != null)
                dismissButton.onClick.AddListener(DismissCurrentHint);
        }

        private void Start()
        {
            // Only show tutorials on first launch or if tutorial not completed
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReady += OnGameReady;
            }
        }

        private void OnGameReady()
        {
            // Check if tutorial was already completed
            if (GameManager.Instance != null && GameManager.Instance.Data != null)
            {
                if (GameManager.Instance.Data.totalPets > 5 &&
                    GameManager.Instance.Data.totalFeedings > 2)
                {
                    // Player has enough experience, skip tutorial
                    _tutorialComplete = true;
                    return;
                }
            }

            _timer = initialDelay;
        }

        private void Update()
        {
            if (_tutorialComplete) return;
            if (_isShowingHint) return;

            // Cooldown between hints
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                TryShowNextHint();
                _timer = hintCooldown; // Reset timer for next check
            }
        }

        /// <summary>
        /// Determine which hint to show next based on game state.
        /// </summary>
        private void TryShowNextHint()
        {
            // Priority order: pet → feed → water → play → needs overview
            if (!_hintsShown[(int)HintType.PetTheCat])
            {
                ShowHint(HintType.PetTheCat,
                    "🐾 Tap and hold your cat to pet it!",
                    "Your cat loves chin scratches. Try holding your tap on the cat!");
            }
            else if (!_hintsShown[(int)HintType.FeedTheCat] && catNeeds != null && catNeeds.Hunger < 70f)
            {
                ShowHint(HintType.FeedTheCat,
                    "🍖 Your cat is getting hungry!",
                    "Tap the food bowl to fill it. Your cat will walk over and eat.");
            }
            else if (!_hintsShown[(int)HintType.WaterTheCat] && catNeeds != null && catNeeds.Thirst < 70f)
            {
                ShowHint(HintType.WaterTheCat,
                    "💧 Time for some water!",
                    "Tap the water bowl to fill it up.");
            }
            else if (!_hintsShown[(int)HintType.PlayWithToy])
            {
                ShowHint(HintType.PlayWithToy,
                    "🪶 Play time!",
                    "Drag the feather toy around. Your cat will chase it!");
            }
            else if (!_hintsShown[(int)HintType.CheckNeeds])
            {
                ShowHint(HintType.CheckNeeds,
                    "📊 Keep an eye on the need bars!",
                    "The bars in the corner show your cat's hunger, thirst, happiness, and cleanliness.");
            }
            else
            {
                // All hints shown
                _tutorialComplete = true;
            }
        }

        /// <summary>
        /// Show a tutorial hint.
        /// </summary>
        private void ShowHint(HintType type, string title, string body)
        {
            _hintsShown[(int)type] = true;
            _isShowingHint = true;

            if (hintText != null)
                hintText.text = $"<b>{title}</b>\n{body}";

            if (hintPanel != null)
                hintPanel.SetActive(true);

            // Fade in
            if (canvasGroup != null)
                StartCoroutine(FadeHint(0f, 1f, fadeDuration));

            // Auto-dismiss after duration
            CancelInvoke(nameof(DismissCurrentHint));
            Invoke(nameof(DismissCurrentHint), hintDuration);

            _hintsCompleted++;
            Debug.Log($"[TutorialHints] Showing hint: {type} ({_hintsCompleted}/{(int)HintType.COUNT})");
        }

        /// <summary>
        /// Dismiss the current hint.
        /// </summary>
        public void DismissCurrentHint()
        {
            CancelInvoke(nameof(DismissCurrentHint));

            if (canvasGroup != null)
            {
                StartCoroutine(FadeHint(1f, 0f, fadeDuration, onComplete: () =>
                {
                    if (hintPanel != null) hintPanel.SetActive(false);
                    _isShowingHint = false;
                    _cooldownTimer = hintCooldown;
                }));
            }
            else
            {
                if (hintPanel != null) hintPanel.SetActive(false);
                _isShowingHint = false;
                _cooldownTimer = hintCooldown;
            }
        }

        /// <summary>
        /// Call this from other scripts when the player performs a tutorial action,
        /// to immediately dismiss the relevant hint.
        /// </summary>
        public void OnActionPerformed(string action)
        {
            switch (action)
            {
                case "pet":
                    _hintsShown[(int)HintType.PetTheCat] = true;
                    break;
                case "feed":
                    _hintsShown[(int)HintType.FeedTheCat] = true;
                    break;
                case "water":
                    _hintsShown[(int)HintType.WaterTheCat] = true;
                    break;
                case "play":
                    _hintsShown[(int)HintType.PlayWithToy] = true;
                    break;
            }

            if (_isShowingHint)
                DismissCurrentHint();
        }

        /// <summary>
        /// Fade the canvas group alpha.
        /// </summary>
        private System.Collections.IEnumerator FadeHint(float from, float to, float duration, System.Action onComplete = null)
        {
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            canvasGroup.alpha = from;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
            onComplete?.Invoke();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameReady -= OnGameReady;
        }
    }
}
