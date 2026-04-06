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

        [Header("Milestone 3 Systems")]
        public CatRaising.Systems.PawCoinManager pawCoinManager;
        public CatRaising.Systems.DailyTaskManager dailyTaskManager;
        public CatRaising.Systems.AchievementManager achievementManager;
        public RoomManager roomManager;

        /// <summary>
        /// Current game data (loaded from save or freshly created).
        /// </summary>
        public GameData Data { get; private set; }

        public bool IsFirstLaunch => Data.isFirstLaunch;
        public string CatName => Data.catName;

        public event Action OnGameReady;
        public event Action<string> OnCatNamed;

        // Auto-save interval
        private const float AUTO_SAVE_INTERVAL = 60f;
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
            if (Input.GetKeyDown(KeyCode.M))
            {
                SaveSystem.DeleteSave(); // TEMP: Clear save for testing purposes
            }
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
                ApplyOfflineDecay();
                InitializeGame();
            }
        }

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

            // First-time unlocked rooms
            if (Data.unlockedRoomIds == null || Data.unlockedRoomIds.Count == 0)
                Data.unlockedRoomIds = new System.Collections.Generic.List<string> { "living_room" };

            SaveGame();
            OnCatNamed?.Invoke(catName);
            InitializeGame();
        }

        private void InitializeGame()
        {
            Debug.Log($"[GameManager] Game initialized. Cat: {Data.catName}, Bond: {Data.bondLevel:F1}, Coins: {Data.pawCoins}");

            // Track days played
            string today = GameData.TodayString;
            if (Data.lastPlayedTime != "" && Data.GetLastPlayedTime().Date < DateTime.Now.Date)
                Data.daysPlayed++;

            Data.SetLastPlayedTime(DateTime.Now);

            // ─── Push saved data into core systems ──────────────
            if (catNeeds != null)
                catNeeds.LoadFromData(Data);

            if (bondSystem != null)
                bondSystem.LoadFromData(Data);

            // ─── Initialize Milestone 3 systems ─────────────────
            if (pawCoinManager != null)
                pawCoinManager.LoadFromData(Data);

            if (achievementManager != null)
                achievementManager.LoadFromData(Data);

            if (roomManager != null)
                roomManager.LoadFromData(Data);

            if (dailyTaskManager != null)
                dailyTaskManager.Initialize(Data);

            // Check achievements on load
            if (achievementManager != null)
                achievementManager.CheckAll();

            OnGameReady?.Invoke();
        }

        private void ApplyOfflineDecay()
        {
            if (TimeManager.Instance == null) return;

            float secondsAway = TimeManager.Instance.GetSecondsSince(Data.GetLastPlayedTime());
            float hoursAway = secondsAway / 3600f;

            Debug.Log($"[GameManager] Player was away for {hoursAway:F1} hours. Applying offline decay.");

            Data.hunger = Mathf.Max(5f, Data.hunger - hoursAway * 25f);
            Data.thirst = Mathf.Max(5f, Data.thirst - hoursAway * 33.3f);
            Data.happiness = Mathf.Max(10f, Data.happiness - hoursAway * 16.7f);
            Data.cleanliness = Mathf.Max(15f, Data.cleanliness - hoursAway * 12.5f);

            Data.foodBowlAmount = Mathf.Max(0f, Data.foodBowlAmount - hoursAway * 20f);
            Data.waterBowlAmount = Mathf.Max(0f, Data.waterBowlAmount - hoursAway * 25f);
        }

        public void SaveGame()
        {
            if (Data == null) return;

            // Pull current state from core systems
            if (catNeeds != null)
                catNeeds.SaveToData(Data);

            if (bondSystem != null)
                bondSystem.SaveToData(Data);

            // Save Milestone 3 systems
            if (pawCoinManager != null)
                pawCoinManager.SaveToData(Data);

            if (achievementManager != null)
                achievementManager.SaveToData(Data);

            if (roomManager != null)
                roomManager.SaveToData(Data);

            SaveSystem.Save(Data);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && Data != null && !Data.isFirstLaunch)
            {
                SaveGame();
                Debug.Log("[GameManager] App paused — game saved.");
            }
        }

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
