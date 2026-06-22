using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Plummet
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float touchMoveSpeed = 7.5f;
        [SerializeField] private float horizontalLimit = 2.95f;
        [SerializeField] private float tiltDeadZone = 0.06f;
        [SerializeField] private float visualLeanDegrees = 10f;
        [SerializeField] private Sprite[] fallingFrames;
        [SerializeField] private float fallingFrameRate = 10f;
        [SerializeField] private Vector3 startPosition = new Vector3(0f, -0.3f, 0f);
        [Tooltip("Every skin is scaled so its sprite is this tall in world units, so different-sized skin art (Mark, Harrison, Evie) all read at the same on-screen size. ~Mark's current height.")]
        [SerializeField] private float skinTargetHeight = 2.94f;

        private Camera mainCamera;
        private SpriteRenderer spriteRenderer;
        private Sprite defaultSprite;
        private float lastInput;
        private float fallingFrameTimer;
        private int currentFallingFrame = -1;

        private void Awake()
        {
            mainCamera = Camera.main;
            spriteRenderer = GetComponent<SpriteRenderer>();
            defaultSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        }

        private void Start()
        {
            ApplySelectedSkin();
        }

        /// <summary>
        /// Swaps the player's sprite and falling frames to the chosen skin from
        /// <see cref="SkinLibrary"/>. Safe to call when no library is present.
        /// </summary>
        public void ApplySelectedSkin()
        {
            if (SkinLibrary.Instance == null)
            {
                return;
            }

            Skin skin = SkinLibrary.Instance.Selected;
            if (skin == null)
            {
                return;
            }

            if (skin.FallingFrames != null && skin.FallingFrames.Length > 0)
            {
                fallingFrames = skin.FallingFrames;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Sprite first = GetFirstFallingFrame();
            defaultSprite = skin.Standing != null ? skin.Standing : (first != null ? first : defaultSprite);

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = first != null ? first : defaultSprite;
                NormalizeSkinScale(spriteRenderer.sprite);
            }

            currentFallingFrame = -1;
        }

        /// <summary>
        /// Scale the player so the sprite is a consistent world height regardless of
        /// the skin art's import size, so Mark, Harrison and Evie all read the same
        /// on-screen size. scale = targetHeight / sprite world height.
        /// </summary>
        private void NormalizeSkinScale(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            float spriteHeight = sprite.bounds.size.y;
            if (spriteHeight <= 0.0001f)
            {
                return;
            }

            float scale = skinTargetHeight / spriteHeight;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            float input = GetHorizontalInput();
            lastInput = Mathf.Lerp(lastInput, input, Time.deltaTime * 8f);

            Vector3 position = transform.position;
            position.x += input * moveSpeed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, -horizontalLimit, horizontalLimit);
            transform.position = position;

            fallingFrameTimer += Time.deltaTime;
            AnimateFallingFrame();
            transform.rotation = Quaternion.Euler(0f, 0f, -lastInput * visualLeanDegrees);
        }

        public void ResetPlayer()
        {
            ApplySelectedSkin();
            lastInput = 0f;
            fallingFrameTimer = 0f;
            currentFallingFrame = -1;
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = GetFirstFallingFrame() != null ? GetFirstFallingFrame() : defaultSprite;
                NormalizeSkinScale(spriteRenderer.sprite);
            }
        }

        private void AnimateFallingFrame()
        {
            if (spriteRenderer == null || fallingFrames == null || fallingFrames.Length == 0 || fallingFrameRate <= 0f)
            {
                return;
            }

            int frameIndex = Mathf.FloorToInt(fallingFrameTimer * fallingFrameRate) % fallingFrames.Length;
            if (frameIndex == currentFallingFrame || fallingFrames[frameIndex] == null)
            {
                return;
            }

            currentFallingFrame = frameIndex;
            spriteRenderer.sprite = fallingFrames[frameIndex];
        }

        private Sprite GetFirstFallingFrame()
        {
            if (fallingFrames == null)
            {
                return null;
            }

            for (int i = 0; i < fallingFrames.Length; i++)
            {
                if (fallingFrames[i] != null)
                {
                    return fallingFrames[i];
                }
            }

            return null;
        }

        private float GetHorizontalInput()
        {
            float keyboard = GetKeyboardInput();
            if (Mathf.Abs(keyboard) > 0.01f)
            {
                return keyboard;
            }

            if (TryGetPointerInput(out float pointerInput))
            {
                return pointerInput;
            }

            float tilt = GetTiltInput();
            return Mathf.Abs(tilt) > tiltDeadZone ? Mathf.Clamp(tilt * 1.8f, -1f, 1f) : 0f;
        }

        private float GetKeyboardInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return 0f;
            }

            float value = 0f;
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            {
                value -= 1f;
            }

            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            {
                value += 1f;
            }

            return value;
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }

        private bool TryGetPointerInput(out float input)
        {
            input = 0f;

            if (mainCamera == null)
            {
                return false;
            }

#if ENABLE_INPUT_SYSTEM
            Vector2? screenPosition = null;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                screenPosition = Mouse.current.position.ReadValue();
            }

            if (!screenPosition.HasValue)
            {
                return false;
            }

            float targetX = mainCamera.ScreenToWorldPoint(screenPosition.Value).x;
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                float targetX = mainCamera.ScreenToWorldPoint(touch.position).x;
                input = Mathf.Clamp(targetX - transform.position.x, -1f, 1f) * touchMoveSpeed / moveSpeed;
                return true;
            }

            if (!Input.GetMouseButton(0))
            {
                return false;
            }

            float targetX = mainCamera.ScreenToWorldPoint(Input.mousePosition).x;
#endif
            input = Mathf.Clamp(targetX - transform.position.x, -1f, 1f) * touchMoveSpeed / moveSpeed;
            return true;
        }

        private float GetTiltInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Accelerometer.current != null ? Accelerometer.current.acceleration.ReadValue().x : 0f;
#else
            return Input.acceleration.x;
#endif
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (other.CompareTag("Obstacle") || other.CompareTag("Wall"))
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}
