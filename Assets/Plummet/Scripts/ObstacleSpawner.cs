using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// Spawns wall-protrusion obstacles that are always passable: each is sized so its
    /// inward reach leaves a clear lane (player width + margin) after the corridor's edge
    /// noise, is glued to its wall edge by an <see cref="ObstacleRider"/>, and is spaced so
    /// opposing obstacles never sandwich the gap. Frequency and depth ramp with difficulty.
    /// </summary>
    public sealed class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private ObjectPool obstaclePool;
        [SerializeField] private PathManager pathManager;
        [SerializeField] private float spawnY = -6.4f;
        [SerializeField] private float releaseY = 8.5f;

        [Header("Fairness")]
        [Tooltip("Clear lane = player collider width + this margin. Obstacle reach is clamped so this lane always survives.")]
        [SerializeField] private float safetyMargin = 0.5f;
        [Tooltip("Don't bother spawning if the fair reach is below this (too shallow to matter).")]
        [SerializeField] private float minReach = 0.2f;
        [SerializeField] private float minScale = 0.45f;
        [SerializeField] private float maxScale = 1.25f;

        [Header("Difficulty ramp")]
        [SerializeField] private float initialInterval = 2.2f;
        [SerializeField] private float minimumInterval = 0.95f;
        [Tooltip("Chance a due spawn actually fires (rare early, certain late).")]
        [SerializeField] private float spawnChanceEarly = 0.45f;
        [SerializeField] private float spawnChanceLate = 1f;
        [Tooltip("Obstacle reach as a fraction of gap width: shallow early, deeper late (still clamped by the lane rule).")]
        [SerializeField] private float reachFractionEarly = 0.12f;
        [SerializeField] private float reachFractionLate = 0.5f;

        [Header("Spacing")]
        [Tooltip("Minimum vertical distance between consecutive obstacles, world units.")]
        [SerializeField] private float minVerticalSpacing = 2.2f;
        [Tooltip("Opposing-side obstacles closer than this (vertical) are forced to the same side to avoid a sandwich.")]
        [SerializeField] private float sandwichSpacing = 4.5f;

        private float timer;
        private float elapsed;
        private int lastSide;
        private float lastSpawnElapsed;
        private float cachedPlayerWidth;

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || GameManager.Instance.InGrace)
            {
                return;
            }

            float difficultyT = GameManager.Instance.DifficultyT;
            elapsed += Time.deltaTime;
            timer -= Time.deltaTime;

            if (timer > 0f)
            {
                return;
            }

            if (Random.value <= Mathf.Lerp(spawnChanceEarly, spawnChanceLate, difficultyT))
            {
                Spawn(difficultyT);
            }

            float interval = Mathf.Lerp(initialInterval, minimumInterval, difficultyT);
            float scroll = Mathf.Max(0.1f, GameManager.Instance.ScrollSpeed);
            timer = Mathf.Max(interval, minVerticalSpacing / scroll);
        }

        public void ResetSpawner()
        {
            elapsed = 0f;
            timer = 0.6f;
            lastSide = 0;
            cachedPlayerWidth = 0f;
            ReleaseAll();
        }

        public void ReleaseAll()
        {
            if (obstaclePool != null)
            {
                obstaclePool.ReleaseAll();
            }
        }

        private void Spawn(float difficultyT)
        {
            if (obstaclePool == null || pathManager == null)
            {
                return;
            }

            int side = Random.value < 0.5f ? -1 : 1;

            // Avoid sandwiches: if the previous obstacle is still close above, keep the new
            // one on the same side so the clear lane stays on a single side.
            float verticalSinceLast = (elapsed - lastSpawnElapsed) * Mathf.Max(0.1f, GameManager.Instance.ScrollSpeed);
            if (lastSide != 0 && side != lastSide && verticalSinceLast < sandwichSpacing)
            {
                side = lastSide;
            }

            if (!pathManager.TryGetCorridorAt(spawnY, out float center, out float width))
            {
                return;
            }

            float lane = PlayerLane();
            float maxReach = pathManager.MaxObstacleReach(width, lane);
            if (maxReach < minReach)
            {
                return; // gap too tight here to fit an obstacle AND a lane: skip (fair).
            }

            float fraction = Mathf.Lerp(reachFractionEarly, reachFractionLate, difficultyT);
            float desiredReach = Mathf.Min(maxReach, width * fraction);
            if (desiredReach < minReach)
            {
                return;
            }

            float edgeX = center + side * (width * 0.5f);
            GameObject obstacle = obstaclePool.GetRandom(new Vector3(edgeX, spawnY, 0f), Quaternion.identity);
            if (obstacle == null)
            {
                return;
            }

            // The prefabs carry a Scroller that double-moves them and deactivates them
            // outside the pool (leak); the ObstacleRider is the sole mover now.
            Scroller scroller = obstacle.GetComponent<Scroller>();
            if (scroller != null)
            {
                scroller.enabled = false;
            }

            // Measure the collider's unscaled half-width, then scale so the protrusion
            // (collider half-width centred on the wall edge) equals the fair reach.
            Collider2D collider = obstacle.GetComponent<Collider2D>();
            obstacle.transform.localScale = Vector3.one;
            Physics2D.SyncTransforms();
            float halfWidth = collider != null && collider.bounds.size.x > 0.05f ? collider.bounds.size.x * 0.5f : 0.5f;

            float scale = Mathf.Clamp(desiredReach / halfWidth, minScale, maxScale);
            float protrusion = halfWidth * scale;
            if (protrusion > maxReach)
            {
                ReleaseObstacle(obstacle);
                return; // can't fit even at min scale: skip rather than block the lane.
            }

            obstacle.transform.localScale = new Vector3(side > 0 ? -scale : scale, scale, 1f);

            ObstacleRider rider = obstacle.GetComponent<ObstacleRider>();
            if (rider == null)
            {
                rider = obstacle.AddComponent<ObstacleRider>();
            }

            rider.Init(pathManager, side, protrusion, lane, releaseY);

            float offset = Mathf.Max(0f, protrusion - Mathf.Max(0f, maxReach));
            obstacle.transform.position = new Vector3(edgeX + side * offset, spawnY, 0f);

            lastSide = side;
            lastSpawnElapsed = elapsed;
        }

        private float PlayerLane()
        {
            if (cachedPlayerWidth <= 0f)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    Collider2D collider = player.GetComponent<Collider2D>();
                    if (collider != null && collider.bounds.size.x > 0.1f)
                    {
                        cachedPlayerWidth = collider.bounds.size.x;
                    }
                }

                if (cachedPlayerWidth <= 0f)
                {
                    cachedPlayerWidth = 1.62f;
                }
            }

            return cachedPlayerWidth + safetyMargin;
        }

        private static void ReleaseObstacle(GameObject obstacle)
        {
            ObjectPoolItem item = obstacle.GetComponent<ObjectPoolItem>();
            if (item != null)
            {
                item.Release();
            }
            else
            {
                obstacle.SetActive(false);
            }
        }
    }
}
