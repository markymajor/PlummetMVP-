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
        [SerializeField] private Color wallColor = new Color(0.008f, 0.208f, 0.282f, 1f);

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

            for (int i = segments.Count - 1; i >= 0; i--)
            {
                AdvanceCorridor(topCenter, topWidth, out float bottomCenter, out float bottomWidth);
                y -= segmentHeight;

                segments[i].Root.transform.position = new Vector3(0f, y, 0f);
                segments[i].Configure(bottomCenter, bottomWidth, topCenter, topWidth, segmentHeight, wallThickness, wallSprite, wallColor);

                topCenter = bottomCenter;
                topWidth = bottomWidth;
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

                segment.Root.transform.position = new Vector3(0f, y, 0f);
                segment.Configure(bottomCenter, bottomWidth, topCenter, topWidth, segmentHeight, wallThickness, wallSprite, wallColor);
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

        private sealed class PathSegment
        {
            private readonly Transform leftWall;
            private readonly Transform rightWall;
            private readonly SpriteRenderer leftRenderer;
            private readonly SpriteRenderer rightRenderer;
            private readonly BoxCollider2D leftCollider;
            private readonly BoxCollider2D rightCollider;

            public PathSegment(Transform parent, int index)
            {
                Root = new GameObject($"Path Segment {index + 1}");
                Root.transform.SetParent(parent, false);

                leftWall = CreateWall("Left Path Wall", Root.transform, out leftRenderer, out leftCollider);
                rightWall = CreateWall("Right Path Wall", Root.transform, out rightRenderer, out rightCollider);
            }

            public GameObject Root { get; }
            public float BottomCenter { get; private set; }
            public float BottomWidth { get; private set; }
            public float TopCenter { get; private set; }
            public float TopWidth { get; private set; }

            public void Configure(float bottomCenter, float bottomWidth, float topCenter, float topWidth, float height, float thickness, Sprite sprite, Color color)
            {
                BottomCenter = bottomCenter;
                BottomWidth = bottomWidth;
                TopCenter = topCenter;
                TopWidth = topWidth;

                Vector2 leftBottom = new Vector2(bottomCenter - bottomWidth * 0.5f - thickness * 0.5f, 0f);
                Vector2 leftTop = new Vector2(topCenter - topWidth * 0.5f - thickness * 0.5f, height);
                Vector2 rightBottom = new Vector2(bottomCenter + bottomWidth * 0.5f + thickness * 0.5f, 0f);
                Vector2 rightTop = new Vector2(topCenter + topWidth * 0.5f + thickness * 0.5f, height);

                ConfigureWall(leftWall, leftRenderer, leftCollider, leftBottom, leftTop, thickness, sprite, color);
                ConfigureWall(rightWall, rightRenderer, rightCollider, rightBottom, rightTop, thickness, sprite, color);
            }

            private static Transform CreateWall(string name, Transform parent, out SpriteRenderer renderer, out BoxCollider2D collider)
            {
                GameObject wall = new GameObject(name);
                wall.tag = "Wall";
                wall.transform.SetParent(parent, false);

                renderer = wall.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 2;

                collider = wall.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                return wall.transform;
            }

            private static void ConfigureWall(Transform wall, SpriteRenderer renderer, BoxCollider2D collider, Vector2 bottom, Vector2 top, float thickness, Sprite sprite, Color color)
            {
                Vector2 delta = top - bottom;
                Vector2 middle = (bottom + top) * 0.5f;
                float length = delta.magnitude;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;

                wall.localPosition = middle;
                wall.localRotation = Quaternion.Euler(0f, 0f, angle);

                renderer.sprite = sprite;
                renderer.color = color;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = new Vector2(thickness, length);

                collider.size = new Vector2(thickness, length);
            }
        }
    }
}
