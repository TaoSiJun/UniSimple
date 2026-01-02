using UnityEngine;

namespace UniSimple.Pool
{
    // 游戏对象
    public class GameObjectPool : ObjectPoolBase<GameObject>
    {
        private readonly GameObject _prefab;
        private readonly Transform _poolContainer;
        private readonly bool _worldPositionStays;

        public GameObjectPool(
            GameObject prefab,
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
                var containerGameObject = new GameObject($"Pool_{prefab.name}");
                _poolContainer = containerGameObject.transform;
            }
            else
            {
                _poolContainer = container;
            }

            Prewarm(initialSize);
        }

        protected override GameObject CreateItem()
        {
            var go = Object.Instantiate(_prefab, _poolContainer, _worldPositionStays);
            go.name = $"{_prefab.name}_{TotalCount}";
            return go;
        }

        protected override void OnGetItem(GameObject item)
        {
            item.transform.SetParent(null, _worldPositionStays);
            item.SetActive(true);
        }

        protected override void OnReleaseItem(GameObject item)
        {
            // 重置状态
            item.transform.SetParent(_poolContainer, _worldPositionStays);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            item.SetActive(false);
        }

        protected override void DestroyItem(GameObject item)
        {
            if (item != null)
            {
                Object.Destroy(item);
            }
        }

        // 扩展方法：在指定位置生成
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var go = Get();
            if (go != null)
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
            }

            return go;
        }
    }
}