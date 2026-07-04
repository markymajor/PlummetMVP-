using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// The two ledge doors covering the shaft mouth on the home surface. They swing
    /// open (downward, into the shaft) as the trapdoor drop begins, and snap shut
    /// whenever the home screen is back. Pure visual: no colliders. Lives on the
    /// Surface Root so the open doors scroll away with the rest of the surface.
    /// </summary>
    public sealed class TrapdoorDoors : MonoBehaviour
    {
        [Tooltip("Hinged at the mouth's LEFT edge; its free edge swings down into the shaft.")]
        [SerializeField] private Transform leftDoor;
        [Tooltip("Hinged at the mouth's RIGHT edge; its free edge swings down into the shaft.")]
        [SerializeField] private Transform rightDoor;
        [SerializeField] private float openAngle = 105f;
        [SerializeField] private float openDuration = 0.3f;

        private float openTimer;

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            if (gm.State == GameState.Start)
            {
                openTimer = 0f;
                Apply(0f);
                return;
            }

            // Any non-home state: swing open once (ease-out), then hold.
            if (openTimer < openDuration)
            {
                openTimer += Time.deltaTime;
                float k = Mathf.Clamp01(openTimer / Mathf.Max(0.0001f, openDuration));
                Apply(1f - (1f - k) * (1f - k));
            }
        }

        private void Apply(float k)
        {
            // Left door extends +x from its hinge, so a negative Z rotation drops its
            // free edge; the right door extends -x and needs the opposite sense.
            if (leftDoor != null)
            {
                leftDoor.localRotation = Quaternion.Euler(0f, 0f, -openAngle * k);
            }

            if (rightDoor != null)
            {
                rightDoor.localRotation = Quaternion.Euler(0f, 0f, openAngle * k);
            }
        }

#if UNITY_EDITOR
        public void Configure(Transform left, Transform right)
        {
            leftDoor = left;
            rightDoor = right;
        }
#endif
    }
}
