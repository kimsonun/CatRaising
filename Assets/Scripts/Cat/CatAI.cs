using UnityEngine;
using CatRaising.Core;

namespace CatRaising.Cat
{
    /// <summary>
    /// Autonomous AI behavior for the cat. Decides what the cat does on its own:
    /// wander around, find a nap spot, idle, sit by food/water, etc.
    /// Works with the CatController FSM to request state changes.
    /// </summary>
    public class CatAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CatController catController;
        [SerializeField] private CatNeeds catNeeds;

        [Header("Wander Settings")]
        [Tooltip("Bounds for random wander targets (world space)")]
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
        [Tooltip("References to food/water bowls in the scene")]
        [SerializeField] private Transform foodBowlTransform;
        [SerializeField] private Transform waterBowlTransform;

        // Internal state
        private float _decisionTimer;
        private float _currentStateDuration;
        private float _stateTimer;
        private Vector3 _wanderTarget;
        private bool _isMovingToTarget;
        private bool _aiEnabled = true;

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
            if (catController.CurrentState == CatController.CatState.BeingPet ||
                catController.CurrentState == CatController.CatState.Playing)
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
        }

        /// <summary>
        /// Make an autonomous decision about what to do next.
        /// Priority: urgent needs > random behavior weighted by time of day.
        /// </summary>
        private void MakeDecision()
        {
            _stateTimer = 0f;

            // Priority 1: Address urgent needs
            if (catNeeds != null && catNeeds.IsHungry && foodBowlTransform != null)
            {
                GoToFoodBowl();
                return;
            }

            if (catNeeds != null && catNeeds.IsThirsty && waterBowlTransform != null)
            {
                GoToWaterBowl();
                return;
            }

            // Priority 2: Time-of-day weighted random behavior
            float sleepChance = GetSleepChance();
            float wanderChance = GetWanderChance();

            float roll = Random.value;

            if (roll < sleepChance)
            {
                StartSleeping();
            }
            else if (roll < sleepChance + wanderChance)
            {
                StartWandering();
            }
            else
            {
                StartIdling();
            }
        }

        /// <summary>
        /// Get the probability of the cat choosing to sleep, based on time of day and needs.
        /// </summary>
        private float GetSleepChance()
        {
            float baseChance = 0.2f;

            // Higher sleep chance at night
            if (TimeManager.Instance != null)
            {
                if (TimeManager.Instance.CurrentPhase == TimeManager.DayPhase.Night)
                    baseChance = 0.6f;
                else if (TimeManager.Instance.CurrentPhase == TimeManager.DayPhase.Evening)
                    baseChance = 0.35f;
                else if (TimeManager.Instance.CurrentPhase == TimeManager.DayPhase.Afternoon)
                    baseChance = 0.25f; // Afternoon nap!
            }

            // Lower happiness = more sleepy
            if (catNeeds != null && catNeeds.IsSad)
                baseChance += 0.15f;

            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Get the probability of wandering, based on time of day and needs.
        /// </summary>
        private float GetWanderChance()
        {
            float baseChance = 0.4f;

            // Less wandering at night
            if (TimeManager.Instance != null && TimeManager.Instance.CurrentPhase == TimeManager.DayPhase.Night)
                baseChance = 0.15f;

            // Happy cats wander more
            if (catNeeds != null && catNeeds.IsContent)
                baseChance += 0.1f;

            return Mathf.Clamp01(baseChance);
        }

        // --- State transitions ---

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
            // Pick a random point within bounds
            float x = Random.Range(wanderBoundsMin.x, wanderBoundsMax.x);
            float y = Random.Range(wanderBoundsMin.y, wanderBoundsMax.y);
            _wanderTarget = new Vector3(x, y, 0f);
            _isMovingToTarget = true;
            _currentStateDuration = 30f; // Timeout

            catController.RequestState(CatController.CatState.Walking);
        }

        private void GoToFoodBowl()
        {
            if (foodBowlTransform == null) return;
            _wanderTarget = foodBowlTransform.position + Vector3.left * 0.5f;
            _isMovingToTarget = true;
            _currentStateDuration = 20f;

            catController.RequestState(CatController.CatState.Walking);
        }

        private void GoToWaterBowl()
        {
            if (waterBowlTransform == null) return;
            _wanderTarget = waterBowlTransform.position + Vector3.left * 0.5f;
            _isMovingToTarget = true;
            _currentStateDuration = 20f;

            catController.RequestState(CatController.CatState.Walking);
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
                // Arrived at target
                _isMovingToTarget = false;
                StartIdling();
                return;
            }

            // Move toward target
            transform.position += direction * wanderSpeed * Time.deltaTime;

            // Face movement direction
            if (catController.CatAnimator != null)
            {
                catController.CatAnimator.SetFacingDirection(direction.x);
                catController.CatAnimator.SetSpeed(wanderSpeed);
            }
        }

        /// <summary>
        /// Temporarily disable AI (e.g., during player interaction or cutscenes).
        /// </summary>
        public void SetAIEnabled(bool enabled)
        {
            _aiEnabled = enabled;
            if (!enabled)
            {
                _isMovingToTarget = false;
            }
        }

        /// <summary>
        /// The current wander target (for debug gizmos).
        /// </summary>
        public Vector3 WanderTarget => _wanderTarget;

        /// <summary>
        /// Whether the cat is currently moving autonomously.
        /// </summary>
        public bool IsMoving => _isMovingToTarget;

        private void ResetDecisionTimer()
        {
            _decisionTimer = Random.Range(minDecisionInterval, maxDecisionInterval);
        }

        // --- Editor Gizmos ---

        private void OnDrawGizmosSelected()
        {
            // Draw wander bounds
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.3f);
            Vector3 center = new Vector3(
                (wanderBoundsMin.x + wanderBoundsMax.x) / 2f,
                (wanderBoundsMin.y + wanderBoundsMax.y) / 2f,
                0f
            );
            Vector3 size = new Vector3(
                wanderBoundsMax.x - wanderBoundsMin.x,
                wanderBoundsMax.y - wanderBoundsMin.y,
                0.1f
            );
            Gizmos.DrawCube(center, size);

            // Draw current target
            if (_isMovingToTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_wanderTarget, 0.2f);
                Gizmos.DrawLine(transform.position, _wanderTarget);
            }
        }
    }
}
