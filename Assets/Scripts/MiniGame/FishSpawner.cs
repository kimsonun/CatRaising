using UnityEngine;

namespace CatRaising.MiniGame
{
    /// <summary>
    /// Spawns FishUI elements inside a RectTransform container.
    /// Fish swim from left/right edges across the panel.
    /// 
    /// SETUP:
    /// 1. Create a FishUI prefab (Image + Button + FishUI script)
    /// 2. Assign this spawner on the mini-game panel
    /// 3. Set fishArea to the RectTransform where fish should appear
    /// </summary>
    public class FishSpawner : MonoBehaviour
    {
        [Header("Fish Prefab")]
        [SerializeField] private GameObject fishUIPrefab; // Must have FishUI component

        [Header("Spawn Area")]
        [Tooltip("The RectTransform area where fish swim (should be full-screen or game area)")]
        [SerializeField] private RectTransform fishArea;

        [Header("Fish Sprites")]
        [SerializeField] private Sprite normalFishSprite;
        [SerializeField] private Sprite goldenFishSprite;

        [Header("Spawn Settings")]
        [SerializeField] private float baseSpawnInterval = 1.5f;
        [SerializeField] private float minSpawnInterval = 0.4f;
        [SerializeField] private float intervalDecreasePerSecond = 0.03f;
        [SerializeField] private float goldenChance = 0.1f;

        [Header("Fish Speed (pixels/sec)")]
        [SerializeField] private float baseSpeed = 250f;
        [SerializeField] private float maxSpeed = 500f;
        [SerializeField] private float speedIncreasePerSecond = 5f;

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

            // Destroy all remaining fish in the area
            if (fishArea != null)
            {
                var allFish = fishArea.GetComponentsInChildren<FishUI>();
                foreach (var f in allFish)
                    Destroy(f.gameObject);
            }
        }

        private void Update()
        {
            if (!_isSpawning) return;

            _elapsed += Time.deltaTime;
            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0f)
            {
                SpawnFish();
                float currentInterval = Mathf.Max(minSpawnInterval,
                    baseSpawnInterval - intervalDecreasePerSecond * _elapsed);
                _spawnTimer = currentInterval + Random.Range(-0.2f, 0.2f);
            }
        }

        private void SpawnFish()
        {
            if (fishUIPrefab == null || fishArea == null) return;

            Rect area = fishArea.rect;
            float halfWidth = area.width * 0.5f;
            float halfHeight = area.height * 0.5f;

            // Random left or right edge
            bool fromLeft = Random.value > 0.5f;
            float spawnX = fromLeft ? -halfWidth - 50f : halfWidth + 50f;
            float spawnY = Random.Range(-halfHeight * 0.6f, halfHeight * 0.6f);
            Vector2 spawnPos = new Vector2(spawnX, spawnY);
            Vector2 direction = fromLeft ? Vector2.right : Vector2.left;

            // Speed increases over time
            float speed = Mathf.Min(maxSpeed, baseSpeed + speedIncreasePerSecond * _elapsed);
            speed += Random.Range(-30f, 30f);

            bool golden = Random.value < goldenChance;

            var fishObj = Instantiate(fishUIPrefab, fishArea);
            var fishUI = fishObj.GetComponent<FishUI>();
            if (fishUI != null)
                fishUI.Initialize(spawnPos, direction, speed, golden, normalFishSprite, goldenFishSprite);
        }
    }
}
