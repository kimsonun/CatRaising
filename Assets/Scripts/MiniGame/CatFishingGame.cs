using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CatRaising.MiniGame
{
    /// <summary>
    /// Cat Fishing mini-game controller. 30-second rounds, tap fish to catch.
    /// 
    /// SETUP:
    /// 1. Create a "MiniGamePanel" under Canvas (full-screen overlay)
    /// 2. Add score text, timer text, Start button, Close button
    /// 3. Create a "GameOverPanel" child with score, coins earned, and play again button
    /// 4. Create a Fish prefab with SpriteRenderer + CircleCollider2D + Fish.cs
    /// 5. Add FishSpawner as child and assign fish prefab
    /// 6. Add MiniGameManager component (on same object or GameManager)
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

        [Header("UI - Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI coinsEarnedText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button exitButton;

        [Header("References")]
        [SerializeField] private FishSpawner fishSpawner;
        [SerializeField] private MiniGameManager miniGameManager;

        [Header("Cat Paw (Visual feedback)")]
        [SerializeField] private GameObject catPaw; // Optional: shows where player tapped

        private void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnStartClicked);
            if (exitButton != null) exitButton.onClick.AddListener(Close);

            if (miniGameManager == null) miniGameManager = MiniGameManager.Instance;

            if (miniGameManager != null)
            {
                miniGameManager.OnScoreChanged += UpdateScore;
                miniGameManager.OnGameStarted += OnGameStarted;
                miniGameManager.OnGameEnded += OnGameEnded;
            }

            if (gamePanel != null) gamePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
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

            if (scoreText != null) scoreText.text = "0";
            if (timerText != null) timerText.text = $"{miniGameManager?.GameDuration ?? 30:F0}s";
            if (instructionText != null) instructionText.text = "Tap fish to catch them!\nGolden fish = 3× points!";
            if (startButton != null) startButton.gameObject.SetActive(true);
        }

        public void Close()
        {
            // Stop game if playing
            if (miniGameManager != null && miniGameManager.IsPlaying)
                miniGameManager.EndGame();

            if (fishSpawner != null) fishSpawner.StopSpawning();
            if (gamePanel != null) gamePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void OnStartClicked()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (startButton != null) startButton.gameObject.SetActive(false);
            if (instructionText != null) instructionText.gameObject.SetActive(false);

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
            if (coinsEarnedText != null) coinsEarnedText.text = $"+{coins}";

            Debug.Log($"[CatFishing] Game over! Score: {score}, Earned: {coins}");
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString();
        }

        private void UpdateTimer()
        {
            if (miniGameManager == null || timerText == null) return;
            float remaining = miniGameManager.TimeRemaining;
            timerText.text = $"{remaining:F1}s";

            // Flash red when low
            if (remaining < 5f)
                timerText.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 3f, 1f));
            else
                timerText.color = Color.white;
        }

        private void UpdateCatPaw()
        {
            if (catPaw == null) return;

            if (CatRaising.Core.TouchInput.WasPressedThisFrame)
            {
                catPaw.SetActive(true);
                catPaw.transform.position = (Vector3)CatRaising.Core.TouchInput.WorldPosition;

                // Brief show then hide
                CancelInvoke(nameof(HidePaw));
                Invoke(nameof(HidePaw), 0.3f);
            }
        }

        private void HidePaw()
        {
            if (catPaw != null) catPaw.SetActive(false);
        }
    }
}
