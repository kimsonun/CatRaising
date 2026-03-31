using UnityEngine;  

namespace CatRaising.Cat
{
    /// <summary>
    /// Main cat controller using a Finite State Machine (FSM).
    /// Coordinates between CatAI, CatAnimator, CatNeeds, and CatInteraction.
    /// </summary>
    public class CatController : MonoBehaviour
    {
        /// <summary>
        /// All possible states the cat can be in.
        /// </summary>
        public enum CatState
        {
            Idle,       // Sitting still, looking around
            Walking,    // Moving to a destination
            Sleeping,   // Lying down, zzz
            BeingPet,   // Player is petting the cat
            Eating,     // At the food bowl
            Drinking,   // At the water bowl
            Playing     // Chasing a toy
        }

        [Header("References")]
        [SerializeField] private CatAnimator _catAnimator;
        [SerializeField] private CatAI _catAI;
        [SerializeField] private CatNeeds _catNeeds;
        [SerializeField] private CatInteraction _catInteraction;

        [Header("State")]
        [SerializeField] private CatState _currentState = CatState.Idle;

        /// <summary>
        /// The current FSM state.
        /// </summary>
        public CatState CurrentState => _currentState;

        /// <summary>
        /// Public accessor for the animator component.
        /// </summary>
        public CatAnimator CatAnimator => _catAnimator;

        /// <summary>
        /// Event fired when the cat's state changes.
        /// </summary>
        public event System.Action<CatState, CatState> OnStateChanged;

        private void Awake()
        {
            // Auto-wire components if not assigned
            if (_catAnimator == null) _catAnimator = GetComponent<CatAnimator>();
            if (_catAI == null) _catAI = GetComponent<CatAI>();
            if (_catNeeds == null) _catNeeds = GetComponent<CatNeeds>();
            if (_catInteraction == null) _catInteraction = GetComponent<CatInteraction>();
        }

        private void Start()
        {
            // Set initial state
            EnterState(_currentState);
        }

        /// <summary>
        /// Request a state change. The FSM will validate the transition.
        /// </summary>
        public void RequestState(CatState newState)
        {
            if (newState == _currentState) return;

            // Validate transition
            if (!CanTransitionTo(newState))
            {
                Debug.Log($"[CatController] Blocked transition: {_currentState} → {newState}");
                return;
            }

            CatState previousState = _currentState;
            ExitState(_currentState);
            _currentState = newState;
            EnterState(newState);

            OnStateChanged?.Invoke(previousState, newState);
            Debug.Log($"[CatController] State: {previousState} → {newState}");
        }

        /// <summary>
        /// Check if a transition to the given state is allowed from the current state.
        /// </summary>
        private bool CanTransitionTo(CatState newState)
        {
            // Player interaction always takes priority
            if (newState == CatState.BeingPet || newState == CatState.Playing)
                return true;

            // Can't interrupt eating/drinking with AI decisions (let the cat finish)
            if (_currentState == CatState.Eating || _currentState == CatState.Drinking)
            {
                // Only player interaction or idle can interrupt
                if (newState != CatState.BeingPet && newState != CatState.Idle)
                    return false;
            }

            // Can't interrupt petting with AI decisions
            if (_currentState == CatState.BeingPet)
            {
                // Only allow transition to Idle (when petting ends)
                if (newState != CatState.Idle)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Called when entering a new state.
        /// </summary>
        private void EnterState(CatState state)
        {
            // Update animation
            if (_catAnimator != null)
                _catAnimator.SetState(state);

            switch (state)
            {
                case CatState.Idle:
                    if (_catAI != null) _catAI.SetAIEnabled(true);
                    break;

                case CatState.Walking:
                    if (_catAI != null) _catAI.SetAIEnabled(true);
                    break;

                case CatState.Sleeping:
                    if (_catAI != null) _catAI.SetAIEnabled(true);
                    break;

                case CatState.BeingPet:
                    // Disable AI while being pet
                    if (_catAI != null) _catAI.SetAIEnabled(false);
                    break;

                case CatState.Eating:
                    if (_catAI != null) _catAI.SetAIEnabled(false);
                    break;

                case CatState.Drinking:
                    if (_catAI != null) _catAI.SetAIEnabled(false);
                    break;

                case CatState.Playing:
                    if (_catAI != null) _catAI.SetAIEnabled(false);
                    break;
            }
        }

        /// <summary>
        /// Called when exiting a state.
        /// </summary>
        private void ExitState(CatState state)
        {
            switch (state)
            {
                case CatState.Walking:
                    if (_catAnimator != null)
                        _catAnimator.SetSpeed(0f);
                    break;

                case CatState.Sleeping:
                    // Small happiness boost from finishing a nap
                    if (_catNeeds != null)
                        _catNeeds.IncreaseHappiness(2f);
                    break;
            }
        }

        /// <summary>
        /// Force the cat into a specific state (bypasses validation).
        /// Use sparingly — for cutscenes, teleportation, etc.
        /// </summary>
        public void ForceState(CatState state)
        {
            CatState previous = _currentState;
            ExitState(_currentState);
            _currentState = state;
            EnterState(state);
            OnStateChanged?.Invoke(previous, state);
        }
    }
}
