using System;
using System.IO;
using UnityEngine;
using CatRaising.Data;

namespace CatRaising.Core
{
    /// <summary>
    /// Handles saving and loading game data to/from persistent storage as JSON.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FILE_NAME = "cat_save.json";

        private static string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        /// <summary>
        /// Save game data to disk.
        /// </summary>
        public static void Save(GameData data)
        {
            try
            {
                data.SetLastPlayedTime(DateTime.Now);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveSystem] Game saved to {SaveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Load game data from disk. Returns a new GameData if no save exists.
        /// </summary>
        public static GameData Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    GameData data = JsonUtility.FromJson<GameData>(json);
                    Debug.Log($"[SaveSystem] Game loaded. Cat: {data.catName}, Bond: {data.bondLevel}");
                    return data;
                }
                else
                {
                    Debug.Log("[SaveSystem] No save file found. Creating new game data.");
                    return new GameData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load: {e.Message}");
                return new GameData();
            }
        }

        /// <summary>
        /// Check if a save file exists.
        /// </summary>
        public static bool SaveExists()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Delete the save file (for debugging/reset).
        /// </summary>
        public static void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveSystem] Save file deleted.");
            }
        }
    }
}
