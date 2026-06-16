using System.Collections.Generic;
using UnityEngine;

namespace Plummet
{
    public sealed class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject[] prefabs;
        [SerializeField] private int warmCountPerPrefab = 4;

        private readonly Dictionary<GameObject, Queue<GameObject>> pooledObjects = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly List<GameObject> activeObjects = new List<GameObject>();

        private void Awake()
        {
            WarmPool();
        }

        public GameObject GetRandom(Vector3 position, Quaternion rotation)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return null;
            }

            return Get(prefabs[Random.Range(0, prefabs.Length)], position, rotation);
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!pooledObjects.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pooledObjects[prefab] = queue;
            }

            GameObject item = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab);
            item.transform.SetPositionAndRotation(position, rotation);
            item.SetActive(true);
            activeObjects.Add(item);
            return item;
        }

        public void Release(GameObject item, GameObject prefab)
        {
            if (item == null || prefab == null)
            {
                return;
            }

            item.SetActive(false);
            activeObjects.Remove(item);

            if (!pooledObjects.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pooledObjects[prefab] = queue;
            }

            queue.Enqueue(item);
        }

        public void ReleaseAll()
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                ObjectPoolItem item = activeObjects[i].GetComponent<ObjectPoolItem>();
                if (item != null)
                {
                    item.Release();
                }
                else
                {
                    activeObjects[i].SetActive(false);
                    activeObjects.RemoveAt(i);
                }
            }
        }

        private void WarmPool()
        {
            if (prefabs == null)
            {
                return;
            }

            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                Queue<GameObject> queue = new Queue<GameObject>();
                pooledObjects[prefab] = queue;

                for (int i = 0; i < warmCountPerPrefab; i++)
                {
                    GameObject item = CreateInstance(prefab);
                    item.SetActive(false);
                    queue.Enqueue(item);
                }
            }
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            GameObject item = Instantiate(prefab, transform);
            ObjectPoolItem poolItem = item.GetComponent<ObjectPoolItem>();
            if (poolItem == null)
            {
                poolItem = item.AddComponent<ObjectPoolItem>();
            }

            poolItem.Configure(this, prefab);
            return item;
        }
    }
}
