using UnityEngine;
using CatRaising.Cat;
using CatRaising.Core;
using CatRaising.Systems;

namespace CatRaising.Interactables
{
    /// <summary>
    /// Food bowl interactable. Player taps to fill it, cat walks over to eat.
    /// The bowl depletes as the cat eats and refills cat's hunger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FoodBowl : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CatController catController;
        [SerializeField] private CatNeeds catNeeds;

        [Header("Bowl Sprites")]
        [Tooltip("Sprite when the bowl is empty")]
        [SerializeField] private Sprite emptySprite;
        [Tooltip("Sprite when the bowl is full")]
        [SerializeField] private Sprite fullSprite;

        [Header("Settings")]
        [Tooltip("How much food is added when the player taps")]
        [SerializeField] private float fillAmount = 100f;
        [Tooltip("How fast the cat eats (food units per second)")]
        [SerializeField] private float eatSpeed = 15f;
        [Tooltip("How much hunger is restored per food unit eaten")]
        [SerializeField] private float hungerPerFoodUnit = 1f;
        [Tooltip("Bond gained each time you feed the cat")]
        [SerializeField] private float bondPerFeed = 1f;
        [Tooltip("Distance the cat must be to start eating")]
        [SerializeField] private float eatDistance = 1f;
        [Tooltip("Duration cat spends eating (seconds)")]
        [SerializeField] private float eatDuration = 5f;

        [Header("Isometric Grid")]
        [Tooltip("Grid cell this bowl occupies")]
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private Vector2Int gridSize = Vector2Int.one;

        [Header("Feedback")]
        [SerializeField] private GameObject fillEffectPrefab;

        // State
        private float _foodAmount = 0f;
        private bool _catIsEating = false;
        private float _eatTimer = 0f;
        private Camera _mainCamera;

        public float FoodAmount => _foodAmount;
        public bool IsFull => _foodAmount > 0f;
        public bool IsEmpty => _foodAmount <= 0f;

        public Vector2Int GridPosition => gridPosition;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (GameManager.Instance != null && GameManager.Instance.Data != null)
                _foodAmount = GameManager.Instance.Data.foodBowlAmount;

            UpdateSprite();

            // Register on isometric grid as occupied (cat walks near, not on)
            if (IsometricGrid.Instance != null)
                IsometricGrid.Instance.SetTilesOccupied(gridPosition, gridSize, true);
        }

        private void OnDestroy()
        {
            if (IsometricGrid.Instance != null)
                IsometricGrid.Instance.SetTilesOccupied(gridPosition, gridSize, false);
        }

        /// <summary>
        /// Get the world position of the nearest walkable tile adjacent to this bowl.
        /// The cat should walk here to eat.
        /// </summary>
        public Vector3 GetAdjacentWalkablePosition()
        {
            if (IsometricGrid.Instance == null)
                return transform.position + Vector3.left * 0.5f;

            var grid = IsometricGrid.Instance;
            // Check all adjacent tiles around the bowl footprint
            Vector2Int[] offsets = {
                new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
                new(-1, -1), new(1, 1), new(-1, 1), new(1, -1), new (0, 0)
            };
            /*
            foreach (var offset in offsets)
            {
                Vector2Int adjacent = gridPosition + offset;
                if (grid.IsTileWalkable(adjacent))
                    return grid.GridToWorld(adjacent);
            }
            */
            return transform.position + Vector3.left * 0.5f;
        }

        private void Update()
        {
            HandleInput();
            HandleCatEating();
        }

        /// <summary>
        /// Handle tap-to-fill input.
        /// </summary>
        private void HandleInput()
        {
            if (TouchInput.WasPressedThisFrame)
            {
                if (IsTouchingBowl() && IsEmpty)
                {
                    FillBowl();
                }
            }
        }

        /// <summary>
        /// Check if the touch/click hits this bowl.
        /// </summary>
        private bool IsTouchingBowl()
        {
            return TouchInput.IsOverGameObject(gameObject);
        }

        /// <summary>
        /// Fill the bowl with food.
        /// </summary>
        public void FillBowl()
        {
            _foodAmount = fillAmount;
            UpdateSprite();

            // Bond gain for feeding
            if (BondSystem.Instance != null)
                BondSystem.Instance.AddBond(bondPerFeed, "feeding");

            if (GameManager.Instance != null && GameManager.Instance.Data != null)
                GameManager.Instance.Data.totalFeedings++;

            // Daily task hook
            if (DailyTaskManager.Instance != null)
                DailyTaskManager.Instance.CheckTask(DailyTaskType.FeedCat);

            // Achievement check
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.CheckAll();

            // Spawn fill effect
            if (fillEffectPrefab != null)
                Instantiate(fillEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

            // Play meow sound when bowl is filled
            if (Systems.SoundEffectHooks.Instance != null)
                Systems.SoundEffectHooks.Instance.PlaySound("meow");

            Debug.Log("[FoodBowl] Bowl filled!");
        }

        /// <summary>
        /// Handle the cat eating from the bowl.
        /// Called when the cat is in range and food is available.
        /// </summary>
        private void HandleCatEating()
        {
            if (_foodAmount <= 0f || catController == null || catNeeds == null)
            {
                if (_catIsEating)
                {
                    StopEating();
                }
                return;
            }

            // Check if cat is close enough to eat
            float distance = Vector2.Distance(transform.position, catController.transform.position);

            if (distance < eatDistance && !_catIsEating && catNeeds.IsHungry)
            {
                StartEating();
            }

            if (_catIsEating)
            {
                _eatTimer += Time.deltaTime;

                // Deplete food
                float eaten = eatSpeed * Time.deltaTime;
                _foodAmount = Mathf.Max(0f, _foodAmount - eaten);

                // Restore hunger
                catNeeds.Feed(eaten * hungerPerFoodUnit);

                // Update sprite
                UpdateSprite();

                // Save bowl state
                if (GameManager.Instance != null && GameManager.Instance.Data != null)
                    GameManager.Instance.Data.foodBowlAmount = _foodAmount;

                // Stop eating after duration or if empty
                if (_eatTimer >= eatDuration || _foodAmount <= 0f)
                {
                    StopEating();
                }
            }
        }

        private void StartEating()
        {
            _catIsEating = true;
            _eatTimer = 0f;
            catController.RequestState(CatController.CatState.Eating);
            Debug.Log("[FoodBowl] Cat started eating.");
        }

        private void StopEating()
        {
            _catIsEating = false;
            _eatTimer = 0f;
            if (catController.CurrentState == CatController.CatState.Eating)
                catController.RequestState(CatController.CatState.Idle);
            Debug.Log("[FoodBowl] Cat finished eating.");
        }

        /// <summary>
        /// Update the bowl's sprite based on fullness.
        /// </summary>
        private void UpdateSprite()
        {
            if (spriteRenderer == null) return;

            if (_foodAmount > 0f && fullSprite != null)
                spriteRenderer.sprite = fullSprite;
            else if (emptySprite != null)
                spriteRenderer.sprite = emptySprite;
        }
    }
}
