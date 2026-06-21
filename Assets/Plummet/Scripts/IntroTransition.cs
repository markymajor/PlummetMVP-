using System;
using System.Collections;
using UnityEngine;

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
        // The standing actor sits on the gameplay player's pinned position, so the
        // drop only needs a short dip through the doors before the run takes over;
        // the continuously-scrolling shaft sells the rest of the fall. Keeping the
        // dip small keeps the hand-off to the pinned player free of a visible jump.
        [SerializeField] private float fallDistance = 160f;
        [SerializeField] private float fallDuration = 0.42f;
        [SerializeField] private float fallSpin = 40f;

        [Header("Timing")]
        [SerializeField] private float holdAfterOpen = 0.05f;
        [Tooltip("Fraction of the fall (0..1) at which the gameplay run takes over. The pinned player appears at the actor's start, so hand off early while the dip is small to avoid a visible jump.")]
        [SerializeField] private float handoffFraction = 0.5f;

        [Header("Optional")]
        [Tooltip("Faded to zero while the doors open (e.g. a group holding the title/tap prompt). Optional.")]
        [SerializeField] private CanvasGroup promptGroup;

        private float leftClosedZ;
        private float rightClosedZ;
        private Vector2 actorHomePosition;
        private float actorHomeZ;
        private bool capturedDefaults;
        private Coroutine routine;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            CaptureDefaults();
        }

        /// <summary>Run the trapdoor sequence, invoking <paramref name="onComplete"/> at handoff.</summary>
        public void Play(Action onComplete)
        {
            if (IsPlaying)
            {
                return;
            }

            CaptureDefaults();
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

            // 2. Drop the character through the gap, accelerating like gravity.
            duration = Mathf.Max(0.0001f, fallDuration);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float linear = Mathf.Clamp01(t / duration);
                float k = EaseIn(linear);
                if (fallingActor != null)
                {
                    fallingActor.anchoredPosition = actorHomePosition + new Vector2(0f, -fallDistance * k);
                    fallingActor.localRotation = Quaternion.Euler(0f, 0f, actorHomeZ + fallSpin * k);
                }

                if (!handedOff && linear >= handoffFraction)
                {
                    handedOff = true;
                    onComplete?.Invoke();
                }

                yield return null;
            }

            if (!handedOff)
            {
                onComplete?.Invoke();
            }

            IsPlaying = false;
            routine = null;
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
