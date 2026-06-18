using UnityEngine;

namespace Plummet
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class PortraitViewportFitter : MonoBehaviour
    {
        [SerializeField] private float targetAspect = 9f / 16f;

        private Camera targetCamera;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            ApplyViewport();
        }

        private void Update()
        {
            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            {
                return;
            }

            ApplyViewport();
        }

        public void ApplyViewport()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            if (lastScreenWidth <= 0 || lastScreenHeight <= 0 || targetAspect <= 0f)
            {
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            float screenAspect = (float)lastScreenWidth / lastScreenHeight;
            if (screenAspect > targetAspect)
            {
                float width = targetAspect / screenAspect;
                targetCamera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
                return;
            }

            float height = screenAspect / targetAspect;
            targetCamera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
    }
}
