using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// Glues a spawned obstacle to the wall edge it spawned on as the corridor wanders,
    /// scrolls it up with the shaft, and keeps the clear lane valid every frame: if the
    /// gap narrows around it, the obstacle retracts into the wall so it can never block
    /// more than its fair share. Releases itself when it scrolls off the top.
    /// </summary>
    [RequireComponent(typeof(ObjectPoolItem))]
    public sealed class ObstacleRider : MonoBehaviour
    {
        private PathManager path;
        private ObjectPoolItem item;
        private int side;          // -1 = left wall, +1 = right wall
        private float protrusion;  // collider half-width at the chosen scale (max inward reach)
        private float laneNeeded;
        private float releaseY;

        public void Init(PathManager pathManager, int wallSide, float colliderHalfWidth, float lane, float releaseAboveY)
        {
            path = pathManager;
            side = wallSide;
            protrusion = colliderHalfWidth;
            laneNeeded = lane;
            releaseY = releaseAboveY;
            if (item == null)
            {
                item = GetComponent<ObjectPoolItem>();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsScrolling || path == null)
            {
                return;
            }

            Vector3 pos = transform.position;
            pos.y += GameManager.Instance.ScrollSpeed * Time.deltaTime;

            if (path.TryGetCorridorAt(pos.y, out float center, out float width))
            {
                float edgeX = center + side * (width * 0.5f);
                // How far we may protrude here right now; retract into the wall if the gap
                // narrowed since we spawned so the clear lane always survives.
                float maxReach = Mathf.Max(0f, path.MaxObstacleReach(width, laneNeeded));
                float offset = Mathf.Max(0f, protrusion - maxReach);
                pos.x = edgeX + side * offset;
            }

            transform.position = pos;

            if (pos.y > releaseY && item != null)
            {
                item.Release();
            }
        }
    }
}
