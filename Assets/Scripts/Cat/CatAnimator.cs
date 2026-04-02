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
    ///                       4=WakingUp, 5=Stretching, 6=Eating, 7=Drinking,
    ///                       8=Playing, 9=Grooming
    ///   - "Speed" (float):  movement speed for walk animation playback rate
    /// 
    /// States (in Animator Controller):
    ///   - Idle       (sitting/standing sprite, looping)
    ///   - Walk       (walk cycle, looping)
    ///   - LayingDown (one-shot, NOT looping)
    ///   - Sleep      (lying down eyes-closed, looping)
    ///   - WakingUp   (one-shot, NOT looping)
    ///   - Stretching (one-shot, NOT looping)
    ///   - Eat, Drink, Play (placeholder states until custom art)
    ///   - Grooming   (one-shot, NOT looping — cat licking paw/body)
    /// 
    /// For one-shot clips (LayingDown, WakingUp, Stretching, Grooming):
    ///   The script uses clip duration to auto-advance. The script fires
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

        [Header("Transition Durations (seconds) — match your clip lengths!")]
        [SerializeField] private float layingDownDuration = 1.0f;
        [SerializeField] private float wakingUpDuration = 1.2f;
        [SerializeField] private float stretchingDuration = 1.5f;
        [SerializeField] private float groomingDuration = 3.0f;

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
            Stretching = 5,
            Eating = 6,
            Drinking = 7,
            Playing = 8,
            Grooming = 9
        }

        /// <summary>
        /// Fired when a one-shot transition animation finishes playing.
        /// CatController listens to this to advance the FSM.
        /// </summary>
        public event System.Action<CatController.CatState> OnTransitionAnimationComplete;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            _useAnimator = _animator != null && _animator.runtimeAnimatorController != null;

            if (!_useAnimator)
                Debug.Log("[CatAnimator] No Animator Controller found — using direct sprite swaps + timers.");
        }

        /// <summary>
        /// Set the current animation state based on the cat's FSM state.
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
                CatController.CatState.Stretching => AnimState.Stretching,
                CatController.CatState.BeingPet => AnimState.Idle,
                CatController.CatState.Eating => AnimState.Eating,
                CatController.CatState.Drinking => AnimState.Drinking,
                CatController.CatState.Playing => AnimState.Playing,
                CatController.CatState.Grooming => AnimState.Grooming,
                _ => AnimState.Idle
            };

            if (_useAnimator)
            {
                _animator.SetInteger(StateParam, (int)animState);
            }
            else
            {
                ApplyFallbackSprite(animState);
            }

            // Start transition timer for one-shot states
            if (IsTransitionState(state))
            {
                float duration = GetTransitionDuration(state);
                _transitionCoroutine = StartCoroutine(TransitionTimerCoroutine(state, duration));
            }
        }

        private IEnumerator TransitionTimerCoroutine(CatController.CatState state, float duration)
        {
            Debug.Log($"[CatAnimator] Transition '{state}' started ({duration:F1}s)");
            yield return new WaitForSeconds(duration);
            Debug.Log($"[CatAnimator] Transition '{state}' complete");
            _transitionCoroutine = null;
            OnTransitionAnimationComplete?.Invoke(state);
        }

        private bool IsTransitionState(CatController.CatState state)
        {
            return state == CatController.CatState.LayingDown ||
                   state == CatController.CatState.WakingUp ||
                   state == CatController.CatState.Stretching ||
                   state == CatController.CatState.Grooming;
        }

        private float GetTransitionDuration(CatController.CatState state)
        {
            return state switch
            {
                CatController.CatState.LayingDown => layingDownDuration,
                CatController.CatState.WakingUp => wakingUpDuration,
                CatController.CatState.Stretching => stretchingDuration,
                CatController.CatState.Grooming => groomingDuration,
                _ => 1.0f
            };
        }

        public void SetSpeed(float speed)
        {
            if (_useAnimator)
                _animator.SetFloat(SpeedParam, speed);
        }

        public void SetFacingDirection(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) > 0.01f)
                _spriteRenderer.flipX = horizontalDirection < 0;
        }

        public void FacePosition(Vector3 targetPosition)
        {
            float direction = targetPosition.x - transform.position.x;
            SetFacingDirection(direction);
        }

        public void SetTint(Color color)
        {
            _spriteRenderer.color = color;
        }

        public void ResetTint()
        {
            _spriteRenderer.color = Color.white;
        }

        public void PlayBounce()
        {
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
                float scaleY = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.1f * (1f - t);
                float scaleX = 1f - Mathf.Sin(t * Mathf.PI * 2f) * 0.05f * (1f - t);
                transform.localScale = new Vector3(scaleX, scaleY, 1f);
                yield return null;
            }

            transform.localScale = originalScale;
        }

        private void ApplyFallbackSprite(AnimState state)
        {
            switch (state)
            {
                case AnimState.Idle:
                case AnimState.Eating:
                case AnimState.Drinking:
                case AnimState.Playing:
                case AnimState.Stretching:
                case AnimState.WakingUp:
                case AnimState.Grooming:
                    if (idleSprite != null)
                        _spriteRenderer.sprite = idleSprite;
                    break;

                case AnimState.Sleeping:
                case AnimState.LayingDown:
                    if (sleepSprite != null)
                        _spriteRenderer.sprite = sleepSprite;
                    break;

                case AnimState.Walking:
                    if (idleSprite != null)
                        _spriteRenderer.sprite = idleSprite;
                    break;
            }
        }

        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public bool IsFlipped => _spriteRenderer != null && _spriteRenderer.flipX;
    }
}
