using UnityEngine;

namespace CatRaising.Cat
{
    /// <summary>
    /// Manages cat sprite animations, handling state-based animation transitions
    /// and sprite direction flipping.
    /// 
    /// Expected Animator Parameters (set up in Unity Editor):
    ///   - "State" (int): 0=Idle, 1=Walking, 2=Sleeping, 3=Eating, 4=Drinking, 5=Playing
    ///   - "Speed" (float): movement speed for walk animation playback rate
    /// 
    /// Expected Animator States:
    ///   - "Idle" (sitting sprite — can add breathing sub-animation)
    ///   - "Walk" (uses the 33-frame walk cycle sprite sheet)
    ///   - "Sleep" (lying down sprite — can add zzz sub-animation)
    ///   - "Eat" (placeholder until custom art is made)
    ///   - "Drink" (placeholder until custom art is made)
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CatAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Idle Sprites (used when no Animator is set up yet)")]
        [Tooltip("The sitting/standing sprite for idle state")]
        [SerializeField] private Sprite idleSprite;
        [Tooltip("The lying down sprite for sleep state")]
        [SerializeField] private Sprite sleepSprite;

        [Header("Settings")]
        [SerializeField] private float flipSmoothing = 0.1f;

        // Animator parameter hashes for performance
        private static readonly int StateParam = Animator.StringToHash("State");
        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        private bool _useAnimator = true;
        private CatController.CatState _currentAnimState;

        public enum AnimState
        {
            Idle = 0,
            Walking = 1,
            Sleeping = 2,
            Eating = 3,
            Drinking = 4,
            Playing = 5
        }

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();

            // Check if the Animator has a valid controller
            _useAnimator = _animator != null && _animator.runtimeAnimatorController != null;

            if (!_useAnimator)
            {
                Debug.Log("[CatAnimator] No Animator Controller found — using direct sprite swaps.");
            }
        }

        /// <summary>
        /// Set the current animation state based on the cat's FSM state.
        /// </summary>
        public void SetState(CatController.CatState state)
        {
            _currentAnimState = state;

            AnimState animState = state switch
            {
                CatController.CatState.Idle => AnimState.Idle,
                CatController.CatState.Walking => AnimState.Walking,
                CatController.CatState.Sleeping => AnimState.Sleeping,
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
            StopAllCoroutines();
            StartCoroutine(BounceCoroutine());
        }

        private System.Collections.IEnumerator BounceCoroutine()
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
        /// </summary>
        private void ApplyFallbackSprite(AnimState state)
        {
            switch (state)
            {
                case AnimState.Idle:
                case AnimState.Eating:
                case AnimState.Drinking:
                case AnimState.Playing:
                    if (idleSprite != null)
                        _spriteRenderer.sprite = idleSprite;
                    break;

                case AnimState.Sleeping:
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
