using UnityEngine;
using CatRaising.Cat;
using CatRaising.Core;

namespace CatRaising.Interactables
{
    /// <summary>
    /// Runtime component for placed furniture. Sits on the isometric grid
    /// and handles cat proximity interaction.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class FurnitureInstance : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string itemId;
        [SerializeField] private string roomId;
        [SerializeField] private FurnitureInteractionType interactionType;

        [Header("Grid Position")]
        [SerializeField] private Vector2Int gridPosition;  // Anchor cell (top-left)
        [SerializeField] private Vector2Int gridSize = Vector2Int.one;

        [Header("Cat Interaction")]
        [Tooltip("How close the cat must be to interact")]
        [SerializeField] private float interactionRange = 1.2f;
        [Tooltip("How long the cat interacts (seconds)")]
        [SerializeField] private float interactionDuration = 5f;
        [Tooltip("Happiness gained from interaction")]
        [SerializeField] private float happinessGain = 3f;
        [Tooltip("Cleanliness change (negative for scratch)")]
        [SerializeField] private float cleanlinessChange = 0f;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private CatController _catController;
        private CatNeeds _catNeeds;
        private float _interactionTimer;
        private bool _catIsInteracting;

        public string ItemId => itemId;
        public string RoomId => roomId;
        public Vector2Int GridPosition => gridPosition;
        public Vector2Int GridSize => gridSize;
        public FurnitureInteractionType InteractionType => interactionType;

        private void Start()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            _catController = FindAnyObjectByType<CatController>();
            _catNeeds = FindAnyObjectByType<CatNeeds>();
        }

        private void Update()
        {
            if (_catController == null || _catNeeds == null) return;
            if (interactionType == FurnitureInteractionType.None) return;

            float distance = Vector2.Distance(transform.position, _catController.transform.position);

            if (!_catIsInteracting && distance < interactionRange &&
                _catController.CurrentState == CatController.CatState.Idle)
            {
                if (Random.value < 0.002f)
                    StartInteraction();
            }

            if (_catIsInteracting)
            {
                _interactionTimer += Time.deltaTime;
                if (_interactionTimer >= interactionDuration)
                    EndInteraction();
            }
        }

        private void StartInteraction()
        {
            _catIsInteracting = true;
            _interactionTimer = 0f;

            if (_catController.CatAnimator != null)
                _catController.CatAnimator.FacePosition(transform.position);

            Debug.Log($"[Furniture] Cat interacting with {itemId} ({interactionType})");
        }

        private void EndInteraction()
        {
            _catIsInteracting = false;

            if (_catNeeds != null)
            {
                if (happinessGain != 0)
                    _catNeeds.IncreaseHappiness(happinessGain);
                if (cleanlinessChange > 0)
                    _catNeeds.IncreaseCleanliness(cleanlinessChange);
                else if (cleanlinessChange < 0)
                    _catNeeds.DecreaseCleanliness(-cleanlinessChange);
            }
        }

        /// <summary>
        /// Configure this instance at runtime (when spawned from placement).
        /// Overload with grid position and size for isometric placement.
        /// </summary>
        public void Setup(string id, string room, Sprite sprite, FurnitureInteractionType type,
                          Vector2Int gridPos, Vector2Int size)
        {
            itemId = id;
            roomId = room;
            interactionType = type;
            gridPosition = gridPos;
            gridSize = size;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null && sprite != null)
                spriteRenderer.sprite = sprite;

            // Set interaction parameters based on type
            switch (type)
            {
                case FurnitureInteractionType.SitOn:
                    happinessGain = 2f; cleanlinessChange = 0f; interactionDuration = 8f; break;
                case FurnitureInteractionType.SleepOn:
                    happinessGain = 5f; cleanlinessChange = 0f; interactionDuration = 15f; break;
                case FurnitureInteractionType.Scratch:
                    happinessGain = 4f; cleanlinessChange = -1f; interactionDuration = 4f; break;
                case FurnitureInteractionType.PlayWith:
                    happinessGain = 6f; cleanlinessChange = -0.5f; interactionDuration = 6f; break;
                case FurnitureInteractionType.HideIn:
                    happinessGain = 3f; cleanlinessChange = 0f; interactionDuration = 10f; break;
            }
        }

        /// <summary>
        /// Legacy overload without grid data (kept for compatibility).
        /// </summary>
        public void Setup(string id, string room, Sprite sprite, FurnitureInteractionType type)
        {
            Setup(id, room, sprite, type, Vector2Int.zero, Vector2Int.one);
        }

        /// <summary>
        /// Release occupied tiles on the grid when this furniture is removed.
        /// </summary>
        private void OnDestroy()
        {
            if (IsometricGrid.Instance != null && gridSize.x > 0 && gridSize.y > 0)
            {
                IsometricGrid.Instance.SetTilesOccupied(gridPosition, gridSize, false);
            }
        }
    }
}
