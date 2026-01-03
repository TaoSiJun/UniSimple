using UnityEngine;

namespace UniSimple.Pool
{
    // Unity组件池
    public sealed class ComponentPool<T> : ObjectPoolBase<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _poolContainer;
        private readonly bool _worldPositionStays;

        public ComponentPool(
            T prefab,
            Transform container = null,
            int initialSize = 10,
            int limitSize = 100,
            bool autoExpand = true)
            : base(limitSize, autoExpand)
        {
            _prefab = prefab;
            _worldPositionStays = false;

            // 创建池容器
            if (container == null)
            {
                var containerGameObject = new GameObject($"Pool_{prefab.gameObject.name}");
                _poolContainer = containerGameObject.transform;
            }
            else
            {
                _poolContainer = container;
            }

            Prewarm(initialSize);
        }

        protected override T CreateItem()
        {
            var item = Object.Instantiate(_prefab, _poolContainer, _worldPositionStays);
            return item;
        }

        protected override void OnGetItem(T item)
        {
            // item.transform.SetParent(null, _worldPositionStays);
            item.gameObject.SetActive(true);
        }

        protected override void OnReleaseItem(T item)
        {
            // 重置状态
            item.transform.SetParent(_poolContainer, _worldPositionStays);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            item.gameObject.SetActive(false);
        }

        protected override void DestroyItem(T item)
        {
            if (item != null)
            {
                Object.Destroy(item.gameObject);
            }
        }

        // 在指定位置生成
        public T Get(Vector3 position, Quaternion rotation)
        {
            var item = Get();
            if (item == null)
                return null;

            item.transform.position = position;
            item.transform.rotation = rotation;

            return item;
        }

        // 设置父节点
        public T Get(Vector3 position, Quaternion rotation, Transform parent)
        {
            var item = Get(position, rotation);
            if (item == null)
                return null;

            item.transform.SetParent(parent);
            return item;
        }
    }
}