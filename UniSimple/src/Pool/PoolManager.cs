using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniSimple.Pool
{
    // 对象池管理器
    public static class PoolManager
    {
        private static readonly Dictionary<string, IObjectPool> Pools = new();

        private static Transform _rootPool;

        public static void DestroyPoolRoot()
        {
            if (_rootPool != null)
            {
                UnityEngine.Object.Destroy(_rootPool.gameObject);
                _rootPool = null;
            }
        }

        // 创建GameObject池
        public static ComponentPool<T> CreateComponentPool<T>(
            string key,
            T prefab,
            int initialSize = 10,
            int maxSize = 100,
            bool autoExpand = true)
            where T : Component
        {
            if (_rootPool == null)
            {
                var root = new GameObject("PoolManager");
                _rootPool = root.transform;
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            if (Pools.TryGetValue(key, out var o))
            {
                Debug.LogWarning($"Pool with key '{key}' already exists!");
                return o as ComponentPool<T>;
            }

            var container = new GameObject($"Pool_{key}");
            container.transform.SetParent(_rootPool);

            var pool = new ComponentPool<T>(prefab, container.transform, initialSize, maxSize, autoExpand);
            Pools[key] = pool;

            return pool;
        }

        // 创建Class池
        public static ClassPool<T> CreateClassPool<T>(
            string key,
            Func<T> createFunc = null,
            Action<T> resetAction = null,
            int initialSize = 10,
            int maxSize = 100,
            bool autoExpand = true)
            where T : class, new()
        {
            if (Pools.TryGetValue(key, out var o))
            {
                Debug.LogWarning($"Pool with key '{key}' already exists!");
                return o as ClassPool<T>;
            }

            var pool = new ClassPool<T>(createFunc, resetAction, initialSize, maxSize, autoExpand);
            Pools[key] = pool;

            return pool;
        }

        // 获取池
        public static T GetPool<T>(string key) where T : class
        {
            if (Pools.TryGetValue(key, out var pool))
            {
                return pool as T;
            }

            return null;
        }

        // 清理所有池
        public static void Clear()
        {
            foreach (var pool in Pools.Values)
            {
                pool.Clear();
            }

            Pools.Clear();
        }
    }
}