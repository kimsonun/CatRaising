using UnityEngine;
using CatRaising.Core;
using CatRaising.Systems;

namespace CatRaising.Cat
{
    /// <summary>
    /// Handles touch/click interactions with the cat: petting, chin scratches, etc.
    /// Only active when ToolModeManager is in Hand mode.
    /// Uses Physics2D raycasting for object detection.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CatInteraction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CatController catController;
        [SerializeField] private CatNeeds catNeeds;
        [SerializeField] private CatAnimator catAnimator;

        [Header("Petting Settings")]
        [Tooltip("How long the player must hold to start petting (seconds)")]
        [SerializeField] private float petStartDelay = 0.15f;
        [Tooltip("Happiness gained per second of petting")]
        [SerializeField] private float petHappinessPerSecond = 3f;
        [Tooltip("Bond gained per pet action")]
        [SerializeField] private float bondPerPet = 0.5f;
        [Tooltip("Minimum time between bond gains from continuous petting")]
        [SerializeField] private float petBondCooldown = 2f;
        [Tooltip("How long after releasing before cat exits BeingPet state")]
        [SerializeField] private float petExitDelay = 1f;

        [Header("Quick Tap Bond Cooldown")]
        [Tooltip("Cooldown between quick-tap bond gains (seconds)")]
        [SerializeField] private float quickTapBondCooldown = 3f;

        [Header("Effects")]
        [Tooltip("Prefab for floating hearts")]
        [SerializeField] private GameObject heartEffectPrefab;
        [Tooltip("Heart spawn offset above the cat")]
        [SerializeField] private Vector3 heartOffset = new Vector3(0, 1f, 0);
        [Tooltip("Seconds between heart spawns while petting")]
        [SerializeField] private float heartSpawnInterval = 0.5f;

        // Internal state
        private bool _isTouching = false;
        private bool _isPetting = false;
        private float _touchTimer = 0f;
        private float _petBondTimer = 0f;
        private float _heartTimer = 0f;
        private float _petExitTimer = 0f;
        private float _quickTapBondCooldownTimer = 0f;

        private void Awake()
        {
            if (catController == null) catController = GetComponent<CatController>();
            if (catNeeds == null) catNeeds = GetComponent<CatNeeds>();
            if (catAnimator == null) catAnimator = GetComponent<CatAnimator>();
        }

        private void Update()
        {
            // Tick cooldown timers
            if (_quickTapBondCooldownTimer > 0f)
                _quickTapBondCooldownTimer -= Time.deltaTime;

            HandleTouchInput();
            UpdatePetting();
            UpdatePetExit();
        }

        /// <summary>
        /// Handle touch/mouse input for interaction with the cat.
        /// Only responds in Hand mode.
        /// </summary>
        private void HandleTouchInput()
        {
            // Only allow petting in Hand mode
            if (ToolModeManager.Instance != null && !ToolModeManager.Instance.IsHandMode)
            {
                // If we were touching, cancel
                if (_isTouching) { _isTouching = false; }
                if (_isPetting) { StopPetting(); }
                return;
            }

            // Touch start
            if (TouchInput.WasPressedThisFrame)
            {
                if (IsTouchingCat())
                {
                    _isTouching = true;
                    _touchTimer = 0f;
                }
            }

            // Touch held
            if (TouchInput.IsPressed && _isTouching)
            {
                _touchTimer += Time.deltaTime;

                if (!_isPetting && _touchTimer >= petStartDelay)
                {
                    StartPetting();
                }
            }

            // Touch released
            if (TouchInput.WasReleasedThisFrame)
            {
                if (_isTouching && !_isPetting)
                {
                    OnQuickTap();
                }

                _isTouching = false;

                if (_isPetting)
                {
                    StopPetting();
                }
            }
        }

        private bool IsTouchingCat()
        {
            return TouchInput.IsOverGameObject(gameObject);
        }

        /// <summary>
        /// Quick tap interaction — single pet with bond cooldown.
        /// </summary>
        private void OnQuickTap()
        {
            if (catNeeds != null)
                catNeeds.IncreaseHappiness(1f);

            if (catAnimator != null)
                catAnimator.PlayBounce();

            SpawnHeart();

            // Bond gain with cooldown
            if (_quickTapBondCooldownTimer <= 0f)
            {
                if (BondSystem.Instance != null)
                    BondSystem.Instance.AddBond(bondPerPet * 0.5f, "quick pet");

                _quickTapBondCooldownTimer = quickTapBondCooldown;
            }

            Debug.Log("[CatInteraction] Quick tap pet!");
        }

        /// <summary>
        /// Start continuous petting (hold interaction).
        /// Triggers camera zoom instead of scaling the cat.
        /// </summary>
        private void StartPetting()
        {
            _isPetting = true;
            _heartTimer = 0f;
            _petExitTimer = 0f;
            _petBondTimer = 0f;

            catController.RequestState(CatController.CatState.BeingPet);

            // Start purring sound
            if (Systems.SoundEffectHooks.Instance != null)
                Systems.SoundEffectHooks.Instance.StartAmbient("purr");

            // Zoom camera toward the cat instead of scaling
            if (CameraController.Instance != null)
                CameraController.Instance.StartPetZoom(transform);

            // Daily task hook
            if (DailyTaskManager.Instance != null)
                DailyTaskManager.Instance.CheckTask(DailyTaskType.PetCat);

            Debug.Log("[CatInteraction] Petting started!");
        }

        /// <summary>
        /// Update continuous petting effects.
        /// </summary>
        private void UpdatePetting()
        {
            if (!_isPetting) return;

            // Increase happiness over time
            if (catNeeds != null)
                catNeeds.IncreaseHappiness(petHappinessPerSecond * Time.deltaTime);

            // Cleanliness increases slightly from petting (grooming)
            if (catNeeds != null)
                catNeeds.IncreaseCleanliness(petHappinessPerSecond * 0.3f * Time.deltaTime);

            // Periodic bond gain with cooldown
            _petBondTimer += Time.deltaTime;
            if (_petBondTimer >= petBondCooldown)
            {
                if (BondSystem.Instance != null)
                    BondSystem.Instance.AddBond(bondPerPet, "petting");

                _petBondTimer = 0f;
            }

            // Spawn hearts periodically
            _heartTimer += Time.deltaTime;
            if (_heartTimer >= heartSpawnInterval)
            {
                SpawnHeart();
                _heartTimer = 0f;
            }
        }

        /// <summary>
        /// Stop petting (player released touch).
        /// Restores camera zoom.
        /// </summary>
        private void StopPetting()
        {
            _isPetting = false;
            _petExitTimer = petExitDelay;

            // Stop purring sound
            if (Systems.SoundEffectHooks.Instance != null)
                Systems.SoundEffectHooks.Instance.StopAmbient();

            // Zoom camera back out
            if (CameraController.Instance != null)
                CameraController.Instance.StopPetZoom();

            if (GameManager.Instance != null && GameManager.Instance.Data != null)
                GameManager.Instance.Data.totalPets++;

            Debug.Log("[CatInteraction] Petting stopped.");
        }

        private void UpdatePetExit()
        {
            if (_petExitTimer <= 0f) return;

            _petExitTimer -= Time.deltaTime;
            if (_petExitTimer <= 0f)
            {
                catController.RequestState(CatController.CatState.Idle);
            }
        }

        private void SpawnHeart()
        {
            if (heartEffectPrefab != null)
            {
                Vector3 spawnPos = transform.position + heartOffset;
                spawnPos += new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.1f, 0.1f), 0f);
                GameObject heart = Instantiate(heartEffectPrefab, spawnPos, Quaternion.identity);
                Destroy(heart, 2f);
            }
        }

        public bool IsPetting => _isPetting;
    }
}
