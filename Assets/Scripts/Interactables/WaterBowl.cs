using UnityEngine;
using CatRaising.Cat;
using CatRaising.Core;
using CatRaising.Systems;

namespace CatRaising.Interactables
{
    /// <summary>
    /// Water bowl interactable. Player taps to fill it, cat walks over to drink.
    /// Functions similarly to FoodBowl but restores thirst.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaterBowl : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CatController catController;
        [SerializeField] private CatNeeds catNeeds;

        [Header("Bowl Sprites")]
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Sprite fullSprite;

        [Header("Settings")]
        [SerializeField] private float fillAmount = 100f;
        [SerializeField] private float drinkSpeed = 12f;
        [SerializeField] private float thirstPerWaterUnit = 1f;
        [SerializeField] private float bondPerWater = 0.8f;
        [SerializeField] private float drinkDistance = 1f;
        [SerializeField] private float drinkDuration = 4f;

        [Header("Isometric Grid")]
        [Tooltip("Grid cell this bowl occupies")]
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private Vector2Int gridSize = Vector2Int.one;

        [Header("Feedback")]
        [SerializeField] private GameObject fillEffectPrefab;

        private float _waterAmount = 0f;
        private bool _catIsDrinking = false;
        private float _drinkTimer = 0f;
        private Camera _mainCamera;

        public float WaterAmount => _waterAmount;
        public bool IsFull => _waterAmount > 0f;
        public bool IsEmpty => _waterAmount <= 0f;

        public Vector2Int GridPosition => gridPosition;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (GameManager.Instance != null && GameManager.Instance.Data != null)
                _waterAmount = GameManager.Instance.Data.waterBowlAmount;

            UpdateSprite();

            // Register on isometric grid
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
        /// </summary>
        public Vector3 GetAdjacentWalkablePosition()
        {
            if (IsometricGrid.Instance == null)
                return transform.position + Vector3.left * 0.5f;

            var grid = IsometricGrid.Instance;
            Vector2Int[] offsets = {
                new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
                new(-1, -1), new(1, 1), new(-1, 1), new(1, -1)
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
            HandleCatDrinking();
        }

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

        private bool IsTouchingBowl()
        {
            return TouchInput.IsOverGameObject(gameObject);
        }

        public void FillBowl()
        {
            _waterAmount = fillAmount;
            UpdateSprite();

            if (BondSystem.Instance != null)
                BondSystem.Instance.AddBond(bondPerWater, "watering");

            // Daily task hook
            if (DailyTaskManager.Instance != null)
                DailyTaskManager.Instance.CheckTask(DailyTaskType.GiveWater);

            // Achievement check
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.CheckAll();

            if (fillEffectPrefab != null)
                Instantiate(fillEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

            // Play meow sound when bowl is filled
            if (SoundEffectHooks.Instance != null)
                SoundEffectHooks.Instance.PlaySound("meow");

            Debug.Log("[WaterBowl] Bowl filled!");
        }

        private void HandleCatDrinking()
        {
            if (_waterAmount <= 0f || catController == null || catNeeds == null)
            {
                if (_catIsDrinking) StopDrinking();
                return;
            }

            float distance = Vector2.Distance(transform.position, catController.transform.position);

            if (distance < drinkDistance && !_catIsDrinking && catNeeds.IsThirsty)
            {
                StartDrinking();
            }

            if (_catIsDrinking)
            {
                _drinkTimer += Time.deltaTime;

                float drunk = drinkSpeed * Time.deltaTime;
                _waterAmount = Mathf.Max(0f, _waterAmount - drunk);
                catNeeds.GiveWater(drunk * thirstPerWaterUnit);

                UpdateSprite();

                if (GameManager.Instance != null && GameManager.Instance.Data != null)
                    GameManager.Instance.Data.waterBowlAmount = _waterAmount;

                if (_drinkTimer >= drinkDuration || _waterAmount <= 0f)
                {
                    StopDrinking();
                }
            }
        }

        private void StartDrinking()
        {
            _catIsDrinking = true;
            _drinkTimer = 0f;
            catController.RequestState(CatController.CatState.Drinking);
            Debug.Log("[WaterBowl] Cat started drinking.");
        }

        private void StopDrinking()
        {
            _catIsDrinking = false;
            _drinkTimer = 0f;
            if (catController.CurrentState == CatController.CatState.Drinking)
                catController.RequestState(CatController.CatState.Idle);
            Debug.Log("[WaterBowl] Cat finished drinking.");
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null) return;

            if (_waterAmount > 0f && fullSprite != null)
                spriteRenderer.sprite = fullSprite;
            else if (emptySprite != null)
                spriteRenderer.sprite = emptySprite;
        }
    }
}
