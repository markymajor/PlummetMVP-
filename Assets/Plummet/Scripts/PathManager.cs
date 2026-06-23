using System.Collections.Generic;
using UnityEngine;

namespace Plummet
{
    public sealed class PathManager : MonoBehaviour
    {
        [SerializeField] private Sprite wallSprite;
        [SerializeField] private int segmentCount = 18;
        [SerializeField] private float segmentHeight = 1.15f;
        [SerializeField] private float spawnTopY = 7.1f;
        [SerializeField] private float recycleY = 7.7f;
        [SerializeField] private float startWidth = 4.15f;
        [SerializeField] private float minimumWidth = 2.55f;
        [SerializeField] private float maximumWidth = 4.65f;
        [SerializeField] private float widthStep = 0.3f;
        [SerializeField] private float minStep = 0.5f;
        [SerializeField] private float maxStep = 1.0f;
        [SerializeField] private float playHalfWidth = 2.65f;
        // Walls fill outward from the corridor edge well past the screen edge so the
        // dark shaft is solid to the sides (the inner collision face is unchanged).
        [SerializeField] private float wallThickness = 4.5f;
        [SerializeField] private Color wallColor = new Color(0.06f, 0.12f, 0.28f, 1f);

        [Header("Organic edge (Perlin)")]
        [Tooltip("How far the inner wall edge wavers in/out from the base diagonal, in world units.")]
        [SerializeField] private float edgeNoiseAmplitude = 0.35f;
        [Tooltip("Frequency of the edge undulation along the corridor (higher = more wobbles).")]
        [SerializeField] private float edgeNoiseScale = 0.7f;
        [Tooltip("Vertical sub-divisions per segment for the wavy edge mesh/collider.")]
        [SerializeField] private int edgeSubdivisions = 10;

        private readonly List<PathSegment> segments = new List<PathSegment>();
        private readonly int[] stepHistory = new int[4];
        private int stepHistoryIndex;

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
                segments[i].Configure(bottomCenter, bottomWidth, topCenter, topWidth, segmentHeight, wallThickness, wallColor, noiseStart, EdgeParams);

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

        private EdgeNoiseParams EdgeParams => new EdgeNoiseParams(edgeNoiseAmplitude, edgeNoiseScale, Mathf.Max(2, edgeSubdivisions));

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
                segment.Configure(bottomCenter, bottomWidth, topCenter, topWidth, segmentHeight, wallThickness, wallColor, noiseStart, EdgeParams);
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
        }

        // Stepped zig-zag corridor, faithful to the original BackgroundLayer: the gap
        // narrows with difficulty, and the walls jump left/right in discrete steps with
        // forced turn-backs at the margins and anti-drift so they never wander one way.
        private void AdvanceCorridor(float topCenter, float topWidth, out float bottomCenter, out float bottomWidth)
        {
            // During the run's grace window keep the corridor centered and wide so the
            // drop lands fair; normal wandering/narrowing resumes once grace ends.
            if (GameManager.Instance != null && GameManager.Instance.InGrace)
            {
                bottomWidth = maximumWidth;
                bottomCenter = 0f;
                RecordStep(0);
                return;
            }

            float difficultyT = GameManager.Instance != null ? GameManager.Instance.DifficultyT : 0f;

            float targetMinimum = Mathf.Lerp(startWidth, minimumWidth, difficultyT);
            bottomWidth = Mathf.Clamp(topWidth + Random.Range(-widthStep, widthStep), targetMinimum, maximumWidth);

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

        private readonly struct EdgeNoiseParams
        {
            public EdgeNoiseParams(float amplitude, float scale, int subdivisions)
            {
                Amplitude = amplitude;
                Scale = scale;
                Subdivisions = subdivisions;
            }

            public float Amplitude { get; }
            public float Scale { get; }
            public int Subdivisions { get; }
        }

        private sealed class PathSegment
        {
            // Distinct Perlin lanes so the two walls undulate independently (organic).
            private const float LeftSeed = 11.3f;
            private const float RightSeed = 67.9f;
            private const float FarOuterX = 12f;

            private static Material sharedMaterial;

            private readonly MeshFilter leftFilter;
            private readonly MeshFilter rightFilter;
            private readonly PolygonCollider2D leftCollider;
            private readonly PolygonCollider2D rightCollider;
            private readonly Mesh leftMesh;
            private readonly Mesh rightMesh;

            public PathSegment(Transform parent, int index)
            {
                Root = new GameObject($"Path Segment {index + 1}");
                Root.transform.SetParent(parent, false);

                leftMesh = NewMesh("Left Wall Mesh");
                rightMesh = NewMesh("Right Wall Mesh");
                CreateWall("Left Path Wall", Root.transform, leftMesh, out leftFilter, out leftCollider);
                CreateWall("Right Path Wall", Root.transform, rightMesh, out rightFilter, out rightCollider);
            }

            public GameObject Root { get; }
            public float BottomCenter { get; private set; }
            public float BottomWidth { get; private set; }
            public float TopCenter { get; private set; }
            public float TopWidth { get; private set; }
            public float NoiseStart { get; private set; }

            public void Configure(float bottomCenter, float bottomWidth, float topCenter, float topWidth, float height, float thickness, Color color, float noiseStart, EdgeNoiseParams edge)
            {
                BottomCenter = bottomCenter;
                BottomWidth = bottomWidth;
                TopCenter = topCenter;
                TopWidth = topWidth;
                NoiseStart = noiseStart;

                BuildWall(leftMesh, leftCollider, -1,
                    bottomCenter - bottomWidth * 0.5f, topCenter - topWidth * 0.5f,
                    height, color, noiseStart, LeftSeed, edge);
                BuildWall(rightMesh, rightCollider, 1,
                    bottomCenter + bottomWidth * 0.5f, topCenter + topWidth * 0.5f,
                    height, color, noiseStart, RightSeed, edge);
            }

            private static Mesh NewMesh(string name)
            {
                return new Mesh { name = name };
            }

            private static void CreateWall(string name, Transform parent, Mesh mesh, out MeshFilter filter, out PolygonCollider2D collider)
            {
                GameObject wall = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D));
                wall.tag = "Wall";
                wall.transform.SetParent(parent, false);
                wall.transform.localPosition = Vector3.zero;

                filter = wall.GetComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = SharedMaterial;
                renderer.sortingOrder = 2;

                collider = wall.GetComponent<PolygonCollider2D>();
                collider.isTrigger = true;
            }

            private static Material SharedMaterial
            {
                get
                {
                    if (sharedMaterial == null)
                    {
                        sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                    }

                    return sharedMaterial;
                }
            }

            // Builds a wall strip from a Perlin-wavering inner edge out to a far straight
            // outer edge, and a matching PolygonCollider2D so deaths follow the visible edge.
            private static void BuildWall(Mesh mesh, PolygonCollider2D collider, int side, float bottomInner, float topInner, float height, Color color, float noiseStart, float seed, EdgeNoiseParams edge)
            {
                int n = edge.Subdivisions;
                int vertCount = (n + 1) * 2;
                Vector3[] verts = new Vector3[vertCount];
                Color[] colors = new Color[vertCount];
                Vector2[] uv = new Vector2[vertCount];
                int[] tris = new int[n * 6];
                Vector2[] path = new Vector2[vertCount];
                float outerX = side * FarOuterX;

                for (int i = 0; i <= n; i++)
                {
                    float t = (float)i / n;
                    float localY = t * height;
                    float baseInner = Mathf.Lerp(bottomInner, topInner, t);
                    float noise = (Mathf.PerlinNoise(seed, (noiseStart + localY) * edge.Scale) - 0.5f) * 2f * edge.Amplitude;
                    // Positive noise pushes the edge into the gap (protrude) on either wall.
                    float innerX = baseInner - side * noise;

                    verts[i * 2] = new Vector3(innerX, localY, 0f);
                    verts[i * 2 + 1] = new Vector3(outerX, localY, 0f);
                    colors[i * 2] = color;
                    colors[i * 2 + 1] = color;
                    uv[i * 2] = Vector2.zero;
                    uv[i * 2 + 1] = Vector2.zero;

                    // Collider path: inner edge bottom->top, then outer edge top->bottom.
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
        }
    }
}
