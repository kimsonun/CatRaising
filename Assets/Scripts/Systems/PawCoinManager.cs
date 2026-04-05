using UnityEngine;
using CatRaising.Data;

namespace CatRaising.Systems
{
    /// <summary>
    /// Manages the player's Paw Coin currency.
    /// </summary>
    public class PawCoinManager : MonoBehaviour
    {
        public static PawCoinManager Instance { get; private set; }

        [SerializeField] private int _coins = 0;

        public int Coins => _coins;

        public event System.Action<int> OnCoinsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void AddCoins(int amount, string source = "")
        {
            if (amount <= 0) return;
            _coins += amount;
            OnCoinsChanged?.Invoke(_coins);
            Debug.Log($"[PawCoin] +{amount} from {source}. Total: {_coins}");
        }

        public bool SpendCoins(int amount)
        {
            if (/*amount <= 0 ||*/ _coins < amount) return false;
            _coins -= amount;
            OnCoinsChanged?.Invoke(_coins);
            Debug.Log($"[PawCoin] -{amount} spent. Remaining: {_coins}");
            return true;
        }

        public bool CanAfford(int amount) => _coins >= amount;

        public void LoadFromData(GameData data) 
        { 
            _coins = data.pawCoins; 
            OnCoinsChanged?.Invoke(_coins);
        }

        public void SaveToData(GameData data) 
        { 
            data.pawCoins = _coins; 
        }
    }
}
