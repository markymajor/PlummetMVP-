using UnityEngine;

namespace Plummet
{
    /// <summary>
    /// The home-screen surface (sky / skyline / ledge) as a single world object behind the
    /// player. Its Scroller carries the whole surface up and out of view once the drop +
    /// run begin (IsScrolling), so the world rushes past the pinned player continuously.
    /// While the home screen is up it snaps back to its resting position so it's ready to
    /// fall away again on the next run.
    /// </summary>
    [RequireComponent(typeof(Scroller))]
    public sealed class WorldSurface : MonoBehaviour
    {
        private Vector3 restPosition;

        private void Awake()
        {
            restPosition = transform.position;
        }

        private void LateUpdate()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State == GameState.Start)
            {
                transform.position = restPosition;
            }
        }
    }
}
