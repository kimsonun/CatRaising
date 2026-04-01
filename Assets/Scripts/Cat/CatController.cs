using UnityEngine;  

namespace CatRaising.Cat
{
    /// <summary>
    /// Main cat controller using a Finite State Machine (FSM).
    /// Coordinates between CatAI, CatAnimator, CatNeeds, and CatInteraction.
    /// 
    /// Transition states (LayingDown, WakingUp, Stretching) are handled
    /// automatically — AI/code requests Sleeping or Idle, and the FSM
    /// routes through the proper transition chain:
    ///   Idle → LayingDown → Sleeping
    ///   Sleeping → WakingUp → Stretching → Idle
    /// </summary>
    public class CatController : MonoBehaviour
    {
        /// <summary>
        /// All possible states the cat can be in.
        /// </summary>
        public enum CatState
        {
            Idle,           // Standing/sitting still, looking around
            Walking,        // Moving to a destination
            LayingDown,     // Transition: standing → lying down (one-shot)
            Sleeping,       // Lying down with eyes closed, zzz
            WakingUp,       // Transition: eyes opening, starting to get up (one-shot)
            Stretching,     // Transition: post-wake stretch (one-shot)
            BeingPet,       // Player is petting the cat
            Eating,         // At the food bowl
            Drinking,       // At the water bowl
            Playing         // Chasing a toy
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
        /// Whether the cat is in a transition animation (LayingDown, WakingUp, Stretching).
        /// During transitions, AI decisions are paused and most interactions are blocked.
        /// </summary>
        public bool IsInTransition =>
            _currentState == CatState.LayingDown ||
            _currentState == CatState.WakingUp ||
            _currentState == CatState.Stretching;

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

            // Listen for transition animation completions from CatAnimator
            if (_catAnimator != null)
                _catAnimator.OnTransitionAnimationComplete += OnTransitionComplete;
        }

        private void OnDestroy()
        {
            if (_catAnimator != null)
                _catAnimator.OnTransitionAnimationComplete -= OnTransitionComplete;
        }

        /// <summary>
        /// Request a state change. The FSM will validate the transition and
        /// automatically insert transition states where needed.
        /// 
        /// Example: RequestState(Sleeping) while in Idle will actually go:
        ///   Idle → LayingDown → (wait for anim) → Sleeping
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

            // Auto-insert transition states
            CatState actualNextState = ResolveTransitionState(newState);

            CatState previousState = _currentState;
            ExitState(_currentState);
            _currentState = actualNextState;
            EnterState(actualNextState);

            OnStateChanged?.Invoke(previousState, actualNextState);
            Debug.Log($"[CatController] State: {previousState} → {actualNextState}" +
                      (actualNextState != newState ? $" (target: {newState})" : ""));
        }

        /// <summary>
        /// Resolve the actual next state, inserting transitions where needed.
        /// If the AI requests "Sleeping" while Idle, we go to "LayingDown" first.
        /// If the AI requests "Idle" while Sleeping, we go to "WakingUp" first.
        /// </summary>
        private CatState ResolveTransitionState(CatState targetState)
        {
            // Going to sleep: insert LayingDown transition
            if (targetState == CatState.Sleeping &&
                _currentState != CatState.LayingDown)
            {
                return CatState.LayingDown;
            }

            // Waking up: insert WakingUp transition
            if (targetState != CatState.Sleeping &&
                _currentState == CatState.Sleeping)
            {
                // Unless being forced by player interaction (pet/play), do wake-up transition
                if (targetState != CatState.BeingPet && targetState != CatState.Playing)
                {
                    return CatState.WakingUp;
                }
            }

            // No transition needed
            return targetState;
        }

        /// <summary>
        /// Called by CatAnimator when a transition animation finishes playing.
        /// Advances the FSM to the next state in the chain.
        /// </summary>
        private void OnTransitionComplete(CatState completedState)
        {
            Debug.Log($"[CatController] Transition animation complete: {completedState}");

            switch (completedState)
            {
                case CatState.LayingDown:
                    // LayingDown finished → go to Sleeping
                    AdvanceToState(CatState.Sleeping);
                    break;

                case CatState.WakingUp:
                    // WakingUp finished → go to Stretching
                    AdvanceToState(CatState.Stretching);
                    break;

                case CatState.Stretching:
                    // Stretching finished → go to Idle
                    AdvanceToState(CatState.Idle);
                    break;
            }
        }

        /// <summary>
        /// Advance to the next state in a transition chain (bypasses ResolveTransitionState).
        /// </summary>
        private void AdvanceToState(CatState nextState)
        {
            CatState previousState = _currentState;
            ExitState(_currentState);
            _currentState = nextState;
            EnterState(nextState);

            OnStateChanged?.Invoke(previousState, nextState);
            Debug.Log($"[CatController] Advanced: {previousState} → {nextState}");
        }

        /// <summary>
        /// Check if a transition to the given state is allowed from the current state.
        /// </summary>
        private bool CanTransitionTo(CatState newState)
        {
            // Player interaction always takes priority (even during transitions)
            if (newState == CatState.BeingPet || newState == CatState.Playing)
                return true;

            // Block AI decisions during transition animations
            if (IsInTransition)
                return false;

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

                case CatState.LayingDown:
                    // Disable AI during transition
                    if (_catAI != null) _catAI.SetAIEnabled(false);
                    break;

                case CatState.Sleeping:
                    // Re-enable AI so it can decide when to wake up
                    if (_catAI != null) _catAI.SetAIEnabled(true);
                    break;

                case CatState.WakingUp:
                    if (_catAI != null) _catAI.SetAIEnabled(false);
                    break;

                case CatState.Stretching:
                    if (_catAI != null) _catAI.SetAIEnabled(false);
                    break;

                case CatState.BeingPet:
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
        /// Force the cat into a specific state (bypasses validation AND transitions).
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
