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

        private RectTransform parentRect;
        private Vector2 lastParentSize;

        private void OnEnable()
        {
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
            if (parentSize == lastParentSize)
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
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return;
            }

            float parentAspect = parentSize.x / parentSize.y;
            Vector2 phoneSize = parentAspect > targetAspect
                ? new Vector2(parentSize.y * targetAspect, parentSize.y)
                : new Vector2(parentSize.x, parentSize.x / targetAspect);

            SetCentered(phoneFrame, Vector2.zero, phoneSize);

            float sideWidth = Mathf.Max(0f, (parentSize.x - phoneSize.x) * 0.5f);
            float verticalHeight = Mathf.Max(0f, (parentSize.y - phoneSize.y) * 0.5f);

            SetBar(leftBar, new Vector2(-phoneSize.x * 0.5f - sideWidth * 0.5f, 0f), new Vector2(sideWidth, parentSize.y));
            SetBar(rightBar, new Vector2(phoneSize.x * 0.5f + sideWidth * 0.5f, 0f), new Vector2(sideWidth, parentSize.y));
            SetBar(topBar, new Vector2(0f, phoneSize.y * 0.5f + verticalHeight * 0.5f), new Vector2(parentSize.x, verticalHeight));
            SetBar(bottomBar, new Vector2(0f, -phoneSize.y * 0.5f - verticalHeight * 0.5f), new Vector2(parentSize.x, verticalHeight));
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
