using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CatRaising.Core;
using CatRaising.Data;
using CatRaising.Interactables;
using CatRaising.Systems;

namespace CatRaising.UI
{
    /// <summary>
    /// Isometric furniture placement with click-to-lock behavior.
    /// 
    /// Flow: Select item → ghost follows cursor → click valid tile to LOCK ghost →
    ///       Confirm places furniture / Cancel releases ghost.
    /// 
    /// SETUP:
    /// 1. Create "FurniturePlacementPanel" with inventory ScrollView + confirm/cancel
    /// 2. For grid layout in inventory: use GridLayoutGroup (4 columns) on the ScrollView Content
    /// 3. Assign furniture prefab (SpriteRenderer + FurnitureInstance)
    /// 4. Ensure IsometricGrid is in scene
    /// </summary>
    public class FurniturePlacementUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject placementPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button removeButton;
        [SerializeField] private Transform inventoryContent; // Should have GridLayoutGroup
        [SerializeField] private GameObject inventorySlotPrefab;

        [Header("Placement")]
        [SerializeField] private GameObject furniturePrefab;
        [SerializeField] private Color validColor = new Color(0.5f, 1f, 0.5f, 0.7f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private Color lockedColor = new Color(0.7f, 1f, 0.7f, 0.9f);

        [Header("Tile Highlight (Optional)")]
        [SerializeField] private GameObject tileHighlightPrefab;

        [Header("References")]
        [SerializeField] private ShopUI shopUI;

        // States
        private enum PlacementState { Idle, Following, Locked }
        private PlacementState _state = PlacementState.Idle;

        private GameObject _ghostFurniture;
        private ShopUI.ShopItem _selectedItem;
        private Vector2Int _ghostGridPos;
        private List<GameObject> _placedFurnitureObjects = new List<GameObject>();
        private List<GameObject> _inventorySlots = new List<GameObject>();
        private List<GameObject> _tileHighlights = new List<GameObject>();

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (confirmButton != null) confirmButton.onClick.AddListener(ConfirmPlacement);
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelPlacement);
            if (removeButton != null) removeButton.onClick.AddListener(RemoveSelectedFurniture);

            if (placementPanel != null) placementPanel.SetActive(false);
            HidePlacementButtons();
        }

        private void Update()
        {
            if (_state == PlacementState.Following && _ghostFurniture != null)
            {
                UpdateGhostFollowing();

                // Click on a valid tile to lock
                if (Input.GetMouseButtonDown(0) && !TouchInput.IsOverUI)
                {
                    TryLockGhost();
                }
            }
        }

        public void Open()
        {
            if (placementPanel != null) placementPanel.SetActive(true);
            LoadPlacedFurniture();
            RefreshInventory();
        }

        public void Close()
        {
            CancelPlacement();
            if (placementPanel != null) placementPanel.SetActive(false);
        }

        // ─── Placement Flow ─────────────────────────────────────

        private void StartPlacing(ShopUI.ShopItem item)
        {
            CancelPlacement();
            _selectedItem = item;
            _state = PlacementState.Following;

            if (furniturePrefab != null)
            {
                _ghostFurniture = Instantiate(furniturePrefab);
                var sr = _ghostFurniture.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && item.itemSprite != null)
                {
                    sr.sprite = item.itemSprite;
                    sr.color = validColor;
                }

                var col = _ghostFurniture.GetComponentInChildren<Collider2D>();
                if (col != null) col.enabled = false;

                var fi = _ghostFurniture.GetComponentInChildren<FurnitureInstance>();
                if (fi != null) fi.enabled = false;
            }

            // Show cancel but NOT confirm yet
            if (cancelButton != null) cancelButton.gameObject.SetActive(true);
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Ghost follows cursor, snapping to isometric grid tiles.
        /// </summary>
        private void UpdateGhostFollowing()
        {
            if (_ghostFurniture == null || IsometricGrid.Instance == null) return;

            var grid = IsometricGrid.Instance;
            Vector2 worldPos = TouchInput.WorldPosition;
            Vector2Int cell = grid.WorldToGrid(worldPos);

            _ghostGridPos = cell;
            Vector3 anchorWorld = CalculateAnchorWorld(grid, cell);
            _ghostFurniture.transform.position = anchorWorld;

            bool valid = IsValidPlacement(cell);
            var sr = _ghostFurniture.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.color = valid ? validColor : invalidColor;

            UpdateTileHighlights(cell, valid);
        }

        /// <summary>
        /// Click on a valid tile to lock the ghost in place.
        /// </summary>
        private void TryLockGhost()
        {
            if (IsometricGrid.Instance == null || _selectedItem == null) return;

            Vector2Int cell = _ghostGridPos;

            if (IsValidPlacement(cell))
            {
                // Lock ghost at this position
                _state = PlacementState.Locked;

                var sr = _ghostFurniture.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    sr.color = lockedColor;

                // Now show both confirm and cancel
                if (confirmButton != null) confirmButton.gameObject.SetActive(true);
                if (cancelButton != null) cancelButton.gameObject.SetActive(true);

                Debug.Log($"[Furniture] Ghost locked at grid ({cell.x}, {cell.y}). Confirm or Cancel.");
            }
        }

        private void ConfirmPlacement()
        {
            if (_state != PlacementState.Locked || _ghostFurniture == null || _selectedItem == null) return;
            if (IsometricGrid.Instance == null) return;

            if (!IsValidPlacement(_ghostGridPos))
            {
                Debug.Log("[Furniture] Invalid — tiles occupied or out of bounds!");
                return;
            }

            var grid = IsometricGrid.Instance;
            grid.SetTilesOccupied(_ghostGridPos, _selectedItem.gridSize, true);

            var saveData = new FurnitureSaveData
            {
                itemId = _selectedItem.itemId,
                roomId = RoomManager.Instance?.CurrentRoomId ?? "living_room",
                gridCol = _ghostGridPos.x,
                gridRow = _ghostGridPos.y
            };

            if (GameManager.Instance?.Data != null)
                GameManager.Instance.Data.placedFurniture.Add(saveData);

            // Convert ghost to real furniture
            var sr = _ghostFurniture.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;

            var col = _ghostFurniture.GetComponentInChildren<Collider2D>();
            if (col != null) col.enabled = true;

            var fi = _ghostFurniture.GetComponentInChildren<FurnitureInstance>();
            if (fi != null)
            {
                fi.enabled = true;
                fi.Setup(_selectedItem.itemId, saveData.roomId, _selectedItem.itemSprite,
                         _selectedItem.catInteraction, _ghostGridPos, _selectedItem.gridSize);
            }

            _placedFurnitureObjects.Add(_ghostFurniture);

            Debug.Log($"[Furniture] Placed at grid ({_ghostGridPos.x}, {_ghostGridPos.y})");

            _ghostFurniture = null;
            _selectedItem = null;
            _state = PlacementState.Idle;

            ClearTileHighlights();
            HidePlacementButtons();

            if (AchievementManager.Instance != null)
                AchievementManager.Instance.CheckAll();

            RefreshInventory();
        }

        private void CancelPlacement()
        {
            if (_ghostFurniture != null)
            {
                Destroy(_ghostFurniture);
                _ghostFurniture = null;
            }
            _state = PlacementState.Idle;
            _selectedItem = null;

            ClearTileHighlights();
            HidePlacementButtons();
        }

        private void HidePlacementButtons()
        {
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        }

        private void RemoveSelectedFurniture()
        {
            Debug.Log("[Furniture] Remove mode: tap a placed furniture to pick it up.");
        }

        // ─── Helpers ────────────────────────────────────────────

        private Vector3 CalculateAnchorWorld(IsometricGrid grid, Vector2Int cell)
        {
            Vector3 anchor = grid.GridToWorld(cell);
            if (_selectedItem != null && (_selectedItem.gridSize.x > 1 || _selectedItem.gridSize.y > 1))
            {
                Vector3 endWorld = grid.GridToWorld(cell.x + _selectedItem.gridSize.x - 1,
                                                     cell.y + _selectedItem.gridSize.y - 1);
                anchor = (anchor + endWorld) * 0.5f;
            }
            return anchor;
        }

        private bool IsValidPlacement(Vector2Int cell)
        {
            if (IsometricGrid.Instance == null || _selectedItem == null) return false;
            return IsometricGrid.Instance.CanPlaceFurniture(cell, _selectedItem.gridSize);
        }

        private void UpdateTileHighlights(Vector2Int anchor, bool valid)
        {
            ClearTileHighlights();
            if (tileHighlightPrefab == null || IsometricGrid.Instance == null || _selectedItem == null) return;

            var grid = IsometricGrid.Instance;
            Color c = valid ? validColor : invalidColor;

            for (int col = anchor.x; col < anchor.x + _selectedItem.gridSize.x; col++)
                for (int row = anchor.y; row < anchor.y + _selectedItem.gridSize.y; row++)
                {
                    Vector3 pos = grid.GridToWorld(col, row);
                    var h = Instantiate(tileHighlightPrefab, pos, Quaternion.identity);
                    var sr = h.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = c;
                    _tileHighlights.Add(h);
                }
        }

        private void ClearTileHighlights()
        {
            foreach (var h in _tileHighlights)
                if (h != null) Destroy(h);
            _tileHighlights.Clear();
        }

        // ─── Loading & Spawning ─────────────────────────────────

        public void LoadPlacedFurniture()
        {
            foreach (var obj in _placedFurnitureObjects)
                if (obj != null) Destroy(obj);
            _placedFurnitureObjects.Clear();

            if (IsometricGrid.Instance != null)
                IsometricGrid.Instance.ClearAllOccupancy();

            if (GameManager.Instance?.Data == null || furniturePrefab == null) return;

            string currentRoom = RoomManager.Instance?.CurrentRoomId ?? "living_room";
            var grid = IsometricGrid.Instance;

            foreach (var save in GameManager.Instance.Data.placedFurniture)
            {
                if (save.roomId != currentRoom) continue;

                var item = shopUI?.GetItem(save.itemId);
                if (item == null) continue;

                Vector2Int cell = new Vector2Int(save.gridCol, save.gridRow);

                Vector3 worldPos;
                if (grid != null)
                {
                    worldPos = grid.GridToWorld(cell);
                    if (item.gridSize.x > 1 || item.gridSize.y > 1)
                    {
                        Vector3 endPos = grid.GridToWorld(cell.x + item.gridSize.x - 1,
                                                          cell.y + item.gridSize.y - 1);
                        worldPos = (worldPos + endPos) * 0.5f;
                    }
                    grid.SetTilesOccupied(cell, item.gridSize, true);
                }
                else
                {
                    worldPos = Vector3.zero;
                }

                var obj = Instantiate(furniturePrefab, worldPos, Quaternion.identity);
                var fi = obj.GetComponent<FurnitureInstance>();
                if (fi != null)
                    fi.Setup(save.itemId, save.roomId, item.itemSprite,
                             item.catInteraction, cell, item.gridSize);

                _placedFurnitureObjects.Add(obj);
            }
        }

        // ─── Inventory ──────────────────────────────────────────

        private void RefreshInventory()
        {
            foreach (var slot in _inventorySlots)
                if (slot != null) Destroy(slot);
            _inventorySlots.Clear();

            if (shopUI == null || inventoryContent == null || inventorySlotPrefab == null) return;

            var owned = shopUI.GetOwnedFurniture();
            foreach (var item in owned)
            {
                bool placed = IsItemPlacedInCurrentRoom(item.itemId);

                var slotObj = Instantiate(inventorySlotPrefab, inventoryContent);
                _inventorySlots.Add(slotObj);

                var texts = slotObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 1) texts[0].text = item.itemName;
                if (texts.Length >= 2) texts[1].text = placed ? "Placed" : "Tap to place";

                var btn = slotObj.GetComponent<Button>();
                if (btn != null && !placed)
                {
                    var capturedItem = item;
                    btn.onClick.AddListener(() => StartPlacing(capturedItem));
                }
            }
        }

        private bool IsItemPlacedInCurrentRoom(string itemId)
        {
            if (GameManager.Instance?.Data == null) return false;
            string currentRoom = RoomManager.Instance?.CurrentRoomId ?? "living_room";
            foreach (var save in GameManager.Instance.Data.placedFurniture)
                if (save.itemId == itemId && save.roomId == currentRoom) return true;
            return false;
        }
    }
}
