using System.Collections.Generic;
using UnityEngine;

namespace CatRaising.Core
{
    /// <summary>
    /// Isometric grid system for furniture placement and cat navigation.
    /// 
    /// Uses diamond-projection isometric coordinates:
    ///   - Grid cell (col, row) maps to a diamond-shaped tile in world space
    ///   - tileWidth = horizontal span of one diamond tile
    ///   - tileHeight = vertical span (typically tileWidth / 2 for 2:1 iso)
    ///   - gridOrigin = world position of tile (0, 0) center
    /// 
    /// SETUP:
    /// 1. Add this component to a "Grid" GameObject in your scene
    /// 2. Set gridWidth/gridHeight to match your room floor area
    /// 3. Set tileWidth/tileHeight to match your pixel art tile dimensions in world units
    ///    Example: 64×32 pixel tiles at 100 PPU → tileWidth=0.64, tileHeight=0.32
    /// 4. Set gridOrigin to the world position where tile (0,0) should be
    /// 5. Enable "Show Gizmos" to visualize the grid in Scene view
    /// </summary>
    public class IsometricGrid : MonoBehaviour
    {
        public static IsometricGrid Instance { get; private set; }

        [Header("Grid Dimensions (in tiles)")]
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 8;

        [Header("Tile Size (world units)")]
        [Tooltip("Horizontal span of one diamond tile in world units")]
        [SerializeField] private float tileWidth = 1f;
        [Tooltip("Vertical span of one diamond tile (usually tileWidth / 2)")]
        [SerializeField] private float tileHeight = 0.5f;

        [Header("Position")]
        [Tooltip("World position of tile (0, 0) center")]
        [SerializeField] private Vector2 gridOrigin = Vector2.zero;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoGridColor = new Color(0.3f, 0.6f, 1f, 0.3f);
        [SerializeField] private Color gizmoOccupiedColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color gizmoWalkableColor = new Color(0.3f, 1f, 0.3f, 0.15f);

        // Occupancy: true = tile is blocked by furniture
        private bool[,] _occupied;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float TileWidth => tileWidth;
        public float TileHeight => tileHeight;
        public Vector2 GridOrigin => gridOrigin;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _occupied = new bool[gridWidth, gridHeight];

            Debug.Log(GridToWorld(4, 0)); // Debug: log world position of tile (0,0)
        }

        // ─── Coordinate Conversion ──────────────────────────────

        /// <summary>
        /// Convert grid cell (col, row) to world position (center of the diamond tile).
        /// </summary>
        public Vector3 GridToWorld(int col, int row)
        {
            float worldX = gridOrigin.x + (col - row) * (tileWidth * 0.5f);
            float worldY = gridOrigin.y - (col + row) * (tileHeight * 0.5f);
            return new Vector3(worldX, worldY, 0f);
        }

        /// <summary>
        /// Convert grid cell to world position using Vector2Int.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int cell)
        {
            return GridToWorld(cell.x, cell.y);
        }

        /// <summary>
        /// Convert world position to the nearest grid cell (col, row).
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float dx = worldPos.x - gridOrigin.x;
            float dy = -(worldPos.y - gridOrigin.y); // Flip Y because iso Y goes down

            // Inverse of the iso projection:
            //   dx = (col - row) * halfW
            //   dy = (col + row) * halfH
            float halfW = tileWidth * 0.5f;
            float halfH = tileHeight * 0.5f;

            float fCol = (dx / halfW + dy / halfH) * 0.5f;
            float fRow = (dy / halfH - dx / halfW) * 0.5f;

            int col = Mathf.RoundToInt(fCol);
            int row = Mathf.RoundToInt(fRow);

            return new Vector2Int(col, row);
        }

        /// <summary>
        /// Snap a world position to the nearest grid cell center.
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPos)
        {
            Vector2Int cell = WorldToGrid(worldPos);
            return GridToWorld(cell);
        }

        // ─── Bounds Checking ────────────────────────────────────

        /// <summary>
        /// Check if a grid cell is within the grid bounds.
        /// </summary>
        public bool IsInBounds(int col, int row)
        {
            return col >= 0 && col < gridWidth && row >= 0 && row < gridHeight;
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return IsInBounds(cell.x, cell.y);
        }

        // ─── Occupancy ──────────────────────────────────────────

        /// <summary>
        /// Check if a tile is walkable (in bounds and not occupied).
        /// </summary>
        public bool IsTileWalkable(int col, int row)
        {
            if (!IsInBounds(col, row)) return false;
            return !_occupied[col, row];
        }

        public bool IsTileWalkable(Vector2Int cell)
        {
            return IsTileWalkable(cell.x, cell.y);
        }

        /// <summary>
        /// Check if furniture of given size can be placed at (col, row).
        /// The anchor is the top-left cell of the furniture footprint.
        /// </summary>
        public bool CanPlaceFurniture(int col, int row, int sizeCols, int sizeRows)
        {
            for (int c = col; c < col + sizeCols; c++)
            {
                for (int r = row; r < row + sizeRows; r++)
                {
                    if (!IsInBounds(c, r) || _occupied[c, r])
                        return false;
                }
            }
            return true;
        }

        public bool CanPlaceFurniture(Vector2Int cell, Vector2Int size)
        {
            return CanPlaceFurniture(cell.x, cell.y, size.x, size.y);
        }

        /// <summary>
        /// Mark tiles as occupied or free for furniture placement.
        /// </summary>
        public void SetTilesOccupied(int col, int row, int sizeCols, int sizeRows, bool occupied)
        {
            for (int c = col; c < col + sizeCols; c++)
            {
                for (int r = row; r < row + sizeRows; r++)
                {
                    if (IsInBounds(c, r))
                        _occupied[c, r] = occupied;
                }
            }
        }

        public void SetTilesOccupied(Vector2Int cell, Vector2Int size, bool occupied)
        {
            SetTilesOccupied(cell.x, cell.y, size.x, size.y, occupied);
        }

        /// <summary>
        /// Clear all occupancy (call when switching rooms).
        /// </summary>
        public void ClearAllOccupancy()
        {
            _occupied = new bool[gridWidth, gridHeight];
        }

        // ─── Cat AI Helpers ─────────────────────────────────────

        /// <summary>
        /// Get all walkable tile positions.
        /// </summary>
        public List<Vector2Int> GetAllWalkableTiles()
        {
            var result = new List<Vector2Int>();
            for (int c = 0; c < gridWidth; c++)
                for (int r = 0; r < gridHeight; r++)
                    if (!_occupied[c, r])
                        result.Add(new Vector2Int(c, r));
            return result;
        }

        /// <summary>
        /// Get a random walkable world position for the cat to wander to.
        /// </summary>
        public Vector3 GetRandomWalkablePosition()
        {
            var walkable = GetAllWalkableTiles();
            if (walkable.Count == 0)
            {
                // Fallback: grid center
                return GridToWorld(gridWidth / 2, gridHeight / 2);
            }
            var cell = walkable[Random.Range(0, walkable.Count)];
            return GridToWorld(cell);
        }

        /// <summary>
        /// Check if a world position is on a walkable tile.
        /// </summary>
        public bool IsWorldPositionWalkable(Vector3 worldPos)
        {
            Vector2Int cell = WorldToGrid(worldPos);
            return IsTileWalkable(cell);
        }

        // ─── Grid Info ──────────────────────────────────────────

        /// <summary>
        /// Get the world-space bounding box of the entire grid (axis-aligned).
        /// </summary>
        public Bounds GetWorldBounds()
        {
            // The 4 corners of the iso diamond grid
            Vector3 topCorner = GridToWorld(0, 0);
            Vector3 rightCorner = GridToWorld(gridWidth - 1, 0);
            Vector3 bottomCorner = GridToWorld(gridWidth - 1, gridHeight - 1);
            Vector3 leftCorner = GridToWorld(0, gridHeight - 1);

            float minX = Mathf.Min(topCorner.x, Mathf.Min(rightCorner.x, Mathf.Min(bottomCorner.x, leftCorner.x)));
            float maxX = Mathf.Max(topCorner.x, Mathf.Max(rightCorner.x, Mathf.Max(bottomCorner.x, leftCorner.x)));
            float minY = Mathf.Min(topCorner.y, Mathf.Min(rightCorner.y, Mathf.Min(bottomCorner.y, leftCorner.y)));
            float maxY = Mathf.Max(topCorner.y, Mathf.Max(rightCorner.y, Mathf.Max(bottomCorner.y, leftCorner.y)));

            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            Vector3 size = new Vector3(maxX - minX + tileWidth, maxY - minY + tileHeight, 0.1f);
            return new Bounds(center, size);
        }

        // ─── Gizmos ─────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            for (int c = 0; c < gridWidth; c++)
            {
                for (int r = 0; r < gridHeight; r++)
                {
                    Vector3 center = GridToWorld(c, r);

                    bool occupied = _occupied != null && c < _occupied.GetLength(0) && r < _occupied.GetLength(1) && _occupied[c, r];

                    Gizmos.color = occupied ? gizmoOccupiedColor : gizmoWalkableColor;

                    // Draw diamond shape
                    Vector3 top = center + new Vector3(0, tileHeight * 0.5f, 0);
                    Vector3 right = center + new Vector3(tileWidth * 0.5f, 0, 0);
                    Vector3 bottom = center + new Vector3(0, -tileHeight * 0.5f, 0);
                    Vector3 left = center + new Vector3(-tileWidth * 0.5f, 0, 0);

                    Gizmos.DrawLine(top, right);
                    Gizmos.DrawLine(right, bottom);
                    Gizmos.DrawLine(bottom, left);
                    Gizmos.DrawLine(left, top);

                    // Fill occupied tiles
                    if (occupied)
                    {
                        Gizmos.color = gizmoOccupiedColor;
                        Gizmos.DrawCube(center, new Vector3(tileWidth * 0.3f, tileHeight * 0.3f, 0));
                    }
                }
            }

            // Draw grid outline
            Gizmos.color = gizmoGridColor;
            Vector3 tl = GridToWorld(0, 0) + new Vector3(0, tileHeight * 0.5f, 0);
            Vector3 tr = GridToWorld(gridWidth - 1, 0) + new Vector3(tileWidth * 0.5f, 0, 0);
            Vector3 br = GridToWorld(gridWidth - 1, gridHeight - 1) + new Vector3(0, -tileHeight * 0.5f, 0);
            Vector3 bl = GridToWorld(0, gridHeight - 1) + new Vector3(-tileWidth * 0.5f, 0, 0);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);
        }
    }
}
