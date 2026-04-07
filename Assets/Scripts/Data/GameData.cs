using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatRaising.Data
{
    [Serializable]
    public class FurnitureSaveData
    {
        public string itemId;
        public string roomId;
        public int gridCol;
        public int gridRow;
    }

    /// <summary>
    /// Serializable data model for save/load. Contains all persistent game state.
    /// </summary>
    [Serializable]
    public class GameData
    {
        // Cat identity
        public string catName = "";
        public bool isFirstLaunch = true;

        // Cat needs (0-100 scale)
        public float hunger = 100f;
        public float thirst = 100f;
        public float happiness = 100f;
        public float cleanliness = 100f;

        // Bond system (0-100)
        public float bondLevel = 0f;
        public int totalPets = 0;
        public int totalFeedings = 0;
        public int totalPlays = 0;

        // Timestamps
        public string lastPlayedTime = "";
        public string lastFedTime = "";
        public string lastWateredTime = "";

        // Interactable states
        public float foodBowlAmount = 0f;
        public float waterBowlAmount = 0f;

        // Statistics
        public int daysPlayed = 0;
        public float totalPlayTimeSeconds = 0f;
        public int totalGroomings = 0;
        public int totalItemsPurchased = 0;

        // ─── Milestone 3: Economy ───────────────────────────────
        public int pawCoins = 0;

        // ─── Milestone 3: Rooms ─────────────────────────────────
        public List<string> unlockedRoomIds = new List<string>();
        public string currentRoomId = "living_room";

        // ─── Milestone 3: Furniture ─────────────────────────────
        public List<string> ownedFurnitureIds = new List<string>();
        public List<FurnitureSaveData> placedFurniture = new List<FurnitureSaveData>();

        // ─── Milestone 3: Daily Tasks ───────────────────────────
        public string lastDailyResetDate = "";
        public bool dailyLogin = false;
        public bool dailyFeed = false;
        public bool dailyWater = false;
        public bool dailyPet = false;
        public bool dailyPlay = false;

        // ─── Daily Task Claimed States ──────────────────────────
        public bool dailyLoginClaimed = false;
        public bool dailyFeedClaimed = false;
        public bool dailyWaterClaimed = false;
        public bool dailyPetClaimed = false;
        public bool dailyPlayClaimed = false;

        // ─── Milestone 3: Daily Streak ──────────────────────────
        public int dailyStreakDays = 0;
        public string lastDailyCompleteDate = "";

        // ─── Milestone 3: Achievements ──────────────────────────
        public List<string> unlockedAchievementIds = new List<string>();
        public List<string> claimedAchievementIds = new List<string>();

        // ─── Milestone 3: Mini-Game ─────────────────────────────
        public int bestFishingScore = 0;

        // ─── Helpers ────────────────────────────────────────────

        public DateTime GetLastPlayedTime()
        {
            if (string.IsNullOrEmpty(lastPlayedTime))
                return DateTime.Now;
            if (DateTime.TryParse(lastPlayedTime, out DateTime result))
                return result;
            return DateTime.Now;
        }

        public void SetLastPlayedTime(DateTime time)
        {
            lastPlayedTime = time.ToString("o");
        }

        /// <summary>
        /// Get today's date string for daily task comparison.
        /// </summary>
        public static string TodayString => DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>
        /// Check if all daily tasks are complete.
        /// </summary>
        public bool AllDailyTasksComplete =>
            dailyLogin && dailyFeed && dailyWater && dailyPet && dailyPlay;

        /// <summary>
        /// Get count of completed daily tasks.
        /// </summary>
        public int CompletedDailyTaskCount
        {
            get
            {
                int count = 0;
                if (dailyLogin) count++;
                if (dailyFeed) count++;
                if (dailyWater) count++;
                if (dailyPet) count++;
                if (dailyPlay) count++;
                return count;
            }
        }
    }
}
