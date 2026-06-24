using System.Collections.Generic;
using UnityEngine;

namespace Plummet
{
    public sealed class PathManager : MonoBehaviour
    {
        [SerializeField] private Sprite wallSprite;
        [Tooltip("Single-brick sprite tiled down the wall's inner (wavy) edge as a brick lining.")]
        [SerializeField] private Sprite brickSprite;
        [SerializeField] private int segmentCount = 18;
        [SerializeField] private float segmentHeight = 1.15f;
        [SerializeField] private float spawnTopY = 7.1f;
        [SerializeField] private float recycleY = 7.7f;
        [SerializeField] private float startWidth = 3.4f;
        [SerializeField] private float minimumWidth = 2.4f;
        [SerializeField] private float maximumWidth = 4.7f;
        [SerializeField] private float widthStep = 0.28f;
        [Header("Width swings")]
        [Tooltip("How fast the gap eases toward its current random target, per segment.")]
        [SerializeField] private float widthEaseRate = 0.5f;
        [Tooltip("A new random target width is chosen every this-many segments (min..max).")]
        [SerializeField] private int retargetMin = 4;
        [SerializeField] private int retargetMax = 9;
        [SerializeField] private float minStep = 0.5f;
        [SerializeField] private float maxStep = 1.0f;
        [SerializeField] private float playHalfWidth = 2.5f;
        // Walls fill outward from the corridor edge well past the screen edge so the
        // dark shaft is solid to the sides (the inner collision face is unchanged).
        [SerializeField] private float wallThickness = 4.5f;
        [SerializeField] private Color wallColor = new Color(0.06f, 0.12f, 0.28f, 1f);

        [Header("Organic edge (Perlin)")]
        [Tooltip("How far the inner wall edge wavers in/out from the base diagonal, in world units.")]
        [SerializeField] private float edgeNoiseAmplitude = 0.3f;
        [Tooltip("Frequency of the edge undulation along the corridor (higher = more wobbles).")]
        [SerializeField] private float edgeNoiseScale = 0.7f;
        [Tooltip("Vertical sub-divisions per segment for the wavy edge mesh/collider.")]
        [SerializeField] private int edgeSubdivisions = 10;

        [Header("Brick lining")]
        [Tooltip("Width (world units) of the brick column running down the inner wall edge.")]
        [SerializeField] private float liningWidth = 0.42f;
        [Tooltip("World height of one brick tile down the lining.")]
        [SerializeField] private float liningTile = 0.55f;
        [SerializeField] private Color liningColor = new Color(0.16f, 0.41f, 0.43f, 1f);
        [Tooltip("World size of one mottled brick tile across the wall fill.")]
        [SerializeField] private float wallTile = 1.6f;

        // Gap that must survive both walls' edge bumps, sized for the widened player collider
        // (~1.84-1.9) plus steering margin, so edge noise can never pinch a tight section
        // below what the player can pass. Shared by the wall mesh + obstacle API.
        private const float SafeGap = 2.2f;
        private const float AmpFloor = 0.04f;

        private readonly List<PathSegment> segments = new List<PathSegment>();
        private readonly int[] stepHistory = new int[4];
        private int stepHistoryIndex;
        private float widthTarget;
        private int segmentsUntilRetarget;

        private void Start()
        {
            ResetPath();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsScrolling)
            {
                return;
            }

            float travel = GameManager.Instance.ScrollSpeed * Time.deltaTime;
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].Root.transform.Translate(Vector3.up * travel, Space.World);
            }

            RecycleSegments();
        }

        public void ResetPath()
        {
            EnsureSegments();
            ResetCorridorState();

            WallStyle style = Style;
            float y = spawnTopY;
            float topCenter = 0f;
            float topWidth = startWidth;
            float noiseTop = spawnTopY;

            for (int i = segments.Count - 1; i >= 0; i--)
            {
                AdvanceCorridor(topCenter, topWidth, out float bottomCenter, out float bottomWidth);
                y -= segmentHeight;
                float noiseStart = noiseTop - segmentHeight;

                segments[i].Root.transform.position = new Vector3(0f, y, 0f);
                segments[i].Configure(bottomCenter, bottomWidth, topCenter, topWidth, segmentHeight, wallThickness, noiseStart, style);

                topCenter = bottomCenter;
                topWidth = bottomWidth;
                noiseTop = noiseStart;
            }
        }

        public bool TryGetWallSpawn(float worldY, int side, float inset, out Vector3 position)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                PathSegment segment = segments[i];
                float localY = worldY - segment.Root.transform.position.y;
                if (localY < 0f || localY > segmentHeight)
                {
                    continue;
                }

                float t = Mathf.Clamp01(localY / segmentHeight);
                float center = Mathf.Lerp(segment.BottomCenter, segment.TopCenter, t);
                float width = Mathf.Lerp(segment.BottomWidth, segment.TopWidth, t);
                float edgeX = center + side * (width * 0.5f - inset);
                position = new Vector3(edgeX, worldY, 0f);
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        // Gap centre + (base) width of the corridor at a world Y, for obstacle fairness.
        public bool TryGetCorridorAt(float worldY, out float center, out float width)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                PathSegment segment = segments[i];
                float localY = worldY - segment.Root.transform.position.y;
                if (localY < 0f || localY > segmentHeight)
                {
                    continue;
                }

                float t = Mathf.Clamp01(localY / segmentHeight);
                center = Mathf.Lerp(segment.BottomCenter, segment.TopCenter, t);
                width = Mathf.Lerp(segment.BottomWidth, segment.TopWidth, t);
                return true;
            }

            center = 0f;
            width = 0f;
            return false;
        }

        // Per-wall edge noise amplitude for a given gap width (mirrors the wall mesh).
        public float EdgeAmplitudeForWidth(float width)
        {
            return Mathf.Clamp((width - SafeGap) * 0.5f, AmpFloor, edgeNoiseAmplitude);
        }

        // Maximum inward protrusion an obstacle may have and still leave a clear lane of
        // laneNeeded after both walls' edge bumps. Negative => no obstacle fits here.
        public float MaxObstacleReach(float width, float laneNeeded)
        {
            return width - 2f * EdgeAmplitudeForWidth(width) - laneNeeded;
        }

        private WallStyle Style => new WallStyle
        {
            WallTexture = wallSprite != null ? wallSprite.texture : null,
            WallColor = wallColor,
            WallTile = Mathf.Max(0.1f, wallTile),
            BrickTexture = brickSprite != null ? brickSprite.texture : null,
            LiningColor = liningColor,
            LiningWidth = liningWidth,
            LiningTile = Mathf.Max(0.1f, liningTile),
            Amplitude = edgeNoiseAmplitude,
            NoiseScale = edgeNoiseScale,
            Subdivisions = Mathf.Max(2, edgeSubdivisions)
        };

        private void EnsureSegments()
        {
            if (segments.Count == 0)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    GameObject child = transform.GetChild(i).gameObject;
                    if (Application.isPlaying)
                    {
                        Destroy(child);
                    }
                    else
                    {
                        DestroyImmediate(child);
                    }
                }
            }

            while (segments.Count < segmentCount)
            {
                segments.Add(new PathSegment(transform, segments.Count));
            }
        }

        private void RecycleSegments()
        {
            WallStyle style = Style;
            for (int i = 0; i < segments.Count; i++)
            {
                PathSegment segment = segments[i];
                if (segment.Root.transform.position.y < recycleY)
                {
                    continue;
                }

                PathSegment lowest = FindLowestSegment();
                float topCenter = lowest.BottomCenter;
                float topWidth = lowest.BottomWidth;
                AdvanceCorridor(topCenter, topWidth, out float bottomCenter, out float bottomWidth);
                float y = lowest.Root.transform.position.y - segmentHeight;
                float noiseStart = lowest.NoiseStart - segmentHeight;

                segment.Root.transform.position = new Vector3(0f, y, 0f);
                segment.Configure(bottomCenter, bottomWidth, topCenter, topWidth, segmentHeight, wallThickness, noiseStart, style);
            }
        }

        private PathSegment FindLowestSegment()
        {
            PathSegment lowest = segments[0];
            for (int i = 1; i < segments.Count; i++)
            {
                if (segments[i].Root.transform.position.y < lowest.Root.transform.position.y)
                {
                    lowest = segments[i];
                }
            }

            return lowest;
        }

        private void ResetCorridorState()
        {
            for (int i = 0; i < stepHistory.Length; i++)
            {
                stepHistory[i] = 0;
            }

            stepHistoryIndex = 0;
            widthTarget = startWidth;
            segmentsUntilRetarget = 0;
        }

        // Stepped zig-zag corridor, faithful to the original BackgroundLayer: the gap
        // narrows with difficulty, and the walls jump left/right in discrete steps with
        // forced turn-backs at the margins and anti-drift so they never wander one way.
        private void AdvanceCorridor(float topCenter, float topWidth, out float bottomCenter, out float bottomWidth)
        {
            // On the home screen and through the grace window keep the corridor centered and
            // wide, so the static shaft the player drops into is an open, fair mouth and the
            // first scrolling moments stay fair; normal wandering/narrowing resumes after.
            if (GameManager.Instance != null && GameManager.Instance.HoldCorridorOpen)
            {
                bottomWidth = maximumWidth;
                bottomCenter = 0f;
                widthTarget = maximumWidth;
                segmentsUntilRetarget = 0;
                RecordStep(0);
                return;
            }

            float difficultyT = GameManager.Instance != null ? GameManager.Instance.DifficultyT : 0f;

            // Every few segments jump to a brand-new random target spanning (most of) the
            // full width range, then ease toward it. This produces distinct tight squeezes
            // and distinct wide openings rather than a slow uniform drift. Big swings
            // persist at every difficulty; difficulty only mildly lowers the wide cap.
            if (segmentsUntilRetarget <= 0)
            {
                float high = Mathf.Lerp(maximumWidth, minimumWidth + (maximumWidth - minimumWidth) * 0.7f, difficultyT);
                widthTarget = Random.Range(minimumWidth, high);
                segmentsUntilRetarget = Random.Range(retargetMin, retargetMax + 1);
            }

            segmentsUntilRetarget--;
            bottomWidth = Mathf.MoveTowards(topWidth, widthTarget, widthEaseRate);
            bottomWidth = Mathf.Clamp(bottomWidth, minimumWidth, maximumWidth);

            float step = Mathf.Lerp(minStep, maxStep, difficultyT) * Random.Range(0.6f, 1f);
            float limit = Mathf.Max(0f, playHalfWidth - bottomWidth * 0.5f);

            int direction = ChooseDirection(topCenter, step, limit);
            bottomCenter = Mathf.Clamp(topCenter + direction * step, -limit, limit);
            RecordStep(direction);
        }

        private int ChooseDirection(float center, float step, float limit)
        {
            if (center + step > limit)
            {
                return -1;
            }

            if (center - step < -limit)
            {
                return 1;
            }

            bool onlyLeft = true;
            bool onlyRight = true;
            for (int i = 0; i < stepHistory.Length; i++)
            {
                if (stepHistory[i] >= 0)
                {
                    onlyLeft = false;
                }

                if (stepHistory[i] <= 0)
                {
                    onlyRight = false;
                }
            }

            if (onlyLeft)
            {
                return 1;
            }

            if (onlyRight)
            {
                return -1;
            }

            return Random.value < 0.5f ? -1 : 1;
        }

        private void RecordStep(int direction)
        {
            stepHistory[stepHistoryIndex] = direction;
            stepHistoryIndex = (stepHistoryIndex + 1) % stepHistory.Length;
        }

        private struct WallStyle
        {
            public Texture WallTexture;
            public Color WallColor;
            public float WallTile;
            public Texture BrickTexture;
            public Color LiningColor;
            public float LiningWidth;
            public float LiningTile;
            public float Amplitude;
            public float NoiseScale;
            public int Subdivisions;
        }

        private sealed class PathSegment
        {
            // Distinct Perlin lanes so the two walls undulate independently (organic).
            private const float LeftSeed = 11.3f;
            private const float RightSeed = 67.9f;
            private const float FarOuterX = 12f;

            // Edge amplitude allowed for a given gap width: full at wide gaps, shrinking to
            // near-zero as the gap approaches SafeGap so both bumps still leave it passable.
            private static float EdgeAmplitude(float gapWidth, float baseAmplitude)
            {
                return Mathf.Clamp((gapWidth - SafeGap) * 0.5f, AmpFloor, baseAmplitude);
            }

            // Lighter blue tint for the (mostly transparent) brick-outline overlay, so the
            // mortar lines read as subtle lighter depth over the solid navy fill.
            private static readonly Color WallDetailColor = new Color(0.34f, 0.44f, 0.62f, 1f);

            private static Material wallMaterial;
            private static Material wallBrickMaterial;
            private static Material liningMaterial;

            private readonly MeshFilter leftFilter;
            private readonly MeshFilter rightFilter;
            private readonly MeshRenderer leftWallRenderer;
            private readonly MeshRenderer rightWallRenderer;
            private readonly MeshRenderer leftBrickRenderer;
            private readonly MeshRenderer rightBrickRenderer;
            private readonly MeshRenderer leftLiningRenderer;
            private readonly MeshRenderer rightLiningRenderer;
            private readonly PolygonCollider2D leftCollider;
            private readonly PolygonCollider2D rightCollider;
            private readonly Mesh leftMesh;
            private readonly Mesh rightMesh;
            private readonly Mesh leftLiningMesh;
            private readonly Mesh rightLiningMesh;

            public PathSegment(Transform parent, int index)
            {
                Root = new GameObject($"Path Segment {index + 1}");
                Root.transform.SetParent(parent, false);

                leftMesh = NewMesh("Left Wall Mesh");
                rightMesh = NewMesh("Right Wall Mesh");
                leftLiningMesh = NewMesh("Left Lining Mesh");
                rightLiningMesh = NewMesh("Right Lining Mesh");

                CreateWall("Left Path Wall", Root.transform, leftMesh, out leftFilter, out leftCollider, out leftWallRenderer);
                CreateWall("Right Path Wall", Root.transform, rightMesh, out rightFilter, out rightCollider, out rightWallRenderer);
                leftBrickRenderer = CreateDecor("Left Wall Brick", Root.transform, leftMesh, 3);
                rightBrickRenderer = CreateDecor("Right Wall Brick", Root.transform, rightMesh, 3);
                leftLiningRenderer = CreateDecor("Left Brick Lining", Root.transform, leftLiningMesh, 4);
                rightLiningRenderer = CreateDecor("Right Brick Lining", Root.transform, rightLiningMesh, 4);
            }

            public GameObject Root { get; }
            public float BottomCenter { get; private set; }
            public float BottomWidth { get; private set; }
            public float TopCenter { get; private set; }
            public float TopWidth { get; private set; }
            public float NoiseStart { get; private set; }

            public void Configure(float bottomCenter, float bottomWidth, float topCenter, float topWidth, float height, float thickness, float noiseStart, WallStyle style)
            {
                BottomCenter = bottomCenter;
                BottomWidth = bottomWidth;
                TopCenter = topCenter;
                TopWidth = topWidth;
                NoiseStart = noiseStart;

                ApplyMaterials(style);
                leftWallRenderer.sharedMaterial = wallMaterial;
                rightWallRenderer.sharedMaterial = wallMaterial;
                leftBrickRenderer.sharedMaterial = wallBrickMaterial;
                rightBrickRenderer.sharedMaterial = wallBrickMaterial;
                leftLiningRenderer.sharedMaterial = liningMaterial;
                rightLiningRenderer.sharedMaterial = liningMaterial;

                BuildWall(leftMesh, leftCollider, -1, bottomCenter - bottomWidth * 0.5f, topCenter - topWidth * 0.5f, bottomWidth, topWidth, height, noiseStart, LeftSeed, style);
                BuildWall(rightMesh, rightCollider, 1, bottomCenter + bottomWidth * 0.5f, topCenter + topWidth * 0.5f, bottomWidth, topWidth, height, noiseStart, RightSeed, style);
                BuildLining(leftLiningMesh, -1, bottomCenter - bottomWidth * 0.5f, topCenter - topWidth * 0.5f, bottomWidth, topWidth, height, noiseStart, LeftSeed, style);
                BuildLining(rightLiningMesh, 1, bottomCenter + bottomWidth * 0.5f, topCenter + topWidth * 0.5f, bottomWidth, topWidth, height, noiseStart, RightSeed, style);
            }

            private static Mesh NewMesh(string name)
            {
                return new Mesh { name = name };
            }

            private static void ApplyMaterials(WallStyle style)
            {
                // Solid navy fill (no texture - the brick texture is transparent outlines).
                if (wallMaterial == null)
                {
                    wallMaterial = new Material(Shader.Find("Sprites/Default"));
                }

                wallMaterial.color = style.WallColor;
                wallMaterial.mainTexture = null;

                // Brick-outline overlay tinted a lighter blue for subtle mottled depth.
                if (wallBrickMaterial == null)
                {
                    wallBrickMaterial = new Material(Shader.Find("Sprites/Default"));
                }

                wallBrickMaterial.color = WallDetailColor;
                if (style.WallTexture != null)
                {
                    style.WallTexture.wrapMode = TextureWrapMode.Repeat;
                    wallBrickMaterial.mainTexture = style.WallTexture;
                }

                if (liningMaterial == null)
                {
                    liningMaterial = new Material(Shader.Find("Sprites/Default"));
                }

                liningMaterial.color = style.LiningColor;
                if (style.BrickTexture != null)
                {
                    style.BrickTexture.wrapMode = TextureWrapMode.Repeat;
                    liningMaterial.mainTexture = style.BrickTexture;
                }
            }

            private static void CreateWall(string name, Transform parent, Mesh mesh, out MeshFilter filter, out PolygonCollider2D collider, out MeshRenderer renderer)
            {
                GameObject wall = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D));
                wall.tag = "Wall";
                wall.transform.SetParent(parent, false);
                wall.transform.localPosition = Vector3.zero;

                filter = wall.GetComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                renderer = wall.GetComponent<MeshRenderer>();
                renderer.sortingOrder = 2;

                collider = wall.GetComponent<PolygonCollider2D>();
                collider.isTrigger = true;
            }

            // Decoration layer (no collider) sharing a mesh: brick overlay or brick lining.
            private static MeshRenderer CreateDecor(string name, Transform parent, Mesh mesh, int sortingOrder)
            {
                GameObject decor = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                decor.tag = "Untagged";
                decor.transform.SetParent(parent, false);
                decor.transform.localPosition = Vector3.zero;

                decor.GetComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = decor.GetComponent<MeshRenderer>();
                renderer.sortingOrder = sortingOrder;
                return renderer;
            }

            // Wall strip from a Perlin-wavering inner edge out to a far straight outer edge,
            // with world-space UVs so the mottled brick fill tiles, plus a matching
            // PolygonCollider2D so deaths follow the visible edge.
            private static void BuildWall(Mesh mesh, PolygonCollider2D collider, int side, float bottomInner, float topInner, float bottomWidth, float topWidth, float height, float noiseStart, float seed, WallStyle style)
            {
                int n = style.Subdivisions;
                int vertCount = (n + 1) * 2;
                Vector3[] verts = new Vector3[vertCount];
                Color[] colors = new Color[vertCount];
                Vector2[] uv = new Vector2[vertCount];
                int[] tris = new int[n * 6];
                Vector2[] path = new Vector2[vertCount];
                float outerX = side * FarOuterX;
                float tile = style.WallTile;

                for (int i = 0; i <= n; i++)
                {
                    float t = (float)i / n;
                    float localY = t * height;
                    float baseInner = Mathf.Lerp(bottomInner, topInner, t);
                    float amp = EdgeAmplitude(Mathf.Lerp(bottomWidth, topWidth, t), style.Amplitude);
                    float noise = (Mathf.PerlinNoise(seed, (noiseStart + localY) * style.NoiseScale) - 0.5f) * 2f * amp;
                    float innerX = baseInner - side * noise;
                    float v = (noiseStart + localY) / tile;

                    verts[i * 2] = new Vector3(innerX, localY, 0f);
                    verts[i * 2 + 1] = new Vector3(outerX, localY, 0f);
                    colors[i * 2] = Color.white;
                    colors[i * 2 + 1] = Color.white;
                    uv[i * 2] = new Vector2(innerX / tile, v);
                    uv[i * 2 + 1] = new Vector2(outerX / tile, v);

                    path[i] = new Vector2(innerX, localY);
                    path[vertCount - 1 - i] = new Vector2(outerX, localY);

                    if (i < n)
                    {
                        int b = i * 2;
                        tris[i * 6 + 0] = b;
                        tris[i * 6 + 1] = b + 1;
                        tris[i * 6 + 2] = b + 2;
                        tris[i * 6 + 3] = b + 1;
                        tris[i * 6 + 4] = b + 3;
                        tris[i * 6 + 5] = b + 2;
                    }
                }

                mesh.Clear();
                mesh.vertices = verts;
                mesh.triangles = tris;
                mesh.colors = colors;
                mesh.uv = uv;
                mesh.RecalculateBounds();

                collider.pathCount = 1;
                collider.SetPath(0, path);
            }

            // A thin brick column hugging the same wavy inner edge, tiled vertically.
            private static void BuildLining(Mesh mesh, int side, float bottomInner, float topInner, float bottomWidth, float topWidth, float height, float noiseStart, float seed, WallStyle style)
            {
                int n = style.Subdivisions;
                int vertCount = (n + 1) * 2;
                Vector3[] verts = new Vector3[vertCount];
                Vector2[] uv = new Vector2[vertCount];
                int[] tris = new int[n * 6];
                float liningWidth = style.LiningWidth;
                float vtile = style.LiningTile;

                for (int i = 0; i <= n; i++)
                {
                    float t = (float)i / n;
                    float localY = t * height;
                    float baseInner = Mathf.Lerp(bottomInner, topInner, t);
                    float amp = EdgeAmplitude(Mathf.Lerp(bottomWidth, topWidth, t), style.Amplitude);
                    float noise = (Mathf.PerlinNoise(seed, (noiseStart + localY) * style.NoiseScale) - 0.5f) * 2f * amp;
                    float innerX = baseInner - side * noise;
                    // Column runs from the inner edge INTO the wall (away from the shaft), so
                    // its shaft-facing face sits exactly at innerX = the wall collider edge.
                    float backX = innerX + side * liningWidth;
                    float v = (noiseStart + localY) / vtile;

                    verts[i * 2] = new Vector3(innerX, localY, 0f);
                    verts[i * 2 + 1] = new Vector3(backX, localY, 0f);
                    uv[i * 2] = new Vector2(0f, v);
                    uv[i * 2 + 1] = new Vector2(1f, v);

                    if (i < n)
                    {
                        int b = i * 2;
                        tris[i * 6 + 0] = b;
                        tris[i * 6 + 1] = b + 1;
                        tris[i * 6 + 2] = b + 2;
                        tris[i * 6 + 3] = b + 1;
                        tris[i * 6 + 4] = b + 3;
                        tris[i * 6 + 5] = b + 2;
                    }
                }

                mesh.Clear();
                mesh.vertices = verts;
                mesh.triangles = tris;
                mesh.uv = uv;
                mesh.RecalculateBounds();
            }
        }
    }
}
