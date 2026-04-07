using System;
using UnityEngine;
using CatRaising.Core;
using CatRaising.Data;

namespace CatRaising.Systems
{
    /// <summary>
    /// Manages 5 daily tasks that reset at midnight.
    /// Tasks are "completed" by game actions, but rewards must be "claimed" via UI.
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

        public event Action<DailyTaskType> OnTaskCompleted; // Task done (not yet claimed)
        public event Action OnAllTasksCompleted;
        public event Action OnTasksReset;
        public event Action OnClaimStateChanged; // For red-dot notification

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(GameData data)
        {
            string today = GameData.TodayString;
            if (data.lastDailyResetDate != today)
            {
                if (data.AllDailyTasksComplete && data.lastDailyResetDate != "")
                {
                    if (DateTime.TryParse(data.lastDailyResetDate, out DateTime lastDate))
                    {
                        if ((DateTime.Now.Date - lastDate).Days == 1)
                            data.dailyStreakDays++;
                        else
                            data.dailyStreakDays = 0;
                    }
                    data.lastDailyCompleteDate = data.lastDailyResetDate;
                }
                else if (data.lastDailyResetDate != "")
                {
                    data.dailyStreakDays = 0;
                }

                // Reset all tasks
                data.dailyLogin = false;
                data.dailyFeed = false;
                data.dailyWater = false;
                data.dailyPet = false;
                data.dailyPlay = false;

                // Reset claimed states
                data.dailyLoginClaimed = false;
                data.dailyFeedClaimed = false;
                data.dailyWaterClaimed = false;
                data.dailyPetClaimed = false;
                data.dailyPlayClaimed = false;

                data.lastDailyResetDate = today;

                OnTasksReset?.Invoke();
                Debug.Log("[DailyTask] Tasks reset for new day.");
            }

            // Auto-complete login task
            CheckTask(DailyTaskType.Login);
        }

        /// <summary>
        /// Mark a task as complete (does NOT award reward — player must claim).
        /// </summary>
        public void CheckTask(DailyTaskType taskType)
        {
            if (GameManager.Instance?.Data == null) return;
            var data = GameManager.Instance.Data;

            if (IsTaskComplete(taskType)) return;

            SetTaskComplete(data, taskType, true);
            OnTaskCompleted?.Invoke(taskType);
            OnClaimStateChanged?.Invoke();
            Debug.Log($"[DailyTask] ✅ {taskType} complete! (needs claiming)");

            if (data.AllDailyTasksComplete)
            {
                OnAllTasksCompleted?.Invoke();
                Debug.Log("[DailyTask] 🎉 All daily tasks complete!");
            }
        }

        /// <summary>
        /// Claim reward for a completed task. Returns coins awarded.
        /// </summary>
        public int ClaimTask(DailyTaskType taskType)
        {
            if (!IsTaskComplete(taskType) || IsTaskClaimed(taskType)) return 0;

            if (GameManager.Instance?.Data == null) return 0;
            var data = GameManager.Instance.Data;

            SetTaskClaimed(data, taskType, true);

            int reward = GetReward(taskType);
            if (PawCoinManager.Instance != null && reward > 0)
                PawCoinManager.Instance.AddCoins(reward, $"daily:{taskType}");

            OnClaimStateChanged?.Invoke();
            Debug.Log($"[DailyTask] Claimed {taskType}: +{reward} 🐾");
            return reward;
        }

        public bool IsTaskComplete(DailyTaskType taskType)
        {
            if (GameManager.Instance?.Data == null) return false;
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

        public bool IsTaskClaimed(DailyTaskType taskType)
        {
            if (GameManager.Instance?.Data == null) return false;
            var data = GameManager.Instance.Data;
            return taskType switch
            {
                DailyTaskType.Login => data.dailyLoginClaimed,
                DailyTaskType.FeedCat => data.dailyFeedClaimed,
                DailyTaskType.GiveWater => data.dailyWaterClaimed,
                DailyTaskType.PetCat => data.dailyPetClaimed,
                DailyTaskType.PlayFeather => data.dailyPlayClaimed,
                _ => false
            };
        }

        /// <summary>
        /// True if there are completed but unclaimed tasks (for red-dot notification).
        /// </summary>
        public bool HasUnclaimedTasks
        {
            get
            {
                foreach (DailyTaskType t in System.Enum.GetValues(typeof(DailyTaskType)))
                    if (IsTaskComplete(t) && !IsTaskClaimed(t)) return true;
                return false;
            }
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

        private void SetTaskClaimed(GameData data, DailyTaskType taskType, bool value)
        {
            switch (taskType)
            {
                case DailyTaskType.Login: data.dailyLoginClaimed = value; break;
                case DailyTaskType.FeedCat: data.dailyFeedClaimed = value; break;
                case DailyTaskType.GiveWater: data.dailyWaterClaimed = value; break;
                case DailyTaskType.PetCat: data.dailyPetClaimed = value; break;
                case DailyTaskType.PlayFeather: data.dailyPlayClaimed = value; break;
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
