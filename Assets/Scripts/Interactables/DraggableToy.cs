using UnityEngine;
using CatRaising.Cat;
using CatRaising.Systems;
using CatRaising.Core;

namespace CatRaising.Interactables
{
    /// <summary>
    /// Feather toy that the player drags around for the cat to chase.
    /// Only active when ToolModeManager is in FeatherToy mode.
    /// 
    /// In FeatherToy mode: any touch on empty space (not on cat/bowls) activates
    /// the feather at the touch position. The cat chases it.
    /// 
    /// Effects while playing:
    /// - Happiness increases
    /// - Hunger decreases (playing burns energy)
    /// - Cleanliness decreases (rolling around)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DraggableToy : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CatController catController;
        [SerializeField] private CatNeeds catNeeds;

        [Header("Drag Settings")]
        [SerializeField] private float dragZOffset = -1f;
        [SerializeField] private float returnSpeed = 3f;

        [Header("Cat Chase Settings")]
        [Tooltip("How close the cat chases toward the toy")]
        [SerializeField] private float chaseStopDistance = 0.8f;
        [SerializeField] private float catChaseSpeed = 3f;
        [Tooltip("Happiness gained per second while playing")]
        [SerializeField] private float happinessPerSecond = 5f;
        [Tooltip("Bond gained per play session")]
        [SerializeField] private float bondPerSession = 1.5f;
        [Tooltip("Minimum drag time to count as a play session")]
        [SerializeField] private float minPlayTime = 2f;

        [Header("Need Drain While Playing")]
        [Tooltip("Hunger lost per second while playing")]
        [SerializeField] private float hungerDrainPerSecond = 1f;
        [Tooltip("Cleanliness lost per second while playing")]
        [SerializeField] private float cleanlinessDrainPerSecond = 0.5f;

        [Header("Effects")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private GameObject pounceEffectPrefab;

        // State
        private bool _isDragging = false;
        private Vector3 _restPosition;
        private float _playTimer = 0f;
        private bool _catIsChasing = false;
        private bool _sessionBondAwarded = false;

        private void Start()
        {
            _restPosition = transform.position;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer != null) trailRenderer.emitting = false;

            // Start hidden if in hand mode
            UpdateVisibility();

            if (ToolModeManager.Instance != null)
                ToolModeManager.Instance.OnModeChanged += OnModeChanged;
        }

        private void OnDestroy()
        {
            if (ToolModeManager.Instance != null)
                ToolModeManager.Instance.OnModeChanged -= OnModeChanged;
        }

        private void Update()
        {
            bool isFeatherMode = ToolModeManager.Instance == null || ToolModeManager.Instance.IsFeatherMode;

            if (isFeatherMode)
            {
                HandleDragInput();
            }
            else if (_isDragging)
            {
                // Switched to hand mode while dragging — stop
                StopDrag();
            }

            if (_isDragging)
            {
                UpdateDrag();
                UpdateCatChase();
            }
            else if (!IsAtRestPosition() && !isFeatherMode)
            {
                ReturnToRest();
            }
        }

        /// <summary>
        /// Handle touch input. In FeatherToy mode, any touch on empty space
        /// (not hitting cat or bowls) starts the drag.
        /// </summary>
        private void HandleDragInput()
        {
            if (TouchInput.WasPressedThisFrame)
            {
                // Don't process if touch is over UI
                if (TouchInput.IsOverUI) return;

                // Check if touch is on an interactable (cat, bowl) — if so, let that handle it
                Collider2D hit = Physics2D.OverlapPoint(TouchInput.WorldPosition);

                // Allow drag if: hitting the toy itself, OR hitting nothing (empty space)
                bool isEmptySpace = (hit == null);
                bool isHittingToy = (hit != null && hit.gameObject == gameObject);

                if (isEmptySpace || isHittingToy)
                {
                    // Move toy to touch position and start drag
                    Vector3 worldPos = TouchInput.WorldPosition;
                    worldPos.z = dragZOffset;
                    transform.position = worldPos;
                    StartDrag();
                }
            }

            if (TouchInput.WasReleasedThisFrame && _isDragging)
            {
                StopDrag();
            }
        }

        private void StartDrag()
        {
            _isDragging = true;
            _playTimer = 0f;
            _sessionBondAwarded = false;

            // Show the toy
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (trailRenderer != null) trailRenderer.emitting = true;

            // Tell the cat to start playing
            if (catController != null)
                catController.RequestState(CatController.CatState.Playing);

            _catIsChasing = true;

            Debug.Log("[DraggableToy] Drag started!");
        }

        private void UpdateDrag()
        {
            Vector3 worldPos = TouchInput.WorldPosition;
            worldPos.z = dragZOffset;
            transform.position = worldPos;

            _playTimer += Time.deltaTime;

            if (catNeeds != null)
            {
                // Happiness increases while playing
                catNeeds.IncreaseHappiness(happinessPerSecond * Time.deltaTime);

                // Hunger decreases (playing burns energy)
                catNeeds.DecreaseHunger(hungerDrainPerSecond * Time.deltaTime);

                // Cleanliness decreases (rolling around getting dirty)
                catNeeds.DecreaseCleanliness(cleanlinessDrainPerSecond * Time.deltaTime);
            }

            // Award bond after minimum play time
            if (!_sessionBondAwarded && _playTimer >= minPlayTime)
            {
                if (BondSystem.Instance != null)
                    BondSystem.Instance.AddBond(bondPerSession, "playing");

                if (GameManager.Instance != null && GameManager.Instance.Data != null)
                    GameManager.Instance.Data.totalPlays++;

                _sessionBondAwarded = true;
            }
        }

        private void UpdateCatChase()
        {
            if (!_catIsChasing || catController == null) return;

            Transform catTransform = catController.transform;
            float distance = Vector2.Distance(catTransform.position, transform.position);

            if (distance > chaseStopDistance)
            {
                Vector3 direction = (transform.position - catTransform.position).normalized;
                catTransform.position += direction * catChaseSpeed * Time.deltaTime;

                if (catController.CatAnimator != null)
                {
                    catController.CatAnimator.SetFacingDirection(direction.x);
                    catController.CatAnimator.SetSpeed(catChaseSpeed);
                    // Show running animation while chasing
                    catController.CatAnimator.OverrideVisualState(CatAnimator.AnimState.Running);
                }
            }
            else
            {
                OnCatPounce();
            }
        }

        private void OnCatPounce()
        {
            if (catController.CatAnimator != null)
            {
                catController.CatAnimator.ClearVisualOverride();
                catController.CatAnimator.PlayBounce();
            }

            if (pounceEffectPrefab != null)
                Instantiate(pounceEffectPrefab, catController.transform.position + Vector3.up * 0.3f, Quaternion.identity);

            if (catNeeds != null)
                catNeeds.IncreaseHappiness(2f);
        }

        private void StopDrag()
        {
            _isDragging = false;
            _catIsChasing = false;

            if (trailRenderer != null) trailRenderer.emitting = false;

            // Clear running animation override
            if (catController != null && catController.CatAnimator != null)
                catController.CatAnimator.ClearVisualOverride();

            // Return cat to idle
            if (catController != null && catController.CurrentState == CatController.CatState.Playing)
                catController.RequestState(CatController.CatState.Idle);

            Debug.Log($"[DraggableToy] Drag stopped. Play time: {_playTimer:F1}s");
        }

        private void ReturnToRest()
        {
            transform.position = Vector3.Lerp(transform.position, _restPosition, returnSpeed * Time.deltaTime);
        }

        private bool IsAtRestPosition()
        {
            return Vector3.Distance(transform.position, _restPosition) < 0.05f;
        }

        public void SetRestPosition(Vector3 position)
        {
            _restPosition = position;
        }

        /// <summary>
        /// Called when tool mode changes. Show/hide the toy.
        /// </summary>
        private void OnModeChanged(ToolModeManager.ToolMode newMode)
        {
            UpdateVisibility();

            // If switching away from feather while dragging, stop
            if (newMode != ToolModeManager.ToolMode.FeatherToy && _isDragging)
                StopDrag();
        }

        private void UpdateVisibility()
        {
            bool show = ToolModeManager.Instance == null || ToolModeManager.Instance.IsFeatherMode;

            if (spriteRenderer != null && !_isDragging)
                spriteRenderer.enabled = show;
        }

        public bool IsDragging => _isDragging;
    }
}
