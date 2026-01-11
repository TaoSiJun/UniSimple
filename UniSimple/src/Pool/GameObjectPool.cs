using System.Collections.Generic;
using UnityEngine;

namespace UniSimple.Pool
{
    /// <summary>
    /// GameObject 对象池
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly int _maxSize;
        private readonly Stack<GameObject> _stack = new();
        private readonly HashSet<int> _inactiveObjects = new();
        private readonly Dictionary<int, IPoolable> _interfaceCache = new();

        public int Count => _stack.Count;

        public GameObjectPool(GameObject prefab, Transform parent = null, int maxSize = 20, int initialSize = 0)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            Prewarm(initialSize);
        }

        private void Prewarm(int count)
        {
            var min = Mathf.Min(count, _maxSize);
            for (var i = 0; i < min; i++)
            {
                var obj = CreateNew();
                _stack.Push(obj);
                _inactiveObjects.Add(obj.GetInstanceID());
                obj.SetActive(false);
            }
        }

        private GameObject CreateNew()
        {
            var go = Object.Instantiate(_prefab, _parent);
            var poolable = go.GetComponent<IPoolable>();
            if (poolable != null)
            {
                var id = go.GetInstanceID();
                _interfaceCache[id] = poolable;
            }

            return go;
        }

        private GameObject GetInternal()
        {
            GameObject go;

            if (_stack.Count > 0)
            {
                go = _stack.Pop();
                _inactiveObjects.Remove(go.GetInstanceID()); // 移出检查集合
            }
            else
            {
                go = CreateNew();
            }

            return go;
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        public GameObject Get()
        {
            var go = GetInternal();
            var id = go.GetInstanceID();

            // 比 GetComponent 快
            if (_interfaceCache.TryGetValue(id, out var poolable))
            {
                poolable.OnSpawn();
            }

            go.SetActive(true);
            return go;
        }

        /// <summary>
        /// 获取对象并设置位置
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var go = GetInternal();
            var id = go.GetInstanceID();

            go.transform.SetPositionAndRotation(position, rotation);

            if (_interfaceCache.TryGetValue(id, out var poolable))
            {
                poolable.OnSpawn();
            }

            return go;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void Release(GameObject go)
        {
            if (go == null) return;

            var id = go.GetInstanceID();
            if (_inactiveObjects.Contains(id))
            {
                Debug.LogWarning($"Trying to release an object that is already in pool: {_prefab.name}");
                return;
            }

            go.SetActive(false);

            if (_interfaceCache.TryGetValue(id, out var poolable))
            {
                poolable.OnDespawn();
            }

            if (_stack.Count < _maxSize)
            {
                if (_parent != null && go.transform.parent != _parent)
                {
                    go.transform.SetParent(_parent);
                }

                _inactiveObjects.Add(id);
                _stack.Push(go);
            }
            else
            {
                Object.Destroy(go);
            }
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            foreach (var go in _stack)
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }

            _stack.Clear();
            _inactiveObjects.Clear();
            _interfaceCache.Clear();
        }
    }
}