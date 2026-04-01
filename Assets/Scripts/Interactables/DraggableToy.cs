using UnityEngine;
using CatRaising.Cat;
using CatRaising.Systems;
using CatRaising.Core;

namespace CatRaising.Interactables
{
    /// <summary>
    /// Draggable toy (like a feather wand) that the player drags around
    /// for the cat to chase. Increases happiness and bond.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DraggableToy : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CatController catController;
        [SerializeField] private CatNeeds catNeeds;

        [Header("Drag Settings")]
        [SerializeField] private float dragZOffset = -1f; // Keep in front of background
        [SerializeField] private float returnSpeed = 3f;  // Speed to return to rest position

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

        [Header("Effects")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private GameObject pounceEffectPrefab;

        // State
        private bool _isDragging = false;
        private Vector3 _restPosition;
        private Camera _mainCamera;
        private float _playTimer = 0f;
        private bool _catIsChasing = false;
        private bool _sessionBondAwarded = false;

        private void Start()
        {
            _mainCamera = Camera.main;
            _restPosition = transform.position;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer != null) trailRenderer.emitting = false;
        }

        private void Update()
        {
            HandleDragInput();

            if (_isDragging)
            {
                UpdateDrag();
                UpdateCatChase();
            }
            else if (!IsAtRestPosition())
            {
                ReturnToRest();
            }
        }

        /// <summary>
        /// Handle touch/mouse drag input.
        /// </summary>
        private void HandleDragInput()
        {
            if (TouchInput.WasPressedThisFrame)
            {
                if (IsTouchingToy())
                {
                    StartDrag();
                }
            }

            if (TouchInput.WasReleasedThisFrame && _isDragging)
            {
                StopDrag();
            }
        }

        /// <summary>
        /// Check if touch is on the toy.
        /// </summary>
        private bool IsTouchingToy()
        {
            return TouchInput.IsOverGameObject(gameObject);
        }

        /// <summary>
        /// Start dragging the toy.
        /// </summary>
        private void StartDrag()
        {
            _isDragging = true;
            _playTimer = 0f;
            _sessionBondAwarded = false;

            if (trailRenderer != null) trailRenderer.emitting = true;

            // Tell the cat to start playing
            if (catController != null)
                catController.RequestState(CatController.CatState.Playing);

            _catIsChasing = true;

            Debug.Log("[DraggableToy] Drag started!");
        }

        /// <summary>
        /// Update the toy position to follow the touch/mouse.
        /// </summary>
        private void UpdateDrag()
        {
            Vector3 worldPos = TouchInput.WorldPosition;
            worldPos.z = dragZOffset;
            transform.position = worldPos;

            _playTimer += Time.deltaTime;

            // Increase cat happiness while playing
            if (catNeeds != null)
                catNeeds.IncreaseHappiness(happinessPerSecond * Time.deltaTime);

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

        /// <summary>
        /// Make the cat chase the toy.
        /// </summary>
        private void UpdateCatChase()
        {
            if (!_catIsChasing || catController == null) return;

            Transform catTransform = catController.transform;
            float distance = Vector2.Distance(catTransform.position, transform.position);

            if (distance > chaseStopDistance)
            {
                // Move cat toward toy
                Vector3 direction = (transform.position - catTransform.position).normalized;
                catTransform.position += direction * catChaseSpeed * Time.deltaTime;

                // Face the toy
                if (catController.CatAnimator != null)
                {
                    catController.CatAnimator.SetFacingDirection(direction.x);
                    catController.CatAnimator.SetSpeed(catChaseSpeed);
                }
            }
            else
            {
                // Cat "caught" the toy — pounce!
                OnCatPounce();
            }
        }

        /// <summary>
        /// Called when the cat reaches the toy (pounce!).
        /// </summary>
        private void OnCatPounce()
        {
            // Bounce effect on the cat
            if (catController.CatAnimator != null)
                catController.CatAnimator.PlayBounce();

            // Spawn pounce effect
            if (pounceEffectPrefab != null)
            {
                Instantiate(pounceEffectPrefab, catController.transform.position + Vector3.up * 0.3f, Quaternion.identity);
            }

            // Extra happiness for catching it
            if (catNeeds != null)
                catNeeds.IncreaseHappiness(2f);
        }

        /// <summary>
        /// Stop dragging the toy.
        /// </summary>
        private void StopDrag()
        {
            _isDragging = false;
            _catIsChasing = false;

            if (trailRenderer != null) trailRenderer.emitting = false;

            // Return cat to idle
            if (catController != null && catController.CurrentState == CatController.CatState.Playing)
                catController.RequestState(CatController.CatState.Idle);

            Debug.Log($"[DraggableToy] Drag stopped. Play time: {_playTimer:F1}s");
        }

        /// <summary>
        /// Smoothly return the toy to its rest position.
        /// </summary>
        private void ReturnToRest()
        {
            transform.position = Vector3.Lerp(transform.position, _restPosition, returnSpeed * Time.deltaTime);
        }

        private bool IsAtRestPosition()
        {
            return Vector3.Distance(transform.position, _restPosition) < 0.05f;
        }

        /// <summary>
        /// Set a new rest position (e.g., when placed in a different spot).
        /// </summary>
        public void SetRestPosition(Vector3 position)
        {
            _restPosition = position;
        }
    }
}
