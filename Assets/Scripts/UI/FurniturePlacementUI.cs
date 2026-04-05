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
    /// Isometric furniture placement mode. Player selects owned furniture and places
    /// on the isometric grid. Furniture snaps to diamond tiles and can occupy multiple tiles.
    /// 
    /// SETUP:
    /// 1. Create a Panel "FurniturePlacementPanel" with inventory list + confirm/cancel buttons
    /// 2. Assign the furniture prefab (generic with FurnitureInstance + SpriteRenderer)
    /// 3. Ensure IsometricGrid is in the scene
    /// 4. Add a "Decorate" HUD button to call Open()
    /// </summary>
    public class FurniturePlacementUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject placementPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button removeButton;
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventorySlotPrefab;

        [Header("Placement")]
        [SerializeField] private GameObject furniturePrefab;
        [SerializeField] private Color validColor = new Color(0.5f, 1f, 0.5f, 0.7f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.5f, 0.5f, 0.7f);

        [Header("Tile Highlight (Optional)")]
        [Tooltip("Prefab for highlighting grid cells. Should be a diamond-shaped sprite.")]
        [SerializeField] private GameObject tileHighlightPrefab;

        [Header("References")]
        [SerializeField] private ShopUI shopUI;

        private bool _isPlacing = false;
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
        }

        private void Update()
        {
            if (_isPlacing && _ghostFurniture != null)
            {
                UpdateGhostPosition();
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
            _isPlacing = true;

            if (furniturePrefab != null)
            {
                _ghostFurniture = Instantiate(furniturePrefab);
                var sr = _ghostFurniture.GetComponent<SpriteRenderer>();
                if (sr != null && item.itemSprite != null)
                {
                    sr.sprite = item.itemSprite;
                    sr.color = validColor;
                }

                // Disable physics on ghost
                var col = _ghostFurniture.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                // Disable FurnitureInstance behavior on ghost
                var fi = _ghostFurniture.GetComponent<FurnitureInstance>();
                if (fi != null) fi.enabled = false;
            }

            if (confirmButton != null) confirmButton.gameObject.SetActive(true);
            if (cancelButton != null) cancelButton.gameObject.SetActive(true);
        }

        private void UpdateGhostPosition()
        {
            if (_ghostFurniture == null || IsometricGrid.Instance == null) return;

            var grid = IsometricGrid.Instance;
            Vector2 worldPos = TouchInput.WorldPosition;

            // Snap to nearest grid cell
            Vector2Int cell = grid.WorldToGrid(worldPos);
            _ghostGridPos = cell;

            // Position ghost at the grid cell center
            // For multi-tile furniture, we anchor at (col, row) and the sprite center
            // is offset to cover all tiles
            Vector3 anchorWorld = grid.GridToWorld(cell);

            if (_selectedItem != null && (_selectedItem.gridSize.x > 1 || _selectedItem.gridSize.y > 1))
            {
                // Calculate center of multi-tile footprint
                Vector3 endWorld = grid.GridToWorld(cell.x + _selectedItem.gridSize.x - 1,
                                                     cell.y + _selectedItem.gridSize.y - 1);
                anchorWorld = (anchorWorld + endWorld) * 0.5f;
            }

            _ghostFurniture.transform.position = anchorWorld;

            // Check validity
            bool valid = IsValidPlacement(cell);

            var sr = _ghostFurniture.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = valid ? validColor : invalidColor;

            // Update tile highlights
            UpdateTileHighlights(cell, valid);
        }

        private bool IsValidPlacement(Vector2Int cell)
        {
            if (IsometricGrid.Instance == null) return false;
            if (_selectedItem == null) return false;

            return IsometricGrid.Instance.CanPlaceFurniture(cell, _selectedItem.gridSize);
        }

        private void UpdateTileHighlights(Vector2Int anchor, bool valid)
        {
            ClearTileHighlights();

            if (tileHighlightPrefab == null || IsometricGrid.Instance == null || _selectedItem == null)
                return;

            var grid = IsometricGrid.Instance;
            Color highlightColor = valid ? validColor : invalidColor;

            for (int c = anchor.x; c < anchor.x + _selectedItem.gridSize.x; c++)
            {
                for (int r = anchor.y; r < anchor.y + _selectedItem.gridSize.y; r++)
                {
                    Vector3 pos = grid.GridToWorld(c, r);
                    var highlight = Instantiate(tileHighlightPrefab, pos, Quaternion.identity);

                    var sr = highlight.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = highlightColor;

                    _tileHighlights.Add(highlight);
                }
            }
        }

        private void ClearTileHighlights()
        {
            foreach (var h in _tileHighlights)
                if (h != null) Destroy(h);
            _tileHighlights.Clear();
        }

        private void ConfirmPlacement()
        {
            if (!_isPlacing || _ghostFurniture == null || _selectedItem == null) return;
            if (IsometricGrid.Instance == null) return;

            if (!IsValidPlacement(_ghostGridPos))
            {
                Debug.Log("[Furniture] Invalid position — tiles occupied or out of bounds!");
                return;
            }

            var grid = IsometricGrid.Instance;

            // Mark tiles as occupied
            grid.SetTilesOccupied(_ghostGridPos, _selectedItem.gridSize, true);

            // Save placement with grid coordinates
            var saveData = new FurnitureSaveData
            {
                itemId = _selectedItem.itemId,
                roomId = RoomManager.Instance?.CurrentRoomId ?? "living_room",
                gridCol = _ghostGridPos.x,
                gridRow = _ghostGridPos.y
            };

            if (GameManager.Instance?.Data != null)
                GameManager.Instance.Data.placedFurniture.Add(saveData);

            // Convert ghost to placed furniture
            var sr = _ghostFurniture.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;

            var col = _ghostFurniture.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            var fi = _ghostFurniture.GetComponent<FurnitureInstance>();
            if (fi != null)
            {
                fi.enabled = true;
                fi.Setup(_selectedItem.itemId, saveData.roomId, _selectedItem.itemSprite,
                         _selectedItem.catInteraction, _ghostGridPos, _selectedItem.gridSize);
            }

            _placedFurnitureObjects.Add(_ghostFurniture);
            _ghostFurniture = null;
            _isPlacing = false;
            _selectedItem = null;

            ClearTileHighlights();
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);

            if (AchievementManager.Instance != null)
                AchievementManager.Instance.CheckAll();

            Debug.Log($"[Furniture] Placed at grid ({_ghostGridPos.x}, {_ghostGridPos.y})");
            RefreshInventory();
        }

        private void CancelPlacement()
        {
            if (_ghostFurniture != null)
            {
                Destroy(_ghostFurniture);
                _ghostFurniture = null;
            }
            _isPlacing = false;
            _selectedItem = null;

            ClearTileHighlights();
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        }

        private void RemoveSelectedFurniture()
        {
            // TODO: implement tap-to-select and remove
            Debug.Log("[Furniture] Remove mode: tap a placed furniture to pick it up.");
        }

        // ─── Loading & Spawning ─────────────────────────────────

        /// <summary>
        /// Load placed furniture from save data and rebuild occupancy grid.
        /// </summary>
        public void LoadPlacedFurniture()
        {
            // Clear existing
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

                // Calculate world position
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

                    // Mark tiles occupied
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
