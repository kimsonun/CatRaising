using UnityEngine;
using CatRaising.Cat;
using CatRaising.Core;
using CatRaising.Systems;

namespace CatRaising.Interactables
{
    /// <summary>
    /// Water bowl interactable. Player taps to fill it, cat walks over to drink.
    /// Functions similarly to FoodBowl but restores thirst.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaterBowl : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CatController catController;
        [SerializeField] private CatNeeds catNeeds;

        [Header("Bowl Sprites")]
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Sprite fullSprite;

        [Header("Settings")]
        [SerializeField] private float fillAmount = 100f;
        [SerializeField] private float drinkSpeed = 12f;
        [SerializeField] private float thirstPerWaterUnit = 1f;
        [SerializeField] private float bondPerWater = 0.8f;
        [SerializeField] private float drinkDistance = 1f;
        [SerializeField] private float drinkDuration = 4f;

        [Header("Feedback")]
        [SerializeField] private GameObject fillEffectPrefab;

        private float _waterAmount = 0f;
        private bool _catIsDrinking = false;
        private float _drinkTimer = 0f;
        private Camera _mainCamera;

        public float WaterAmount => _waterAmount;
        public bool IsFull => _waterAmount > 0f;
        public bool IsEmpty => _waterAmount <= 0f;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (GameManager.Instance != null && GameManager.Instance.Data != null)
                _waterAmount = GameManager.Instance.Data.waterBowlAmount;

            UpdateSprite();
        }

        private void Update()
        {
            HandleInput();
            HandleCatDrinking();
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsTouchingBowl() && IsEmpty)
                {
                    FillBowl();
                }
            }
        }

        private bool IsTouchingBowl()
        {
            if (_mainCamera == null) return false;
            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            return hit != null && hit.gameObject == gameObject;
        }

        public void FillBowl()
        {
            _waterAmount = fillAmount;
            UpdateSprite();

            if (BondSystem.Instance != null)
                BondSystem.Instance.AddBond(bondPerWater, "watering");

            if (fillEffectPrefab != null)
                Instantiate(fillEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

            Debug.Log("[WaterBowl] Bowl filled!");
        }

        private void HandleCatDrinking()
        {
            if (_waterAmount <= 0f || catController == null || catNeeds == null)
            {
                if (_catIsDrinking) StopDrinking();
                return;
            }

            float distance = Vector2.Distance(transform.position, catController.transform.position);

            if (distance < drinkDistance && !_catIsDrinking && catNeeds.IsThirsty)
            {
                StartDrinking();
            }

            if (_catIsDrinking)
            {
                _drinkTimer += Time.deltaTime;

                float drunk = drinkSpeed * Time.deltaTime;
                _waterAmount = Mathf.Max(0f, _waterAmount - drunk);
                catNeeds.GiveWater(drunk * thirstPerWaterUnit);

                UpdateSprite();

                if (GameManager.Instance != null && GameManager.Instance.Data != null)
                    GameManager.Instance.Data.waterBowlAmount = _waterAmount;

                if (_drinkTimer >= drinkDuration || _waterAmount <= 0f)
                {
                    StopDrinking();
                }
            }
        }

        private void StartDrinking()
        {
            _catIsDrinking = true;
            _drinkTimer = 0f;
            catController.RequestState(CatController.CatState.Drinking);
            Debug.Log("[WaterBowl] Cat started drinking.");
        }

        private void StopDrinking()
        {
            _catIsDrinking = false;
            _drinkTimer = 0f;
            if (catController.CurrentState == CatController.CatState.Drinking)
                catController.RequestState(CatController.CatState.Idle);
            Debug.Log("[WaterBowl] Cat finished drinking.");
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null) return;

            if (_waterAmount > 0f && fullSprite != null)
                spriteRenderer.sprite = fullSprite;
            else if (emptySprite != null)
                spriteRenderer.sprite = emptySprite;
        }
    }
}
