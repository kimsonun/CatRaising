using UnityEngine;

namespace CatRaising.MiniGame
{
    /// <summary>
    /// Spawns fish from screen edges during the mini-game.
    /// Attached to CatFishingGame or a child object.
    /// </summary>
    public class FishSpawner : MonoBehaviour
    {
        [Header("Fish Prefab")]
        [SerializeField] private GameObject fishPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private float baseSpawnInterval = 1.5f;
        [SerializeField] private float minSpawnInterval = 0.4f;
        [SerializeField] private float intervalDecreasePerSecond = 0.03f;
        [SerializeField] private float goldenChance = 0.1f;

        [Header("Fish Speed")]
        [SerializeField] private float baseSpeed = 2f;
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float speedIncreasePerSecond = 0.05f;

        [Header("Spawn Bounds")]
        [SerializeField] private float spawnXOffset = 10f;
        [SerializeField] private float minY = -3f;
        [SerializeField] private float maxY = 2f;

        private float _spawnTimer;
        private float _elapsed;
        private bool _isSpawning = false;

        public void StartSpawning()
        {
            _isSpawning = true;
            _elapsed = 0f;
            _spawnTimer = 0f;
        }

        public void StopSpawning()
        {
            _isSpawning = false;

            // Destroy all remaining fish
            var fish = FindObjectsByType<Fish>(FindObjectsSortMode.None);
            foreach (var f in fish)
                Destroy(f.gameObject);
        }

        private void Update()
        {
            if (!_isSpawning) return;

            _elapsed += Time.deltaTime;
            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0f)
            {
                SpawnFish();

                // Decrease interval over time (game gets harder)
                float currentInterval = Mathf.Max(minSpawnInterval,
                    baseSpawnInterval - intervalDecreasePerSecond * _elapsed);
                _spawnTimer = currentInterval + Random.Range(-0.2f, 0.2f);
            }
        }

        private void SpawnFish()
        {
            if (fishPrefab == null) return;

            // Random side (left or right)
            bool fromLeft = Random.value > 0.5f;
            float spawnX = fromLeft ? -spawnXOffset : spawnXOffset;
            float spawnY = Random.Range(minY, maxY);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);
            Vector3 direction = fromLeft ? Vector3.right : Vector3.left;

            // Speed increases over time
            float speed = Mathf.Min(maxSpeed, baseSpeed + speedIncreasePerSecond * _elapsed);
            speed += Random.Range(-0.5f, 0.5f);

            // Golden chance
            bool golden = Random.value < goldenChance;

            var fishObj = Instantiate(fishPrefab, spawnPos, Quaternion.identity);
            var fish = fishObj.GetComponent<Fish>();
            if (fish != null)
                fish.Initialize(spawnPos, direction, speed, golden);
        }
    }
}
