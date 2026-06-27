using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// A scrolling window decal (lit window on a wall, or faint window in the shaft centre).
    /// Placement is STRATIFIED so the set never clusters: the loop span is split into
    /// <c>count</c> equal vertical bands and this decal owns band <c>slot</c>. It scrolls the
    /// full span (wrapping off-screen at the top) but, because every decal shares the same
    /// scroll phase plus a fixed per-slot offset, the windows stay evenly spread top-to-bottom
    /// and keep their relative order. Its exact Y within its band, plus side/x/scale, are
    /// re-randomised each time it wraps - only ever within its own band, so it stays in its
    /// lane. A jitter margin guarantees a minimum vertical gap between neighbouring windows.
    /// Pure decoration: no collider.
    /// </summary>
    public sealed class WindowDecal : MonoBehaviour
    {
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private float loopTopY = 7.5f;
        [SerializeField] private float loopBottomY = -9f;
        [Tooltip("True: a lit window on the wall edges (picks a random side). False: a faint window in the shaft centre.")]
        [SerializeField] private bool onWall = true;
        [SerializeField] private float wallXMin = 2.7f;
        [SerializeField] private float wallXMax = 3.05f;
        [SerializeField] private float centreXRange = 1.2f;
        [SerializeField] private float minScale = 0.45f;
        [SerializeField] private float maxScale = 0.62f;
        [Tooltip("This decal's band index in its set (0..count-1).")]
        [SerializeField] private int slot;
        [Tooltip("Number of decals sharing this loop span (number of bands).")]
        [SerializeField] private int count = 7;
        [Tooltip("Minimum vertical gap kept between neighbouring windows (backstop via the jitter margin).")]
        [SerializeField] private float minVerticalGap = 1.4f;

        private float span;
        private float bandHeight;
        private float jitterMargin;
        private float scrollAccum;
        private float jitter;
        private float currentX;
        private float currentScale;
        private int lastCycle;

        private void OnEnable()
        {
            span = Mathf.Max(0.01f, loopTopY - loopBottomY);
            bandHeight = span / Mathf.Max(1, count);
            // Keep jitter inside the band so neighbouring windows never get closer than the
            // requested gap (worst-case neighbour gap = 2 * jitterMargin).
            jitterMargin = Mathf.Clamp(minVerticalGap * 0.5f, 0f, bandHeight * 0.45f);
            scrollAccum = 0f;
            Respawn();

            float arg = slot * bandHeight + jitter;
            lastCycle = Mathf.FloorToInt(arg / span);
            Apply(arg);
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !gm.IsScrolling)
            {
                return;
            }

            scrollAccum += gm.ScrollSpeed * speedMultiplier * Time.deltaTime;
            float arg = slot * bandHeight + jitter + scrollAccum;
            int cycle = Mathf.FloorToInt(arg / span);
            if (cycle != lastCycle)
            {
                // Wrapped off the top: re-randomise within this same band only.
                Respawn();
                arg = slot * bandHeight + jitter + scrollAccum;
                lastCycle = Mathf.FloorToInt(arg / span);
            }

            Apply(arg);
        }

        // Re-roll the in-band height jitter plus the cosmetic side / x / scale.
        private void Respawn()
        {
            jitter = Random.Range(jitterMargin, Mathf.Max(jitterMargin, bandHeight - jitterMargin));

            if (onWall)
            {
                // Alternate walls by band so the windows can't all clump on one side (a
                // random side per window clumps just like a random Y did); the exact spot
                // on the wall stays random for variation.
                int side = (slot % 2 == 0) ? -1 : 1;
                currentX = side * Random.Range(wallXMin, wallXMax);
            }
            else
            {
                currentX = Random.Range(-centreXRange, centreXRange);
            }

            currentScale = Random.Range(minScale, maxScale);
        }

        private void Apply(float arg)
        {
            float y = loopBottomY + (arg - Mathf.Floor(arg / span) * span);
            transform.position = new Vector3(currentX, y, transform.position.z);
            // Flip lit windows on the right wall so they face into the shaft consistently.
            transform.localScale = new Vector3(onWall && currentX > 0f ? -currentScale : currentScale, currentScale, 1f);
        }
    }
}
