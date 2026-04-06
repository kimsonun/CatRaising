using System;
using System.Collections.Generic;
using UnityEngine;
using CatRaising.Data;
using CatRaising.Cat;
using CatRaising.Systems;

namespace CatRaising.Core
{
    /// <summary>
    /// Manages rooms: switching, unlocking, wander bounds.
    /// 
    /// SETUP:
    /// 1. Add this component to a "RoomManager" object
    /// 2. Configure the rooms array with backgrounds, wander bounds
    /// 3. Assign the background SpriteRenderer and CatAI reference
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Serializable]
        public class RoomConfig
        {
            public string roomId;
            public string roomName;
            public Sprite backgroundSprite;
            public int unlockCost; // 0 = free
            public bool unlockedByDefault;
            public Vector2 wanderBoundsMin;
            public Vector2 wanderBoundsMax;
        }

        [Header("Room Definitions")]
        [SerializeField] private RoomConfig[] rooms = new RoomConfig[]
        {
            new() { roomId = "living_room", roomName = "Living Room", unlockCost = 0, unlockedByDefault = true,
                    wanderBoundsMin = new Vector2(-7f, -3f), wanderBoundsMax = new Vector2(7f, -1f) },
            new() { roomId = "kitchen",     roomName = "Kitchen",     unlockCost = 500, unlockedByDefault = false,
                    wanderBoundsMin = new Vector2(-7f, -3f), wanderBoundsMax = new Vector2(7f, -1f) },
            new() { roomId = "bedroom",     roomName = "Bedroom",     unlockCost = 800, unlockedByDefault = false,
                    wanderBoundsMin = new Vector2(-7f, -3f), wanderBoundsMax = new Vector2(7f, -1f) },
        };

        [Header("References")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private CatAI catAI;

        private RoomConfig _currentRoom;
        private HashSet<string> _unlockedRoomIds = new HashSet<string>();

        public RoomConfig CurrentRoom => _currentRoom;
        public string CurrentRoomId => _currentRoom?.roomId ?? "living_room";
        public RoomConfig[] AllRooms => rooms;

        public event Action<RoomConfig> OnRoomChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void LoadFromData(GameData data)
        {
            _unlockedRoomIds.Clear();

            // Add default rooms
            foreach (var room in rooms)
                if (room.unlockedByDefault)
                    _unlockedRoomIds.Add(room.roomId);

            // Add saved unlocked rooms
            if (data.unlockedRoomIds != null)
                foreach (var id in data.unlockedRoomIds)
                    _unlockedRoomIds.Add(id);

            // Switch to saved room
            string targetRoom = data.currentRoomId;
            if (!_unlockedRoomIds.Contains(targetRoom))
                targetRoom = "living_room";

            SwitchRoom(targetRoom);
        }

        public void SaveToData(GameData data)
        {
            data.unlockedRoomIds = new List<string>(_unlockedRoomIds);
            data.currentRoomId = CurrentRoomId;
        }

        /// <summary>
        /// Switch to a room by ID.
        /// </summary>
        public void SwitchRoom(string roomId)
        {
            RoomConfig room = GetRoom(roomId);
            if (room == null)
            {
                Debug.LogWarning($"[RoomManager] Room not found: {roomId}");
                return;
            }

            if (!IsRoomUnlocked(roomId))
            {
                Debug.Log($"[RoomManager] Room '{roomId}' is locked!");
                return;
            }

            _currentRoom = room;

            // Update background
            if (backgroundRenderer != null && room.backgroundSprite != null)
                backgroundRenderer.sprite = room.backgroundSprite;

            // Clear isometric grid occupancy and reload furniture for new room
            if (IsometricGrid.Instance != null)
                IsometricGrid.Instance.ClearAllOccupancy();

            // Reload placed furniture (this rebuilds grid occupancy)
            var furniturePlacer = FindAnyObjectByType<CatRaising.UI.FurniturePlacementUI>();
            if (furniturePlacer != null)
                furniturePlacer.LoadPlacedFurniture();

            // Fallback: update rectangular wander bounds for CatAI
            if (catAI != null)
                catAI.SetWanderBounds(room.wanderBoundsMin, room.wanderBoundsMax);

            OnRoomChanged?.Invoke(room);
            Debug.Log($"[RoomManager] Switched to: {room.roomName}");
        }

        /// <summary>
        /// Cycle to the next unlocked room.
        /// </summary>
        public void NextRoom()
        {
            int currentIndex = GetRoomIndex(CurrentRoomId);
            for (int i = 1; i <= rooms.Length; i++)
            {
                int nextIndex = (currentIndex + i) % rooms.Length;
                if (IsRoomUnlocked(rooms[nextIndex].roomId))
                {
                    SwitchRoom(rooms[nextIndex].roomId);
                    return;
                }
            }
        }

        /// <summary>
        /// Cycle to the previous unlocked room.
        /// </summary>
        public void PreviousRoom()
        {
            int currentIndex = GetRoomIndex(CurrentRoomId);
            for (int i = 1; i <= rooms.Length; i++)
            {
                int prevIndex = (currentIndex - i + rooms.Length) % rooms.Length;
                if (IsRoomUnlocked(rooms[prevIndex].roomId))
                {
                    SwitchRoom(rooms[prevIndex].roomId);
                    return;
                }
            }
        }

        /// <summary>
        /// Unlock a room. Returns true if purchase was successful.
        /// </summary>
        public bool UnlockRoom(string roomId)
        {
            if (IsRoomUnlocked(roomId)) return true;

            RoomConfig room = GetRoom(roomId);
            Debug.Log(room);
            if (room == null) return false;

            if (PawCoinManager.Instance == null || !PawCoinManager.Instance.CanAfford(room.unlockCost))
            {
                Debug.Log($"[RoomManager] Can't afford room '{roomId}' ({room.unlockCost})");
                return false;
            }

            PawCoinManager.Instance.SpendCoins(room.unlockCost);
            _unlockedRoomIds.Add(roomId);

            if (GameManager.Instance?.Data != null)
                GameManager.Instance.Data.totalItemsPurchased++;

            // Check achievements
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.CheckAll();

            Debug.Log($"[RoomManager] Room unlocked: {room.roomName}!");
            return true;
        }

        public bool IsRoomUnlocked(string roomId) => _unlockedRoomIds.Contains(roomId);

        public RoomConfig GetRoom(string roomId)
        {
            foreach (var room in rooms)
                if (room.roomId == roomId) return room;
            return null;
        }

        private int GetRoomIndex(string roomId)
        {
            for (int i = 0; i < rooms.Length; i++)
                if (rooms[i].roomId == roomId) return i;
            return 0;
        }

        public int UnlockedRoomCount => _unlockedRoomIds.Count;
    }
}
