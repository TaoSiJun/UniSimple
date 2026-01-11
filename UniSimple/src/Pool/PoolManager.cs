using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniSimple.Pool
{
    public static class PoolManager
    {
        private static Transform _root;

        // 这里用 InstanceID 作为 Key
        private static readonly Dictionary<int, GameObjectPool> PrefabPools = new();

        // 记录正在使用的对象属于哪个池子
        private static readonly Dictionary<int, GameObjectPool> ActiveObjectPools = new();

        private static GameObjectPool GetOrCreatePool(GameObject prefab)
        {
            var id = prefab.GetInstanceID();
            if (PrefabPools.TryGetValue(id, out var pool))
            {
                return pool;
            }

            var poolParent = new GameObject($"Pool_{prefab.name}");
            poolParent.transform.SetParent(_root);

            pool = new GameObjectPool(prefab, poolParent.transform);
            PrefabPools.Add(id, pool);

            return pool;
        }

        private static void EnsureInitialize()
        {
            if (_root == null)
            {
                var go = new GameObject("PoolManager");
                _root = go.transform;
                Object.DontDestroyOnLoad(go);
            }
        }

        /// <summary>
        /// 获取游戏对象
        /// </summary>
        public static GameObject Get(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            EnsureInitialize();

            var pool = GetOrCreatePool(prefab);
            var go = pool.Get();

            ActiveObjectPools[go.GetInstanceID()] = pool;

            return go;
        }

        /// <summary>
        /// 获取游戏对象 并设置位置和旋转
        /// </summary>
        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            EnsureInitialize();

            var pool = GetOrCreatePool(prefab);
            var go = pool.Get(position, rotation);

            ActiveObjectPools[go.GetInstanceID()] = pool;

            return go;
        }

        /// <summary>
        /// 回收游戏对象
        /// </summary>
        public static void Release(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (ActiveObjectPools.Remove(go.GetInstanceID(), out var pool))
            {
                pool.Release(go);
            }
            else
            {
                Debug.LogWarning($"'{go.name}' is not managed by PoolManager. Destroying directly.");
                Object.Destroy(go);
            }
        }

        /// <summary>
        /// 清空所有
        /// </summary>
        public static void Clear()
        {
            foreach (var pool in PrefabPools.Values)
            {
                pool.Clear();
            }

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }

            PrefabPools.Clear();
            ActiveObjectPools.Clear();
        }
    }
}