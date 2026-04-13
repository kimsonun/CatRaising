using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Core;
using CatRaising.Cat;

namespace CatRaising.UI
{
    /// <summary>
    /// Sequential tutorial system for first-time players.
    /// Guides the player through all major features in order:
    ///   0. Food Bowl & Water Bowl
    ///   1. Need Bars
    ///   2. Bond System
    ///   3. Daily Tasks
    ///   4. Achievements
    ///   5. Shop System
    ///   6. Switch Room System
    ///   7. Furniture System
    ///   8. Mini Game
    ///
    /// Each step shows a hint panel with title, description, and an optional arrow
    /// pointing at the relevant UI element. Steps advance when the player taps "Next".
    /// Progress is persisted via GameData.tutorialStepCompleted.
    ///
    /// SETUP:
    /// 1. Create a "TutorialPanel" under Canvas with: hint text (TMP), next button,
    ///    skip button, arrow image, and a CanvasGroup for fading.
    /// 2. Assign the panel and arrow targets for each step in the Inspector.
    /// 3. Attach this script to the TutorialPanel or a manager object.
    /// </summary>
    public class TutorialHints : MonoBehaviour
    {
        public static TutorialHints Instance { get; private set; }
        [Header("UI References")]
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private TextMeshProUGUI hintTitleText;
        [SerializeField] private TextMeshProUGUI hintBodyText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Image arrowImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject dimOverlay; // Semi-transparent overlay behind panel

        [Header("Settings")]
        [Tooltip("Time before the tutorial starts after game ready")]
        [SerializeField] private float initialDelay = 2f;
        [Tooltip("Fade in/out duration")]
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Arrow Targets (assign the RectTransform of each UI element)")]
        [Tooltip("Step 0: Food/Water bowl area (world-space — can be null if bowls are in-game objects)")]
        [SerializeField] private RectTransform arrowTarget_Bowls;
        [Tooltip("Step 1: Need bars area")]
        [SerializeField] private RectTransform arrowTarget_NeedBars;
        [Tooltip("Step 2: Bond meter area")]
        [SerializeField] private RectTransform arrowTarget_Bond;
        [Tooltip("Step 3: Daily task button")]
        [SerializeField] private RectTransform arrowTarget_DailyTask;
        [Tooltip("Step 4: Achievement button")]
        [SerializeField] private RectTransform arrowTarget_Achievement;
        [Tooltip("Step 5: Shop button")]
        [SerializeField] private RectTransform arrowTarget_Shop;
        [Tooltip("Step 6: Room switch button")]
        [SerializeField] private RectTransform arrowTarget_RoomSwitch;
        [Tooltip("Step 7: Decorate/Furniture button")]
        [SerializeField] private RectTransform arrowTarget_Furniture;
        [Tooltip("Step 8: Mini-game button")]
        [SerializeField] private RectTransform arrowTarget_MiniGame;

        [Header("References")]
        [SerializeField] private CatNeeds catNeeds;

        // ─── Tutorial Step Definitions ──────────────────────────

        private static readonly int TOTAL_STEPS = 9;

        private struct TutorialStep
        {
            public string title;
            public string body;
        }

        private readonly TutorialStep[] _steps = new TutorialStep[]
        {
            // 0: Food & Water Bowl
            new()
            {
                title = "Feed & Hydrate Your Cat!",
                body = "See the bowls on the floor? Tap the <b>food bowl</b> to fill it when it's empty — your cat will walk over and eat!\n\nDo the same with the <b>water bowl</b> to keep your cat hydrated."
            },
            // 1: Need Bars
            new()
            {
                title = "Need Bars",
                body = "These bars show your cat's <b>Hunger</b>, <b>Thirst</b>, <b>Happiness</b>, and <b>Cleanliness</b>.\n\nKeep them high by feeding, watering, petting, and playing with your cat!"
            },
            // 2: Bond System
            new()
            {
                title = "Bond System",
                body = "The <b>Bond Meter</b> grows as you care for your cat. Pet it, feed it, and play together to strengthen your bond!\n\nHigher bond levels unlock new tiers: Friend, Companion, Best Friend, and Soulmate!"
            },
            // 3: Daily Tasks
            new()
            {
                title = "Daily Tasks",
                body = "Every day you get <b>5 tasks</b> to complete — like feeding, petting, and playing.\n\nComplete them to earn <b>PawCoins</b>! Tap this button to see your tasks."
            },
            // 4: Achievements
            new()
            {
                title = "Achievements",
                body = "Unlock <b>achievements</b> by reaching milestones — first pet, 50 feedings, high bond, and more!\n\nClaim them for <b>bonus rewards</b>. Tap to see all achievements."
            },
            // 5: Shop System
            new()
            {
                title = "Shop",
                body = "Spend your <b>PawCoins</b> in the shop to buy <b>furniture</b> and unlock new <b>rooms</b>!\n\nNew items let you decorate and make your cat's home cozy."
            },
            // 6: Switch Room System
            new()
            {
                title = "Room System",
                body = "Your cat lives in different rooms! Tap this button to <b>switch between rooms</b>.\n\nUnlock more rooms from the shop to give your cat more space."
            },
            // 7: Furniture System
            new()
            {
                title = "Decorate!",
                body = "Tap the <b>Decorate</b> button to place furniture you've bought.\n\nDrag to position, tap to lock in place, then confirm. You can <b>flip</b> items and <b>remove</b> them too!"
            },
            // 8: Mini Game
            new()
            {
                title = "Mini Games",
                body = "Play the <b>Cat Fishing</b> mini-game to earn extra <b>PawCoins</b>!\n\nTap fish to catch them — golden fish are worth 3× points. Have fun!"
            },
        };

        // ─── State ──────────────────────────────────────────────

        private int _currentStep = -1;
        private bool _tutorialActive = false;
        private bool _isTransitioning = false;

        // ─── Lifecycle ──────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (hintPanel != null) hintPanel.SetActive(false);
            if (dimOverlay != null) dimOverlay.SetActive(false);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);

            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameReady += OnGameReady;
        }

        private void OnGameReady()
        {
            if (GameManager.Instance?.Data == null) return;

            int savedStep = GameManager.Instance.Data.tutorialStepCompleted;

            // Tutorial already completed
            if (savedStep >= TOTAL_STEPS - 1)
                return;

            // Resume from the next uncompleted step
            _currentStep = savedStep; // Will be incremented in ShowNextStep
            _tutorialActive = true;

            // Small delay before starting
            Invoke(nameof(ShowNextStep), initialDelay);
        }

        // ─── Step Navigation ────────────────────────────────────

        private void OnNextClicked()
        {
            if (!_tutorialActive || _isTransitioning) return;
            Systems.SoundEffectHooks.Instance?.PlayButtonClick();
            ShowNextStep();
        }

        private void OnSkipClicked()
        {
            if (!_tutorialActive || _isTransitioning) return;
            Systems.SoundEffectHooks.Instance?.PlayButtonClick();
            CompleteTutorial();
        }

        private void ShowNextStep()
        {
            _currentStep++;

            if (_currentStep >= TOTAL_STEPS)
            {
                CompleteTutorial();
                return;
            }

            // Save progress
            if (GameManager.Instance?.Data != null)
                GameManager.Instance.Data.tutorialStepCompleted = _currentStep;

            ShowStep(_currentStep);
        }

        /// <summary>
        /// Display a specific tutorial step.
        /// </summary>
        private void ShowStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= _steps.Length) return;

            var step = _steps[stepIndex];

            // Update text
            if (hintTitleText != null)
                hintTitleText.text = step.title;

            if (hintBodyText != null)
                hintBodyText.text = step.body;

            // Update next button text
            var nextBtnText = nextButton?.GetComponentInChildren<TextMeshProUGUI>();
            if (nextBtnText != null)
                nextBtnText.text = stepIndex >= TOTAL_STEPS - 1 ? "Finish!" : "Next";

            // Position arrow toward target
            PositionArrow(stepIndex);

            // Show panel
            if (dimOverlay != null) dimOverlay.SetActive(true);
            if (hintPanel != null) hintPanel.SetActive(true);

            // Fade in
            if (canvasGroup != null)
                StartCoroutine(Fade(0f, 1f, fadeDuration));

            Debug.Log($"[Tutorial] Step {stepIndex + 1}/{TOTAL_STEPS}: {step.title}");
        }

        /// <summary>
        /// Position and rotate the arrow image toward the relevant UI element.
        /// </summary>
        private void PositionArrow(int stepIndex)
        {
            RectTransform target = GetArrowTarget(stepIndex);
            if (arrowImage == null || target == null)
            {
                arrowImage?.gameObject.SetActive(false);
                return;
            }

            arrowImage.gameObject.SetActive(true);
            RectTransform arrowRect = arrowImage.GetComponent<RectTransform>();
            RectTransform panelRect = hintPanel.GetComponent<RectTransform>();

            // 1. Get the World Positions of the two points
            // panelRect.position is the world center of your hint panel
            Vector3 startWorldPos = panelRect.position;
            Vector3 targetWorldPos = target.position;

            // 2. Find the point that is 90% of the way to the target in World Space
            Vector3 finalWorldPos = Vector3.Lerp(startWorldPos, targetWorldPos, 0.9f);

            // 3. Convert that World Position into the Arrow Parent's Local Space
            RectTransform arrowParent = arrowRect.parent as RectTransform;
            Vector2 localPoint = arrowParent.InverseTransformPoint(finalWorldPos);

            // 4. Set the position
            arrowRect.anchoredPosition = localPoint;

            // 5. Rotation Logic
            // We use World Space direction for accuracy
            Vector3 dir = targetWorldPos - startWorldPos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Use localRotation to avoid inheritance issues
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        private RectTransform GetArrowTarget(int stepIndex)
        {
            return stepIndex switch
            {
                0 => arrowTarget_Bowls,
                1 => arrowTarget_NeedBars,
                2 => arrowTarget_Bond,
                3 => arrowTarget_DailyTask,
                4 => arrowTarget_Achievement,
                5 => arrowTarget_Shop,
                6 => arrowTarget_RoomSwitch,
                7 => arrowTarget_Furniture,
                8 => arrowTarget_MiniGame,
                _ => null
            };
        }

        // ─── Completion ─────────────────────────────────────────

        private void CompleteTutorial()
        {
            _tutorialActive = false;

            // Mark all steps complete
            if (GameManager.Instance?.Data != null)
                GameManager.Instance.Data.tutorialStepCompleted = TOTAL_STEPS;

            // Fade out and hide
            if (canvasGroup != null)
            {
                StartCoroutine(Fade(1f, 0f, fadeDuration, () =>
                {
                    if (hintPanel != null) hintPanel.SetActive(false);
                    if (dimOverlay != null) dimOverlay.SetActive(false);
                }));
            }
            else
            {
                if (hintPanel != null) hintPanel.SetActive(false);
                if (dimOverlay != null) dimOverlay.SetActive(false);
            }

            Debug.Log("[Tutorial] Tutorial completed!");
        }

        // ─── External Hooks ─────────────────────────────────────

        /// <summary>
        /// Call from other scripts when the player performs a tutorial-relevant action.
        /// If the tutorial is currently showing a step related to the action, it
        /// auto-advances to the next step.
        /// </summary>
        public void OnActionPerformed(string action)
        {
            if (!_tutorialActive) return;

            bool shouldAdvance = false;

            switch (action)
            {
                case "feed":
                case "water":
                    shouldAdvance = _currentStep == 0;
                    break;
                case "pet":
                    shouldAdvance = _currentStep == 2; // Bond step
                    break;
                case "daily_task":
                    shouldAdvance = _currentStep == 3;
                    break;
                case "achievement":
                    shouldAdvance = _currentStep == 4;
                    break;
                case "shop":
                    shouldAdvance = _currentStep == 5;
                    break;
                case "room_switch":
                    shouldAdvance = _currentStep == 6;
                    break;
                case "furniture":
                    shouldAdvance = _currentStep == 7;
                    break;
                case "mini_game":
                    shouldAdvance = _currentStep == 8;
                    break;
            }

            if (shouldAdvance)
                ShowNextStep();
        }

        /// <summary>
        /// Check if the tutorial is currently active (for other systems to check).
        /// </summary>
        public bool IsTutorialActive => _tutorialActive;

        /// <summary>
        /// Get the current tutorial step index (0-based). -1 if not started.
        /// </summary>
        public int CurrentStep => _currentStep;

        // ─── Utilities ──────────────────────────────────────────

        /// <summary>
        /// Fade the canvas group alpha.
        /// </summary>
        private System.Collections.IEnumerator Fade(float from, float to, float duration,
            System.Action onComplete = null)
        {
            _isTransitioning = true;

            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                _isTransitioning = false;
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
            _isTransitioning = false;
            onComplete?.Invoke();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameReady -= OnGameReady;
        }
    }
}
