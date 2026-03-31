using UnityEngine;
using CatRaising.Core;
using CatRaising.Systems;

namespace CatRaising.Cat
{
    /// <summary>
    /// Handles touch/click interactions with the cat: petting, chin scratches, etc.
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
        [Tooltip("Minimum time between bond gains from petting (prevents spam)")]
        [SerializeField] private float petBondCooldown = 2f;
        [Tooltip("How long after releasing before cat exits BeingPet state")]
        [SerializeField] private float petExitDelay = 1f;

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
        private Camera _mainCamera;

        private void Awake()
        {
            if (catController == null) catController = GetComponent<CatController>();
            if (catNeeds == null) catNeeds = GetComponent<CatNeeds>();
            if (catAnimator == null) catAnimator = GetComponent<CatAnimator>();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleTouchInput();
            UpdatePetting();
            UpdatePetExit();
        }

        /// <summary>
        /// Handle touch/mouse input for interaction with the cat.
        /// </summary>
        private void HandleTouchInput()
        {
            // Touch start
            if (Input.GetMouseButtonDown(0))
            {
                if (IsTouchingCat())
                {
                    _isTouching = true;
                    _touchTimer = 0f;
                }
            }

            // Touch held
            if (Input.GetMouseButton(0) && _isTouching)
            {
                _touchTimer += Time.deltaTime;

                // Start petting after delay
                if (!_isPetting && _touchTimer >= petStartDelay)
                {
                    StartPetting();
                }
            }

            // Touch released
            if (Input.GetMouseButtonUp(0))
            {
                if (_isTouching && !_isPetting)
                {
                    // Quick tap = single pet
                    OnQuickTap();
                }

                _isTouching = false;

                if (_isPetting)
                {
                    StopPetting();
                }
            }
        }

        /// <summary>
        /// Check if the touch/click position hits the cat's collider.
        /// </summary>
        private bool IsTouchingCat()
        {
            if (_mainCamera == null) return false;

            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);

            return hit != null && hit.gameObject == gameObject;
        }

        /// <summary>
        /// Quick tap interaction — single pet.
        /// </summary>
        private void OnQuickTap()
        {
            if (catNeeds != null)
                catNeeds.IncreaseHappiness(1f);

            // Bounce effect
            if (catAnimator != null)
                catAnimator.PlayBounce();

            // Spawn a single heart
            SpawnHeart();

            // Quick bond gain
            if (BondSystem.Instance != null)
                BondSystem.Instance.AddBond(bondPerPet * 0.5f, "quick pet");

            Debug.Log("[CatInteraction] Quick tap pet!");
        }

        /// <summary>
        /// Start continuous petting (hold interaction).
        /// </summary>
        private void StartPetting()
        {
            _isPetting = true;
            _heartTimer = 0f;
            _petExitTimer = 0f;

            catController.RequestState(CatController.CatState.BeingPet);

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

            // Periodic bond gain
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
        /// </summary>
        private void StopPetting()
        {
            _isPetting = false;
            _petExitTimer = petExitDelay;

            if (GameManager.Instance != null && GameManager.Instance.Data != null)
                GameManager.Instance.Data.totalPets++;

            Debug.Log("[CatInteraction] Petting stopped.");
        }

        /// <summary>
        /// After petting stops, wait briefly before returning to normal state.
        /// </summary>
        private void UpdatePetExit()
        {
            if (_petExitTimer <= 0f) return;

            _petExitTimer -= Time.deltaTime;
            if (_petExitTimer <= 0f)
            {
                catController.RequestState(CatController.CatState.Idle);
            }
        }

        /// <summary>
        /// Spawn a floating heart effect above the cat.
        /// </summary>
        private void SpawnHeart()
        {
            if (heartEffectPrefab != null)
            {
                Vector3 spawnPos = transform.position + heartOffset;
                // Add slight random offset for visual variety
                spawnPos += new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.1f, 0.1f), 0f);

                GameObject heart = Instantiate(heartEffectPrefab, spawnPos, Quaternion.identity);
                Destroy(heart, 2f); // Auto-cleanup
            }
        }

        /// <summary>
        /// Whether the cat is currently being pet.
        /// </summary>
        public bool IsPetting => _isPetting;
    }
}
