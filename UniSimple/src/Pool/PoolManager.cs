using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniSimple.Pool
{
    // 对象池管理器
    public class PoolManager
    {
        private readonly Dictionary<string, object> _pools = new();
        private readonly Transform _poolRoot;

        public PoolManager()
        {
            var root = new GameObject("[Pool Manager]");
            _poolRoot = root.transform;
            Object.DontDestroyOnLoad(root);
        }

        // 创建GameObject池
        public GameObjectPool CreateGameObjectPool(
            string key,
            GameObject prefab,
            int initialSize = 10,
            int maxSize = 100,
            bool autoExpand = true)
        {
            if (_pools.TryGetValue(key, out var o))
            {
                Debug.LogWarning($"Pool with key '{key}' already exists!");
                return o as GameObjectPool;
            }

            var container = new GameObject($"Pool_{key}");
            container.transform.SetParent(_poolRoot);

            var pool = new GameObjectPool(prefab, container.transform, initialSize, maxSize, autoExpand);
            _pools[key] = pool;

            return pool;
        }

        // 创建Class池
        public ClassPool<T> CreateClassPool<T>(
            string key,
            Func<T> createFunc = null,
            Action<T> resetAction = null,
            int initialSize = 10,
            int maxSize = 100,
            bool autoExpand = true)
            where T : class, new()
        {
            if (_pools.TryGetValue(key, out var o))
            {
                Debug.LogWarning($"Pool with key '{key}' already exists!");
                return o as ClassPool<T>;
            }

            var pool = new ClassPool<T>(createFunc, resetAction, initialSize, maxSize, autoExpand);
            _pools[key] = pool;

            return pool;
        }

        // 获取池
        public T GetPool<T>(string key) where T : class
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                return pool as T;
            }

            return null;
        }

        // 清理所有池
        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                switch (pool)
                {
                    case IObjectPool<GameObject> goPool:
                        goPool.Clear();
                        break;
                    case IObjectPool<object> objPool:
                        objPool.Clear();
                        break;
                }
            }

            _pools.Clear();
        }

        // 销毁管理器
        public void Dispose()
        {
            ClearAll();
            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot.gameObject);
            }
        }
    }
}