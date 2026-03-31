using System;
using UnityEngine;
using CatRaising.Data;

namespace CatRaising.Core
{
    /// <summary>
    /// Central game manager. Handles game lifecycle, save/load orchestration,
    /// and first-launch detection.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Reference to the cat's CatNeeds component")]
        public CatRaising.Cat.CatNeeds catNeeds;

        [Tooltip("Reference to the BondSystem component")]
        public CatRaising.Systems.BondSystem bondSystem;

        [Tooltip("Reference to the NamingScreenUI")]
        public CatRaising.UI.NamingScreenUI namingScreen;

        /// <summary>
        /// Current game data (loaded from save or freshly created).
        /// </summary>
        public GameData Data { get; private set; }

        /// <summary>
        /// Whether this is the player's first time launching the game.
        /// </summary>
        public bool IsFirstLaunch => Data.isFirstLaunch;

        /// <summary>
        /// The cat's name as set by the player.
        /// </summary>
        public string CatName => Data.catName;

        /// <summary>
        /// Event fired when the game data is ready (after load + name entry).
        /// </summary>
        public event Action OnGameReady;

        /// <summary>
        /// Event fired when the cat is named (first launch complete).
        /// </summary>
        public event Action<string> OnCatNamed;

        // Auto-save interval
        private const float AUTO_SAVE_INTERVAL = 60f; // Save every 60 seconds
        private float _autoSaveTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            LoadGame();
        }

        private void Update()
        {
            if (Data == null || Data.isFirstLaunch) return;

            // Track play time
            Data.totalPlayTimeSeconds += Time.deltaTime;

            // Auto-save
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
            {
                SaveGame();
                _autoSaveTimer = 0f;
            }
        }

        /// <summary>
        /// Load game data and handle first launch vs returning player.
        /// </summary>
        private void LoadGame()
        {
            Data = SaveSystem.Load();

            if (Data.isFirstLaunch)
            {
                Debug.Log("[GameManager] First launch detected — showing naming screen.");
                ShowNamingScreen();
            }
            else
            {
                // Calculate offline time and apply need decay
                ApplyOfflineDecay();
                InitializeGame();
            }
        }

        /// <summary>
        /// Show the naming screen for first-time players.
        /// </summary>
        private void ShowNamingScreen()
        {
            if (namingScreen != null)
            {
                namingScreen.Show(OnNameConfirmed);
            }
            else
            {
                Debug.LogWarning("[GameManager] No NamingScreenUI assigned! Using default name.");
                OnNameConfirmed("Kitty");
            }
        }

        /// <summary>
        /// Called when the player confirms their cat's name.
        /// </summary>
        private void OnNameConfirmed(string catName)
        {
            Data.catName = catName;
            Data.isFirstLaunch = false;
            Data.SetLastPlayedTime(DateTime.Now);

            // Start with full needs
            Data.hunger = 100f;
            Data.thirst = 100f;
            Data.happiness = 100f;
            Data.cleanliness = 100f;

            SaveGame();
            OnCatNamed?.Invoke(catName);
            InitializeGame();
        }

        /// <summary>
        /// Initialize all systems after data is ready.
        /// </summary>
        private void InitializeGame()
        {
            Debug.Log($"[GameManager] Game initialized. Cat: {Data.catName}, Bond: {Data.bondLevel:F1}");

            // Push saved data into systems
            if (catNeeds != null)
                catNeeds.LoadFromData(Data);

            if (bondSystem != null)
                bondSystem.LoadFromData(Data);

            OnGameReady?.Invoke();
        }

        /// <summary>
        /// Apply need decay for time spent offline.
        /// This ensures needs drop while the player is away, but never punishingly.
        /// </summary>
        private void ApplyOfflineDecay()
        {
            if (TimeManager.Instance == null) return;

            float secondsAway = TimeManager.Instance.GetSecondsSince(Data.GetLastPlayedTime());
            float hoursAway = secondsAway / 3600f;

            Debug.Log($"[GameManager] Player was away for {hoursAway:F1} hours. Applying offline decay.");

            // Decay rates (per hour): hunger=25, thirst=33.3, happiness=16.7, cleanliness=12.5
            // These match the design doc (hunger empties in 4h, thirst in 3h, etc.)
            Data.hunger = Mathf.Max(5f, Data.hunger - hoursAway * 25f);
            Data.thirst = Mathf.Max(5f, Data.thirst - hoursAway * 33.3f);
            Data.happiness = Mathf.Max(10f, Data.happiness - hoursAway * 16.7f);
            Data.cleanliness = Mathf.Max(15f, Data.cleanliness - hoursAway * 12.5f);

            // Food and water bowls also deplete over time (cat eats/drinks while away)
            Data.foodBowlAmount = Mathf.Max(0f, Data.foodBowlAmount - hoursAway * 20f);
            Data.waterBowlAmount = Mathf.Max(0f, Data.waterBowlAmount - hoursAway * 25f);
        }

        /// <summary>
        /// Save current game state to disk.
        /// </summary>
        public void SaveGame()
        {
            if (Data == null) return;

            // Pull current state from systems
            if (catNeeds != null)
                catNeeds.SaveToData(Data);

            if (bondSystem != null)
                bondSystem.SaveToData(Data);

            SaveSystem.Save(Data);
        }

        /// <summary>
        /// Called when the app is about to be paused/backgrounded (mobile).
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && Data != null && !Data.isFirstLaunch)
            {
                SaveGame();
                Debug.Log("[GameManager] App paused — game saved.");
            }
        }

        /// <summary>
        /// Called when the app is about to quit.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (Data != null && !Data.isFirstLaunch)
            {
                SaveGame();
                Debug.Log("[GameManager] App quitting — game saved.");
            }
        }
    }
}
