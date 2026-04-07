using System;
using System.Collections.Generic;
using UnityEngine;
using CatRaising.Core;
using CatRaising.Data;

namespace CatRaising.Systems
{
    /// <summary>
    /// Manages 25 achievements. Unlocking notifies the UI popup,
    /// but rewards are only given when the player presses "Claim".
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Serializable]
        public class AchievementDef
        {
            public AchievementId id;
            public string name;
            public string description;
            public int coinReward;
            public float bondReward;
        }

        private static readonly AchievementDef[] AllAchievements = new AchievementDef[]
        {
            new() { id = AchievementId.FirstTouch,         name = "First Touch",         description = "Pet the cat for the first time",           coinReward = 25,  bondReward = 0 },
            new() { id = AchievementId.HungryKitty,        name = "Hungry Kitty",        description = "Feed the cat for the first time",           coinReward = 25,  bondReward = 0 },
            new() { id = AchievementId.HydrationStation,   name = "Hydration Station",   description = "Give water for the first time",             coinReward = 25,  bondReward = 0 },
            new() { id = AchievementId.Playtime,           name = "Playtime!",           description = "Play with feather toy for the first time",  coinReward = 25,  bondReward = 0 },
            new() { id = AchievementId.CatWhisperer,       name = "Cat Whisperer",       description = "Pet the cat 50 times",                      coinReward = 50,  bondReward = 0 },
            new() { id = AchievementId.MasterChef,         name = "Master Chef",         description = "Feed the cat 50 times",                     coinReward = 50,  bondReward = 0 },
            new() { id = AchievementId.Bartender,          name = "Bartender",           description = "Give water 50 times",                       coinReward = 50,  bondReward = 0 },
            new() { id = AchievementId.PlayBuddy,          name = "Play Buddy",          description = "Play 25 sessions",                          coinReward = 50,  bondReward = 0 },
            new() { id = AchievementId.BondAcquaintance,   name = "Acquaintance",        description = "Reach Acquaintance bond tier",               coinReward = 50,  bondReward = 2 },
            new() { id = AchievementId.BondFriend,         name = "Friend",              description = "Reach Friend bond tier",                    coinReward = 75,  bondReward = 3 },
            new() { id = AchievementId.BondCompanion,      name = "Companion",           description = "Reach Companion bond tier",                 coinReward = 100, bondReward = 5 },
            new() { id = AchievementId.BondBestFriend,     name = "Best Friend",         description = "Reach Best Friend bond tier",               coinReward = 150, bondReward = 5 },
            new() { id = AchievementId.BondSoulmate,       name = "Soulmate",            description = "Reach Soulmate bond tier",                  coinReward = 200, bondReward = 10 },
            new() { id = AchievementId.Day1,               name = "Day 1",               description = "Play for 1 day",                            coinReward = 30,  bondReward = 0 },
            new() { id = AchievementId.OneWeek,            name = "One Week",            description = "Play for 7 days",                           coinReward = 75,  bondReward = 0 },
            new() { id = AchievementId.TwoWeeks,           name = "Two Weeks",           description = "Play for 14 days",                          coinReward = 150, bondReward = 0 },
            new() { id = AchievementId.CleanKitty,         name = "Clean Kitty",         description = "Cat grooms itself 10 times",                coinReward = 40,  bondReward = 0 },
            new() { id = AchievementId.InteriorDesigner,   name = "Interior Designer",   description = "Place your first furniture",                 coinReward = 30,  bondReward = 0 },
            new() { id = AchievementId.Homeowner,          name = "Homeowner",           description = "Unlock a new room",                         coinReward = 50,  bondReward = 0 },
            new() { id = AchievementId.FullHouse,          name = "Full House",          description = "Unlock all 3 rooms",                        coinReward = 100, bondReward = 0 },
            new() { id = AchievementId.Completionist,      name = "Completionist",       description = "Complete all 5 daily tasks in one day",     coinReward = 50,  bondReward = 0 },
            new() { id = AchievementId.Dedicated,          name = "Dedicated",           description = "Complete all daily tasks 7 days in a row",  coinReward = 200, bondReward = 0 },
            new() { id = AchievementId.FishingPro,         name = "Fishing Pro",         description = "Score 20+ in Cat Fishing",                  coinReward = 75,  bondReward = 0 },
            new() { id = AchievementId.GoldenCatch,        name = "Golden Catch",        description = "Catch a golden fish",                       coinReward = 40,  bondReward = 0 },
            new() { id = AchievementId.Shopaholic,         name = "Shopaholic",          description = "Buy 10 items from the shop",                coinReward = 100, bondReward = 0 },
        };

        public event Action<AchievementDef> OnAchievementUnlocked;
        public event Action OnClaimStateChanged; // For red-dot notification

        private HashSet<string> _unlocked = new HashSet<string>();
        private HashSet<string> _claimed = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void LoadFromData(GameData data)
        {
            _unlocked.Clear();
            _claimed.Clear();

            if (data.unlockedAchievementIds != null)
                foreach (var id in data.unlockedAchievementIds)
                    _unlocked.Add(id);

            if (data.claimedAchievementIds != null)
                foreach (var id in data.claimedAchievementIds)
                    _claimed.Add(id);
        }

        public void SaveToData(GameData data)
        {
            data.unlockedAchievementIds = new List<string>(_unlocked);
            data.claimedAchievementIds = new List<string>(_claimed);
        }

        /// <summary>
        /// Try to unlock an achievement. Does NOT award reward — player must claim.
        /// </summary>
        public void TryUnlock(AchievementId id)
        {
            string key = id.ToString();
            if (_unlocked.Contains(key)) return;

            var def = GetDefinition(id);
            if (def == null) return;

            _unlocked.Add(key);
            OnAchievementUnlocked?.Invoke(def);
            OnClaimStateChanged?.Invoke();
            Debug.Log($"[Achievement] 🏆 Unlocked: {def.name} (needs claiming)");
        }

        /// <summary>
        /// Claim an unlocked achievement and receive its reward.
        /// </summary>
        public void ClaimAchievement(AchievementId id)
        {
            string key = id.ToString();
            if (!_unlocked.Contains(key) || _claimed.Contains(key)) return;

            var def = GetDefinition(id);
            if (def == null) return;

            _claimed.Add(key);

            // Award rewards on claim
            if (def.coinReward > 0 && PawCoinManager.Instance != null)
                PawCoinManager.Instance.AddCoins(def.coinReward, $"achievement:{def.name}");

            if (def.bondReward > 0 && BondSystem.Instance != null)
                BondSystem.Instance.AddBond(def.bondReward, $"achievement:{def.name}");

            OnClaimStateChanged?.Invoke();
            Debug.Log($"[Achievement] ✅ Claimed: {def.name} (+{def.coinReward} 🐾)");
        }

        public void CheckAll()
        {
            if (GameManager.Instance?.Data == null) return;
            var data = GameManager.Instance.Data;

            if (data.totalPets >= 1) TryUnlock(AchievementId.FirstTouch);
            if (data.totalPets >= 50) TryUnlock(AchievementId.CatWhisperer);
            if (data.totalFeedings >= 1) TryUnlock(AchievementId.HungryKitty);
            if (data.totalFeedings >= 50) TryUnlock(AchievementId.MasterChef);
            if (data.totalPlays >= 1) TryUnlock(AchievementId.Playtime);
            if (data.totalPlays >= 25) TryUnlock(AchievementId.PlayBuddy);

            if (data.bondLevel >= 11) TryUnlock(AchievementId.BondAcquaintance);
            if (data.bondLevel >= 26) TryUnlock(AchievementId.BondFriend);
            if (data.bondLevel >= 51) TryUnlock(AchievementId.BondCompanion);
            if (data.bondLevel >= 76) TryUnlock(AchievementId.BondBestFriend);
            if (data.bondLevel >= 91) TryUnlock(AchievementId.BondSoulmate);

            if (data.daysPlayed >= 1) TryUnlock(AchievementId.Day1);
            if (data.daysPlayed >= 7) TryUnlock(AchievementId.OneWeek);
            if (data.daysPlayed >= 14) TryUnlock(AchievementId.TwoWeeks);

            if (data.totalGroomings >= 10) TryUnlock(AchievementId.CleanKitty);

            if (data.AllDailyTasksComplete) TryUnlock(AchievementId.Completionist);
            if (data.dailyStreakDays >= 7) TryUnlock(AchievementId.Dedicated);

            if (data.totalItemsPurchased >= 10) TryUnlock(AchievementId.Shopaholic);

            if (data.unlockedRoomIds != null)
            {
                if (data.unlockedRoomIds.Count >= 2) TryUnlock(AchievementId.Homeowner);
                if (data.unlockedRoomIds.Count >= 3) TryUnlock(AchievementId.FullHouse);
            }

            if (data.placedFurniture != null && data.placedFurniture.Count >= 1)
                TryUnlock(AchievementId.InteriorDesigner);

            if (data.bestFishingScore >= 20) TryUnlock(AchievementId.FishingPro);
        }

        public bool IsUnlocked(AchievementId id) => _unlocked.Contains(id.ToString());
        public bool IsClaimed(AchievementId id) => _claimed.Contains(id.ToString());

        public bool HasUnclaimedAchievements
        {
            get
            {
                foreach (var key in _unlocked)
                    if (!_claimed.Contains(key)) return true;
                return false;
            }
        }

        public static AchievementDef GetDefinition(AchievementId id)
        {
            foreach (var def in AllAchievements)
                if (def.id == id) return def;
            return null;
        }

        public static AchievementDef[] GetAllDefinitions() => AllAchievements;

        public int UnlockedCount => _unlocked.Count;
        public int TotalCount => AllAchievements.Length;
    }
}
