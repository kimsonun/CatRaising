using UnityEngine;
using CatRaising.Data;

namespace CatRaising.Systems
{
    /// <summary>
    /// Manages the bond/affection level between the player and the cat.
    /// Bond grows through consistent care and interaction. It never fully resets.
    /// </summary>
    public class BondSystem : MonoBehaviour
    {
        public static BondSystem Instance { get; private set; }

        [Header("Bond Level")]
        [SerializeField] private float _bondLevel = 0f;
        [SerializeField] private float maxBond = 100f;

        [Header("Bond Milestones")]
        [Tooltip("Bond never drops below the last reached milestone")]
        [SerializeField] private float[] milestones = { 0f, 10f, 25f, 50f, 75f, 90f };
        private int _currentMilestoneIndex = 0;

        /// <summary>
        /// Current bond level (0-100).
        /// </summary>
        public float BondLevel => _bondLevel;

        /// <summary>
        /// Normalized bond (0-1), useful for UI.
        /// </summary>
        public float NormalizedBond => _bondLevel / maxBond;

        /// <summary>
        /// The current bond tier name based on level.
        /// </summary>
        public string BondTierName
        {
            get
            {
                if (_bondLevel >= 91f) return "Soulmate";
                if (_bondLevel >= 76f) return "Best Friend";
                if (_bondLevel >= 51f) return "Companion";
                if (_bondLevel >= 26f) return "Friend";
                if (_bondLevel >= 11f) return "Acquaintance";
                return "Stranger";
            }
        }

        /// <summary>
        /// Event fired when bond level changes.
        /// </summary>
        public event System.Action<float> OnBondChanged;

        /// <summary>
        /// Event fired when a new milestone is reached.
        /// </summary>
        public event System.Action<int, string> OnMilestoneReached;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Add bond points from a specific interaction.
        /// </summary>
        public void AddBond(float amount, string source = "")
        {
            float previousLevel = Mathf.FloorToInt(_bondLevel);
            _bondLevel = Mathf.Min(maxBond, _bondLevel + amount);
            // Check for milestone
            CheckMilestones(previousLevel, _bondLevel);

            OnBondChanged?.Invoke(_bondLevel);

            if (!string.IsNullOrEmpty(source))
                Debug.Log($"[BondSystem] +{amount:F1} bond from {source}. Total: {_bondLevel:F1} ({BondTierName})");
        }

        /// <summary>
        /// Decay bond slightly (called for long absences, very gentle).
        /// Bond never drops below the last milestone.
        /// </summary>
        public void DecayBond(float amount)
        {
            float floor = GetMilestoneFloor();
            _bondLevel = Mathf.Max(floor, _bondLevel - amount);
            OnBondChanged?.Invoke(_bondLevel);
        }

        /// <summary>
        /// Get the minimum bond value (last reached milestone).
        /// </summary>
        private float GetMilestoneFloor()
        {
            float floor = 0f;
            for (int i = 0; i < milestones.Length; i++)
            {
                if (_bondLevel >= milestones[i])
                    floor = milestones[i];
                else
                    break;
            }
            return floor;
        }

        /// <summary>
        /// Check if any new milestones were reached.
        /// </summary>
        private void CheckMilestones(float oldLevel, float newLevel)
        {
            for (int i = _currentMilestoneIndex + 1; i < milestones.Length; i++)
            {
                if (oldLevel <= milestones[i] && newLevel >= milestones[i])
                {
                    _currentMilestoneIndex = i;
                    string tierName = BondTierName;
                    Debug.Log($"[BondSystem] 🎉 Milestone reached! Bond tier: {tierName} (Level {milestones[i]})");
                    OnMilestoneReached?.Invoke(i, tierName);
                }
            }
        }

        // --- Save/Load ---

        public void LoadFromData(GameData data)
        {
            _bondLevel = data.bondLevel;

            // Recalculate milestone index
            _currentMilestoneIndex = 0;
            for (int i = 0; i < milestones.Length; i++)
            {
                if (_bondLevel >= milestones[i])
                    _currentMilestoneIndex = i;
            }

            OnBondChanged?.Invoke(_bondLevel);
            Debug.Log($"[BondSystem] Loaded. Bond: {_bondLevel:F1} ({BondTierName})");
        }

        public void SaveToData(GameData data)
        {
            data.bondLevel = _bondLevel;
        }
    }
}
