using UnityEngine;
using CatRaising.Systems;
using CatRaising.Core;

namespace CatRaising.MiniGame
{
    /// <summary>
    /// Base mini-game framework. Handles score, timer, and coin conversion.
    /// </summary>
    public class MiniGameManager : MonoBehaviour
    {
        public static MiniGameManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float gameDuration = 30f;
        [SerializeField] private int coinMultiplier = 2; // coins = score × multiplier

        // State
        private int _score = 0;
        private float _timer = 0f;
        private bool _isPlaying = false;

        public int Score => _score;
        public float TimeRemaining => Mathf.Max(0f, gameDuration - _timer);
        public bool IsPlaying => _isPlaying;
        public float GameDuration => gameDuration;

        public event System.Action<int> OnScoreChanged;
        public event System.Action OnGameStarted;
        public event System.Action<int, int> OnGameEnded; // score, coins earned

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!_isPlaying) return;

            _timer += Time.deltaTime;
            if (_timer >= gameDuration)
            {
                EndGame();
            }
        }

        public void StartGame()
        {
            _score = 0;
            _timer = 0f;
            _isPlaying = true;
            OnScoreChanged?.Invoke(_score);
            OnGameStarted?.Invoke();
            Debug.Log("[MiniGame] Game started!");
        }

        public void AddScore(int points)
        {
            if (!_isPlaying) return;
            _score += points;
            OnScoreChanged?.Invoke(_score);
        }

        public void EndGame()
        {
            if (!_isPlaying) return;
            _isPlaying = false;

            int coinsEarned = _score * coinMultiplier;

            // Award coins
            if (PawCoinManager.Instance != null && coinsEarned > 0)
                PawCoinManager.Instance.AddCoins(coinsEarned, "mini-game");

            // Track best score
            if (GameManager.Instance?.Data != null)
            {
                if (_score > GameManager.Instance.Data.bestFishingScore)
                    GameManager.Instance.Data.bestFishingScore = _score;
            }

            // Check achievements
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.CheckAll();

            OnGameEnded?.Invoke(_score, coinsEarned);
            Debug.Log($"[MiniGame] Game over! Score: {_score}, Coins: {coinsEarned}");
        }
    }
}
