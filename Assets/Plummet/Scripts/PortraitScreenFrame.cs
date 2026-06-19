using UnityEngine;
using UnityEngine.UI;

namespace Plummet
{
    [ExecuteAlways]
    public sealed class PortraitScreenFrame : MonoBehaviour
    {
        [SerializeField] private RectTransform phoneFrame;
        [SerializeField] private RectTransform leftBar;
        [SerializeField] private RectTransform rightBar;
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField] private float targetAspect = 9f / 16f;

        private Canvas rootCanvas;
        private RectTransform parentRect;
        private Vector2 lastParentSize;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private float lastScaleFactor;

        private void OnEnable()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            parentRect = transform.parent as RectTransform;
            ApplyFrame();
        }

        private void Update()
        {
            if (parentRect == null)
            {
                parentRect = transform.parent as RectTransform;
            }

            Vector2 parentSize = parentRect != null ? parentRect.rect.size : Vector2.zero;
            float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            if (parentSize == lastParentSize
                && Screen.width == lastScreenWidth
                && Screen.height == lastScreenHeight
                && Mathf.Approximately(scaleFactor, lastScaleFactor))
            {
                return;
            }

            ApplyFrame();
        }

        public void Configure(RectTransform frame, RectTransform left, RectTransform right, RectTransform top, RectTransform bottom)
        {
            phoneFrame = frame;
            leftBar = left;
            rightBar = right;
            topBar = top;
            bottomBar = bottom;
            ApplyFrame();
        }

        public void ApplyFrame()
        {
            if (phoneFrame == null || targetAspect <= 0f)
            {
                return;
            }

            parentRect = transform.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            Vector2 parentSize = parentRect.rect.size;
            lastParentSize = parentSize;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastScaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;

            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return;
            }

            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);
            float screenAspect = (float)screenWidth / screenHeight;
            Vector2 phonePixels = screenAspect > targetAspect
                ? new Vector2(screenHeight * targetAspect, screenHeight)
                : new Vector2(screenWidth, screenWidth / targetAspect);

            float scaleFactor = Mathf.Max(0.01f, lastScaleFactor);
            Vector2 phoneSize = phonePixels / scaleFactor;
            Vector2 frameCenter = GetScreenCenterInParent();

            SetCentered(phoneFrame, frameCenter, phoneSize);

            float sideWidth = Mathf.Max(0f, (screenWidth - phonePixels.x) * 0.5f) / scaleFactor;
            float verticalHeight = Mathf.Max(0f, (screenHeight - phonePixels.y) * 0.5f) / scaleFactor;

            SetBar(leftBar, frameCenter + new Vector2(-phoneSize.x * 0.5f - sideWidth * 0.5f, 0f), new Vector2(sideWidth, parentSize.y));
            SetBar(rightBar, frameCenter + new Vector2(phoneSize.x * 0.5f + sideWidth * 0.5f, 0f), new Vector2(sideWidth, parentSize.y));
            SetBar(topBar, frameCenter + new Vector2(0f, phoneSize.y * 0.5f + verticalHeight * 0.5f), new Vector2(parentSize.x, verticalHeight));
            SetBar(bottomBar, frameCenter + new Vector2(0f, -phoneSize.y * 0.5f - verticalHeight * 0.5f), new Vector2(parentSize.x, verticalHeight));
        }

        private Vector2 GetScreenCenterInParent()
        {
            if (parentRect == null)
            {
                return Vector2.zero;
            }

            Camera uiCamera = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = rootCanvas.worldCamera;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenCenter, uiCamera, out Vector2 localPoint))
            {
                return localPoint;
            }

            return Vector2.zero;
        }

        private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetBar(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.gameObject.SetActive(size.x > 0.01f && size.y > 0.01f);
            SetCentered(rect, position, size);

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.black;
                image.raycastTarget = false;
            }
        }
    }
}
