using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Core;
using CatRaising.Systems;
using CatRaising.Interactables;

namespace CatRaising.UI
{
    /// <summary>
    /// Shop panel UI. Displays items for purchase in category tabs.
    /// 
    /// SETUP:
    /// 1. Create a Panel "ShopPanel" under Canvas
    /// 2. Add tabs for Rooms/Furniture, a ScrollView with content area
    /// 3. Create an item slot prefab (ShopItemSlotUI)
    /// 4. Assign references in Inspector
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Serializable]
        public class ShopItem
        {
            public string itemId;
            public string itemName;
            public string description;
            public Sprite icon;
            public Sprite itemSprite; // The actual furniture sprite to place
            public int cost;
            public ShopCategory category;
            public string targetRoomId; // For furniture: which room
            public FurnitureInteractionType catInteraction;
            [Tooltip("How many isometric grid tiles this furniture occupies (cols × rows)")]
            public Vector2Int gridSize = Vector2Int.one; // e.g. (1,1), (2,1), (2,2)
        }

        [Header("Shop Catalog")]
        [SerializeField] private ShopItem[] catalog;

        [Header("UI References")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Transform itemContainer; // ScrollView content
        [SerializeField] private GameObject itemSlotPrefab; // Prefab with ShopItemSlotUI
        [SerializeField] private TextMeshProUGUI coinDisplay;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button tabRoomsButton;
        [SerializeField] private Button tabFurnitureButton;

        [Header("Confirm Dialog")]
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private TextMeshProUGUI confirmText;
        [SerializeField] private Button confirmBuyButton;
        [SerializeField] private Button confirmCancelButton;

        private ShopCategory _currentTab = ShopCategory.Furniture;
        private ShopItem _pendingPurchase;
        private List<GameObject> _spawnedSlots = new List<GameObject>();

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (tabRoomsButton != null) tabRoomsButton.onClick.AddListener(() => ShowTab(ShopCategory.Room));
            if (tabFurnitureButton != null) tabFurnitureButton.onClick.AddListener(() => ShowTab(ShopCategory.Furniture));
            if (confirmBuyButton != null) confirmBuyButton.onClick.AddListener(ConfirmPurchase);
            if (confirmCancelButton != null) confirmCancelButton.onClick.AddListener(CancelPurchase);

            if (shopPanel != null) shopPanel.SetActive(false);
            if (confirmDialog != null) confirmDialog.SetActive(false);

            if (PawCoinManager.Instance != null)
                PawCoinManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
        }

        private void OnDestroy()
        {
            if (PawCoinManager.Instance != null)
                PawCoinManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
        }

        public void Open()
        {
            if (shopPanel != null) shopPanel.SetActive(true);
            ShowTab(_currentTab);
            UpdateCoinDisplay(PawCoinManager.Instance?.Coins ?? 0);
        }

        public void Close()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
        }

        public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

        private void ShowTab(ShopCategory category)
        {
            _currentTab = category;
            ClearSlots();

            foreach (var item in catalog)
            {
                if (item.category != category) continue;

                bool owned = IsOwned(item);
                bool canAfford = PawCoinManager.Instance != null && PawCoinManager.Instance.CanAfford(item.cost);

                var slotObj = Instantiate(itemSlotPrefab, itemContainer);
                _spawnedSlots.Add(slotObj);

                var slot = slotObj.GetComponent<ShopItemSlotUI>();
                if (slot != null)
                {
                    slot.Setup(item, owned, canAfford, OnItemClicked);
                }
            }
        }

        private void ClearSlots()
        {
            foreach (var slot in _spawnedSlots)
                if (slot != null) Destroy(slot);
            _spawnedSlots.Clear();
        }

        private void OnItemClicked(ShopItem item)
        {
            if (IsOwned(item)) return;

            _pendingPurchase = item;
            if (confirmDialog != null)
            {
                confirmDialog.SetActive(true);
                if (confirmText != null)
                    confirmText.text = $"Buy {item.itemName}\nfor {item.cost} 🐾?";
            }
        }

        private void ConfirmPurchase()
        {
            if (confirmDialog != null) confirmDialog.SetActive(false);
            if (_pendingPurchase == null) return;

            var item = _pendingPurchase;
            _pendingPurchase = null;

            if (item.category == ShopCategory.Room)
            {
                if (RoomManager.Instance != null && RoomManager.Instance.UnlockRoom(item.itemId))
                {
                    Debug.Log($"[Shop] Purchased room: {item.itemName}");
                    ShowTab(_currentTab); // Refresh
                }
            }
            else if (item.category == ShopCategory.Furniture)
            {
                if (PawCoinManager.Instance != null && PawCoinManager.Instance.SpendCoins(item.cost))
                {
                    if (GameManager.Instance?.Data != null)
                    {
                        GameManager.Instance.Data.ownedFurnitureIds.Add(item.itemId);
                        GameManager.Instance.Data.totalItemsPurchased++;
                    }
                    Debug.Log($"[Shop] Purchased furniture: {item.itemName}");

                    if (AchievementManager.Instance != null)
                        AchievementManager.Instance.CheckAll();

                    ShowTab(_currentTab); // Refresh
                }
            }
        }

        private void CancelPurchase()
        {
            _pendingPurchase = null;
            if (confirmDialog != null) confirmDialog.SetActive(false);
        }

        private bool IsOwned(ShopItem item)
        {
            if (GameManager.Instance?.Data == null) return false;

            if (item.category == ShopCategory.Room)
                return RoomManager.Instance != null && RoomManager.Instance.IsRoomUnlocked(item.itemId);

            return GameManager.Instance.Data.ownedFurnitureIds.Contains(item.itemId);
        }

        private void UpdateCoinDisplay(int coins)
        {
            if (coinDisplay != null)
                coinDisplay.text = $"{coins} 🐾";
        }

        /// <summary>
        /// Get a shop item by ID (for furniture placement).
        /// </summary>
        public ShopItem GetItem(string itemId)
        {
            foreach (var item in catalog)
                if (item.itemId == itemId) return item;
            return null;
        }

        public ShopItem[] GetOwnedFurniture()
        {
            if (GameManager.Instance?.Data == null) return Array.Empty<ShopItem>();
            var result = new List<ShopItem>();
            foreach (var item in catalog)
            {
                if (item.category == ShopCategory.Furniture &&
                    GameManager.Instance.Data.ownedFurnitureIds.Contains(item.itemId))
                    result.Add(item);
            }
            return result.ToArray();
        }
    }
}
