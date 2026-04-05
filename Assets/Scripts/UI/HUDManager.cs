using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Cat;
using CatRaising.Systems;
using CatRaising.Core;

namespace CatRaising.UI
{
    /// <summary>
    /// Main HUD controller. Displays need bars, bond meter, cat name,
    /// and time-of-day indicator.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CatNeeds catNeeds;
        [SerializeField] private BondSystem bondSystem;

        [Header("Need Bars")]
        [SerializeField] private NeedBarUI hungerBar;
        [SerializeField] private NeedBarUI thirstBar;
        [SerializeField] private NeedBarUI happinessBar;
        [SerializeField] private NeedBarUI cleanlinessBar;

        [Header("Bond Display")]
        [SerializeField] private Image bondFillImage;
        [SerializeField] private TextMeshProUGUI bondLevelText;
        [SerializeField] private TextMeshProUGUI bondTierText;

        [Header("Cat Info")]
        [SerializeField] private TextMeshProUGUI catNameText;
        [SerializeField] private TextMeshProUGUI catStateText;

        [Header("Time Display")]
        [SerializeField] private TextMeshProUGUI timeText;

        [Header("Welcome Back Panel")]
        [SerializeField] private GameObject welcomeBackPanel;
        [SerializeField] private TextMeshProUGUI welcomeBackText;
        [SerializeField] private Button welcomeBackButton;
        [SerializeField] private float welcomeBackDuration = 5f;

        [Header("Milestone Popup")]
        [SerializeField] private GameObject milestonePopup;
        [SerializeField] private TextMeshProUGUI milestoneText;
        [SerializeField] private Button milestoneCloseButton;

        private void Start()
        {
            // Subscribe to events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReady += OnGameReady;
                GameManager.Instance.OnCatNamed += OnCatNamed;
            }

            if (bondSystem != null)
            {
                bondSystem.OnMilestoneReached += OnMilestoneReached;
            }

            // Hide popups initially
            if (welcomeBackPanel != null) welcomeBackPanel.SetActive(false);
            if (milestonePopup != null) milestonePopup.SetActive(false);

            // Setup welcome back button
            if (welcomeBackButton != null)
                welcomeBackButton.onClick.AddListener(HideWelcomeBack);

            // Setup milestone close button
            if (milestoneCloseButton != null)
                milestoneCloseButton.onClick.AddListener(HideMilestonePopup);
        }

        private void Update()
        {
            UpdateNeedBars();
            UpdateBondDisplay();
            UpdateTimeDisplay();
            UpdateCatState();
        }

        /// <summary>
        /// Update need bars from the CatNeeds component.
        /// </summary>
        private void UpdateNeedBars()
        {
            if (catNeeds == null) return;

            if (hungerBar != null) hungerBar.SetValue100(catNeeds.Hunger);
            if (thirstBar != null) thirstBar.SetValue100(catNeeds.Thirst);
            if (happinessBar != null) happinessBar.SetValue100(catNeeds.Happiness);
            if (cleanlinessBar != null) cleanlinessBar.SetValue100(catNeeds.Cleanliness);
        }

        /// <summary>
        /// Update bond meter display.
        /// </summary>
        private void UpdateBondDisplay()
        {
            if (bondSystem == null) return;

            if (bondFillImage != null)
                bondFillImage.fillAmount = bondSystem.NormalizedBond;

            if (bondLevelText != null)
                bondLevelText.text = $"{Mathf.RoundToInt(bondSystem.BondLevel)}";

            if (bondTierText != null)
                bondTierText.text = bondSystem.BondTierName;
        }

        /// <summary>
        /// Update time-of-day display.
        /// </summary>
        private void UpdateTimeDisplay()
        {
            if (TimeManager.Instance == null) return;

            if (timeText != null)
            {
                string phase = TimeManager.Instance.CurrentPhase switch
                {
                    TimeManager.DayPhase.Morning => "",
                    TimeManager.DayPhase.Afternoon => "",
                    TimeManager.DayPhase.Evening => "",
                    TimeManager.DayPhase.Night => "",
                    _ => ""
                };
                timeText.text = $"{System.DateTime.Now:HH:mm} {phase}";
            }
        }

        /// <summary>
        /// Update the cat's current state display (for debugging / player info).
        /// </summary>
        private void UpdateCatState()
        {
            if (catStateText == null) return;

            // Find cat controller if needed
            CatController catController = FindAnyObjectByType<CatController>();
            if (catController == null) return;

            string stateEmoji = catController.CurrentState switch
            {
                CatController.CatState.Idle => "Sitting",
                CatController.CatState.Walking => "Walking",
                CatController.CatState.Sleeping => "Sleeping...",
                CatController.CatState.BeingPet => "Being Pet ^^!",
                CatController.CatState.Eating => "Eating",
                CatController.CatState.Drinking => "Drinking",
                CatController.CatState.Playing => "Playing",
                CatController.CatState.Grooming => "Grooming",
                _ => "Goofing around..."
            };

            catStateText.text = stateEmoji;
        }

        /// <summary>
        /// Called when the game is ready (data loaded, cat named).
        /// </summary>
        private void OnGameReady()
        {
            if (catNameText != null && GameManager.Instance != null)
                catNameText.text = GameManager.Instance.CatName;

            // Show welcome back message if returning player
            if (GameManager.Instance != null && !GameManager.Instance.IsFirstLaunch)
            {
                ShowWelcomeBack();
            }
        }

        /// <summary>
        /// Called when the cat is named for the first time.
        /// </summary>
        private void OnCatNamed(string name)
        {
            if (catNameText != null)
                catNameText.text = name;
        }

        /// <summary>
        /// Show the "Welcome back!" panel.
        /// </summary>
        private void ShowWelcomeBack()
        {
            if (welcomeBackPanel == null) return;

            string catName = GameManager.Instance != null ? GameManager.Instance.CatName : "Kitty";
            if (welcomeBackText != null)
                welcomeBackText.text = $"Welcome back!\n{catName} missed you! 💕";

            welcomeBackPanel.SetActive(true);
            Invoke(nameof(HideWelcomeBack), welcomeBackDuration);
        }

        private void HideWelcomeBack()
        {
            if (welcomeBackPanel != null)
                welcomeBackPanel.SetActive(false);
        }

        /// <summary>
        /// Called when a bond milestone is reached.
        /// </summary>
        private void OnMilestoneReached(int index, string tierName)
        {
            Debug.Log("called");
            if (milestonePopup == null) return;

            if (milestoneText != null)
                milestoneText.text = $"You and {catNameText.text} are now\n{tierName}!";

            milestonePopup.SetActive(true);
        }

        private void HideMilestonePopup()
        {
            if (milestonePopup != null)
                milestonePopup.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReady -= OnGameReady;
                GameManager.Instance.OnCatNamed -= OnCatNamed;
            }

            if (bondSystem != null)
                bondSystem.OnMilestoneReached -= OnMilestoneReached;
        }
    }
}
