using System;
using UnityEngine;

namespace CatRaising.Data
{
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
        public float foodBowlAmount = 0f;  // 0-100, how full the bowl is
        public float waterBowlAmount = 0f; // 0-100, how full the bowl is

        // Statistics
        public int daysPlayed = 0;
        public float totalPlayTimeSeconds = 0f;

        /// <summary>
        /// Get the last played time as a DateTime. Returns DateTime.Now if not set.
        /// </summary>
        public DateTime GetLastPlayedTime()
        {
            if (string.IsNullOrEmpty(lastPlayedTime))
                return DateTime.Now;

            if (DateTime.TryParse(lastPlayedTime, out DateTime result))
                return result;

            return DateTime.Now;
        }

        /// <summary>
        /// Set the last played time from a DateTime.
        /// </summary>
        public void SetLastPlayedTime(DateTime time)
        {
            lastPlayedTime = time.ToString("o"); // ISO 8601 format
        }
    }
}
