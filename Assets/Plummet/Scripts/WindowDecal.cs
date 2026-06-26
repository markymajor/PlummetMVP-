using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// A scrolling window decal (lit window on a wall, or faint window in the shaft centre)
    /// that re-randomises its side, x position, height spacing and scale every time it loops
    /// back to the bottom - so the windows never read as a fixed, repeating pattern.
    /// Pure decoration: no collider.
    /// </summary>
    public sealed class WindowDecal : MonoBehaviour
    {
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private float wrapTopY = 7.5f;
        [SerializeField] private float respawnYMin = -9f;
        [SerializeField] private float respawnYMax = -6.5f;
        [Tooltip("True: a lit window on the wall edges (picks a random side). False: a faint window in the shaft centre.")]
        [SerializeField] private bool onWall = true;
        [SerializeField] private float wallXMin = 2.7f;
        [SerializeField] private float wallXMax = 3.05f;
        [SerializeField] private float centreXRange = 1.2f;
        [SerializeField] private float minScale = 0.45f;
        [SerializeField] private float maxScale = 0.62f;

        private void OnEnable()
        {
            // Initial random placement spread anywhere across the loop range.
            Place(Random.Range(respawnYMin, wrapTopY));
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !gm.IsScrolling)
            {
                return;
            }

            transform.Translate(Vector3.up * (gm.ScrollSpeed * speedMultiplier * Time.deltaTime), Space.World);

            if (transform.position.y >= wrapTopY)
            {
                Place(Random.Range(respawnYMin, respawnYMax));
            }
        }

        private void Place(float y)
        {
            float x;
            if (onWall)
            {
                int side = Random.value < 0.5f ? -1 : 1;
                x = side * Random.Range(wallXMin, wallXMax);
            }
            else
            {
                x = Random.Range(-centreXRange, centreXRange);
            }

            transform.position = new Vector3(x, y, transform.position.z);

            float s = Random.Range(minScale, maxScale);
            // Flip lit windows on the right wall so they face into the shaft consistently.
            transform.localScale = new Vector3(onWall && x > 0f ? -s : s, s, 1f);
        }
    }
}
