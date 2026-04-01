using UnityEngine;

namespace CatRaising.Cat
{
    /// <summary>
    /// Adds variety to the cat's idle state with sub-behaviors:
    /// yawn, stretch, groom, and tail-flick.
    /// 
    /// When the cat is in the Idle state, this script randomly triggers
    /// mini-animations (or sprite effects when full animations aren't set up yet).
    /// 
    /// Attach to the same GameObject as CatController.
    /// </summary>
    public class CatIdleBehaviors : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CatController catController;
        [SerializeField] private CatAnimator catAnimator;
        [SerializeField] private CatNeeds catNeeds;

        [Header("Timing")]
        [Tooltip("Minimum seconds between idle behaviors")]
        [SerializeField] private float minInterval = 8f;
        [Tooltip("Maximum seconds between idle behaviors")]
        [SerializeField] private float maxInterval = 25f;
        [Tooltip("Duration of each idle behavior animation")]
        [SerializeField] private float behaviorDuration = 2f;

        [Header("Behavior Probabilities (should sum to ~1.0)")]
        [SerializeField] private float yawnChance = 0.3f;
        [SerializeField] private float stretchChance = 0.2f;
        [SerializeField] private float groomChance = 0.3f;
        [SerializeField] private float tailFlickChance = 0.2f;

        [Header("Optional Sprites (for visual feedback without Animator)")]
        [SerializeField] private Sprite yawnSprite;
        [SerializeField] private Sprite stretchSprite;
        [SerializeField] private Sprite groomSprite;

        [Header("Sound Hooks")]
        [Tooltip("Play sounds via SoundEffectHooks when behaviors trigger")]
        [SerializeField] private bool playSounds = true;

        public enum IdleBehavior
        {
            None,
            Yawn,
            Stretch,
            Groom,
            TailFlick
        }

        // State
        private float _timer;
        private bool _isPerforming = false;
        private float _performTimer = 0f;
        private IdleBehavior _currentBehavior = IdleBehavior.None;
        private Sprite _originalSprite;
        private SpriteRenderer _spriteRenderer;

        /// <summary>
        /// The currently active idle behavior (or None).
        /// </summary>
        public IdleBehavior CurrentBehavior => _currentBehavior;

        /// <summary>
        /// Event fired when an idle behavior starts.
        /// </summary>
        public event System.Action<IdleBehavior> OnBehaviorStarted;

        /// <summary>
        /// Event fired when an idle behavior ends.
        /// </summary>
        public event System.Action<IdleBehavior> OnBehaviorEnded;

        private void Awake()
        {
            if (catController == null) catController = GetComponent<CatController>();
            if (catAnimator == null) catAnimator = GetComponent<CatAnimator>();
            if (catNeeds == null) catNeeds = GetComponent<CatNeeds>();

            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            ResetTimer();
        }

        private void Update()
        {
            // Only trigger idle behaviors when the cat is actually idle
            if (catController == null || catController.CurrentState != CatController.CatState.Idle)
            {
                if (_isPerforming)
                    EndBehavior();
                return;
            }

            // If currently performing a behavior, update its timer
            if (_isPerforming)
            {
                _performTimer -= Time.deltaTime;
                UpdateBehaviorAnimation();

                if (_performTimer <= 0f)
                {
                    EndBehavior();
                }
                return;
            }

            // Count down to next idle behavior
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                PickAndStartBehavior();
                ResetTimer();
            }
        }

        /// <summary>
        /// Randomly pick an idle behavior based on weighted probabilities.
        /// </summary>
        private void PickAndStartBehavior()
        {
            float roll = Random.value;
            float cumulative = 0f;

            cumulative += yawnChance;
            if (roll < cumulative)
            {
                StartBehavior(IdleBehavior.Yawn);
                return;
            }

            cumulative += stretchChance;
            if (roll < cumulative)
            {
                StartBehavior(IdleBehavior.Stretch);
                return;
            }

            cumulative += groomChance;
            if (roll < cumulative)
            {
                StartBehavior(IdleBehavior.Groom);
                return;
            }

            StartBehavior(IdleBehavior.TailFlick);
        }

        /// <summary>
        /// Start an idle behavior with visual/audio feedback.
        /// </summary>
        private void StartBehavior(IdleBehavior behavior)
        {
            _currentBehavior = behavior;
            _isPerforming = true;
            _performTimer = behaviorDuration;

            // Store original sprite for restoration
            if (_spriteRenderer != null)
                _originalSprite = _spriteRenderer.sprite;

            switch (behavior)
            {
                case IdleBehavior.Yawn:
                    if (yawnSprite != null && _spriteRenderer != null)
                        _spriteRenderer.sprite = yawnSprite;
                    PlayBehaviorSound("yawn");
                    Debug.Log("[CatIdleBehaviors] 🥱 Yawn");
                    break;

                case IdleBehavior.Stretch:
                    if (stretchSprite != null && _spriteRenderer != null)
                        _spriteRenderer.sprite = stretchSprite;
                    // Stretch visual: squash & stretch effect
                    if (catAnimator != null)
                        catAnimator.PlayBounce();
                    PlayBehaviorSound("stretch");
                    Debug.Log("[CatIdleBehaviors] 🙆 Stretch");
                    break;

                case IdleBehavior.Groom:
                    if (groomSprite != null && _spriteRenderer != null)
                        _spriteRenderer.sprite = groomSprite;
                    // Grooming increases cleanliness slightly
                    if (catNeeds != null)
                        catNeeds.IncreaseCleanliness(1f);
                    Debug.Log("[CatIdleBehaviors] 🧹 Groom");
                    break;

                case IdleBehavior.TailFlick:
                    // Tail flick: quick horizontal flip back and forth
                    StartCoroutine(TailFlickCoroutine());
                    Debug.Log("[CatIdleBehaviors] 🐱 Tail flick");
                    break;
            }

            OnBehaviorStarted?.Invoke(behavior);
        }

        /// <summary>
        /// Update running behavior animations.
        /// </summary>
        private void UpdateBehaviorAnimation()
        {
            switch (_currentBehavior)
            {
                case IdleBehavior.Groom:
                    // Subtle bobbing during grooming
                    float bob = Mathf.Sin(Time.time * 6f) * 0.02f;
                    transform.localPosition = new Vector3(
                        transform.localPosition.x,
                        transform.localPosition.y + bob * Time.deltaTime,
                        transform.localPosition.z
                    );
                    break;
            }
        }

        /// <summary>
        /// End the current idle behavior and restore normal state.
        /// </summary>
        private void EndBehavior()
        {
            IdleBehavior endedBehavior = _currentBehavior;
            _isPerforming = false;
            _currentBehavior = IdleBehavior.None;

            // Restore original sprite if we changed it
            if (_originalSprite != null && _spriteRenderer != null)
            {
                // Only restore if we're still idle (CatAnimator handles state changes)
                if (catController != null && catController.CurrentState == CatController.CatState.Idle)
                    _spriteRenderer.sprite = _originalSprite;
            }

            OnBehaviorEnded?.Invoke(endedBehavior);
        }

        /// <summary>
        /// Quick sprite-flip effect for tail flick (no dedicated sprite needed).
        /// </summary>
        private System.Collections.IEnumerator TailFlickCoroutine()
        {
            if (_spriteRenderer == null) yield break;

            bool originalFlip = _spriteRenderer.flipX;

            // Quick flip sequence: flip → unflip → flip → unflip
            for (int i = 0; i < 3; i++)
            {
                _spriteRenderer.flipX = !_spriteRenderer.flipX;
                yield return new WaitForSeconds(0.15f);
                _spriteRenderer.flipX = originalFlip;
                yield return new WaitForSeconds(0.15f);
            }
        }

        /// <summary>
        /// Play a sound if the sound system is available and sounds are enabled.
        /// </summary>
        private void PlayBehaviorSound(string soundName)
        {
            if (!playSounds) return;

            if (Systems.SoundEffectHooks.Instance != null)
                Systems.SoundEffectHooks.Instance.PlaySound(soundName);
        }

        private void ResetTimer()
        {
            _timer = Random.Range(minInterval, maxInterval);
        }

        // --- Editor Gizmos ---
        private void OnDrawGizmosSelected()
        {
            if (_isPerforming)
            {
                // Show a label with current behavior
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
            }
        }
    }
}
