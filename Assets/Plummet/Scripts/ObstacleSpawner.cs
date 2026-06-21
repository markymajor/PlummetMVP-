using UnityEngine;

namespace Plummet
{
    public sealed class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private ObjectPool obstaclePool;
        [SerializeField] private PathManager pathManager;
        [SerializeField] private float spawnY = -6.4f;
        [SerializeField] private float horizontalRange = 2.05f;
        [SerializeField] private bool spawnOnWalls = true;
        [SerializeField] private float wallX = 2.55f;
        [SerializeField] private float wallInset = 0.35f;
        [SerializeField] private float wallVerticalJitter = 0.4f;
        [SerializeField] private float initialInterval = 1.45f;
        [SerializeField] private float minimumInterval = 0.65f;
        [SerializeField] private float intervalDifficultyRate = 0.02f;

        private float timer;
        private float elapsed;

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            // Hold obstacles off during the start-of-run grace window.
            if (GameManager.Instance.InGrace)
            {
                return;
            }

            elapsed += Time.deltaTime;
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                Spawn();
                float interval = Mathf.Max(minimumInterval, initialInterval - elapsed * intervalDifficultyRate);
                timer = interval;
            }
        }

        public void ResetSpawner()
        {
            elapsed = 0f;
            timer = 0.4f;
            ReleaseAll();
        }

        public void ReleaseAll()
        {
            obstaclePool.ReleaseAll();
        }

        private void Spawn()
        {
            int side = Random.value < 0.5f ? -1 : 1;
            float y = spawnY + Random.Range(-wallVerticalJitter, wallVerticalJitter);
            Vector3 position;

            if (spawnOnWalls && pathManager != null && pathManager.TryGetWallSpawn(y, side, wallInset, out Vector3 pathPosition))
            {
                position = pathPosition;
            }
            else
            {
                float x = spawnOnWalls ? side * wallX : Random.Range(-horizontalRange, horizontalRange);
                position = new Vector3(x, y, 0f);
            }

            GameObject item = obstaclePool.GetRandom(position, Quaternion.identity);

            if (item == null)
            {
                return;
            }

            float scale = Random.Range(0.72f, 1.08f);
            item.transform.localScale = new Vector3(spawnOnWalls && side > 0 ? -scale : scale, scale, scale);
        }
    }
}
