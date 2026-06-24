using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Plummet
{
    /// <summary>
    /// Plays the home-screen "trapdoor" transition: the grey doors under the
    /// standing character swing open, the character drops through the gap, and
    /// once he is off-screen the supplied callback hands control to the run.
    /// Lives on the Canvas so the coroutine survives the Start Panel hiding.
    /// </summary>
    public sealed class IntroTransition : MonoBehaviour
    {
        [Header("Trapdoor")]
        [SerializeField] private RectTransform leftDoor;
        [SerializeField] private RectTransform rightDoor;
        [SerializeField] private float doorOpenAngle = 105f;
        [SerializeField] private float doorOpenDuration = 0.35f;

        [Header("Falling Actor")]
        [SerializeField] private RectTransform fallingActor;
        [Tooltip("Falling-pose sprite swapped in when the drop starts so the actor matches the gameplay player's frame (same pose and size) for a pop-free hand-off.")]
        [SerializeField] private Sprite fallingSprite;
        // Mark falls straight down from the surface ledge through the trapdoor and into
        // the shaft; fallDistance carries his centre to the gameplay player's pinned
        // position, where the run takes over (handoffFraction 1 = at the bottom of the
        // fall). The actor is sized to the world player, so there is no size jump.
        [SerializeField] private float fallDistance = 500f;
        [SerializeField] private float fallDuration = 0.6f;
        [SerializeField] private float fallSpin = 0f;
        [Tooltip("Forward tip (deg) the standing sprite tips into mid-fall before recovering to 0, so the pose dives procedurally without bespoke frames (Mark + single-pose kids).")]
        [SerializeField] private float diveTipAngle = -75f;
        [Tooltip("Fraction of the fall at which the standing sprite swaps to the falling frame.")]
        [SerializeField] private float poseSwapFraction = 0.45f;

        [Header("Timing")]
        [SerializeField] private float holdAfterOpen = 0.05f;
        [Tooltip("Fraction of the fall (0..1) at which the gameplay run takes over. 1 = hand off at the bottom, where the falling actor has reached the gameplay player's position, so the swap is seamless.")]
        [SerializeField] private float handoffFraction = 1f;

        [Header("Optional")]
        [Tooltip("Faded to zero while the doors open (e.g. a group holding the title/tap prompt). Optional.")]
        [SerializeField] private CanvasGroup promptGroup;

        private float leftClosedZ;
        private float rightClosedZ;
        private Vector2 actorHomePosition;
        private float actorHomeZ;
        private bool capturedDefaults;
        private Coroutine routine;
        private Image actorImage;
        private Sprite standingSprite;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            CaptureDefaults();
        }

        /// <summary>Sets the falling-pose sprite shown during the drop (per selected skin).</summary>
        public void SetFallingSprite(Sprite sprite)
        {
            fallingSprite = sprite;
        }

        /// <summary>Run the trapdoor sequence, invoking <paramref name="onComplete"/> at handoff.</summary>
        public void Play(Action onComplete)
        {
            if (IsPlaying)
            {
                return;
            }

            CaptureDefaults();
            if (actorImage != null)
            {
                standingSprite = actorImage.sprite;
            }

            routine = StartCoroutine(Run(onComplete));
        }

        /// <summary>Snap the doors closed and the actor back to the standing pose for a fresh home screen.</summary>
        public void ResetIntro()
        {
            CaptureDefaults();

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            IsPlaying = false;
            SetDoorOpen(0f);

            if (fallingActor != null)
            {
                fallingActor.anchoredPosition = actorHomePosition;
                fallingActor.localRotation = Quaternion.Euler(0f, 0f, actorHomeZ);
            }

            // Restore the standing pose only if we're resetting mid/after a drop, so a
            // live skin pick (which sets the sprite directly) is never overridden.
            if (actorImage != null && standingSprite != null && actorImage.sprite == fallingSprite)
            {
                actorImage.sprite = standingSprite;
            }

            if (promptGroup != null)
            {
                promptGroup.alpha = 1f;
            }
        }

        private void CaptureDefaults()
        {
            if (capturedDefaults)
            {
                return;
            }

            if (leftDoor != null)
            {
                leftClosedZ = leftDoor.localEulerAngles.z;
            }

            if (rightDoor != null)
            {
                rightClosedZ = rightDoor.localEulerAngles.z;
            }

            if (fallingActor != null)
            {
                actorHomePosition = fallingActor.anchoredPosition;
                actorHomeZ = fallingActor.localEulerAngles.z;
                actorImage = fallingActor.GetComponent<Image>();
            }

            capturedDefaults = true;
        }

        private IEnumerator Run(Action onComplete)
        {
            IsPlaying = true;
            bool handedOff = false;

            // 1. Swing the doors open, fading the prompt out alongside them.
            float duration = Mathf.Max(0.0001f, doorOpenDuration);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = EaseOut(Mathf.Clamp01(t / duration));
                SetDoorOpen(k);
                if (promptGroup != null)
                {
                    promptGroup.alpha = 1f - k;
                }

                yield return null;
            }

            SetDoorOpen(1f);
            if (promptGroup != null)
            {
                promptGroup.alpha = 0f;
            }

            if (holdAfterOpen > 0f)
            {
                yield return new WaitForSeconds(holdAfterOpen);
            }

            // 2. Drop straight down, accelerating like gravity, while procedurally turning
            //    the pose standing -> falling: tip forward into a dive over the first part of
            //    the fall, swap to the falling frame at poseSwapFraction, then recover the
            //    rotation back to 0 so the hand-off to the gameplay player (upright falling
            //    frame, no spin) is seamless. Works for Mark (distinct frames) and the
            //    single-pose kids (tip-and-recover only; no frame to swap to).
            bool swapped = false;
            duration = Mathf.Max(0.0001f, fallDuration);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float linear = Mathf.Clamp01(t / duration);
                float fallK = EaseIn(linear);

                if (fallingActor != null)
                {
                    fallingActor.anchoredPosition = actorHomePosition + new Vector2(0f, -fallDistance * fallK);

                    float tip;
                    if (linear < poseSwapFraction)
                    {
                        tip = Mathf.Lerp(0f, diveTipAngle, EaseOut(linear / Mathf.Max(0.0001f, poseSwapFraction)));
                    }
                    else
                    {
                        if (!swapped)
                        {
                            SwapToFallingPose();
                            swapped = true;
                        }

                        float recover = (linear - poseSwapFraction) / Mathf.Max(0.0001f, 1f - poseSwapFraction);
                        tip = Mathf.Lerp(diveTipAngle, 0f, recover);
                    }

                    fallingActor.localRotation = Quaternion.Euler(0f, 0f, actorHomeZ + tip);
                }

                if (!handedOff && linear >= handoffFraction)
                {
                    handedOff = true;
                    onComplete?.Invoke();
                }

                yield return null;
            }

            if (!swapped)
            {
                SwapToFallingPose();
            }

            if (fallingActor != null)
            {
                fallingActor.localRotation = Quaternion.Euler(0f, 0f, actorHomeZ);
            }

            if (!handedOff)
            {
                onComplete?.Invoke();
            }

            IsPlaying = false;
            routine = null;
        }

        // Swap the actor to its falling frame, keeping the on-screen height and refitting
        // width so different-aspect skins stay the same size as the standing character.
        // No-op for single-pose skins where the falling sprite is the standing sprite.
        private void SwapToFallingPose()
        {
            if (actorImage == null || fallingActor == null || fallingSprite == null)
            {
                return;
            }

            if (actorImage.sprite == fallingSprite)
            {
                return;
            }

            float height = fallingActor.rect.height;
            actorImage.sprite = fallingSprite;
            float aspect = fallingSprite.rect.height > 0f ? fallingSprite.rect.width / fallingSprite.rect.height : 1f;
            fallingActor.sizeDelta = new Vector2(height * aspect, height);
        }

        private void SetDoorOpen(float k)
        {
            if (leftDoor != null)
            {
                leftDoor.localRotation = Quaternion.Euler(0f, 0f, leftClosedZ - doorOpenAngle * k);
            }

            if (rightDoor != null)
            {
                rightDoor.localRotation = Quaternion.Euler(0f, 0f, rightClosedZ + doorOpenAngle * k);
            }
        }

        private static float EaseOut(float x)
        {
            return 1f - (1f - x) * (1f - x);
        }

        private static float EaseIn(float x)
        {
            return x * x;
        }
    }
}
