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
        [Tooltip("Canonical in-world player size: every skin is scaled so its VISIBLE (non-transparent) sprite height is this tall in world units, so different skin art (Mark, Harrison, Evie) read at the same on-screen size regardless of transparent padding.")]
        [SerializeField] private float skinTargetHeight = 1.66f;
        [Tooltip("Collider covers the sprite's VISIBLE body including the legs/arms that read as the character (high width inset), inset only slightly for fairness. Recomputed per skin so all skins collide consistently.")]
        [SerializeField] private float colliderWidthInset = 0.85f;
        [SerializeField] private float colliderHeightInset = 0.62f;
        [Tooltip("Cap on the collider's WORLD width so wide-flail skins (Harrison/Evie) can still fit the corridor; keeps the lethal width consistent across skins.")]
        [SerializeField] private float maxColliderWidth = 1.9f;
        [Tooltip("Forward tip (deg) the standing sprite tips into mid-drop before recovering to 0, so the pose dives procedurally without bespoke frames.")]
        [SerializeField] private float diveTipAngle = -75f;
        [Tooltip("Drop progress (0..1) at which the standing sprite swaps to the falling frame.")]
        [SerializeField] private float poseSwapFraction = 0.45f;

        /// <summary>Canonical visible world height shared with the home-screen character (so they match).</summary>
        public float CanonicalVisibleHeight => skinTargetHeight;

        private Camera mainCamera;
        private SpriteRenderer spriteRenderer;
        private Sprite defaultSprite;
        private float lastInput;
        private float fallingFrameTimer;
        private int currentFallingFrame = -1;
        private Sprite standingSprite;
        private bool dropSwapped;
        private Sprite[] diveFrames;
        private int currentDiveFrame = -1;
        // One scale per skin, anchored to the falling reference frame and held constant
        // across all dive + fall frames (frames within a set are drawn consistently, so a
        // constant scale is correct; per-frame re-normalizing made the character pulse).
        private float skinScale = 1f;
        // Standing poses are separately normalized so every skin stands at the same height
        // on the home screen; the drop eases from this to skinScale (see ApplyDropPose).
        private float standingScale = 1f;

        private Collider2D bodyCollider;

        private void Awake()
        {
            mainCamera = Camera.main;
            spriteRenderer = GetComponent<SpriteRenderer>();
            defaultSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            bodyCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            // Start in the home standing pose; the run/drop switch the pose explicitly.
            ShowStanding();
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

            diveFrames = skin.HasDiveFrames ? skin.DiveFrames : null;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Sprite first = GetFirstFallingFrame();
            defaultSprite = skin.Standing != null ? skin.Standing : (first != null ? first : defaultSprite);

            // Anchor the skin's ONE scale to the falling reference frame (for dive skins
            // that is also the last dive frame), and fit the collider once from it.
            Sprite reference = first != null ? first : defaultSprite;
            skinScale = ComputeSkinScale(reference);
            transform.localScale = new Vector3(skinScale, skinScale, 1f);
            FitColliderToBody(reference);

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = reference;
            }

            currentFallingFrame = -1;
        }

        /// <summary>
        /// Scale that makes a sprite's VISIBLE (non-transparent) height match the target,
        /// so skins read at the same on-screen size regardless of how much transparent
        /// padding their art has. Computed once per skin from its reference frame.
        /// </summary>
        private float ComputeSkinScale(Sprite sprite)
        {
            float visibleHeight = VisibleSpriteHeight(sprite);
            if (visibleHeight <= 0.0001f)
            {
                return skinScale;
            }

            return skinTargetHeight / visibleHeight;
        }

        // Standing poses are normalized to their own visible height so every skin stands
        // at the same height on the home screen (a standing pose's bbox is much taller
        // than the same character's fall pose, so the constant set scale can't be reused).
        private void ApplyStandingScale()
        {
            float visibleHeight = VisibleSpriteHeight(standingSprite);
            if (visibleHeight <= 0.0001f)
            {
                standingScale = skinScale;
                return;
            }

            standingScale = skinTargetHeight / visibleHeight;
            transform.localScale = new Vector3(standingScale, standingScale, 1f);
        }

        /// <summary>
        /// Sizes the collider to the sprite's VISIBLE body (from the tight sprite mesh, not
        /// the transparent quad), inset for forgiveness. Done in LOCAL space so that, with the
        /// per-skin localScale from <see cref="NormalizeSkinScale"/>, the WORLD collider height
        /// is always skinTargetHeight*inset — i.e. every skin collides at the same size.
        /// </summary>
        private void FitColliderToBody(Sprite sprite)
        {
            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider2D>();
            }

            if (bodyCollider == null || sprite == null)
            {
                return;
            }

            Vector2[] verts = sprite.vertices;
            if (verts == null || verts.Length == 0)
            {
                return;
            }

            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].x < minX) minX = verts[i].x;
                if (verts[i].x > maxX) maxX = verts[i].x;
                if (verts[i].y < minY) minY = verts[i].y;
                if (verts[i].y > maxY) maxY = verts[i].y;
            }

            float visibleW = maxX - minX;
            float visibleH = maxY - minY;
            if (visibleW <= 0.0001f || visibleH <= 0.0001f)
            {
                return;
            }

            Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            Vector2 size = new Vector2(visibleW * colliderWidthInset, visibleH * colliderHeightInset);

            // Cap the WORLD width (local size scales by transform.localScale) so very wide
            // flail sprites don't make tight corridor sections impassable.
            float worldScale = Mathf.Abs(transform.localScale.x);
            if (maxColliderWidth > 0f && worldScale > 0.0001f && size.x * worldScale > maxColliderWidth)
            {
                size.x = maxColliderWidth / worldScale;
            }

            if (bodyCollider is CapsuleCollider2D capsule)
            {
                capsule.offset = center;
                capsule.size = size;
                capsule.direction = size.x >= size.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
            }
            else if (bodyCollider is BoxCollider2D box)
            {
                box.offset = center;
                box.size = size;
            }
        }

        /// <summary>
        /// World-space height of the sprite's opaque pixels. Uses the tight sprite mesh
        /// (which hugs the non-transparent area) so transparent padding is excluded;
        /// falls back to the full bounds if a tight mesh isn't available.
        /// </summary>
        private static float VisibleSpriteHeight(Sprite sprite)
        {
            if (sprite == null)
            {
                return 0f;
            }

            Vector2[] verts = sprite.vertices;
            if (verts != null && verts.Length > 0)
            {
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                for (int i = 0; i < verts.Length; i++)
                {
                    float y = verts[i].y;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                float extent = maxY - minY;
                if (extent > 0.0001f)
                {
                    return extent;
                }
            }

            return sprite.bounds.size.y;
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            // During the trapdoor drop the player stays pinned and tips procedurally from
            // standing into the dive (the world rushes up past it); no steering/death yet.
            if (gm.State == GameState.Dropping)
            {
                ApplyDropPose(gm.DropProgress);
                return;
            }

            if (!gm.IsPlaying)
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
            }
        }

        /// <summary>Home pose: the chosen skin standing upright, pinned at the run position.</summary>
        public void ShowStanding()
        {
            ApplySelectedSkin();
            lastInput = 0f;
            fallingFrameTimer = 0f;
            currentFallingFrame = -1;
            currentDiveFrame = -1;
            dropSwapped = false;
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;

            Skin skin = SkinLibrary.Instance != null ? SkinLibrary.Instance.Selected : null;
            standingSprite = skin != null && skin.Standing != null ? skin.Standing : defaultSprite;
            if (spriteRenderer != null && standingSprite != null)
            {
                spriteRenderer.sprite = standingSprite;
                ApplyStandingScale();
            }
        }

        /// <summary>Start of the trapdoor drop: pinned, standing sprite, ready to tip in.</summary>
        public void BeginDrop()
        {
            dropSwapped = false;
            currentDiveFrame = -1;
            transform.position = startPosition;
            if (spriteRenderer != null && standingSprite != null)
            {
                spriteRenderer.sprite = standingSprite;
                ApplyStandingScale();
            }
        }

        /// <summary>Drop finished: switch to the falling pose for the run (keeps position).</summary>
        public void BeginRun()
        {
            lastInput = 0f;
            fallingFrameTimer = 0f;
            currentFallingFrame = -1;
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
            if (!dropSwapped)
            {
                SwapToFallingFrame();
            }

            dropSwapped = false;
        }

        // Pinned dive: skins with bespoke dive frames (Evie's backflip) play them once
        // across the drop, upright; everyone else tips the standing sprite forward into a
        // dive, swaps to the falling frame mid-drop, then recovers rotation to 0 so the
        // run takes over upright.
        private void ApplyDropPose(float progress)
        {
            transform.position = startPosition;

            if (diveFrames != null && diveFrames.Length > 0)
            {
                transform.rotation = Quaternion.identity;
                // Ease from the standing-normalized scale to the constant set scale over
                // the early drop (settled by 60% progress, masked by the tumble). From
                // there the scale is constant through the fall: no pulsing, no handoff pop.
                float s = Mathf.Lerp(standingScale, skinScale, Mathf.Clamp01(progress / 0.6f));
                transform.localScale = new Vector3(s, s, 1f);
                AnimateDiveFrame(progress);
                return;
            }

            float tip;
            if (progress < poseSwapFraction)
            {
                tip = Mathf.Lerp(0f, diveTipAngle, EaseOut(progress / Mathf.Max(0.0001f, poseSwapFraction)));
            }
            else
            {
                if (!dropSwapped)
                {
                    SwapToFallingFrame();
                    dropSwapped = true;
                }

                float recover = (progress - poseSwapFraction) / Mathf.Max(0.0001f, 1f - poseSwapFraction);
                tip = Mathf.Lerp(diveTipAngle, 0f, recover);
            }

            transform.rotation = Quaternion.Euler(0f, 0f, tip);
        }

        // One-shot: drop progress 0..1 sweeps the dive frames in order, holding the last
        // frame until BeginRun hands over to the looping fall.
        private void AnimateDiveFrame(float progress)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(progress * diveFrames.Length), 0, diveFrames.Length - 1);
            if (frameIndex == currentDiveFrame || diveFrames[frameIndex] == null)
            {
                return;
            }

            currentDiveFrame = frameIndex;
            spriteRenderer.sprite = diveFrames[frameIndex];
        }

        private void SwapToFallingFrame()
        {
            Sprite first = GetFirstFallingFrame();
            if (spriteRenderer != null && first != null)
            {
                spriteRenderer.sprite = first;
                // Back to the constant fall scale (a no-op for dive skins; returns Mark
                // from his separately-normalized standing pose).
                transform.localScale = new Vector3(skinScale, skinScale, 1f);
            }
        }

        private static float EaseOut(float x)
        {
            return 1f - (1f - x) * (1f - x);
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
