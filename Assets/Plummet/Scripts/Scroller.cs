using UnityEngine;

namespace Plummet
{
    public sealed class Scroller : MonoBehaviour
    {
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private bool loop;
        [SerializeField] private float loopHeight = 15f;
        [SerializeField] private float recycleY = 8f;

        private ObjectPoolItem poolItem;

        private void Awake()
        {
            poolItem = GetComponent<ObjectPoolItem>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            transform.Translate(Vector3.up * (GameManager.Instance.ScrollSpeed * speedMultiplier * Time.deltaTime), Space.World);

            if (loop && transform.position.y >= loopHeight)
            {
                transform.position -= Vector3.up * loopHeight * 2f;
            }
            else if (!loop && transform.position.y >= recycleY)
            {
                if (poolItem != null)
                {
                    poolItem.Release();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
