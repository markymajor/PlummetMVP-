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
        [SerializeField] private float maximumCenterX = 1.0f;
        [SerializeField] private float centerStep = 0.42f;
        [SerializeField] private float wallThickness = 0.78f;
        [SerializeField] private Color wallColor = new Color(0.015f, 0.19f, 0.23f, 1f);

        private readonly List<PathSegment> segments = new List<PathSegment>();

        private void Start()
        {
            ResetPath();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
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

            float y = spawnTopY;
            float topCenter = 0f;
            float topWidth = startWidth;

            for (int i = segments.Count - 1; i >= 0; i--)
            {
                float bottomCenter = NextCenter(topCenter);
                float bottomWidth = NextWidth(topWidth);
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
                float bottomCenter = NextCenter(topCenter);
                float bottomWidth = NextWidth(topWidth);
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

        private float NextCenter(float current)
        {
            return Mathf.Clamp(current + Random.Range(-centerStep, centerStep), -maximumCenterX, maximumCenterX);
        }

        private float NextWidth(float current)
        {
            float difficultyT = GameManager.Instance != null ? GameManager.Instance.DifficultyT : 0f;
            float targetMinimum = Mathf.Lerp(startWidth, minimumWidth, difficultyT);
            return Mathf.Clamp(current + Random.Range(-widthStep, widthStep), targetMinimum, maximumWidth);
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
