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
        /// <summary>Extra clearance the obstacle's wall-side end is buried past the wavy
        /// edge + lining, shared by the spawner's scale math so both use one convention.</summary>
        public const float RootMargin = 0.15f;

        private PathManager path;
        private ObjectPoolItem item;
        private int side;            // -1 = left wall, +1 = right wall
        private float totalHalfWidth; // collider half-width at the chosen scale
        private float laneNeeded;
        private float releaseY;

        /// <summary>How deep the wall-side end sits INSIDE the wall at this width: past the
        /// edge undulation and the lining bricks, so the base always reads as rooted in
        /// brick regardless of how far the wavy visual edge recedes.</summary>
        public static float RootOffset(PathManager path, float width)
        {
            return path.EdgeAmplitudeForWidth(width) + path.LiningWidth + RootMargin;
        }

        public void Init(PathManager pathManager, int wallSide, float colliderHalfWidth, float lane, float releaseAboveY)
        {
            path = pathManager;
            side = wallSide;
            totalHalfWidth = colliderHalfWidth;
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
                // The wall-side end stays buried inside the brick (root), and the inward
                // reach past the smooth edge = totalHalfWidth - root. Retract further if
                // the gap narrowed since spawn, so the clear lane always survives.
                float root = RootOffset(path, width);
                float maxReach = Mathf.Max(0f, path.MaxObstacleReach(width, laneNeeded));
                float retract = Mathf.Max(0f, totalHalfWidth - root - maxReach);
                pos.x = edgeX + side * (root + retract);
            }

            transform.position = pos;

            if (pos.y > releaseY && item != null)
            {
                item.Release();
            }
        }
    }
}
