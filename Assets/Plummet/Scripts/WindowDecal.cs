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
        [Tooltip("Centre decals only: x-lane index (0..2 = left/centre/right third of the shaft, jittered within the lane) so centre decals never stack in a column. -1 = fully random x.")]
        [SerializeField] private int xLane = -1;
        [Tooltip("Wall decals only: place fully INSIDE the wall band (behind the vertical lining, inside the screen edge) at respawn, and SKIP the cycle when the wall is too thin at that Y — instead of shrinking, clipping, or poking into the shaft.")]
        [SerializeField] private bool containInWall;
        [Tooltip("Clearance kept between a contained decal and both the lining bricks and the screen edge.")]
        [SerializeField] private float containMargin = 0.15f;
        [Tooltip("Minimum vertical gap kept between neighbouring windows (backstop via the jitter margin). When maxNeighbourHeight is set this acts as the CLEARANCE on top of the size-aware gap.")]
        [SerializeField] private float minVerticalGap = 1.4f;
        [Tooltip("World height of the tallest decal sharing this lattice. When > 0 the jitter margin is derived from actual decal sizes — requiredGap = (ownHeight + tallestNeighbour)/2 + clearance — so bigger decals automatically get more spacing instead of overlapping.")]
        [SerializeField] private float maxNeighbourHeight;
        [Tooltip("Wall decals: -1/+1 pins this decal to the left/right wall (per-side lattice); 0 alternates by slot (legacy).")]
        [SerializeField] private int wallSide;

        private float span;
        private float bandHeight;
        private float jitterMargin;
        private float scrollAccum;
        private float jitter;
        private float currentX;
        private float currentScale;
        private int lastCycle;
        private bool visibleThisCycle = true;
        private GameState lastGmState = (GameState)(-1);
        private PathManager path;
        private SpriteRenderer spriteRenderer;

        private void OnEnable()
        {
            path = FindFirstObjectByType<PathManager>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            span = Mathf.Max(0.01f, loopTopY - loopBottomY);
            bandHeight = span / Mathf.Max(1, count);
            // Keep jitter inside the band so neighbouring decals never get closer than the
            // required gap (worst-case adjacent-band gap = margin_a + margin_b). With
            // maxNeighbourHeight set, the gap is derived from the decals' actual world
            // heights, so bigger art automatically gets more spacing.
            float gapNeed = minVerticalGap;
            if (maxNeighbourHeight > 0f && spriteRenderer != null && spriteRenderer.sprite != null)
            {
                float ownHeight = spriteRenderer.sprite.bounds.size.y * maxScale;
                gapNeed = (ownHeight + maxNeighbourHeight) * 0.5f + minVerticalGap;
            }

            jitterMargin = Mathf.Clamp(gapNeed * 0.5f, 0f, bandHeight * 0.45f);
            scrollAccum = 0f;
            Respawn();

            float arg = slot * bandHeight + jitter;
            lastCycle = Mathf.FloorToInt(arg / span);
            Apply(arg);
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            // The corridor is regenerated (ResetPath) when the home screen shows and when
            // a run starts, invalidating any placement made against the old geometry —
            // contained decals must re-check their wall band then, not just on wrap.
            if (gm.State != lastGmState)
            {
                lastGmState = gm.State;
                if (containInWall && (gm.State == GameState.Start || gm.State == GameState.Playing))
                {
                    Respawn();
                    Apply(slot * bandHeight + jitter + scrollAccum);
                }
            }

            if (!gm.IsScrolling)
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
            visibleThisCycle = true;

            if (onWall)
            {
                // Per-side lattice: wallSide pins this decal to one wall so each wall's
                // decor shares a single band set; 0 falls back to alternating by slot.
                int side = wallSide != 0 ? (int)Mathf.Sign(wallSide) : ((slot % 2 == 0) ? -1 : 1);
                currentX = side * Random.Range(wallXMin, wallXMax);

                if (containInWall)
                {
                    currentScale = Random.Range(minScale, maxScale);
                    float y = YFor(slot * bandHeight + jitter + scrollAccum);
                    visibleThisCycle = TryPlaceInWall(side, y, out currentX);
                }
            }
            else if (xLane >= 0)
            {
                // Stratified x: this decal owns one of three lanes across the shaft and
                // only jitters within it, so centre decals never stack in a column.
                float laneWidth = 2f * centreXRange / 3f;
                float laneLeft = -centreXRange + xLane * laneWidth;
                currentX = laneLeft + Random.Range(0.15f, 0.85f) * laneWidth;
            }
            else
            {
                currentX = Random.Range(-centreXRange, centreXRange);
            }

            if (!containInWall)
            {
                currentScale = Random.Range(minScale, maxScale);
            }
        }

        private float YFor(float arg)
        {
            return loopBottomY + (arg - Mathf.Floor(arg / span) * span);
        }

        // Contained decals may only spawn where the wall band (wavy inner edge + lining,
        // out to the screen edge) is deep enough for their FULL width plus clearance; a
        // too-thin band skips the cycle instead of shrinking or clipping the decal.
        private bool TryPlaceInWall(int side, float y, out float x)
        {
            x = currentX;
            if (path == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return true;
            }

            float halfWidth = spriteRenderer.sprite.bounds.extents.x * currentScale;
            float halfHeight = spriteRenderer.sprite.bounds.extents.y * currentScale;

            // The corridor edge shifts across the decal's own height (zig-zag steps +
            // noise), so take the deepest intrusion over its vertical extent.
            float edge = 0f;
            bool sampled = false;
            for (int i = -1; i <= 1; i++)
            {
                if (path.TryGetCorridorAt(y + i * halfHeight, out float center, out float width))
                {
                    edge = Mathf.Max(edge, Mathf.Abs(center + side * (width * 0.5f + path.EdgeAmplitudeForWidth(width))));
                    sampled = true;
                }
            }

            if (!sampled)
            {
                return true;
            }

            // The decal's INNER edge must always clear the lining; its OUTER side may
            // crop off-screen like the OG's wall clusters do. So "enough wall" means at
            // least half the decal fits between lining and screen edge — otherwise skip.
            Camera cam = Camera.main;
            float screenHalf = cam != null && cam.orthographic ? cam.orthographicSize * cam.aspect : 3.1f;
            float innerMost = edge + path.LiningWidth + containMargin + halfWidth;
            float outerMost = screenHalf;
            if (outerMost < innerMost)
            {
                return false; // wall band too thin at this Y: sit this cycle out.
            }

            x = side * Random.Range(innerMost, outerMost);
            return true;
        }

        private void Apply(float arg)
        {
            float y = YFor(arg);
            // Contained decals are fully placed (or skipped) at respawn; others get the
            // outward push as a backstop against poking into the corridor.
            float x = onWall && !containInWall ? ClampOutsideCorridor(currentX, y) : currentX;
            transform.position = new Vector3(x, y, transform.position.z);
            // Flip lit windows on the right wall so they face into the shaft consistently.
            transform.localScale = new Vector3(onWall && currentX > 0f ? -currentScale : currentScale, currentScale, 1f);

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visibleThisCycle;
            }
        }

        // Keep wall decals fully INSIDE the wall: push x outward so the decal's near edge
        // stays behind the corridor's wavy inner edge (base width + noise amplitude) at
        // this decal's current Y. Without this a wide corridor section can reach past a
        // decal's random x and the decal pokes into the shaft.
        private float ClampOutsideCorridor(float x, float y)
        {
            if (path == null || !path.TryGetCorridorAt(y, out float center, out float width))
            {
                return x;
            }

            float side = Mathf.Sign(x);
            float halfWidth = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds.extents.x * currentScale
                : 0.5f;
            float edge = Mathf.Abs(center + side * (width * 0.5f + path.EdgeAmplitudeForWidth(width)));
            return side * Mathf.Max(Mathf.Abs(x), edge + halfWidth + 0.05f);
        }
    }
}
