using System;
using UnityEngine;
using CatRaising.Core;
using CatRaising.Data;

namespace CatRaising.Systems
{
    /// <summary>
    /// Manages 5 daily tasks that reset at midnight (real-world time).
    /// Tasks: Login, Feed Cat, Give Water, Pet Cat, Play with Feather.
    /// </summary>
    public class DailyTaskManager : MonoBehaviour
    {
        public static DailyTaskManager Instance { get; private set; }

        [Header("Rewards (Paw Coins)")]
        [SerializeField] private int loginReward = 10;
        [SerializeField] private int feedReward = 15;
        [SerializeField] private int waterReward = 15;
        [SerializeField] private int petReward = 20;
        [SerializeField] private int playReward = 25;

        public event Action<DailyTaskType> OnTaskCompleted;
        public event Action OnAllTasksCompleted;
        public event Action OnTasksReset;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Initialize daily tasks. Call after GameData is loaded.
        /// </summary>
        public void Initialize(GameData data)
        {
            // Check if we need to reset (new day)
            string today = GameData.TodayString;
            if (data.lastDailyResetDate != today)
            {
                // Check if yesterday was a complete day (for streak)
                if (data.AllDailyTasksComplete && data.lastDailyResetDate != "")
                {
                    // Check if it was actually yesterday
                    if (DateTime.TryParse(data.lastDailyResetDate, out DateTime lastDate))
                    {
                        if ((DateTime.Now.Date - lastDate).Days == 1)
                            data.dailyStreakDays++;
                        else
                            data.dailyStreakDays = 0; // Streak broken
                    }
                    data.lastDailyCompleteDate = data.lastDailyResetDate;
                }
                else if (data.lastDailyResetDate != "")
                {
                    data.dailyStreakDays = 0; // Didn't complete all tasks
                }

                // Reset all tasks
                data.dailyLogin = false;
                data.dailyFeed = false;
                data.dailyWater = false;
                data.dailyPet = false;
                data.dailyPlay = false;
                data.lastDailyResetDate = today;

                OnTasksReset?.Invoke();
                Debug.Log("[DailyTask] Tasks reset for new day.");
            }

            // Auto-complete login task
            CheckTask(DailyTaskType.Login);
        }

        /// <summary>
        /// Mark a task as complete. Called by game systems when actions happen.
        /// </summary>
        public void CheckTask(DailyTaskType taskType)
        {
            if (GameManager.Instance == null || GameManager.Instance.Data == null) return;
            var data = GameManager.Instance.Data;

            if (IsTaskComplete(taskType)) return; // Already done today

            // Mark complete
            SetTaskComplete(data, taskType, true);

            // Award coins
            int reward = GetReward(taskType);
            if (PawCoinManager.Instance != null && reward > 0)
                PawCoinManager.Instance.AddCoins(reward, $"daily:{taskType}");

            OnTaskCompleted?.Invoke(taskType);
            Debug.Log($"[DailyTask] ✅ {taskType} complete! +{reward} 🐾");

            // Check if all complete
            if (data.AllDailyTasksComplete)
            {
                OnAllTasksCompleted?.Invoke();
                Debug.Log("[DailyTask] 🎉 All daily tasks complete!");
            }
        }

        public bool IsTaskComplete(DailyTaskType taskType)
        {
            if (GameManager.Instance == null || GameManager.Instance.Data == null) return false;
            var data = GameManager.Instance.Data;

            return taskType switch
            {
                DailyTaskType.Login => data.dailyLogin,
                DailyTaskType.FeedCat => data.dailyFeed,
                DailyTaskType.GiveWater => data.dailyWater,
                DailyTaskType.PetCat => data.dailyPet,
                DailyTaskType.PlayFeather => data.dailyPlay,
                _ => false
            };
        }

        private void SetTaskComplete(GameData data, DailyTaskType taskType, bool value)
        {
            switch (taskType)
            {
                case DailyTaskType.Login: data.dailyLogin = value; break;
                case DailyTaskType.FeedCat: data.dailyFeed = value; break;
                case DailyTaskType.GiveWater: data.dailyWater = value; break;
                case DailyTaskType.PetCat: data.dailyPet = value; break;
                case DailyTaskType.PlayFeather: data.dailyPlay = value; break;
            }
        }

        public int GetReward(DailyTaskType taskType)
        {
            return taskType switch
            {
                DailyTaskType.Login => loginReward,
                DailyTaskType.FeedCat => feedReward,
                DailyTaskType.GiveWater => waterReward,
                DailyTaskType.PetCat => petReward,
                DailyTaskType.PlayFeather => playReward,
                _ => 0
            };
        }

        public string GetTaskName(DailyTaskType taskType)
        {
            return taskType switch
            {
                DailyTaskType.Login => "Log In",
                DailyTaskType.FeedCat => "Feed the Cat",
                DailyTaskType.GiveWater => "Give Water",
                DailyTaskType.PetCat => "Pet the Cat",
                DailyTaskType.PlayFeather => "Play with Feather",
                _ => "Unknown"
            };
        }

        public int CompletedCount
        {
            get
            {
                if (GameManager.Instance?.Data == null) return 0;
                return GameManager.Instance.Data.CompletedDailyTaskCount;
            }
        }

        public int TotalTasks => 5;
    }
}
