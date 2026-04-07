using UnityEngine;
using CatRaising.Core;
using CatRaising.Interactables;

namespace CatRaising.Cat
{
    /// <summary>
    /// Autonomous AI behavior for the cat. Decides what the cat does on its own:
    /// wander around, find a nap spot, idle, groom, sit by food/water, etc.
    /// Works with the CatController FSM to request state changes.
    /// </summary>
    public class CatAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CatController catController;
        [SerializeField] private CatNeeds catNeeds;

        [Header("Wander Settings")]
        [Tooltip("Fallback bounds if no IsometricGrid is available")]
        [SerializeField] private Vector2 wanderBoundsMin = new Vector2(-7f, -3f);
        [SerializeField] private Vector2 wanderBoundsMax = new Vector2(7f, -1f);
        [SerializeField] private float wanderSpeed = 1.5f;
        [SerializeField] private float arrivalThreshold = 0.2f;

        [Header("Behavior Timing")]
        [Tooltip("Min seconds between AI decisions")]
        [SerializeField] private float minDecisionInterval = 3f;
        [Tooltip("Max seconds between AI decisions")]
        [SerializeField] private float maxDecisionInterval = 10f;
        [Tooltip("How long the cat sleeps (seconds)")]
        [SerializeField] private float sleepDurationMin = 15f;
        [SerializeField] private float sleepDurationMax = 45f;
        [Tooltip("How long the cat idles (seconds)")]
        [SerializeField] private float idleDurationMin = 5f;
        [SerializeField] private float idleDurationMax = 15f;

        [Header("Need-Driven Behavior")]
        [Tooltip("Reference to FoodBowl script (for checking if bowl has food)")]
        [SerializeField] private FoodBowl foodBowl;
        [Tooltip("Reference to WaterBowl script (for checking if bowl has water)")]
        [SerializeField] private WaterBowl waterBowl;

        [Header("Grooming")]
        [Tooltip("Base probability of grooming during idle decision")]
        [SerializeField] private float groomingChance = 0.15f;
        [Tooltip("Extra grooming chance when cat is dirty (cleanliness < 70)")]
        [SerializeField] private float dirtyGroomingBonus = 0.25f;

        // Internal state
        private float _decisionTimer;
        private float _currentStateDuration;
        private float _stateTimer;
        private Vector3 _wanderTarget;
        private bool _isMovingToTarget;
        private bool _aiEnabled = true;
        private bool _playerCalledCat = false; // True when player taps ground

        private void Awake()
        {
            if (catController == null) catController = GetComponent<CatController>();
            if (catNeeds == null) catNeeds = GetComponent<CatNeeds>();
        }

        private void Start()
        {
            ResetDecisionTimer();
        }

        private void Update()
        {
            if (!_aiEnabled) return;
            if (catController == null) return;

            // Don't make AI decisions if the cat is being interacted with
            // or is in a transition animation
            if (catController.CurrentState == CatController.CatState.BeingPet ||
                catController.CurrentState == CatController.CatState.Playing ||
                catController.IsInTransition)
            {
                return;
            }

            // Handle current movement
            if (_isMovingToTarget)
            {
                MoveToTarget();
                return;
            }

            // State duration timer
            _stateTimer += Time.deltaTime;

            // Decision timer
            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer <= 0f)
            {
                MakeDecision();
                ResetDecisionTimer();
            }

            // Check if current state has expired
            if (_stateTimer >= _currentStateDuration)
            {
                MakeDecision();
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                StartGrooming();    
            }
        }

        /// <summary>
        /// Make an autonomous decision about what to do next.
        /// Priority: player call > urgent needs (with food available) > random behavior.
        /// </summary>
        private void MakeDecision()
        {
            _stateTimer = 0f;

            // Priority 0: Player called the cat (ground tap)
            // _playerCalledCat is handled via WalkToPosition, which sets _isMovingToTarget

            // Priority 1: Address urgent needs — ONLY if bowl has food/water
            if (catNeeds != null && catNeeds.IsHungry && foodBowl != null && !foodBowl.IsEmpty)
            {
                GoToFoodBowl();
                return;
            }

            if (catNeeds != null && catNeeds.IsThirsty && waterBowl != null && !waterBowl.IsEmpty)
            {
                GoToWaterBowl();
                return;
            }

            // Priority 2: Time-of-day weighted random behavior
            float sleepChance = GetSleepChance();
            float wanderChance = GetWanderChance();
            float groomChance = GetGroomingChance();

            float roll = Random.value;
            float cumulative = 0f;

            cumulative += sleepChance;
            if (roll < cumulative)
            {
                StartSleeping();
                return;
            }

            cumulative += groomChance;
            if (roll < cumulative)
            {
                StartGrooming();
                return;
            }

            cumulative += wanderChance;
            if (roll < cumulative)
            {
                StartWandering();
                return;
            }

            StartIdling();
        }

        // ─── Chance Calculations ────────────────────────────────

        private float GetSleepChance()
        {
            float baseChance = 0.2f;

            if (TimeManager.Instance != null)
            {
                if (TimeManager.Instance.CurrentPhase == TimeManager.DayPhase.Night)
                    baseChance = 0.6f;
                else if (TimeManager.Instance.CurrentPhase == TimeManager.DayPhase.Evening)
                    baseChance = 0.35f;
                else if (TimeManager.Instance.CurrentPhase == TimeManager.DayPhase.Afternoon)
                    baseChance = 0.25f;
            }

            if (catNeeds != null && catNeeds.IsSad)
                baseChance += 0.15f;

            return Mathf.Clamp01(baseChance);
        }

        private float GetWanderChance()
        {
            float baseChance = 0.35f;

            if (TimeManager.Instance != null && TimeManager.Instance.CurrentPhase == TimeManager.DayPhase.Night)
                baseChance = 0.15f;

            if (catNeeds != null && catNeeds.IsContent)
                baseChance += 0.1f;

            return Mathf.Clamp01(baseChance);
        }

        private float GetGroomingChance()
        {
            float chance = groomingChance;

            // More likely to groom when dirty
            if (catNeeds != null && catNeeds.IsDirty)
                chance += dirtyGroomingBonus;

            // Only groom when in Idle state
            if (catController.CurrentState != CatController.CatState.Idle)
                chance = 0f;

            return Mathf.Clamp01(chance);
        }

        // ─── State Transitions ──────────────────────────────────

        private void StartIdling()
        {
            _currentStateDuration = Random.Range(idleDurationMin, idleDurationMax);
            catController.RequestState(CatController.CatState.Idle);
        }

        private void StartSleeping()
        {
            _currentStateDuration = Random.Range(sleepDurationMin, sleepDurationMax);
            catController.RequestState(CatController.CatState.Sleeping);
        }

        private void StartWandering()
        {
            // Use isometric grid if available, else fall back to rectangular bounds
            if (IsometricGrid.Instance != null)
            {
                _wanderTarget = IsometricGrid.Instance.GetRandomWalkablePosition();
            }
            else
            {
                float x = Random.Range(wanderBoundsMin.x, wanderBoundsMax.x);
                float y = Random.Range(wanderBoundsMin.y, wanderBoundsMax.y);
                _wanderTarget = new Vector3(x, y, 0f);
            }

            _isMovingToTarget = true;
            _currentStateDuration = 30f;
            _playerCalledCat = false;

            catController.RequestState(CatController.CatState.Walking);
        }

        private void StartGrooming()
        {
            _currentStateDuration = 7f; // Will be overridden by animation completion
            catController.RequestState(CatController.CatState.Grooming);
            Debug.Log("[CatAI] Cat decided to groom itself.");
        }

        private void GoToFoodBowl()
        {
            if (foodBowl == null) return;
            _wanderTarget = foodBowl.GetAdjacentWalkablePosition();
            _isMovingToTarget = true;
            _currentStateDuration = 20f;
            _playerCalledCat = false;

            catController.RequestState(CatController.CatState.Walking);
        }

        private void GoToWaterBowl()
        {
            if (waterBowl == null) return;
            _wanderTarget = waterBowl.GetAdjacentWalkablePosition();
            _isMovingToTarget = true;
            _currentStateDuration = 20f;
            _playerCalledCat = false;

            catController.RequestState(CatController.CatState.Walking);
        }

        /// <summary>
        /// Called by CameraController when the player taps on empty ground.
        /// The cat walks to the tapped position.
        /// </summary>
        public void WalkToPosition(Vector3 target)
        {
            // Snap to nearest walkable tile on the isometric grid
            if (IsometricGrid.Instance != null)
            {
                var grid = IsometricGrid.Instance;
                Vector2Int cell = grid.WorldToGrid(target);

                // If the target tile is walkable, go there directly
                if (grid.IsTileWalkable(cell))
                {
                    target = grid.GridToWorld(cell);
                }
                else
                {
                    // Find the nearest walkable tile
                    target = FindNearestWalkable(target);
                }
            }
            else
            {
                // Fallback: clamp to rectangular bounds
                target.x = Mathf.Clamp(target.x, wanderBoundsMin.x, wanderBoundsMax.x);
                target.y = Mathf.Clamp(target.y, wanderBoundsMin.y, wanderBoundsMax.y);
            }

            target.z = 0f;
            _wanderTarget = target;
            _isMovingToTarget = true;
            _playerCalledCat = true;
            _currentStateDuration = 30f;
            _stateTimer = 0f;

            catController.RequestState(CatController.CatState.Walking);
            Debug.Log($"[CatAI] Player called cat to {target}");
        }

        /// <summary>
        /// Find the nearest walkable tile to a world position.
        /// </summary>
        private Vector3 FindNearestWalkable(Vector3 worldPos)
        {
            if (IsometricGrid.Instance == null) return worldPos;

            var grid = IsometricGrid.Instance;
            var walkable = grid.GetAllWalkableTiles();
            if (walkable.Count == 0) return worldPos;

            float bestDist = float.MaxValue;
            Vector3 bestPos = worldPos;

            foreach (var cell in walkable)
            {
                Vector3 cellWorld = grid.GridToWorld(cell);
                float dist = Vector3.SqrMagnitude(cellWorld - worldPos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPos = cellWorld;
                }
            }

            return bestPos;
        }

        /// <summary>
        /// Move the cat toward the current wander target.
        /// </summary>
        private void MoveToTarget()
        {
            if (catController.CurrentState != CatController.CatState.Walking)
            {
                _isMovingToTarget = false;
                return;
            }

            Vector3 currentPos = transform.position;
            Vector3 direction = (_wanderTarget - currentPos).normalized;
            float distance = Vector3.Distance(currentPos, _wanderTarget);

            if (distance < arrivalThreshold)
            {
                _isMovingToTarget = false;
                _playerCalledCat = false;
                StartIdling();
                return;
            }

            transform.position += direction * wanderSpeed * Time.deltaTime;

            if (catController.CatAnimator != null)
            {
                catController.CatAnimator.SetFacingDirection(direction.x);
                catController.CatAnimator.SetSpeed(wanderSpeed);
            }
        }

        public void SetAIEnabled(bool enabled)
        {
            _aiEnabled = enabled;
            if (!enabled)
                _isMovingToTarget = false;
        }

        /// <summary>
        /// Update wander bounds (called by RoomManager on room switch).
        /// </summary>
        public void SetWanderBounds(Vector2 min, Vector2 max)
        {
            wanderBoundsMin = min;
            wanderBoundsMax = max;
            Debug.Log($"[CatAI] Wander bounds updated: {min} to {max}");
        }

        public Vector3 WanderTarget => _wanderTarget;
        public bool IsMoving => _isMovingToTarget;

        private void ResetDecisionTimer()
        {
            _decisionTimer = Random.Range(minDecisionInterval, maxDecisionInterval);
        }

        // ─── Editor Gizmos ──────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.3f);
            Vector3 center = new Vector3(
                (wanderBoundsMin.x + wanderBoundsMax.x) / 2f,
                (wanderBoundsMin.y + wanderBoundsMax.y) / 2f, 0f);
            Vector3 size = new Vector3(
                wanderBoundsMax.x - wanderBoundsMin.x,
                wanderBoundsMax.y - wanderBoundsMin.y, 0.1f);
            Gizmos.DrawCube(center, size);

            if (_isMovingToTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_wanderTarget, 0.2f);
                Gizmos.DrawLine(transform.position, _wanderTarget);
            }
        }
    }
}
