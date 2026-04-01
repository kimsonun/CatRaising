using UnityEngine;
using System.Collections;

namespace CatRaising.Cat
{
    /// <summary>
    /// Manages cat sprite animations, handling state-based animation transitions
    /// and sprite direction flipping.
    /// 
    /// ANIMATOR SETUP (in Unity Editor):
    /// 
    /// Parameters:
    ///   - "State" (int):    0=Idle, 1=Walking, 2=LayingDown, 3=Sleeping, 
    ///                       4=WakingUp, 5=StandingUp, 6=Eating, 7=Drinking, 8=Playing
    ///   - "Speed" (float):  movement speed for walk animation playback rate
    /// 
    /// States (in Animator Controller):
    ///   - Idle       (sitting/standing sprite, looping)
    ///   - Walk       (33-frame walk cycle, looping)
    ///   - LayingDown (one-shot transition clip, NOT looping)
    ///   - Sleep      (lying down eyes-closed, looping or static)
    ///   - WakingUp   (one-shot transition clip, NOT looping)
    ///   - Stretching (one-shot transition clip, NOT looping)
    ///   - Eat, Drink, Play (placeholder states)
    /// 
    /// For one-shot clips (LayingDown, WakingUp, Stretching):
    ///   The script uses clip duration to auto-advance. No need for 
    ///   "Has Exit Time" transitions in the Animator — the script fires
    ///   OnTransitionAnimationComplete when the clip finishes.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CatAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Idle Sprites (fallback when no Animator Controller)")]
        [Tooltip("The sitting/standing sprite for idle state")]
        [SerializeField] private Sprite idleSprite;
        [Tooltip("The lying down sprite for sleep state")]
        [SerializeField] private Sprite sleepSprite;

        [Header("Transition Durations (seconds)")]
        [Tooltip("Duration of the laying down animation clip")]
        [SerializeField] private float layingDownDuration = 1.0f;
        [Tooltip("Duration of the waking up animation clip")]
        [SerializeField] private float wakingUpDuration = 1.2f;
        [Tooltip("Duration of the stretching animation clip")]
        [SerializeField] private float stretchingDuration = 1.5f;

        [Header("Settings")]
        [SerializeField] private float flipSmoothing = 0.1f;

        // Animator parameter hashes for performance
        private static readonly int StateParam = Animator.StringToHash("State");
        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        private bool _useAnimator = true;
        private CatController.CatState _currentAnimState;
        private Coroutine _transitionCoroutine;

        /// <summary>
        /// Animation state IDs matching the Animator "State" int parameter.
        /// </summary>
        public enum AnimState
        {
            Idle = 0,
            Walking = 1,
            LayingDown = 2,
            Sleeping = 3,
            WakingUp = 4,
            StandingUp = 5,
            Eating = 6,
            Drinking = 7,
            Playing = 8
        }

        /// <summary>
        /// Fired when a one-shot transition animation (LayingDown, WakingUp, StandingUp)
        /// finishes playing. CatController listens to this to advance the FSM.
        /// </summary>
        public event System.Action<CatController.CatState> OnTransitionAnimationComplete;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            // Check if the Animator has a valid controller
            _useAnimator = _animator != null && _animator.runtimeAnimatorController != null;

            if (!_useAnimator)
            {
                Debug.Log("[CatAnimator] No Animator Controller found — using direct sprite swaps + timers.");
            }
        }

        /// <summary>
        /// Set the current animation state based on the cat's FSM state.
        /// For transition states, starts a timer that fires OnTransitionAnimationComplete
        /// when the clip should be done.
        /// </summary>
        public void SetState(CatController.CatState state)
        {
            _currentAnimState = state;

            // Stop any running transition timer
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            AnimState animState = state switch
            {
                CatController.CatState.Idle => AnimState.Idle,
                CatController.CatState.Walking => AnimState.Walking,
                CatController.CatState.LayingDown => AnimState.LayingDown,
                CatController.CatState.Sleeping => AnimState.Sleeping,
                CatController.CatState.WakingUp => AnimState.WakingUp,
                CatController.CatState.Stretching => AnimState.StandingUp,
                CatController.CatState.BeingPet => AnimState.Idle, // Use idle anim while being pet
                CatController.CatState.Eating => AnimState.Eating,
                CatController.CatState.Drinking => AnimState.Drinking,
                CatController.CatState.Playing => AnimState.Playing,
                _ => AnimState.Idle
            };

            if (_useAnimator)
            {
                _animator.SetInteger(StateParam, (int)animState);
            }
            else
            {
                // Fallback: direct sprite swap when no Animator Controller is set up
                ApplyFallbackSprite(animState);
            }

            // Start transition timer for one-shot states
            if (IsTransitionState(state))
            {
                float duration = GetTransitionDuration(state);
                _transitionCoroutine = StartCoroutine(TransitionTimerCoroutine(state, duration));
            }
        }

        /// <summary>
        /// Coroutine that waits for a transition animation to complete,
        /// then notifies the CatController to advance the FSM.
        /// </summary>
        private IEnumerator TransitionTimerCoroutine(CatController.CatState state, float duration)
        {
            Debug.Log($"[CatAnimator] Transition '{state}' started ({duration:F1}s)");
            yield return new WaitForSeconds(duration);
            Debug.Log($"[CatAnimator] Transition '{state}' complete");
            _transitionCoroutine = null;
            OnTransitionAnimationComplete?.Invoke(state);
        }

        /// <summary>
        /// Whether the given state is a one-shot transition animation.
        /// </summary>
        private bool IsTransitionState(CatController.CatState state)
        {
            return state == CatController.CatState.LayingDown ||
                   state == CatController.CatState.WakingUp ||
                   state == CatController.CatState.Stretching;
        }

        /// <summary>
        /// Get the duration for a transition animation.
        /// These should match your actual animation clip lengths.
        /// </summary>
        private float GetTransitionDuration(CatController.CatState state)
        {
            return state switch
            {
                CatController.CatState.LayingDown => layingDownDuration,
                CatController.CatState.WakingUp => wakingUpDuration,
                CatController.CatState.Stretching => stretchingDuration,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Set the movement speed (affects walk animation playback rate).
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (_useAnimator)
            {
                _animator.SetFloat(SpeedParam, speed);
            }
        }

        /// <summary>
        /// Flip the sprite horizontally based on movement direction.
        /// </summary>
        public void SetFacingDirection(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) > 0.01f)
            {
                // The walk sprite sheet faces RIGHT by default
                // Flip when moving left
                _spriteRenderer.flipX = horizontalDirection < 0;
            }
        }

        /// <summary>
        /// Flip to face a specific world position.
        /// </summary>
        public void FacePosition(Vector3 targetPosition)
        {
            float direction = targetPosition.x - transform.position.x;
            SetFacingDirection(direction);
        }

        /// <summary>
        /// Apply a temporary color tint (used for petting feedback, etc).
        /// </summary>
        public void SetTint(Color color)
        {
            _spriteRenderer.color = color;
        }

        /// <summary>
        /// Reset color to white (no tint).
        /// </summary>
        public void ResetTint()
        {
            _spriteRenderer.color = Color.white;
        }

        /// <summary>
        /// Trigger a small bounce/squash-stretch effect (for petting, landing, etc).
        /// </summary>
        public void PlayBounce()
        {
            // Don't bounce during transitions
            if (IsTransitionState(_currentAnimState)) return;

            StopCoroutine(nameof(BounceCoroutine));
            StartCoroutine(BounceCoroutine());
        }

        private IEnumerator BounceCoroutine()
        {
            Vector3 originalScale = Vector3.one;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Squash and stretch
                float scaleY = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.1f * (1f - t);
                float scaleX = 1f - Mathf.Sin(t * Mathf.PI * 2f) * 0.05f * (1f - t);

                transform.localScale = new Vector3(scaleX, scaleY, 1f);
                yield return null;
            }

            transform.localScale = originalScale;
        }

        /// <summary>
        /// Fallback sprite swap for when no Animator Controller is assigned yet.
        /// This lets the game run immediately even before animations are set up.
        /// For transition states, it swaps between existing sprites as a placeholder.
        /// </summary>
        private void ApplyFallbackSprite(AnimState state)
        {
            switch (state)
            {
                case AnimState.Idle:
                case AnimState.Eating:
                case AnimState.Drinking:
                case AnimState.Playing:
                case AnimState.StandingUp:  // fallback: use idle sprite
                case AnimState.WakingUp:    // fallback: use idle sprite
                    if (idleSprite != null)
                        _spriteRenderer.sprite = idleSprite;
                    break;

                case AnimState.Sleeping:
                case AnimState.LayingDown:  // fallback: use sleep sprite
                    if (sleepSprite != null)
                        _spriteRenderer.sprite = sleepSprite;
                    break;

                case AnimState.Walking:
                    // For walking without animator, use idle sprite (walk cycle needs Animator)
                    if (idleSprite != null)
                        _spriteRenderer.sprite = idleSprite;
                    break;
            }
        }

        /// <summary>
        /// Get the current SpriteRenderer (for external effects).
        /// </summary>
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        /// <summary>
        /// Whether the sprite is currently flipped.
        /// </summary>
        public bool IsFlipped => _spriteRenderer != null && _spriteRenderer.flipX;
    }
}
