using UnityEngine;

namespace Plummet
{
    public sealed class ObjectPoolItem : MonoBehaviour
    {
        private ObjectPool pool;
        private GameObject prefab;
        private bool isReleased = true;

        public void Configure(ObjectPool sourcePool, GameObject sourcePrefab)
        {
            pool = sourcePool;
            prefab = sourcePrefab;
        }

        private void OnEnable()
        {
            isReleased = false;
        }

        public void Release()
        {
            if (isReleased)
            {
                return;
            }

            isReleased = true;
            pool.Release(gameObject, prefab);
        }
    }
}
