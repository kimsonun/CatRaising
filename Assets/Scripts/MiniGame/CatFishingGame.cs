using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Core;
using CatRaising.Cat;

namespace CatRaising.MiniGame
{
    /// <summary>
    /// Cat Fishing mini-game as a UI overlay. While active, all cat interactions
    /// are disabled. Fish are UI elements spawned inside a RectTransform area.
    /// 
    /// SETUP:
    /// 1. Create "MiniGamePanel" under Canvas — full-screen overlay
    /// 2. Add a "FishArea" child (stretch to fill) — this is where fish swim
    /// 3. Create FishUI prefab (Image + Button + FishUI script)
    /// 4. Add FishSpawner component and assign fishArea + fishUIPrefab
    /// 5. Wire score/timer/buttons
    /// </summary>
    public class CatFishingGame : MonoBehaviour
    {
        [Header("UI - Game")]
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private GameObject instructionPanel;

        [Header("UI - Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI coinsEarnedText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button exitButton;

        [Header("References")]
        [SerializeField] private FishSpawner fishSpawner;
        [SerializeField] private MiniGameManager miniGameManager;

        [Header("Cat Paw (UI feedback)")]
        [Tooltip("Optional: Image of a cat paw at tap location")]
        [SerializeField] private RectTransform catPawUI;

        // References for disabling interaction
        private CatInteraction _catInteraction;
        private CatAI _catAI;

        private void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); OnStartClicked(); });
            if (closeButton != null) closeButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); Close(); });
            if (playAgainButton != null) playAgainButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); OnStartClicked(); });
            if (exitButton != null) exitButton.onClick.AddListener(() => { Systems.SoundEffectHooks.Instance?.PlayButtonClick(); Close(); });

            if (miniGameManager == null) miniGameManager = MiniGameManager.Instance;

            if (miniGameManager != null)
            {
                miniGameManager.OnScoreChanged += UpdateScore;
                miniGameManager.OnGameStarted += OnGameStarted;
                miniGameManager.OnGameEnded += OnGameEnded;
            }

            if (gamePanel != null) gamePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Find cat components for disabling
            _catInteraction = FindAnyObjectByType<CatInteraction>();
            _catAI = FindAnyObjectByType<CatAI>();
        }

        private void OnDestroy()
        {
            if (miniGameManager != null)
            {
                miniGameManager.OnScoreChanged -= UpdateScore;
                miniGameManager.OnGameStarted -= OnGameStarted;
                miniGameManager.OnGameEnded -= OnGameEnded;
            }
        }

        private void Update()
        {
            if (miniGameManager != null && miniGameManager.IsPlaying)
            {
                UpdateTimer();
                UpdateCatPaw();
            }
        }

        public void Open()
        {
            if (gamePanel != null) gamePanel.SetActive(true);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Reset all UI to pre-game state
            if (scoreText != null) scoreText.text = "Score: 0";
            if (timerText != null)
            {
                timerText.text = $"{miniGameManager?.GameDuration ?? 30:F0}s";
                timerText.color = Color.white;
            }
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text = "Tap fish to catch them!\nGolden fish = 3× points!";
            }
            if (instructionPanel != null) instructionPanel.SetActive(true);
            if (startButton != null) startButton.gameObject.SetActive(true);

            // Switch to mini-game BGM
            if (Systems.SoundEffectHooks.Instance != null)
                Systems.SoundEffectHooks.Instance.StartMiniGameBGM();

            // Disable cat interactions while mini-game is open
            SetCatInteractionsEnabled(false);
        }

        public void Close()
        {
            if (miniGameManager != null && miniGameManager.IsPlaying)
                miniGameManager.EndGame();

            if (fishSpawner != null) fishSpawner.StopSpawning();
            if (gamePanel != null) gamePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Resume normal BGM
            if (Systems.SoundEffectHooks.Instance != null)
                Systems.SoundEffectHooks.Instance.StopMiniGameBGM();

            // Re-enable cat interactions
            SetCatInteractionsEnabled(true);
        }

        private void SetCatInteractionsEnabled(bool enabled)
        {
            if (_catInteraction != null) _catInteraction.enabled = enabled;
            if (_catAI != null) _catAI.SetAIEnabled(enabled);

            Debug.Log($"[CatFishing] Cat interactions {(enabled ? "enabled" : "disabled")}");
        }

        private void OnStartClicked()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (instructionText != null) instructionText.gameObject.SetActive(false);
            if (instructionPanel != null) instructionPanel.SetActive(false);

            if (miniGameManager != null) miniGameManager.StartGame();
            if (fishSpawner != null) fishSpawner.StartSpawning();
        }

        private void OnGameStarted()
        {
            Debug.Log("[CatFishing] 🐟 Game started!");
        }

        private void OnGameEnded(int score, int coins)
        {
            if (fishSpawner != null) fishSpawner.StopSpawning();

            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (finalScoreText != null) finalScoreText.text = $"Score: {score}";
            if (coinsEarnedText != null) coinsEarnedText.text = $"+{coins} ";

            Debug.Log($"[CatFishing] Game over! Score: {score}, Earned: {coins} ");
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = "Score: " + score.ToString();
        }

        private void UpdateTimer()
        {
            if (miniGameManager == null || timerText == null) return;
            float remaining = miniGameManager.TimeRemaining;
            timerText.text = $"{remaining:F1}s";

            if (remaining < 10f)
                timerText.color = Color.Lerp(Color.red, Color.black, Mathf.PingPong(Time.time * 3f, 1f));
            else
                timerText.color = Color.black;
        }

        private void UpdateCatPaw()
        {
            if (catPawUI == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                catPawUI.gameObject.SetActive(true);
                catPawUI.position = Input.mousePosition;

                CancelInvoke(nameof(HidePaw));
                Invoke(nameof(HidePaw), 0.3f);
            }
        }

        private void HidePaw()
        {
            if (catPawUI != null) catPawUI.gameObject.SetActive(false);
        }
    }
}
