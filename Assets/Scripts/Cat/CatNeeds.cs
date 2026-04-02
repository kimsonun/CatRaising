using UnityEngine;
using CatRaising.Data;

namespace CatRaising.Cat
{
    /// <summary>
    /// Manages the cat's four core needs: Hunger, Thirst, Happiness, Cleanliness.
    /// Needs decay in real-time and affect cat behavior.
    /// </summary>
    public class CatNeeds : MonoBehaviour
    {
        [Header("Current Needs (0-100)")]
        [SerializeField] private float _hunger = 100f;
        [SerializeField] private float _thirst = 100f;
        [SerializeField] private float _happiness = 100f;
        [SerializeField] private float _cleanliness = 100f;

        [Header("Decay Rates (per second)")]
        [Tooltip("Hunger empties in ~4 hours")]
        [SerializeField] private float hungerDecayRate = 0.00694f;   // 100 / (4 * 3600)
        [Tooltip("Thirst empties in ~3 hours")]
        [SerializeField] private float thirstDecayRate = 0.00926f;   // 100 / (3 * 3600)
        [Tooltip("Happiness empties in ~6 hours")]
        [SerializeField] private float happinessDecayRate = 0.00463f; // 100 / (6 * 3600)
        [Tooltip("Cleanliness empties in ~8 hours")]
        [SerializeField] private float cleanlinessDecayRate = 0.00347f; // 100 / (8 * 3600)

        [Header("Minimum Values (needs never drop below these)")]
        [SerializeField] private float hungerFloor = 5f;
        [SerializeField] private float thirstFloor = 5f;
        [SerializeField] private float happinessFloor = 10f;
        [SerializeField] private float cleanlinessFloor = 15f;

        // Public read-only access
        public float Hunger => _hunger;
        public float Thirst => _thirst;
        public float Happiness => _happiness;
        public float Cleanliness => _cleanliness;

        /// <summary>
        /// Returns the average of all needs (0-100). Useful as an overall "mood" indicator.
        /// </summary>
        public float OverallMood => (_hunger + _thirst + _happiness + _cleanliness) / 4f;

        /// <summary>
        /// Returns the lowest need value. The cat will prioritize this.
        /// </summary>
        public float LowestNeed => Mathf.Min(_hunger, Mathf.Min(_thirst, Mathf.Min(_happiness, _cleanliness)));

        /// <summary>
        /// Which need is currently the lowest.
        /// </summary>
        public NeedType MostUrgentNeed
        {
            get
            {
                float min = LowestNeed;
                if (Mathf.Approximately(min, _hunger)) return NeedType.Hunger;
                if (Mathf.Approximately(min, _thirst)) return NeedType.Thirst;
                if (Mathf.Approximately(min, _happiness)) return NeedType.Happiness;
                return NeedType.Cleanliness;
            }
        }


        private void Update()
        {
            DecayNeeds();
        }

        /// <summary>
        /// Apply real-time decay to all needs.
        /// </summary>
        private void DecayNeeds()
        {
            float dt = Time.deltaTime;

            _hunger = Mathf.Max(hungerFloor, _hunger - hungerDecayRate * dt);
            _thirst = Mathf.Max(thirstFloor, _thirst - thirstDecayRate * dt);
            _happiness = Mathf.Max(happinessFloor, _happiness - happinessDecayRate * dt);
            _cleanliness = Mathf.Max(cleanlinessFloor, _cleanliness - cleanlinessDecayRate * dt);
        }

        // --- Modification methods (called by interactions) ---

        /// <summary>
        /// Feed the cat. Adds to hunger satisfaction.
        /// </summary>
        public void Feed(float amount)
        {
            _hunger = Mathf.Min(100f, _hunger + amount);
            Debug.Log($"[CatNeeds] Fed! Hunger: {_hunger:F1}");
        }

        /// <summary>
        /// Give the cat water. Adds to thirst satisfaction.
        /// </summary>
        public void GiveWater(float amount)
        {
            _thirst = Mathf.Min(100f, _thirst + amount);
            Debug.Log($"[CatNeeds] Watered! Thirst: {_thirst:F1}");
        }

        /// <summary>
        /// Increase happiness (from petting, playing, etc).
        /// </summary>
        public void IncreaseHappiness(float amount)
        {
            _happiness = Mathf.Min(100f, _happiness + amount);
        }

        /// <summary>
        /// Increase cleanliness (from grooming/cleaning).
        /// </summary>
        public void IncreaseCleanliness(float amount)
        {
            _cleanliness = Mathf.Min(100f, _cleanliness + amount);
        }

        /// <summary>
        /// Check if a particular need is low (below threshold).
        /// </summary>
        public bool IsNeedLow(NeedType need, float threshold = 30f)
        {
            return need switch
            {
                NeedType.Hunger => _hunger < threshold,
                NeedType.Thirst => _thirst < threshold,
                NeedType.Happiness => _happiness < threshold,
                NeedType.Cleanliness => _cleanliness < threshold,
                _ => false
            };
        }

        /// <summary>
        /// Check if hunger is critically low.
        /// </summary>
        public bool IsHungry => _hunger < 30f;

        /// <summary>
        /// Check if thirst is critically low.
        /// </summary>
        public bool IsThirsty => _thirst < 30f;

        /// <summary>
        /// Check if happiness is low.
        /// </summary>
        public bool IsSad => _happiness < 30f;

        /// <summary>
        /// Check if the cat is content (all needs above 60).
        /// </summary>
        public bool IsContent => _hunger > 60f && _thirst > 60f && _happiness > 60f && _cleanliness > 60f;

        // --- Save/Load ---

        public void LoadFromData(GameData data)
        {
            _hunger = data.hunger;
            _thirst = data.thirst;
            _happiness = data.happiness;
            _cleanliness = data.cleanliness;
        }

        public void SaveToData(GameData data)
        {
            data.hunger = _hunger;
            data.thirst = _thirst;
            data.happiness = _happiness;
            data.cleanliness = _cleanliness;
        }
    }
}
